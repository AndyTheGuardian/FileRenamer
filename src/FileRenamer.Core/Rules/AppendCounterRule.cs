using System.Globalization;
using FileRenamer.Core.Naming;
using FileRenamer.Core.Validation;

namespace FileRenamer.Core.Rules;

/// <summary>
/// Verfahren „Zähler anhängen“: vergibt nach einem Sortierkriterium fortlaufende, mit Nullen
/// aufgefüllte Zählerwerte und fügt sie vor oder nach dem Namen ein.
/// </summary>
public sealed record AppendCounterRule : RenameRule
{
    /// <summary>
    /// Größte erlaubte Stellenanzahl.
    /// </summary>
    public const int MaxDigits = 18;

    /// <summary>
    /// Vor oder nach dem Namen.
    /// </summary>
    public AffixPosition Position { get; init; } = AffixPosition.AfterName;

    /// <summary>
    /// Wert der ersten Datei.
    /// </summary>
    public long Start { get; init; } = 1;

    /// <summary>
    /// Abstand zwischen zwei aufeinanderfolgenden Werten.
    /// </summary>
    public long Step { get; init; } = 1;

    /// <summary>
    /// Mindeststellenanzahl; kürzere Werte werden mit führenden Nullen aufgefüllt.
    /// </summary>
    public int Digits { get; init; } = 3;

    /// <summary>
    /// Trennzeichen zwischen Name und Zähler.
    /// </summary>
    public string Separator { get; init; } = "-";

    /// <summary>
    /// Sortierkriterium für die Reihenfolge der Zählerwerte.
    /// </summary>
    public CounterSortCriterion SortBy { get; init; } = CounterSortCriterion.Name;

    /// <summary>
    /// Ob absteigend sortiert wird.
    /// </summary>
    public bool Descending { get; init; }

    /// <inheritdoc />
    public override RuleKind Kind => RuleKind.AppendCounter;

    /// <summary>
    /// Prüft Stellenanzahl und Trennzeichen.
    /// </summary>
    /// <returns>Fehlermeldung oder <c>null</c>.</returns>
    public override string? ValidateSettings() =>
        Digits is < 1 or > MaxDigits
            ? $"Die Stellenanzahl muss zwischen 1 und {MaxDigits} liegen."
            : FileNameValidator.DescribeInvalidCharacter(Separator);

    /// <summary>
    /// Sortiert die Dateien und fügt jeder Datei ihren Zählerwert an.
    /// </summary>
    /// <param name="inputs">Die Dateien mit ihren Namen an dieser Stelle der Kette.</param>
    /// <returns>Ein Ergebnis je Eingabe in derselben Reihenfolge.</returns>
    public override IReadOnlyList<RuleOutput> Apply(IReadOnlyList<RuleInput> inputs)
    {
        var outputs = new RuleOutput[inputs.Count];
        var order = SortIndices(inputs);

        for (var rank = 0; rank < order.Count; rank++)
        {
            var index = order[rank];
            var counter = FormatCounter(Start + rank * Step);
            var name = Affix.Add(inputs[index].Name, counter, Separator, Position, Id);
            outputs[index] = new RuleOutput(name, CreateIssues(inputs[index]));
        }

        return outputs;
    }

    /// <summary>
    /// Formatiert einen Zählerwert mit führenden Nullen; negative Werte erhalten ein Minuszeichen.
    /// </summary>
    /// <param name="value">Der Zählerwert.</param>
    /// <returns>Der formatierte Wert.</returns>
    internal string FormatCounter(long value)
    {
        var digits = Math.Abs(value).ToString(CultureInfo.InvariantCulture).PadLeft(Digits, '0');
        return value < 0 ? "-" + digits : digits;
    }

    /// <summary>
    /// Liefert die Indizes der Eingaben in Sortierreihenfolge. Bei Gleichstand entscheidet der Dateiname.
    /// </summary>
    /// <param name="inputs">Die Eingaben.</param>
    /// <returns>Sortierte Indizes.</returns>
    private List<int> SortIndices(IReadOnlyList<RuleInput> inputs)
    {
        var indices = Enumerable.Range(0, inputs.Count).ToList();
        indices.Sort((a, b) =>
        {
            var result = CompareByCriterion(inputs[a], inputs[b]);
            if (result == 0)
            {
                result = NaturalStringComparer.Instance.Compare(inputs[a].File.FileName, inputs[b].File.FileName);
            }

            return Descending ? -result : result;
        });
        return indices;
    }

    /// <summary>
    /// Vergleicht zwei Dateien nach dem gewählten Kriterium.
    /// </summary>
    /// <param name="x">Erste Datei.</param>
    /// <param name="y">Zweite Datei.</param>
    /// <returns>Vergleichsergebnis.</returns>
    private int CompareByCriterion(RuleInput x, RuleInput y) => SortBy switch
    {
        CounterSortCriterion.CreationDate => GetCreationOrFallback(x).CompareTo(GetCreationOrFallback(y)),
        CounterSortCriterion.ModificationDate => x.File.LastWriteTime.CompareTo(y.File.LastWriteTime),
        CounterSortCriterion.Size => x.File.Size.CompareTo(y.File.Size),
        _ => NaturalStringComparer.Instance.Compare(x.File.FileName, y.File.FileName),
    };

    /// <summary>
    /// Liefert das Erstellungsdatum oder ersatzweise das Änderungsdatum.
    /// </summary>
    /// <param name="input">Die Eingabe.</param>
    /// <returns>Der Zeitpunkt.</returns>
    private static DateTime GetCreationOrFallback(RuleInput input) =>
        input.File.CreationTime ?? input.File.LastWriteTime;

    /// <summary>
    /// Erzeugt den Fallback-Hinweis, wenn nach Erstellungsdatum sortiert wird, es aber fehlt.
    /// </summary>
    /// <param name="input">Die Eingabe.</param>
    /// <returns>Die Hinweise.</returns>
    private IReadOnlyList<RenameIssue> CreateIssues(RuleInput input) =>
        SortBy == CounterSortCriterion.CreationDate && input.File.CreationTime is null
            ? [RenameIssue.Hint("Erstellungsdatum nicht verfügbar – für die Sortierung wurde das Änderungsdatum verwendet.", Id)]
            : [];
}
