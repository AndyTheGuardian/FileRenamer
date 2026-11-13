using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileRenamer.App.Localization;
using FileRenamer.App.Services;
using FileRenamer.Core.Files;
using FileRenamer.Core.Preview;
using FileRenamer.Core.Tagging;
using FileRenamer.Core.Tagging.Changes;

namespace FileRenamer.App.ViewModels.Tags;

/// <summary>
/// ViewModel des Bereichs „Tags“ (E114): Funktionsliste und Editor (oben), vorgemerkte Änderungen und Tag-Vorschau
/// (unten). Die Dateiliste wird mit dem Umbenennen geteilt; Tags werden erst gelesen, wenn der Bereich aktiv ist.
/// Wie beim Umbenennen wählt die Checkbox einer Zeile die Dateien, mit denen gearbeitet wird; die Zeilenmarkierung
/// dient nur dem Kopieren (E150).
/// </summary>
public sealed partial class TagAreaViewModel : ObservableObject, ITagEditorContext
{
    private readonly ITagStore _store;
    private readonly Func<IReadOnlyList<FileEntry>> _files;
    private readonly Dictionary<string, (DateTime Stamp, AudioTags? Tags, string? Error)> _disk = new(StringComparer.Ordinal);
    private readonly IClipboardService _clipboard;
    private readonly Dictionary<string, TagFilePreview> _previews = new(StringComparer.Ordinal);
    private readonly HashSet<string> _selectedPaths = new(StringComparer.Ordinal);
    private readonly HashSet<string> _markedPaths = new(StringComparer.Ordinal);
    private readonly Dictionary<TagFunction, TagEditorViewModel> _editors = [];
    private IReadOnlyList<string> _audioFiles = [];

    /// <summary>
    /// Erzeugt den Bereich.
    /// </summary>
    /// <param name="store">Zugriff auf die Tags.</param>
    /// <param name="files">Liefert die gemeinsame Dateiliste.</param>
    /// <param name="dialogs">Dialoge und Dateiauswahl.</param>
    /// <param name="images">Bildverarbeitung für Cover.</param>
    /// <param name="library">Programmweite Einstellungen des Bereichs.</param>
    public TagAreaViewModel(
        ITagStore store, Func<IReadOnlyList<FileEntry>> files, IDialogService dialogs, IClipboardService clipboard,
        ICoverImageProcessor images, TagLibrary library)
    {
        _clipboard = clipboard;
        _store = store;
        _files = files;
        Dialogs = dialogs;
        Images = images;
        Library = library;
        _writer = new TagWriter(store);
        _rowCallbacks = new TagRowCallbacks(_selectedPaths.Contains, _markedPaths.Contains, SetSelected, SetMarked);
        FunctionEntries = TagFunctionItem.CreateEntries();
        SelectedFunction = FunctionEntries.OfType<TagFunctionItem>().First();
    }

    /// <summary>
    /// Wird nach dem Speichern bzw. Widerrufen mit den geschriebenen Pfaden ausgelöst (Metadaten neu lesen, E126).
    /// </summary>
    public event EventHandler<IReadOnlyList<string>>? FilesWritten;

    /// <inheritdoc />
    public IDialogService Dialogs { get; }

    /// <inheritdoc />
    public ICoverImageProcessor Images { get; }

