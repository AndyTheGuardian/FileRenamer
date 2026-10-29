using CommunityToolkit.Mvvm.Input;

namespace FileRenamer.App.ViewModels;

/// <summary>
/// Teil des Hauptfenster-ViewModels: Dateien und Ordner hinzufügen bzw. entfernen.
/// </summary>
public sealed partial class MainWindowViewModel
{
    /// <summary>
    /// Fügt Dateien und Ordner hinzu (Ordner ohne Unterordner, E16). Bereits enthaltene Dateien werden übersprungen.
    /// </summary>
    /// <param name="paths">Datei- oder Ordnerpfade, z. B. aus Drag &amp; Drop.</param>
    public void AddPaths(IEnumerable<string> paths)
    {
        var comparer = _fileSystem.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var known = new HashSet<string>(_files.Select(file => file.FullPath), comparer);

        foreach (var path in paths.SelectMany(ExpandPath).Where(known.Add))
        {
            TryAddFile(path);
        }

        RefreshPreview();
    }

    /// <summary>
    /// Öffnet die Dateiauswahl.
    /// </summary>
    /// <returns>Ein Task.</returns>
    [RelayCommand]
    private async Task AddFilesAsync() => AddPaths(await _dialogs.PickFilesAsync());

    /// <summary>
    /// Öffnet die Ordnerauswahl.
    /// </summary>
    /// <returns>Ein Task.</returns>
    [RelayCommand]
    private async Task AddFolderAsync()
    {
        if (await _dialogs.PickFolderAsync() is { } folder)
        {
            AddPaths([folder]);
        }
    }

    /// <summary>
    /// Leert die Dateiliste.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasFiles))]
    private void ClearFiles()
    {
        _files.Clear();
        _deselectedPaths.Clear();
        RefreshPreview();
    }

    /// <summary>
    /// Ersetzt einen Ordner durch die Dateien direkt darin; Dateien bleiben unverändert.
    /// </summary>
    /// <param name="path">Datei- oder Ordnerpfad.</param>
    /// <returns>Die Dateipfade.</returns>
    private IEnumerable<string> ExpandPath(string path) =>
        _fileSystem.IsDirectory(path) ? _fileSystem.GetFilesInDirectory(path) : [path];

    /// <summary>
    /// Liest eine Datei ein; nicht lesbare Dateien werden übersprungen.
    /// </summary>
    /// <param name="path">Der Dateipfad.</param>
    private void TryAddFile(string path)
    {
        try
        {
            _files.Add(_fileSystem.ReadFileEntry(path));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Nicht lesbare Einträge werden ignoriert; die Liste zeigt nur verwendbare Dateien.
        }
    }
}
