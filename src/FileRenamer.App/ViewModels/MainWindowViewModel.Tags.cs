using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileRenamer.App.Localization;
using FileRenamer.App.ViewModels.Tags;
using FileRenamer.Core.Tagging;
using FileRenamer.Core.Tagging.Changes;

namespace FileRenamer.App.ViewModels;

/// <summary>
/// Teil des Hauptfenster-ViewModels: Umschalten zwischen den Bereichen „Umbenennen“ und „Tags“ (E114, E119) und
/// Anbindung des Bereichs „Tags“ an die gemeinsame Dateiliste.
/// </summary>
public sealed partial class MainWindowViewModel
{
    /// <summary>
    /// Programmweite Einstellungen des Bereichs „Tags“.
    /// </summary>
    public TagLibrary TagLibrary { get; private set; } = new(new Core.Profiles.AppSettings());

    /// <summary>
    /// Der Bereich „Tags“.
    /// </summary>
    public TagAreaViewModel Tags { get; private set; } = null!;

    /// <summary>
    /// Ob der Bereich „Tags“ angezeigt wird.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRenameArea))]
    public partial bool IsTagArea { get; set; }

    /// <summary>
    /// Ob der Bereich „Umbenennen“ angezeigt wird (Profilfeld nur dann bedienbar, E130).
    /// </summary>
    public bool IsRenameArea
    {
        get => !IsTagArea;
        set => IsTagArea = !value;
    }

    /// <summary>
    /// Zeigt den Bereich „Umbenennen“ (Strg+1).
    /// </summary>
    [RelayCommand]
    private void ShowRenameArea() => IsTagArea = false;

    /// <summary>
    /// Zeigt den Bereich „Tags“ (Strg+2).
    /// </summary>
    [RelayCommand]
    private void ShowTagArea() => IsTagArea = true;

    /// <summary>
    /// Zeigt den Bereich „Tags“ mit einer bestimmten Funktion (Menü „Tags“).
    /// </summary>
    /// <param name="function">Die Funktion.</param>
    [RelayCommand]
    private void ShowTagFunction(TagFunction function)
    {
        IsTagArea = true;
        Tags.SelectedFunction = Tags.FunctionEntries.OfType<TagFunctionItem>().First(item => item.Function == function);
    }

    /// <summary>
    /// „Liste leeren“ in beiden Bereichen und im Menü „Datei“ (F68). Sind Tag-Änderungen vorgemerkt, wird vorher
    /// nachgefragt; bei Zustimmung werden sie mit verworfen.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasFiles))]
    private async Task ClearFilesAsync()
    {
        if (Tags.PendingChanges.Count > 0
            && !await _dialogs.ConfirmAsync(Loc.T("TagClear.ConfirmTitle"), Loc.F("TagClear.ConfirmText", Tags.PendingChanges.Count), Loc.T("TagClear.Confirm")))
        {
            return;
        }

        if (Tags.DiscardCommand.CanExecute(null))
        {
            Tags.DiscardCommand.Execute(null);
        }

        ClearFileList();
    }

    /// <summary>
    /// Erzeugt den Bereich „Tags“.
    /// </summary>
    /// <param name="store">Zugriff auf die Tags.</param>
    /// <param name="images">Bildverarbeitung.</param>
    private void InitializeTags(ITagStore store, ICoverImageProcessor images)
    {
        Tags = new TagAreaViewModel(store, () => _files, _dialogs, _clipboard, images, TagLibrary);
        Tags.FilesWritten += (_, paths) => ReloadFileEntries(paths);
        ConnectColumns(TagColumnsKey, Tags.Columns);
    }

    /// <summary>
    /// Aktiviert bzw. deaktiviert den Bereich „Tags“.
    /// </summary>
    /// <param name="value">Ob der Bereich „Tags“ angezeigt wird.</param>
    partial void OnIsTagAreaChanged(bool value) => Tags.IsActive = value;

    /// <summary>
    /// Liest Dateieinträge samt Metadaten nach dem Schreiben der Tags neu, damit „Name aus Vorlage“ die neuen Tags
    /// verwendet (E126).
    /// </summary>
    /// <param name="paths">Die geschriebenen Dateien.</param>
    private void ReloadFileEntries(IReadOnlyList<string> paths)
    {
        var written = paths.ToHashSet(StringComparer.Ordinal);
        for (var index = 0; index < _files.Count; index++)
        {
            if (written.Contains(_files[index].FullPath))
            {
                _files[index] = _fileSystem.ReadFileEntry(_files[index].FullPath);
            }
        }

        RefreshPreview();
    }
}