    /// <inheritdoc />
    public TagLibrary Library { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> AudioFiles => _audioFiles;

    /// <inheritdoc />
    public IReadOnlyList<string> SelectedFiles => _audioFiles.Where(_selectedPaths.Contains).ToList();

    /// <summary>
    /// Funktionsliste mit Gruppenüberschriften.
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<TagFunctionEntry> FunctionEntries { get; private set; }

    /// <summary>
    /// Gewählte Funktion.
    /// </summary>
    [ObservableProperty]
    public partial TagFunctionEntry? SelectedFunction { get; set; }

    /// <summary>
    /// Editor der gewählten Funktion.
    /// </summary>
    [ObservableProperty]
    public partial TagEditorViewModel? Editor { get; private set; }

    /// <summary>
    /// Vorgemerkte Änderungen in Anwendungsreihenfolge.
    /// </summary>
    public ObservableCollection<PendingChangeViewModel> PendingChanges { get; } = [];

    /// <summary>
    /// In der Liste gewählte Änderung (wählt ihre Dateien aus).
    /// </summary>
    [ObservableProperty]
    public partial PendingChangeViewModel? SelectedPending { get; set; }

    /// <summary>
    /// Zeilen der Tag-Vorschau.
    /// </summary>
    public ObservableCollection<TagPreviewRowViewModel> Rows { get; } = [];

    /// <summary>
    /// Spalten der Tag-Vorschau (E146); Standard wie bisher: feste Spalten für Datei, Track, Jahr, Cover und Lyrics,
    /// die übrigen teilen sich den Platz.
    /// </summary>
    public ColumnLayout Columns { get; } = new(
    [
        new("file", 190), new("track", 56), new("title", null, 60), new("artist", null, 60), new("album", null, 60),
        new("albumartist", null, 60), new("year", 56), new("cover", 52), new("lyrics", 48),
    ]);

    /// <summary>
    /// Ob in der Vorschau auch die vorherigen Werte angezeigt werden.
    /// </summary>
    [ObservableProperty]
    public partial bool ShowOldValues { get; set; }

    /// <summary>
    /// Ob der Bereich sichtbar ist (erst dann werden Tags gelesen).
    /// </summary>
    [ObservableProperty]
    public partial bool IsActive { get; set; }

    /// <summary>
    /// Zusammenfassung, z. B. „6 Dateien · 5 Audiodateien · 4 werden geändert · 1 Hinweis“.
    /// </summary>
    [ObservableProperty]
    public partial string StatusText { get; private set; } = string.Empty;

    /// <summary>
    /// Kennzeichnung neben der Überschrift: Name der ausgewählten Datei oder „n Dateien ausgewählt“.
    /// </summary>
    [ObservableProperty]
    public partial string? SelectedSummary { get; private set; }

    /// <summary>
    /// Ob gespeichert werden kann.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial bool CanSave { get; private set; }

    /// <inheritdoc />
    public AudioTags CurrentTags(string path) => _previews.TryGetValue(path, out var preview) ? preview.Result : DiskTags(path);

    /// <inheritdoc />
    public AudioTags DiskTags(string path) => _disk.TryGetValue(path, out var entry) && entry.Tags is { } tags ? tags : AudioTags.Empty;

    /// <inheritdoc />
    public void Stage(TagChange change)
    {
        PendingChanges.Add(new PendingChangeViewModel(change, RefreshPreview));
        RefreshPreview();
    }

    /// <summary>
    /// Übernimmt eine geänderte Dateiliste (hinzugefügt, entfernt, umbenannt).
    /// </summary>
    public void OnFilesChanged()
    {
        if (IsActive)
        {
            Reload();
        }
    }

    /// <summary>
    /// Baut Texte nach einem Sprachwechsel neu auf.
    /// </summary>
    public void OnLanguageChanged()
    {
        var function = (SelectedFunction as TagFunctionItem)?.Function;
        _editors.Clear();
        FunctionEntries = TagFunctionItem.CreateEntries();
        SelectedFunction = FunctionEntries.OfType<TagFunctionItem>().FirstOrDefault(item => item.Function == function)
            ?? FunctionEntries.OfType<TagFunctionItem>().First();
        var pending = PendingChanges.Select(item => (item.Change, item.IsEnabled)).ToList();
        PendingChanges.Clear();
        foreach (var (change, enabled) in pending)
        {
            PendingChanges.Add(new PendingChangeViewModel(change, RefreshPreview) { IsEnabled = enabled });
        }

        RefreshPreview();
    }

    /// <summary>
    /// Entfernt eine vorgemerkte Änderung (✕).
    /// </summary>
    /// <param name="item">Die Änderung.</param>
    [RelayCommand]
    private void RemovePending(PendingChangeViewModel item)
    {
        PendingChanges.Remove(item);
        RefreshPreview();
    }

    /// <summary>
    /// Liest die Tags neu, wenn der Bereich sichtbar wird.
    /// </summary>
    /// <param name="value">Ob sichtbar.</param>
    partial void OnIsActiveChanged(bool value)
    {
        if (value)
        {
            Reload();
        }
    }

    /// <summary>
    /// Zeigt den Editor der gewählten Funktion.
    /// </summary>
    /// <param name="value">Die Auswahl.</param>
    partial void OnSelectedFunctionChanged(TagFunctionEntry? value)
    {
        if (value is TagFunctionItem item)
        {
            Editor = GetEditor(item.Function);
            Editor.Refresh();
        }
    }

    /// <summary>
    /// Wählt die Dateien der gewählten Änderung aus und öffnet ihre Funktion.
    /// </summary>
    /// <param name="value">Die Auswahl.</param>
    partial void OnSelectedPendingChanged(PendingChangeViewModel? value)
    {
        if (value is not null)
        {
            ActivatePending(value);
        }
    }

    /// <summary>
    /// Wählt die Dateien einer vorgemerkten Änderung aus (Checkboxen) und öffnet ihre Funktion (A21, E150). Wird bei jedem Klick
    /// aufgerufen, auch wenn die Änderung schon ausgewählt ist.
    /// </summary>
    /// <param name="item">Die vorgemerkte Änderung.</param>
    [RelayCommand]
    private void ActivatePending(PendingChangeViewModel item)
    {
        _selectedPaths.Clear();
        _selectedPaths.UnionWith(item.Change.Files);
        SelectedFunction = FunctionEntries.OfType<TagFunctionItem>().First(entry => entry.Function == item.Function);
        RefreshPreview();
    }

    /// <summary>
    /// Liefert den Editor einer Funktion; Editoren bleiben erhalten, damit Einstellungen beim Wechsel nicht verloren gehen.
    /// </summary>
    /// <param name="function">Die Funktion.</param>
    /// <returns>Der Editor.</returns>
    private TagEditorViewModel GetEditor(TagFunction function)
    {
        if (!_editors.TryGetValue(function, out var editor))
        {
            editor = function switch
            {
                TagFunction.FromFileName => new FromFileNameEditorViewModel(this),
                TagFunction.TrackNumbers => new TrackNumbersEditorViewModel(this),
                TagFunction.Cover => new CoverEditorViewModel(this),
                TagFunction.Lyrics => new LyricsEditorViewModel(this),
                _ => new EditTagsEditorViewModel(this),
            };
            _editors[function] = editor;
        }

        return editor;
    }

    /// <summary>
    /// Liest die Tags der Audiodateien (zwischengespeichert nach Änderungszeit) und berechnet die Vorschau.
    /// </summary>
    private void Reload()
    {
        var files = _files();
        foreach (var file in files.Where(file => _store.IsSupported(file.FullPath)))
        {
            ReadIfChanged(file);
        }

        _audioFiles = files.Select(file => file.FullPath).Where(path => _disk.TryGetValue(path, out var entry) && entry.Tags is not null).ToList();
        _selectedPaths.IntersectWith(_audioFiles);
        _markedPaths.IntersectWith(_audioFiles);
        if (_selectedPaths.Count == 0 && _audioFiles.Count > 0)
        {
            _selectedPaths.Add(_audioFiles[0]);
        }

        RefreshPreview();
    }

    /// <summary>
    /// Liest die Tags einer Datei, sofern sie sich seit dem letzten Lesen geändert hat.
    /// </summary>
    /// <param name="file">Die Datei.</param>
    private void ReadIfChanged(FileEntry file)
    {
        if (_disk.TryGetValue(file.FullPath, out var entry) && entry.Stamp == file.LastWriteTime)
        {
            return;
        }

        try
        {
            _disk[file.FullPath] = (file.LastWriteTime, _store.Read(file.FullPath, Library.CustomFieldNames), null);
        }
        catch (TagStoreException exception)
        {
            _disk[file.FullPath] = (file.LastWriteTime, null, exception.Message);
        }
    }

    /// <summary>
    /// Berechnet die Vorschau aus den aktiven Änderungen und baut Zeilen, Editor und Status neu auf.
    /// </summary>
    private void RefreshPreview()
    {
        var active = PendingChanges.Where(item => item.IsEnabled).Select(item => item.Change).ToList();
        var previews = TagPreviewPipeline.Create(_audioFiles.Select(path => (path, DiskTags(path))).ToList(), active);
        _previews.Clear();
        foreach (var preview in previews)
        {
            _previews[preview.Path] = preview;
        }

        RebuildRows(PendingChanges.ToDictionary(item => item.Change.Id, item => item.Function));
        Editor?.Refresh();
        UpdateStatus();
    }

    /// <summary>
    /// Baut die Zeilen in der Reihenfolge der Dateiliste auf.
    /// </summary>
    /// <param name="functions">Id der Änderung → Funktion.</param>
    private void RebuildRows(IReadOnlyDictionary<Guid, TagFunction> functions)
    {
        Rows.Clear();
        foreach (var file in _files())
        {
            Rows.Add(_previews.TryGetValue(file.FullPath, out var preview)
                ? new TagPreviewRowViewModel(preview, functions, _rowCallbacks)
                : new TagPreviewRowViewModel(file.FullPath, ReadError(file.FullPath)));
        }
    }

    /// <summary>
    /// Liefert den Lesefehler einer Datei oder den Hinweis „keine Audiodatei“.
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <returns>Der Text.</returns>
    private string ReadError(string path) =>
        _disk.TryGetValue(path, out var entry) && entry.Error is { } error ? error : Loc.T("Tags.NotAudio");

    /// <summary>
    /// Aktualisiert Editor und Status nach einer Änderung der Auswahl.
    /// </summary>
    private void OnSelectionChanged()
    {
        Editor?.Refresh();
        UpdateStatus();
    }

    /// <summary>
    /// Aktualisiert Status und Speicherbarkeit.
    /// </summary>
    private void UpdateStatus()
    {
        var changed = _previews.Values.Count(preview => preview.IsChanged);
        var hints = _previews.Values.Count(preview => preview.Hints.Count > 0);
        var text = Loc.F("Tags.Status", _files().Count, _audioFiles.Count, changed, _selectedPaths.Count);
        StatusText = hints > 0 ? text + " · " + Loc.F("Tags.StatusHints", hints) : text;
        var selected = SelectedFiles;
        SelectedSummary = selected.Count switch
        {
            0 => null,
            1 => Path.GetFileName(selected[0]),
            _ => Loc.F("Tags.SelectedCount", selected.Count),
        };
        UpdateAllSelected(selected.Count);
        CanSave = changed > 0;
        DiscardCommand.NotifyCanExecuteChanged();
        CopyRowsCommand.NotifyCanExecuteChanged();
    }
}
