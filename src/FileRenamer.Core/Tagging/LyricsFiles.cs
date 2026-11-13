using System.Text;
using System.Text.RegularExpressions;
using FileRenamer.Core.Localization;

namespace FileRenamer.Core.Tagging;

/// <summary>
/// Format beim Export von Lyrics.
/// </summary>
public enum LyricsFormat
{
    /// <summary>LRC: der Text unverändert, Zeitstempel bleiben erhalten.</summary>
    Lrc,

    /// <summary>Reiner Text: Zeitstempel und LRC-Kopfzeilen werden entfernt.</summary>
    Txt,
}

/// <summary>
/// Import und Export von Lyrics als Begleitdateien (<c>.lrc</c>, <c>.txt</c>) mit dem Namen der Audiodatei (E118).
/// </summary>
public static class LyricsFiles
{
    /// <summary>
    /// Endungen, die beim Import gesucht werden, in dieser Reihenfolge.
    /// </summary>
    public static IReadOnlyList<string> Extensions { get; } = [".lrc", ".txt"];

    private static readonly Regex TimeStamp = new(@"\[\d{1,3}:\d{2}(?:[.:]\d{1,3})?\]", RegexOptions.CultureInvariant);
    private static readonly Regex HeaderLine = new(@"^\[[a-z]+:.*\]$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>
    /// Sucht die Begleitdatei gleichen Namens im Ordner der Audiodatei.
    /// </summary>
    /// <param name="audioPath">Pfad der Audiodatei.</param>
    /// <returns>Pfad der gefundenen Datei oder <c>null</c>.</returns>
    public static string? FindSidecar(string audioPath)
    {
        var basePath = Path.Combine(Path.GetDirectoryName(audioPath) ?? string.Empty, Path.GetFileNameWithoutExtension(audioPath));
        return Extensions.Select(extension => basePath + extension).FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// Liest eine Lyrics-Datei (UTF-8, ein BOM wird erkannt); Zeilenumbrüche werden vereinheitlicht.
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <returns>Der Text.</returns>
    public static string Read(string path) => NormalizeLineBreaks(File.ReadAllText(path, Encoding.UTF8)).Trim();

    /// <summary>
    /// Wandelt LRC-Text in reinen Text um: Zeitstempel und Kopfzeilen wie <c>[ar:…]</c> entfallen.
    /// </summary>
    /// <param name="lyrics">Der Text.</param>
    /// <returns>Der reine Text.</returns>
    public static string ToPlainText(string lyrics) =>
        string.Join('\n', NormalizeLineBreaks(lyrics).Split('\n')
            .Where(line => !HeaderLine.IsMatch(line.Trim()))
            .Select(line => TimeStamp.Replace(line, string.Empty).Trim())).Trim();

    /// <summary>
    /// Exportiert die Lyrics der Dateien; Dateien ohne Lyrics werden übersprungen.
    /// </summary>
    /// <param name="files">Pfad und Tags der Audiodateien.</param>
    /// <param name="format">Das Format.</param>
    /// <param name="targetFolder">Zielordner oder <c>null</c> für den Ordner der Audiodatei.</param>
    /// <param name="overwrite">Ob vorhandene Dateien überschrieben werden.</param>
    /// <returns>Das Ergebnis.</returns>
    public static ExportResult Export(IEnumerable<(string Path, AudioTags Tags)> files, LyricsFormat format, string? targetFolder, bool overwrite)
    {
        int written = 0, skipped = 0;
        var errors = new List<string>();
        foreach (var (path, tags) in files)
        {
            var ok = tags.Lyrics.Length > 0 && ExportOne(path, tags.Lyrics, format, targetFolder, overwrite, errors);
            written += ok ? 1 : 0;
            skipped += ok ? 0 : 1;
        }

        return new ExportResult(written, skipped, errors);
    }

    /// <summary>
    /// Schreibt die Lyrics einer Datei.
    /// </summary>
    /// <param name="audioPath">Pfad der Audiodatei.</param>
    /// <param name="lyrics">Die Lyrics.</param>
    /// <param name="format">Das Format.</param>
    /// <param name="targetFolder">Zielordner oder <c>null</c>.</param>
    /// <param name="overwrite">Ob überschrieben wird.</param>
    /// <param name="errors">Sammelt Fehler.</param>
    /// <returns><c>true</c>, wenn geschrieben wurde.</returns>
    private static bool ExportOne(string audioPath, string lyrics, LyricsFormat format, string? targetFolder, bool overwrite, List<string> errors)
    {
        var extension = format == LyricsFormat.Lrc ? ".lrc" : ".txt";
        var folder = targetFolder ?? Path.GetDirectoryName(audioPath) ?? string.Empty;
        var path = Path.Combine(folder, Path.GetFileNameWithoutExtension(audioPath) + extension);
        if (File.Exists(path) && !overwrite)
        {
            return false;
        }

        var text = format == LyricsFormat.Lrc ? lyrics : ToPlainText(lyrics);
        return SidecarFile.TryWrite(path, new UTF8Encoding(false).GetBytes(text.Replace("\n", Environment.NewLine)), errors);
    }

    /// <summary>
    /// Vereinheitlicht Zeilenumbrüche auf <c>\n</c>.
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <returns>Der Text mit <c>\n</c>.</returns>
    private static string NormalizeLineBreaks(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');
}
