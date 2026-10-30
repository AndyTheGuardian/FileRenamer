namespace FileRenamer.Core.Metadata;

/// <summary>
/// Ordnet Dateiendungen den unterstützten Bild- und Audioformaten zu.
/// </summary>
public static class MediaTypes
{
    /// <summary>
    /// Endungen, deren Datei vollständig TIFF-aufgebaut ist (TIFF und TIFF-basierte RAW-Formate).
    /// </summary>
    internal static readonly HashSet<string> TiffBased = new(
        [".tif", ".tiff", ".dng", ".cr2", ".nef", ".arw", ".orf", ".rw2", ".pef"], StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ImageExtensions = new(
        [.. TiffBased, ".jpg", ".jpeg", ".jpe", ".heic", ".heif", ".png", ".cr3", ".raf"], StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> AudioExtensions = new([".mp3", ".flac", ".m4a"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Bestimmt die Art einer Datei anhand ihrer Endung.
    /// </summary>
    /// <param name="fileName">Dateiname oder Pfad.</param>
    /// <returns>Die Art der Datei.</returns>
    public static MediaKind GetKind(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (ImageExtensions.Contains(extension))
        {
            return MediaKind.Image;
        }

        return AudioExtensions.Contains(extension) ? MediaKind.Audio : MediaKind.None;
    }
}
