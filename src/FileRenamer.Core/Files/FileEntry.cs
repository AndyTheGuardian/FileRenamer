using FileRenamer.Core.Metadata;

namespace FileRenamer.Core.Files;

/// <summary>
/// Beschreibt eine zum Umbenennen ausgewählte Datei mit den für die Verfahren relevanten Metadaten.
/// </summary>
/// <param name="FullPath">Vollständiger Pfad der Datei.</param>
/// <param name="CreationTime">Erstellungsdatum (lokale Zeit) oder <c>null</c>, wenn das Dateisystem es nicht liefert.</param>
/// <param name="LastWriteTime">Änderungsdatum (lokale Zeit).</param>
/// <param name="Size">Dateigröße in Bytes.</param>
public sealed record FileEntry(string FullPath, DateTime? CreationTime, DateTime LastWriteTime, long Size)
{
    /// <summary>
    /// Verzeichnis, in dem die Datei liegt.
    /// </summary>
    public string DirectoryPath => Path.GetDirectoryName(FullPath) ?? string.Empty;

    /// <summary>
    /// Dateiname inklusive Erweiterung.
    /// </summary>
    public string FileName => Path.GetFileName(FullPath);

    /// <summary>
    /// Beim Einlesen einmal ermittelte Bild- bzw. Audioinformationen (E66).
    /// </summary>
    public FileMetadata Metadata { get; init; } = FileMetadata.None;

    /// <summary>
    /// Art der Datei laut Endung (Bild, Audio oder keine Mediendatei).
    /// </summary>
    public MediaKind Kind => MediaTypes.GetKind(FileName);

    /// <summary>
    /// Name des Ordners, in dem die Datei liegt.
    /// </summary>
    public string FolderName => Path.GetFileName(DirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
}
