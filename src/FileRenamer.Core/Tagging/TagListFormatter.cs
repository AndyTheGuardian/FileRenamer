namespace FileRenamer.Core.Tagging;

/// <summary>
/// Welche Spalten der Tag-Vorschau beim Kopieren in die Zwischenablage übernommen werden (F66).
/// </summary>
public enum TagCopyColumns
{
    /// <summary>Nur der Dateiname.</summary>
    FileNames,

    /// <summary>Nur die Tag-Spalten (Track, Titel, Interpret, Album, Albuminterpret, Jahr, Cover, Lyrics).</summary>
    Tags,

    /// <summary>Dateiname und Tag-Spalten.</summary>
    FileNamesAndTags,

    /// <summary>Dateiname und Tag-Spalten mit Kopfzeile.</summary>
    Rows,
}

/// <summary>
/// Eine Zeile zum Kopieren: Dateiname und Tags im gespeicherten Stand (F67).
/// </summary>
/// <param name="FileName">Der Dateiname.</param>
/// <param name="Tags">Die Tags, wie sie in der Datei stehen.</param>
public sealed record TagCopyRow(string FileName, AudioTags Tags);

/// <summary>
/// Beschriftungen für die Ausgabe: Kopfzeile in Spaltenreihenfolge (Datei, Track, Titel, Interpret, Album,
/// Albuminterpret, Jahr, Cover, Lyrics) und der Text für „vorhanden“ bei Cover und Lyrics.
/// </summary>
/// <param name="Headers">Die neun Spaltenüberschriften.</param>
/// <param name="Present">Text für ein vorhandenes Cover bzw. vorhandene Lyrics.</param>
public sealed record TagCopyLabels(IReadOnlyList<string> Headers, string Present);

/// <summary>
/// Erzeugt den Text, der beim Kopieren aus der Tag-Vorschau in die Zwischenablage gelegt wird: eine Zeile je Datei,
/// Spalten durch Tabulatoren getrennt, leere Werte als leere Zelle. Tabulatoren und Zeilenumbrüche in Werten werden
/// durch Leerzeichen ersetzt, damit die Tabelle erhalten bleibt.
/// </summary>
public static class TagListFormatter
{
    /// <summary>
    /// Trennzeichen zwischen den Spalten.
    /// </summary>
    public const char ColumnSeparator = '\t';

    private static readonly string[] TextFields =
        [TagFields.Track, TagFields.Title, TagFields.Artist, TagFields.Album, TagFields.AlbumArtist, TagFields.Year];

    /// <summary>
    /// Fügt die Zeilen zu einem Text zusammen.
    /// </summary>
    /// <param name="rows">Die Zeilen in Anzeigereihenfolge.</param>
    /// <param name="columns">Die zu übernehmenden Spalten.</param>
    /// <param name="labels">Kopfzeile und Text für „vorhanden“.</param>
    /// <param name="lineSeparator">Zeilentrenner; Standard ist der Zeilenumbruch des Betriebssystems.</param>
    /// <returns>Der Text; bei <see cref="TagCopyColumns.Rows"/> mindestens die Kopfzeile.</returns>
    public static string Format(IEnumerable<TagCopyRow> rows, TagCopyColumns columns, TagCopyLabels labels, string? lineSeparator = null)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(labels);
        var lines = rows.Select(row => FormatLine(row, columns, labels.Present));
        if (columns == TagCopyColumns.Rows)
        {
            lines = lines.Prepend(string.Join(ColumnSeparator, labels.Headers.Select(Clean)));
        }

        return string.Join(lineSeparator ?? Environment.NewLine, lines);
    }

    /// <summary>
    /// Erzeugt die Zeile einer Datei.
    /// </summary>
    /// <param name="row">Die Datei.</param>
    /// <param name="columns">Die zu übernehmenden Spalten.</param>
    /// <param name="present">Text für „vorhanden“.</param>
    /// <returns>Die Zeile ohne Zeilentrenner.</returns>
    private static string FormatLine(TagCopyRow row, TagCopyColumns columns, string present) => columns switch
    {
        TagCopyColumns.FileNames => Clean(row.FileName),
        TagCopyColumns.Tags => string.Join(ColumnSeparator, TagValues(row.Tags, present)),
        TagCopyColumns.FileNamesAndTags or TagCopyColumns.Rows =>
            string.Join(ColumnSeparator, TagValues(row.Tags, present).Prepend(Clean(row.FileName))),
        _ => throw new ArgumentOutOfRangeException(nameof(columns), columns, null),
    };

    /// <summary>
    /// Liefert die Werte der Tag-Spalten.
    /// </summary>
    /// <param name="tags">Die Tags.</param>
    /// <param name="present">Text für „vorhanden“.</param>
    /// <returns>Track, Titel, Interpret, Album, Albuminterpret, Jahr, Cover, Lyrics.</returns>
    private static IEnumerable<string> TagValues(AudioTags tags, string present) =>
        TextFields.Select(key => Clean(tags.Get(key)))
            .Append(tags.Covers.Count > 0 ? present : string.Empty)
            .Append(tags.Lyrics.Length > 0 ? present : string.Empty);

    /// <summary>
    /// Ersetzt Tabulatoren und Zeilenumbrüche durch Leerzeichen.
    /// </summary>
    /// <param name="value">Der Wert.</param>
    /// <returns>Der bereinigte Wert.</returns>
    private static string Clean(string value) =>
        value.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
}
