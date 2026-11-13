using FileRenamer.Core.Localization;

namespace FileRenamer.Core.Tagging;

/// <summary>
/// Schreibt Begleitdateien (Cover, Lyrics) neben oder unabhängig von Audiodateien, ohne vorhandene Dateien zu überschreiben.
/// </summary>
internal static class SidecarFile
{
    private const int MaxSuffix = 99;

    /// <summary>
    /// Schreibt Daten unter einem freien Namen: <c>name.ext</c>, sonst <c>name (2).ext</c> usw.
    /// </summary>
    /// <param name="folder">Zielordner.</param>
    /// <param name="baseName">Dateiname ohne Endung.</param>
    /// <param name="extension">Endung mit Punkt.</param>
    /// <param name="data">Die Daten.</param>
    /// <param name="skipIdentical">Ob eine vorhandene Datei mit gleichem Inhalt als „schon vorhanden“ gilt.</param>
    /// <param name="errors">Sammelt Fehler.</param>
    /// <returns><c>true</c>, wenn eine Datei geschrieben wurde.</returns>
    public static bool TryWriteUnique(string folder, string baseName, string extension, byte[] data, bool skipIdentical, List<string> errors)
    {
        for (var suffix = 1; suffix <= MaxSuffix; suffix++)
        {
            var path = Path.Combine(folder, suffix == 1 ? baseName + extension : $"{baseName} ({suffix}){extension}");
            if (!File.Exists(path))
            {
                return TryWrite(path, data, errors);
            }

            if (skipIdentical && File.ReadAllBytes(path).AsSpan().SequenceEqual(data))
            {
                return false;
            }
        }

        errors.Add(CoreText.NoFreeFileName(baseName + extension));
        return false;
    }

    /// <summary>
    /// Schreibt eine Datei und überschreibt dabei, falls gewünscht.
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <param name="data">Die Daten.</param>
    /// <param name="errors">Sammelt Fehler.</param>
    /// <returns><c>true</c> bei Erfolg.</returns>
    public static bool TryWrite(string path, byte[] data, List<string> errors)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, data);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            errors.Add(CoreText.ExportFailed(Path.GetFileName(path), exception.Message));
            return false;
        }
    }
}
