using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileRenamer.App.Localization;
using FileRenamer.Core.Tagging;
using FileRenamer.Core.Tagging.Changes;

namespace FileRenamer.App.ViewModels.Tags;

/// <summary>
/// Sprache der Lyrics zur Auswahl (dreistelliger Code nach ISO 639-2).
/// </summary>
/// <param name="Code">Der Code.</param>
public sealed record LyricsLanguageOption(string Code)
{
    /// <summary>
    /// Angezeigter Name, z. B. „Englisch (eng)“.
    /// </summary>
    public string DisplayName => $"{Loc.T("Language." + Code)} ({Code})";
}

/// <summary>
/// Editor „Lyrics“: Text der einzigen markierten Datei bearbeiten, Lyrics aus Begleitdateien importieren und als
/// <c>.lrc</c> oder <c>.txt</c> exportieren (E118, E123). Exportiert werden die Lyrics, wie sie in den Dateien stehen.
/// </summary>
public sealed partial class LyricsEditorViewModel : TagEditorViewModel
{
    private string _loadedText = string.Empty;

    /// <summary>
    /// Erzeugt den Editor.
    /// </summary>
    /// <param name="context">Der Bereich „Tags“.</param>
    public LyricsEditorViewModel(ITagEditorContext context)
        : base(TagFunction.Lyrics, context) => Refresh();

    /// <summary>
    /// Wählbare Sprachen.
    /// </summary>
    public IReadOnlyList<LyricsLanguageOption> Languages { get; } =
        new[] { "eng", "deu", "fra", "spa", "ita", "nld", "por", AudioTags.UnknownLanguage }.Select(code => new LyricsLanguageOption(code)).ToList();

    /// <summary>
    /// Ob genau eine Datei markiert ist (nur dann ist der Text bearbeitbar).
    /// </summary>
    [ObservableProperty]
    public partial bool IsSingle { get; set; }

    /// <summary>
    /// Name der markierten Datei oder Anzahl.
    /// </summary>
    [ObservableProperty]
    public partial string? FocusText { get; set; }

    /// <summary>
    /// Die Lyrics der markierten Datei.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StageCommand))]
    public partial string Text { get; set; } = string.Empty;

    /// <summary>
    /// Die Sprache.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StageCommand))]
    public partial LyricsLanguageOption? Language { get; set; }

    /// <summary>
    /// Ob aus einer gewählten Datei importiert wird (sonst Datei gleichen Namens).
    /// </summary>
    [ObservableProperty]
    public partial bool ImportFromChosenFile { get; set; }

    /// <summary>
    /// Format beim Export.
    /// </summary>
    [ObservableProperty]
    public partial LyricsFormat ExportFormat { get; set; }

    /// <summary>
    /// Ob in einen gewählten Ordner exportiert wird.
    /// </summary>
    [ObservableProperty]
    public partial bool ExportToChosenFolder { get; set; }

    /// <summary>
    /// Ob vorhandene Dateien überschrieben werden.
    /// </summary>
    [ObservableProperty]
    public partial bool Overwrite { get; set; }

    /// <summary>
    /// Beispiel für den Import.
    /// </summary>
    [ObservableProperty]
    public partial string? ImportExample { get; set; }

    /// <summary>
    /// Vorgemerkt wird immer für die markierten Dateien.
    /// </summary>
    protected override IReadOnlyList<string> StageFiles => Context.SelectedFiles;

    /// <summary>
    /// Lädt die Lyrics der markierten Datei (nach den vorgemerkten Änderungen).
    /// </summary>
    public override void Refresh()
    {
        var marked = Context.SelectedFiles;
        IsSingle = marked.Count == 1;
        FocusText = marked.Count switch
        {
            0 => null,
            1 => Path.GetFileName(marked[0]),
            _ => Loc.F("Tags.SelectedCount", marked.Count),
        };
        var tags = IsSingle ? Context.CurrentTags(marked[0]) : AudioTags.Empty;
        _loadedText = tags.Lyrics;
        Text = tags.Lyrics;
        Language = Languages.FirstOrDefault(option => option.Code == tags.LyricsLanguage) ?? Languages[0];
        ImportExample = CreateImportExample();
        ImportCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
        base.Refresh();
    }

