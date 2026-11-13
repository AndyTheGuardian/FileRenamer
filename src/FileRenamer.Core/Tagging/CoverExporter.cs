using System.Security.Cryptography;
using FileRenamer.Core.Localization;
using FileRenamer.Core.Validation;

namespace FileRenamer.Core.Tagging;

/// <summary>
/// Einstellungen für „Cover extrahieren“.
/// </summary>
/// <param name="Type">Der Bildtyp oder <c>null</c> für alle Bilder.</param>
/// <param name="TargetFolder">Zielordner oder <c>null</c> für den Ordner der jeweiligen Audiodatei.</param>
/// <param name="FileNameTemplate">Dateiname ohne Endung mit Platzhaltern <c>%type%</c>, <c>%album%</c>, <c>%artist%</c>, <c>%name%</c>.</param>
/// <param name="SkipDuplicates">Ob gleiche Bilder je Zielordner nur einmal gespeichert werden.</param>
public sealed record CoverExportOptions(CoverType? Type, string? TargetFolder, string FileNameTemplate, bool SkipDuplicates);

/// <summary>
/// Ergebnis von „Cover extrahieren“ bzw. „Lyrics exportieren“.
/// </summary>
/// <param name="Written">Anzahl geschriebener Dateien.</param>
/// <param name="Skipped">Anzahl übersprungener Dateien (doppelt, vorhanden oder ohne Inhalt).</param>
/// <param name="Errors">Fehlermeldungen.</param>
public sealed record ExportResult(int Written, int Skipped, IReadOnlyList<string> Errors);

/// <summary>
/// Speichert eingebettete Bilder als Dateien. Vorhandene Dateien werden nie überschrieben: Ein anderer Inhalt erhält
/// den Zusatz „ (2)“, „ (3)“ …; ein gleicher Inhalt gilt bei <see cref="CoverExportOptions.SkipDuplicates"/> als doppelt.
/// </summary>
public static class CoverExporter
{
    /// <summary>
    /// Exportiert die Bilder der Dateien.
    /// </summary>
    /// <param name="files">Pfad und Tags der Audiodateien.</param>
    /// <param name="options">Die Einstellungen.</param>
    /// <returns>Das Ergebnis.</returns>
    public static ExportResult Export(IEnumerable<(string Path, AudioTags Tags)> files, CoverExportOptions options)
    {
        var known = new HashSet<string>(StringComparer.Ordinal);
        int written = 0, skipped = 0;
        var errors = new List<string>();
        foreach (var (path, tags) in files)
        {
            foreach (var cover in tags.Covers.Where(cover => options.Type is null || cover.Type == options.Type))
            {
                var outcome = ExportOne(path, tags, cover, options, known, errors);
                written += outcome ? 1 : 0;
                skipped += outcome ? 0 : 1;
            }
        }

        return new ExportResult(written, skipped, errors);
    }

    /// <summary>
    /// Bildet den Dateinamen eines Bildes ohne Endung; verbotene Zeichen werden durch „_“ ersetzt.
    /// </summary>
    /// <param name="template">Die Vorlage.</param>
    /// <param name="audioPath">Pfad der Audiodatei.</param>
    /// <param name="tags">Tags der Audiodatei.</param>
    /// <param name="type">Bildtyp.</param>
    /// <returns>Der Dateiname.</returns>
    public static string CreateFileName(string template, string audioPath, AudioTags tags, CoverType type)
    {
        var name = (string.IsNullOrWhiteSpace(template) ? "cover" : template)
            .Replace("%type%", CoreText.CoverTypeFileName(type), StringComparison.Ordinal)
            .Replace("%album%", tags.Get(TagFields.Album), StringComparison.Ordinal)
            .Replace("%artist%", tags.Get(TagFields.Artist), StringComparison.Ordinal)
            .Replace("%name%", Path.GetFileNameWithoutExtension(audioPath), StringComparison.Ordinal);
        var cleaned = FileNameValidator.ReplaceInvalidCharacters(name, '_').Trim();
        return cleaned.Length == 0 ? "cover" : cleaned;
    }

    /// <summary>
    /// Schreibt ein Bild.
    /// </summary>
    /// <param name="audioPath">Pfad der Audiodatei.</param>
    /// <param name="tags">Tags der Audiodatei.</param>
    /// <param name="cover">Das Bild.</param>
    /// <param name="options">Die Einstellungen.</param>
    /// <param name="known">Schon geschriebene Inhalte je Ordner (Ordner + Hash).</param>
    /// <param name="errors">Sammelt Fehler.</param>
    /// <returns><c>true</c>, wenn eine Datei geschrieben wurde.</returns>
    private static bool ExportOne(
        string audioPath, AudioTags tags, CoverImage cover, CoverExportOptions options, HashSet<string> known, List<string> errors)
    {
        var folder = options.TargetFolder ?? Path.GetDirectoryName(audioPath) ?? string.Empty;
        var hash = folder + "|" + Convert.ToHexString(SHA256.HashData(cover.Data));
        if (options.SkipDuplicates && !known.Add(hash))
        {
            return false;
        }

        var baseName = CreateFileName(options.FileNameTemplate, audioPath, tags, cover.Type);
        return SidecarFile.TryWriteUnique(folder, baseName, cover.Extension, cover.Data, options.SkipDuplicates, errors);
    }
}
