namespace FileRenamer.Core.Files;

/// <summary>
/// Implementierung von <see cref="IFileSystem"/> auf Basis des echten Dateisystems.
/// </summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    /// <summary>
    /// Linux-Dateisysteme unterscheiden in der Regel Groß-/Kleinschreibung, Windows und macOS (Standard) nicht.
    /// </summary>
    public bool IsCaseSensitive => OperatingSystem.IsLinux();

    /// <summary>
    /// Prüft, ob unter dem Pfad eine Datei oder ein Verzeichnis existiert.
    /// </summary>
    /// <param name="path">Der zu prüfende Pfad.</param>
    /// <returns><c>true</c>, wenn der Pfad belegt ist.</returns>
    public bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

    /// <summary>
    /// Prüft, ob der Pfad ein Verzeichnis ist.
    /// </summary>
    /// <param name="path">Der zu prüfende Pfad.</param>
    /// <returns><c>true</c> bei einem Verzeichnis.</returns>
    public bool IsDirectory(string path) => Directory.Exists(path);

    /// <summary>
    /// Benennt eine Datei um; überschreibt niemals eine vorhandene Datei.
    /// </summary>
    /// <param name="sourcePath">Bisheriger Pfad.</param>
    /// <param name="targetPath">Neuer Pfad.</param>
    public void MoveFile(string sourcePath, string targetPath) => File.Move(sourcePath, targetPath, overwrite: false);

    /// <summary>
    /// Liest die Dateiangaben samt Bild-/Audio-Metadaten. Unter Linux wird das Erstellungsdatum als nicht
    /// verfügbar gemeldet, weil .NET dort keinen verlässlichen Wert liefert.
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <returns>Die Dateibeschreibung.</returns>
    public FileEntry ReadFileEntry(string path)
    {
        var info = new FileInfo(path);
        DateTime? creationTime = OperatingSystem.IsLinux() ? null : info.CreationTime;
        return new FileEntry(info.FullName, creationTime, info.LastWriteTime, info.Length)
        {
            Metadata = Metadata.MetadataReader.Read(info.FullName),
        };
    }

    /// <summary>
    /// Liefert die Dateien direkt in einem Verzeichnis, natürlich sortiert.
    /// </summary>
    /// <param name="directoryPath">Pfad des Verzeichnisses.</param>
    /// <returns>Vollständige Pfade der Dateien.</returns>
    public IReadOnlyList<string> GetFilesInDirectory(string directoryPath) =>
        Directory.GetFiles(directoryPath, "*", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, Naming.NaturalStringComparer.Instance)
            .ToList();
}
