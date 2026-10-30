using FileRenamer.Core.Localization;
using System.Globalization;
using System.Text.Json.Serialization;
using FileRenamer.Core.Files;
using FileRenamer.Core.Validation;

namespace FileRenamer.Core.Rules;

/// <summary>
/// Verfahren „Datum / Zeit anfügen“: fügt das Erstellungs- oder Änderungsdatum (optional mit Uhrzeit)
/// vor oder nach dem Namen ein. Fehlt das Erstellungsdatum, wird das Änderungsdatum mit Hinweis verwendet.
/// </summary>
public sealed record AppendDateRule : PerFileRenameRule
{
    /// <summary>
    /// Welches Datum verwendet wird.
    /// </summary>
    public DateSource Source { get; init; } = DateSource.CreationDate;

    /// <summary>
    /// Vor oder nach dem Namen.
    /// </summary>
    public AffixPosition Position { get; init; } = AffixPosition.BeforeName;

    /// <summary>
    /// Datumsformat in .NET-Schreibweise.
    /// </summary>
    public string DateFormat { get; init; } = "yyyy-MM-dd";

    /// <summary>
    /// Ob zusätzlich die Uhrzeit angefügt wird.
    /// </summary>
    public bool IncludeTime { get; init; }

    /// <summary>
    /// Uhrzeitformat in .NET-Schreibweise.
    /// </summary>
    public string TimeFormat { get; init; } = "HH-mm-ss";

    /// <summary>
    /// Trennzeichen zwischen Name und Datum.
    /// </summary>
    public string NameSeparator { get; init; } = "_";

    /// <summary>
    /// Trennzeichen zwischen Datum und Uhrzeit.
    /// </summary>
    public string DateTimeSeparator { get; init; } = "_";

    /// <summary>
    /// Kultur für Monatsnamen u. Ä.; wird nicht gespeichert, Standard ist die aktuelle Kultur.
    /// </summary>
    [JsonIgnore]
    public CultureInfo Culture { get; init; } = CultureInfo.CurrentCulture;

    /// <inheritdoc />
    public override RuleKind Kind => RuleKind.AppendDate;

    /// <summary>
    /// Prüft Formate und Trennzeichen.
    /// </summary>
    /// <returns>Fehlermeldung oder <c>null</c>.</returns>
    public override string? ValidateSettings() =>
        DateFormatValidator.ValidateDateFormat(DateFormat, Culture)
        ?? (IncludeTime ? DateFormatValidator.ValidateTimeFormat(TimeFormat, Culture) : null)
        ?? FileNameValidator.DescribeInvalidCharacter(NameSeparator)
        ?? (IncludeTime ? FileNameValidator.DescribeInvalidCharacter(DateTimeSeparator) : null);

    /// <summary>
    /// Fügt das formatierte Datum an.
    /// </summary>
    /// <param name="input">Die Datei mit ihrem aktuellen Namen.</param>
    /// <returns>Das Ergebnis für diese Datei.</returns>
    protected override RuleOutput ApplyToFile(RuleInput input)
    {
        var (timestamp, issues) = ResolveTimestamp(input.File);
        var name = Affix.Add(input.Name, FormatTimestamp(timestamp), NameSeparator, Position, Id);
        return new RuleOutput(name, issues);
    }

    /// <summary>
    /// Ermittelt den zu verwendenden Zeitpunkt inklusive Fallback-Hinweis.
    /// </summary>
    /// <param name="file">Die Datei.</param>
    /// <returns>Zeitpunkt und ggf. Hinweis.</returns>
    private (DateTime Timestamp, IReadOnlyList<RenameIssue> Issues) ResolveTimestamp(FileEntry file)
    {
        if (Source == DateSource.ModificationDate)
        {
            return (file.LastWriteTime, []);
        }

        if (Source == DateSource.ExifDate)
        {
            return file.Metadata.Image?.DateTaken is { } taken
                ? (taken, [])
                : (file.LastWriteTime, [RenameIssue.Hint(CoreText.ExifDateFallback, Id)]);
        }

        return file.CreationTime is { } created
            ? (created, [])
            : (file.LastWriteTime, [RenameIssue.Hint(CoreText.CreationDateFallback, Id)]);
    }

    /// <summary>
    /// Formatiert Datum und optional Uhrzeit.
    /// </summary>
    /// <param name="timestamp">Der Zeitpunkt.</param>
    /// <returns>Der formatierte Zusatz.</returns>
    private string FormatTimestamp(DateTime timestamp)
    {
        var date = DateFormatValidator.Format(timestamp, DateFormat, Culture);
        return IncludeTime
            ? date + DateTimeSeparator + DateFormatValidator.Format(timestamp, TimeFormat, Culture)
            : date;
    }
}
