using FileRenamer.Core.Localization;
using FileRenamer.Core.Naming;

namespace FileRenamer.Core.Tagging.Changes;

/// <summary>
/// „Tags bearbeiten“: setzt die im Formular geänderten Felder für alle Dateien. Ein leerer Wert löscht das Feld.
/// </summary>
/// <param name="Values">Feldschlüssel → neuer Wert.</param>
public sealed record SetFieldsChange(IReadOnlyDictionary<string, string> Values) : TagChange
{
    /// <summary>
    /// Funktion „Tags bearbeiten“.
    /// </summary>
    public override TagFunction Function => TagFunction.Edit;

    /// <summary>
    /// Setzt die Felder.
    /// </summary>
    /// <param name="input">Die Datei.</param>
    /// <returns>Das Ergebnis.</returns>
    protected override TagStepResult ApplyToFile(TagStepInput input) =>
        new(input.Tags.WithFields(Values.ToDictionary(pair => pair.Key, pair => (string?)pair.Value)));
}

/// <summary>
/// „Tags aus Dateinamen“: liest Felder mit einer Vorlage aus Dateiname und Ordnern (siehe <see cref="FileNameTagParser"/>).
/// </summary>
/// <param name="Template">Die Vorlage, z. B. <c>%track% %title%</c>.</param>
/// <param name="UnderscoreAsSpace">Ob Unterstriche als Leerzeichen gelesen werden.</param>
public sealed record FieldsFromFileNameChange(string Template, bool UnderscoreAsSpace) : TagChange
{
    /// <summary>
    /// Funktion „Tags aus Dateinamen“.
    /// </summary>
    public override TagFunction Function => TagFunction.FromFileName;

    /// <summary>
    /// Übernimmt die gelesenen Felder; passt der Name nicht, bleibt die Datei unverändert und erhält einen Hinweis.
    /// </summary>
    /// <param name="input">Die Datei.</param>
    /// <returns>Das Ergebnis.</returns>
    protected override TagStepResult ApplyToFile(TagStepInput input)
    {
        var values = FileNameTagParser.Parse(Template, input.Path, UnderscoreAsSpace);
        return values is null
            ? new TagStepResult(input.Tags, CoreText.FileNameDoesNotMatch(Template))
            : new TagStepResult(input.Tags.WithFields(values.ToDictionary(pair => pair.Key, pair => (string?)pair.Value)));
    }
}

/// <summary>
/// Reihenfolge beim Vergeben von Tracknummern.
/// </summary>
public enum TrackOrder
{
    /// <summary>Wie in der Liste.</summary>
    List,

    /// <summary>Nach Dateiname (natürlich sortiert).</summary>
    FileName,

    /// <summary>Nach Titel (natürlich sortiert).</summary>
    Title,
}

/// <summary>
/// „Tracknummern vergeben“: nummeriert die Dateien fortlaufend, optional mit Anzahl der Tracks und Discnummer.
/// </summary>
/// <param name="Start">Startwert.</param>
/// <param name="Order">Reihenfolge.</param>
/// <param name="SetTrackCount">Ob „von“ auf die Anzahl der Dateien gesetzt wird.</param>
/// <param name="Disc">Discnummer oder <c>null</c> (unverändert).</param>
/// <param name="DiscCount">Anzahl der Discs oder <c>null</c> (unverändert).</param>
public sealed record TrackNumberChange(int Start, TrackOrder Order, bool SetTrackCount, int? Disc, int? DiscCount) : TagChange
{
    /// <summary>
    /// Funktion „Tracknummern vergeben“.
    /// </summary>
    public override TagFunction Function => TagFunction.TrackNumbers;

    /// <summary>
    /// Nummeriert alle Dateien gemeinsam.
    /// </summary>
    /// <param name="inputs">Die Dateien in Listenreihenfolge.</param>
    /// <returns>Die Ergebnisse in Eingabereihenfolge.</returns>
    public override IReadOnlyList<TagStepResult> Apply(IReadOnlyList<TagStepInput> inputs)
    {
        var numbers = Sort(inputs).Select((input, index) => (input, number: Start + index)).ToDictionary(pair => pair.input, pair => pair.number);
        return inputs.Select(input => new TagStepResult(input.Tags.WithFields(Values(numbers[input], inputs.Count)))).ToList();
    }

    /// <summary>
    /// Nicht verwendet; die Nummerierung braucht alle Dateien gemeinsam.
    /// </summary>
    /// <param name="input">Die Datei.</param>
    /// <returns>Das unveränderte Ergebnis.</returns>
    protected override TagStepResult ApplyToFile(TagStepInput input) => new(input.Tags);

    /// <summary>
    /// Sortiert die Dateien nach der gewählten Reihenfolge.
    /// </summary>
    /// <param name="inputs">Die Dateien.</param>
    /// <returns>Die sortierten Dateien.</returns>
    private IEnumerable<TagStepInput> Sort(IReadOnlyList<TagStepInput> inputs) => Order switch
    {
        TrackOrder.FileName => inputs.OrderBy(input => Path.GetFileName(input.Path), NaturalStringComparer.Instance),
        TrackOrder.Title => inputs.OrderBy(input => input.Tags.Get(TagFields.Title), NaturalStringComparer.Instance),
        _ => inputs,
    };

    /// <summary>
    /// Bildet die zu setzenden Felder einer Datei.
    /// </summary>
    /// <param name="number">Die Tracknummer.</param>
    /// <param name="count">Anzahl der Dateien.</param>
    /// <returns>Die Felder.</returns>
    private Dictionary<string, string?> Values(int number, int count)
    {
        var values = new Dictionary<string, string?> { [TagFields.Track] = Text(number) };
        if (SetTrackCount)
        {
            values[TagFields.TrackCount] = Text(count);
        }

        if (Disc is { } disc)
        {
            values[TagFields.Disc] = Text(disc);
        }

        if (DiscCount is { } discCount)
        {
            values[TagFields.DiscCount] = Text(discCount);
        }

        return values;
    }

    /// <summary>
    /// Formatiert eine Zahl; Werte kleiner 1 ergeben einen leeren Text (Feld entfernen).
    /// </summary>
    /// <param name="value">Die Zahl.</param>
    /// <returns>Der Text.</returns>
    private static string Text(int value) => value < 1 ? string.Empty : value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
