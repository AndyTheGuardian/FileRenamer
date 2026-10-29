namespace FileRenamer.Core.Files;

/// <summary>
/// Abstraktion der benötigten Dateisystemzugriffe, damit Vorschau und Umbenennung
/// ohne echte Dateien getestet werden können.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Gibt an, ob Dateinamen in diesem Dateisystem Groß-/Kleinschreibung unterscheiden.
    /// </summary>
    bool IsCaseSensitive { get; }

    /// <summary>
    /// Prüft, ob unter dem Pfad eine Datei oder ein Verzeichnis existiert.
    /// </summary>
    /// <param name="path">Der zu prüfende Pfad.</param>
    /// <returns><c>true</c>, wenn der Pfad belegt ist.</returns>
    bool Exists(string path);

    /// <summary>
    /// Prüft, ob der Pfad ein Verzeichnis ist.
    /// </summary>
    /// <param name="path">Der zu prüfende Pfad.</param>
    /// <returns><c>true</c> bei einem Verzeichnis.</returns>
    bool IsDirectory(string path);

    /// <summary>
    /// Benennt eine Datei um bzw. verschiebt sie. Existiert das Ziel bereits, wird eine <see cref="IOException"/> ausgelöst.
    /// </summary>
    /// <param name="sourcePath">Bisheriger Pfad.</param>
    /// <param name="targetPath">Neuer Pfad.</param>
    void MoveFile(string sourcePath, string targetPath);

    /// <summary>
    /// Liest die Metadaten einer Datei.
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <returns>Die Dateibeschreibung.</returns>
    FileEntry ReadFileEntry(string path);

    /// <summary>
    /// Liefert die Dateien direkt in einem Verzeichnis (ohne Unterverzeichnisse).
    /// </summary>
    /// <param name="directoryPath">Pfad des Verzeichnisses.</param>
    /// <returns>Vollständige Pfade der Dateien.</returns>
    IReadOnlyList<string> GetFilesInDirectory(string directoryPath);
}