    /// <summary>
    /// Importiert Lyrics für die Dateien des Geltungsbereichs.
    /// </summary>
    /// <returns>Ein Task.</returns>
    [RelayCommand(CanExecute = nameof(HasScopeFiles))]
    private async Task ImportAsync()
    {
        var lyrics = ImportFromChosenFile ? await ImportChosenAsync() : ImportSidecars();
        if (lyrics is not null)
        {
            Context.Stage(new SetLyricsChange(lyrics, Language?.Code ?? AudioTags.UnknownLanguage) { Files = ScopeFiles });
        }
    }

    /// <summary>
    /// Exportiert die Lyrics der Dateien des Geltungsbereichs und meldet das Ergebnis.
    /// </summary>
    /// <returns>Ein Task.</returns>
    [RelayCommand(CanExecute = nameof(HasScopeFiles))]
    private async Task ExportAsync()
    {
        var folder = ExportToChosenFolder ? await Context.Dialogs.PickFolderAsync() : null;
        if (ExportToChosenFolder && folder is null)
        {
            return;
        }

        var result = LyricsFiles.Export(ScopeFiles.Select(path => (path, Context.DiskTags(path))), ExportFormat, folder, Overwrite);
        await Context.Dialogs.ShowMessageAsync(Loc.T("Lyrics.ExportTitle"), TagAreaViewModel.DescribeExport(result));
    }

    /// <summary>
    /// Ob Dateien im Geltungsbereich liegen.
    /// </summary>
    /// <returns><c>true</c> mit Dateien.</returns>
    private bool HasScopeFiles() => ScopeFiles.Count > 0;

    /// <summary>
    /// Vormerken des Textes nur bei genau einer markierten Datei und geändertem Text oder geänderter Sprache.
    /// </summary>
    /// <returns><c>true</c>, wenn vorgemerkt werden kann.</returns>
    protected override bool CanStage() =>
        IsSingle && (Text != _loadedText || Language?.Code != Context.CurrentTags(Context.SelectedFiles[0]).LyricsLanguage);

    /// <summary>
    /// Erzeugt die Änderung aus dem Textfeld.
    /// </summary>
    /// <param name="files">Die markierte Datei.</param>
    /// <returns>Die Änderung.</returns>
    protected override TagChange CreateChange(IReadOnlyList<string> files) =>
        new SetLyricsChange(new Dictionary<string, string> { [files[0]] = Text.Trim() }, Language?.Code ?? AudioTags.UnknownLanguage);

    /// <summary>Quelle geändert.</summary>
    /// <param name="value">Der Wert.</param>
    partial void OnImportFromChosenFileChanged(bool value) => ImportExample = CreateImportExample();

    /// <summary>
    /// Liest die Begleitdateien gleichen Namens.
    /// </summary>
    /// <returns>Pfad der Audiodatei → Lyrics.</returns>
    private Dictionary<string, string> ImportSidecars() =>
        ScopeFiles.Select(path => (path, sidecar: LyricsFiles.FindSidecar(path)))
            .Where(pair => pair.sidecar is not null)
            .ToDictionary(pair => pair.path, pair => LyricsFiles.Read(pair.sidecar!));

    /// <summary>
    /// Liest eine gewählte Datei für alle Dateien des Geltungsbereichs.
    /// </summary>
    /// <returns>Pfad der Audiodatei → Lyrics oder <c>null</c> bei Abbruch.</returns>
    private async Task<Dictionary<string, string>?> ImportChosenAsync()
    {
        var picked = await Context.Dialogs.PickFileAsync(Loc.T("Lyrics.PickTitle"), Loc.T("Lyrics.Filter"), LyricsFiles.Extensions);
        if (picked is null)
        {
            return null;
        }

        var text = LyricsFiles.Read(picked);
        return ScopeFiles.ToDictionary(path => path, _ => text);
    }

    /// <summary>
    /// Beispiel „01 Like A Prayer.lrc → 01 Like A Prayer.mp3“.
    /// </summary>
    /// <returns>Das Beispiel oder <c>null</c>.</returns>
    private string? CreateImportExample()
    {
        var first = ScopeFiles.FirstOrDefault();
        return first is null || ImportFromChosenFile
            ? null
            : $"{Path.GetFileNameWithoutExtension(first)}.lrc → {Path.GetFileName(first)}";
    }
}
