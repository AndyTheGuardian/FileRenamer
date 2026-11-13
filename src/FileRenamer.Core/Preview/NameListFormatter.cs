namespace FileRenamer.Core.Preview;

/// <summary>
/// Welche Spalten der Vorschau beim Kopieren in die Zwischenablage übernommen werden.
/// </summary>
public enum NameColumns
{
    /// <summary>Nur der aktuelle Name.</summary>
    Original,

    /// <summary>Nur der neue Name.</summary>
    New,

    /// <summary>Aktueller und neuer Name, durch einen Tabulator getrennt (z. B. für Tabellenkalkulationen).</summary>
    Both,
}

/// <summary>
/// Ein Paar aus aktuellem und neuem Namen, so wie es in der Vorschau angezeigt wird.
/// </summary>
/// <param name="Original">Der angezeigte aktuelle Name.</param>
/// <param name="New">Der angezeigte neue Name.</param>
public sealed record NamePair(string Original, string New);

/// <summary>
/// Erzeugt den Text, der beim Kopieren von Vorschauzeilen in die Zwischenablage gelegt wird:
/// eine Zeile je Datei, bei <see cref="NameColumns.Both"/> beide Namen durch einen Tabulator getrennt.
/// </summary>
public static class NameListFormatter
{
    /// <summary>
    /// Trennzeichen zwischen aktuellem und neuem Namen.
    /// </summary>
    public const char ColumnSeparator = '\t';

    /// <summary>
    /// Fügt die Namen zu einem Text zusammen.
    /// </summary>
    /// <param name="pairs">Die Namen in Anzeigereihenfolge.</param>
    /// <param name="columns">Die zu übernehmenden Spalten.</param>
    /// <param name="lineSeparator">Zeilentrenner; Standard ist der Zeilenumbruch des Betriebssystems.</param>
    /// <returns>Der Text; leer, wenn keine Namen übergeben wurden.</returns>
    public static string Format(IEnumerable<NamePair> pairs, NameColumns columns, string? lineSeparator = null)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        return string.Join(lineSeparator ?? Environment.NewLine, pairs.Select(pair => FormatLine(pair, columns)));
    }

    /// <summary>
    /// Erzeugt die Zeile einer Datei.
    /// </summary>
    /// <param name="pair">Die Namen.</param>
    /// <param name="columns">Die zu übernehmenden Spalten.</param>
    /// <returns>Die Zeile ohne Zeilentrenner.</returns>
    private static string FormatLine(NamePair pair, NameColumns columns) => columns switch
    {
        NameColumns.Original => pair.Original,
        NameColumns.New => pair.New,
        NameColumns.Both => pair.Original + ColumnSeparator + pair.New,
        _ => throw new ArgumentOutOfRangeException(nameof(columns), columns, null),
    };
}
