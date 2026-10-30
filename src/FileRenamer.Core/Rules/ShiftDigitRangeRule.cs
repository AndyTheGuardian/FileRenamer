using FileRenamer.Core.Localization;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace FileRenamer.Core.Rules;

/// <summary>
/// Verfahren „Ziffernkette Wertebereich verschieben“: addiert einen (auch negativen) Wert auf eine
/// Ziffernkette im Namen. Standardmäßig wird die letzte Ziffernkette verwendet, alternativ die n-te von links.
/// Namen ohne Ziffernkette bleiben unverändert (Hinweis), negative Ergebnisse sind Konflikte.
/// </summary>
public sealed partial record ShiftDigitRangeRule : PerFileRenameRule
{
    /// <summary>
    /// Größte erlaubte Mindeststellenanzahl.
    /// </summary>
    public const int MaxDigits = 18;

    /// <summary>
    /// Der zu addierende Wert (negativ zum Verringern).
    /// </summary>
    public long Offset { get; init; } = 1;

    /// <summary>
    /// Ob die letzte gefundene Ziffernkette verwendet wird.
    /// </summary>
    public bool UseLastRun { get; init; } = true;

    /// <summary>
    /// Nummer der Ziffernkette von links (ab 1), wenn <see cref="UseLastRun"/> nicht gesetzt ist.
    /// </summary>
    public int RunNumber { get; init; } = 1;

    /// <summary>
    /// Ob die Breite der ursprünglichen Ziffernkette als Mindestbreite gilt.
    /// </summary>
    public bool KeepOriginalWidth { get; init; } = true;

    /// <summary>
    /// Mindeststellenanzahl, wenn <see cref="KeepOriginalWidth"/> nicht gesetzt ist.
    /// </summary>
    public int MinDigits { get; init; } = 1;

    /// <inheritdoc />
    public override RuleKind Kind => RuleKind.ShiftDigitRange;

    /// <summary>
    /// Ermittelt die größte Ziffernketten-Nummer, die in allen Namen mit Ziffernketten vorkommt
    /// (Grundlage für die Eingabebegrenzung an dieser Stelle der Kette).
    /// </summary>
    /// <param name="names">Die Namen an dieser Stelle der Kette.</param>
    /// <returns>Die größte gültige Nummer oder <c>null</c>, wenn kein Name eine Ziffernkette enthält.</returns>
    public static int? GetMaxRunNumber(IEnumerable<string> names)
    {
        var counts = names.Select(name => DigitRunPattern().Count(name)).Where(count => count > 0).ToList();
        return counts.Count == 0 ? null : counts.Min();
    }

    /// <summary>
    /// Prüft Nummer und Mindeststellen.
    /// </summary>
    /// <returns>Fehlermeldung oder <c>null</c>.</returns>
    public override string? ValidateSettings()
    {
        if (!UseLastRun && RunNumber < 1)
        {
            return CoreText.RunNumberTooSmall;
        }

        return !KeepOriginalWidth && MinDigits is < 1 or > MaxDigits
            ? CoreText.MinDigitsRange(MaxDigits)
            : null;
    }

    /// <summary>
    /// Verschiebt den Wert der gewählten Ziffernkette.
    /// </summary>
    /// <param name="input">Die Datei mit ihrem aktuellen Namen.</param>
    /// <returns>Das Ergebnis für diese Datei.</returns>
    protected override RuleOutput ApplyToFile(RuleInput input)
    {
        var runs = DigitRunPattern().Matches(input.Name.Text);
        if (runs.Count == 0)
        {
            return RuleOutput.Unchanged(input.Name, RenameIssue.Hint(CoreText.NoDigitRun, Id));
        }

        if (!UseLastRun && RunNumber > runs.Count)
        {
            return RuleOutput.Unchanged(input.Name,
                RenameIssue.Conflict(CoreText.RunNumberMissing(RunNumber, runs.Count), Id));
        }

        var run = UseLastRun ? runs[^1] : runs[RunNumber - 1];
        var value = BigInteger.Parse(run.Value, CultureInfo.InvariantCulture) + Offset;
        if (value < 0)
        {
            return RuleOutput.Unchanged(input.Name,
                RenameIssue.Conflict(CoreText.NegativeResult($"{run.Value} {Offset:+#;-#;0} = {value}"), Id));
        }

        var width = KeepOriginalWidth ? run.Length : MinDigits;
        var digits = value.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
        return RuleOutput.Changed(input.Name.Replace(run.Index, run.Length, digits, Id));
    }

    /// <summary>
    /// Muster für zusammenhängende Ziffern (0–9).
    /// </summary>
    /// <returns>Der kompilierte reguläre Ausdruck.</returns>
    [GeneratedRegex("[0-9]+")]
    private static partial Regex DigitRunPattern();
}
