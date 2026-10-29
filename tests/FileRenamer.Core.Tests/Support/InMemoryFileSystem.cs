using FileRenamer.Core.Files;

namespace FileRenamer.Core.Tests.Support;

/// <summary>
/// Dateisystem im Speicher für Tests; kann gezielt Fehler beim Verschieben auslösen.
/// </summary>
/// <param name="isCaseSensitive">Ob Dateinamen Groß-/Kleinschreibung unterscheiden.</param>
internal sealed class InMemoryFileSystem(bool isCaseSensitive = false) : IFileSystem
{
    private readonly Dictionary<string, FileEntry> _files =
        new(isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Wenn gesetzt, schlägt jedes Verschieben fehl, dessen Ziel diese Bedingung erfüllt.
    /// </summary>
    public Func<string, bool>? FailWhenTarget { get; set; }

    /// <inheritdoc />
    public bool IsCaseSensitive => isCaseSensitive;

    /// <summary>
    /// Alle aktuell vorhandenen Dateipfade.
    /// </summary>
    public IReadOnlyCollection<string> Paths => _files.Values.Select(file => file.FullPath).ToList();

    /// <summary>
    /// Legt eine Datei an.
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <param name="creationTime">Erstellungsdatum oder <c>null</c>.</param>
    /// <param name="lastWriteTime">Änderungsdatum (Standard: 1.1.2026).</param>
    /// <param name="size">Größe in Bytes.</param>
    /// <returns>Die angelegte Datei.</returns>
    public FileEntry Add(string path, DateTime? creationTime = null, DateTime? lastWriteTime = null, long size = 0)
    {
        var entry = new FileEntry(path, creationTime, lastWriteTime ?? new DateTime(2026, 1, 1), size);
        _files[path] = entry;
        return entry;
    }

    /// <inheritdoc />
    public bool Exists(string path) => _files.ContainsKey(path);

    /// <inheritdoc />
    public bool IsDirectory(string path) => false;

    /// <inheritdoc />
    public void MoveFile(string sourcePath, string targetPath)
    {
        if (!_files.Remove(sourcePath, out var entry))
        {
            throw new FileNotFoundException("Quelle fehlt.", sourcePath);
        }

        if (_files.ContainsKey(targetPath) || FailWhenTarget?.Invoke(targetPath) == true)
        {
            _files[sourcePath] = entry;
            throw new IOException($"Verschieben nach „{targetPath}“ fehlgeschlagen.");
        }

        _files[targetPath] = entry with { FullPath = targetPath };
    }

    /// <inheritdoc />
    public FileEntry ReadFileEntry(string path) => _files[path];

    /// <inheritdoc />
    public IReadOnlyList<string> GetFilesInDirectory(string directoryPath) =>
        _files.Keys.Where(path => Path.GetDirectoryName(path) == directoryPath).ToList();
}
