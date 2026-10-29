using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileRenamer.App.Services;
using FileRenamer.App.ViewModels.Editors;
using FileRenamer.Core.Execution;
using FileRenamer.Core.Files;
using FileRenamer.Core.Preview;
using FileRenamer.Core.Profiles;

namespace FileRenamer.App.ViewModels;

/// <summary>
/// ViewModel des Hauptfensters: Verfahrensauswahl und Editor (oben), Kette und Vorschau (unten).
/// Dateiverwaltung und Profile sind in eigenen Teildateien untergebracht.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IFileSystem _fileSystem;
    private readonly RenamePipeline _pipeline;
    private readonly FileRenameExecutor _executor;
    private readonly IDialogService _dialogs;
    private readonly List<FileEntry> _files = [];
    private PreviewResult _preview = PreviewResult.Empty;
    private RenameExecutionResult? _lastRename;

    /// <summary>
    /// Erzeugt das ViewModel und stellt den zuletzt gespeicherten Zustand (Theme, Profil) wieder her.
    /// </summary>
    /// <param name="fileSystem">Dateisystem.</param>
    /// <param name="profileStore">Profilablage.</param>
    /// <param name="settingsStore">Einstellungsablage.</param>
    /// <param name="dialogs">Dialogdienst.</param>
    /// <param name="themeService">Theme-Dienst.</param>
    public MainWindowViewModel(
        IFileSystem fileSystem, ProfileStore profileStore, AppSettingsStore settingsStore, IDialogService dialogs, IThemeService themeService)
    {
        _fileSystem = fileSystem;
        _pipeline = new RenamePipeline(fileSystem);
        _executor = new FileRenameExecutor(fileSystem);
        _profileStore = profileStore;
        _settingsStore = settingsStore;
        _dialogs = dialogs;
        _themeService = themeService;

        Chain.CollectionChanged += OnChainChanged;
        SelectedRuleKind = RuleKinds[0];
        RestoreSettings();
    }

    /// <summary>
    /// Die wählbaren Verfahren.
    /// </summary>
    public IReadOnlyList<RuleKindItem> RuleKinds => RuleCatalog.All;

    /// <summary>
    /// Die Kette der angewandten Verfahren.
    /// </summary>
    public ObservableCollection<ChainItemViewModel> Chain { get; } = [];

    /// <summary>
    /// Die Zeilen der Vorschau.
    /// </summary>
    public ObservableCollection<PreviewRowViewModel> PreviewRows { get; } = [];

    /// <summary>
    /// In der Verfahrensliste gewähltes Verfahren (Editor für ein neues Verfahren).
    /// </summary>
    [ObservableProperty]
    public partial RuleKindItem? SelectedRuleKind { get; set; }

    /// <summary>
    /// In der Kette gewählter Eintrag (Editor bearbeitet diesen Eintrag, E37).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingChainItem))]
    public partial ChainItemViewModel? SelectedChainItem { get; set; }

    /// <summary>
    /// Der angezeigte Options-Editor.
    /// </summary>
    [ObservableProperty]
    public partial RuleEditorViewModel? Editor { get; set; }

    /// <summary>
    /// Ob der Editor einen Ketteneintrag bearbeitet (Änderungen wirken sofort).
    /// </summary>
    public bool IsEditingChainItem => SelectedChainItem is not null;

    /// <summary>
    /// Ob die (letzte) Dateierweiterung ignoriert und ausgeblendet wird.
    /// </summary>
    [ObservableProperty]
    public partial bool IgnoreExtension { get; set; } = true;

    /// <summary>
    /// Zusammenfassung der Vorschau (Dateien, Hinweise).
    /// </summary>
    [ObservableProperty]
    public partial string StatusText { get; set; } = string.Empty;

    /// <summary>
    /// Meldung, warum das Umbenennen gesperrt ist, oder leer.
    /// </summary>
    [ObservableProperty]
    public partial string BlockingText { get; set; } = string.Empty;

    /// <summary>
    /// Ob umbenannt werden kann.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RenameCommand))]
    public partial bool CanRename { get; set; }

    /// <summary>
    /// Ob eine ausgeführte Umbenennung widerrufen werden kann.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UndoRenameCommand))]
    public partial bool CanUndoRename { get; set; }

    /// <summary>
    /// Ob Dateien geladen sind (steuert den Ablagehinweis).
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ClearFilesCommand))]
    public partial bool HasFiles { get; set; }

    /// <summary>
    /// Fügt das Verfahren aus dem Editor als neuen Eintrag am Ende der Kette hinzu.
    /// </summary>
    [RelayCommand]
    private void AddToChain()
    {
        if (Editor is null)
        {
            return;
        }

        var item = new ChainItemViewModel(Editor.CreateRule());
        Chain.Add(item);
        SelectedChainItem = item;
    }

    /// <summary>
    /// Setzt die Einstellungen im Editor auf Standardwerte.
    /// </summary>
    [RelayCommand]
    private void ResetEditor() => Editor?.Reset();

    /// <summary>
    /// Entfernt einen Eintrag aus der Kette.
    /// </summary>
    /// <param name="item">Der Eintrag.</param>
    [RelayCommand]
    private void RemoveChainItem(ChainItemViewModel item)
    {
        if (SelectedChainItem == item)
        {
            SelectedRuleKind = RuleKinds.First(kind => kind.Kind == item.Kind);
        }

        Chain.Remove(item);
    }

    /// <summary>
    /// Verwirft alle Änderungen der Vorschau, indem die Kette geleert wird.
    /// </summary>
    [RelayCommand]
    private void Discard() => ClearChain();

    /// <summary>
    /// Entfernt alle Einträge der Kette einzeln, damit ihre Ereignisbindungen gelöst werden.
    /// </summary>
    private void ClearChain()
    {
        SelectedChainItem = null;
        SelectedRuleKind ??= RuleKinds[0];
        while (Chain.Count > 0)
        {
            Chain.RemoveAt(Chain.Count - 1);
        }
    }

    /// <summary>
    /// Benennt nach Sicherheitsabfrage alle geänderten Dateien um (alles oder nichts).
    /// </summary>
    /// <returns>Ein Task.</returns>
    [RelayCommand(CanExecute = nameof(CanRename))]
    private async Task RenameAsync()
    {
        var preview = _preview;
        var confirmed = await _dialogs.ConfirmAsync(
            "Umbenennen bestätigen",
            $"{preview.ChangedCount} Datei(en) werden umbenannt.\n\nTritt ein Fehler auf, werden alle bereits umbenannten Dateien zurückbenannt.",
            "Umbenennen");
        if (!confirmed)
        {
            return;
        }

        var result = _executor.Execute(preview.CreateOperations());
        if (result.Success)
        {
            ApplyPathChanges(result.ExecutedOperations);
            _lastRename = result;
            CanUndoRename = true;
        }

        RefreshPreview();
        await _dialogs.ShowMessageAsync("Umbenennen", DescribeResult(result, "umbenannt"));
    }

    /// <summary>
    /// Widerruft nach Sicherheitsabfrage die zuletzt ausgeführte Umbenennung (E49, alles oder nichts).
    /// </summary>
    /// <returns>Ein Task.</returns>
    [RelayCommand(CanExecute = nameof(CanUndoRename))]
    private async Task UndoRenameAsync()
    {
        if (_lastRename is not { } lastRename)
        {
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync(
            "Letzte Umbenennung widerrufen",
            $"{lastRename.RenamedCount} Datei(en) erhalten wieder ihre vorherigen Namen.",
            "Widerrufen");
        if (!confirmed)
        {
            return;
        }

        var result = _executor.Execute(lastRename.CreateUndoOperations());
        if (result.Success)
        {
            ApplyPathChanges(result.ExecutedOperations);
            _lastRename = null;
            CanUndoRename = false;
        }

        RefreshPreview();
        await _dialogs.ShowMessageAsync("Widerrufen", DescribeResult(result, "zurückbenannt"));
    }

    /// <summary>
    /// Verschiebt einen Ketteneintrag (Drag &amp; Drop, E13).
    /// </summary>
    /// <param name="fromIndex">Bisherige Position.</param>
    /// <param name="toIndex">Neue Position.</param>
    public void MoveChainItem(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex || fromIndex < 0 || toIndex < 0 || fromIndex >= Chain.Count || toIndex >= Chain.Count)
        {
            return;
        }

        Chain.Move(fromIndex, toIndex);
    }

    /// <summary>
    /// Öffnet einen neuen Editor, wenn ein Verfahren in der Liste gewählt wird.
    /// </summary>
    /// <param name="value">Das gewählte Verfahren.</param>
    partial void OnSelectedRuleKindChanged(RuleKindItem? value)
    {
        if (value is null)
        {
            return;
        }

        SelectedChainItem = null;
        SetEditor(RuleCatalog.CreateEditor(value.Kind));
        RefreshPreview();
    }

    /// <summary>
    /// Lädt den gewählten Ketteneintrag in den Editor und aktualisiert die Hervorhebung (E5).
    /// </summary>
    /// <param name="value">Der gewählte Eintrag.</param>
    partial void OnSelectedChainItemChanged(ChainItemViewModel? value)
    {
        if (value is not null)
        {
            SelectedRuleKind = null;
            SetEditor(RuleCatalog.CreateEditorFor(value.Rule));
        }

        RefreshPreview();
    }

    /// <summary>
    /// Aktualisiert die Vorschau, wenn die Erweiterungseinstellung wechselt.
    /// </summary>
    /// <param name="value">Neuer Wert.</param>
    partial void OnIgnoreExtensionChanged(bool value) => RefreshPreview();

    /// <summary>
    /// Wechselt den Editor und verbindet dessen Änderungsereignis.
    /// </summary>
    /// <param name="editor">Der neue Editor.</param>
    private void SetEditor(RuleEditorViewModel editor)
    {
        if (Editor is not null)
        {
            Editor.SettingsChanged -= OnEditorSettingsChanged;
        }

        Editor = editor;
        Editor.SettingsChanged += OnEditorSettingsChanged;
    }

    /// <summary>
    /// Überträgt Editor-Änderungen sofort in den bearbeiteten Ketteneintrag.
    /// </summary>
    /// <param name="sender">Der Editor.</param>
    /// <param name="e">Leer.</param>
    private void OnEditorSettingsChanged(object? sender, EventArgs e)
    {
        if (SelectedChainItem is { } item && sender is RuleEditorViewModel editor)
        {
            item.Rule = editor.ApplyTo(item.Rule);
        }
    }

    /// <summary>
    /// Verbindet neue Ketteneinträge und aktualisiert die Vorschau bei jeder Änderung der Kette.
    /// </summary>
    /// <param name="sender">Die Kette.</param>
    /// <param name="e">Die Änderung.</param>
    private void OnChainChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var item in e.NewItems?.OfType<ChainItemViewModel>() ?? [])
        {
            item.PropertyChanged += OnChainItemPropertyChanged;
        }

        foreach (var item in e.OldItems?.OfType<ChainItemViewModel>() ?? [])
        {
            item.PropertyChanged -= OnChainItemPropertyChanged;
        }

        RefreshPreview();
    }

    /// <summary>
    /// Aktualisiert die Vorschau, wenn sich ein Verfahren (Parameter oder Checkbox) ändert.
    /// </summary>
    /// <param name="sender">Der Eintrag.</param>
    /// <param name="e">Die Änderung.</param>
    private void OnChainItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChainItemViewModel.Rule))
        {
            RefreshPreview();
        }
    }

    /// <summary>
    /// Berechnet die Vorschau neu und aktualisiert Zeilen, Fehler der Kette, Editor-Grenzen und Status.
    /// </summary>
    private void RefreshPreview()
    {
        var rules = Chain.Select(item => item.Rule).ToList();
        var selection = _files.Select(file => !_deselectedPaths.Contains(file.FullPath)).ToList();
        _preview = _pipeline.CreatePreview(_files, rules, IgnoreExtension, selection);

        var kindsById = rules.ToDictionary(rule => rule.Id, rule => rule.Kind);
        var preferred = SelectedChainItem?.Rule.Id;
        PreviewRows.Clear();
        foreach (var file in _preview.Files)
        {
            PreviewRows.Add(new PreviewRowViewModel(file, kindsById, preferred, SetFileSelected));
        }

        foreach (var item in Chain)
        {
            item.Error = _preview.FindStep(item.Rule.Id)?.Error;
        }

        UpdateEditorContext();
        UpdateStatus();
    }

    /// <summary>
    /// Gibt dem Editor die Namen an seiner Stelle der Kette (E41); bei neuen Verfahren die Namen am Kettenende.
    /// </summary>
    private void UpdateEditorContext()
    {
        var names = SelectedChainItem is { } item
            ? _preview.FindStep(item.Rule.Id)?.InputNames
            : _preview.Files.Select(file => file.NewDisplayName.Text).ToList();
        Editor?.UpdateContext(names ?? []);
    }

    /// <summary>
    /// Aktualisiert Statuszeile und Freigabe der Umbenennen-Schaltfläche.
    /// </summary>
    private void UpdateStatus()
    {
        HasFiles = _files.Count > 0;
        var selectedCount = _preview.Files.Count(file => file.IsSelected);
        StatusText = $"{_files.Count} Dateien · {selectedCount} ausgewählt · {_preview.ChangedCount} geändert · {_preview.HintCount} Hinweise";
        UpdateAllSelected(selectedCount);
        BlockingText = _preview.HasBlockingIssues
            ? $"· {_preview.ConflictCount} Konflikt(e) – Umbenennen gesperrt"
            : string.Empty;
        CanRename = _preview.CanRename;
    }

    /// <summary>
    /// Ersetzt die Dateieinträge, deren Pfad sich durch ausgeführte Aufträge geändert hat.
    /// </summary>
    /// <param name="operations">Die ausgeführten Aufträge.</param>
    private void ApplyPathChanges(IReadOnlyList<RenameOperation> operations)
    {
        var targets = operations.ToDictionary(operation => operation.SourcePath, operation => operation.TargetPath, StringComparer.Ordinal);
        for (var index = 0; index < _files.Count; index++)
        {
            if (targets.TryGetValue(_files[index].FullPath, out var target))
            {
                _files[index] = _fileSystem.ReadFileEntry(target);
            }
        }
    }

    /// <summary>
    /// Formuliert die Ergebnismeldung einer Umbenennung oder eines Widerrufs.
    /// </summary>
    /// <param name="result">Das Ergebnis.</param>
    /// <param name="verb">Partizip für die Erfolgsmeldung, z. B. „umbenannt“.</param>
    /// <returns>Der Meldungstext.</returns>
    private static string DescribeResult(RenameExecutionResult result, string verb)
    {
        if (result.Success)
        {
            return $"{result.RenamedCount} Datei(en) wurden {verb}.";
        }

        var message = $"Der Vorgang ist fehlgeschlagen: {result.ErrorMessage}\n\nAlle Dateien wurden auf ihre vorherigen Namen zurückgesetzt.";
        return result.RollbackErrors.Count == 0
            ? message
            : message + "\n\nAchtung, nicht alle Dateien konnten zurückbenannt werden:\n" + string.Join("\n", result.RollbackErrors);
    }
}
