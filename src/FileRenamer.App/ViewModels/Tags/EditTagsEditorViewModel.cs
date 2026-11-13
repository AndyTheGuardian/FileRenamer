using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileRenamer.Core.Localization;
using FileRenamer.Core.Tagging;
using FileRenamer.Core.Tagging.Changes;

namespace FileRenamer.App.ViewModels.Tags;

/// <summary>
/// Eintrag der Genre-Liste: eigenes Genre (mit ✕), Trennlinie oder Standardgenre.
/// </summary>
public abstract record GenreEntry
{
    /// <summary>
    /// Ob der Eintrag gewählt werden kann.
    /// </summary>
    public virtual bool IsSelectable => true;
}

/// <summary>
/// Ein Genre der Liste.
/// </summary>
/// <param name="Name">Das Genre.</param>
/// <param name="IsCustom">Ob es ein eigenes Genre ist (entfernbar).</param>
public sealed record GenreOption(string Name, bool IsCustom) : GenreEntry
{
    /// <summary>
    /// Text, den die editierbare Auswahl beim Wählen übernimmt.
    /// </summary>
    /// <returns>Das Genre.</returns>
    public override string ToString() => Name;
}

/// <summary>
/// Trennlinie zwischen eigenen und Standardgenres.
/// </summary>
public sealed record GenreSeparator : GenreEntry
{
    /// <summary>
    /// Die Trennlinie ist nicht wählbar.
    /// </summary>
    public override bool IsSelectable => false;

    /// <summary>
    /// Die Trennlinie hat keinen Text.
    /// </summary>
    /// <returns>Leerer Text.</returns>
    public override string ToString() => string.Empty;
}

/// <summary>
/// Editor „Tags bearbeiten“: Formular mit Standardfeldern und hinzugefügten Feldern für die markierten Dateien
/// (Mehrfachbearbeitung, E116), Genre-Liste mit eigenen Genres (E134) und „Feld hinzufügen“ (E125, E131).
/// </summary>
public sealed partial class EditTagsEditorViewModel : TagEditorViewModel
{
    private readonly Dictionary<string, TagFieldViewModel> _standard;

    /// <summary>
    /// Erzeugt das Formular.
    /// </summary>
    /// <param name="context">Der Bereich „Tags“.</param>
    public EditTagsEditorViewModel(ITagEditorContext context)
        : base(TagFunction.Edit, context)
    {
        _standard = TagFields.Standard.ToDictionary(key => key, key => CreateField(key, isRemovable: false));
        Refresh();
    }

    /// <summary>Titel.</summary>
    public TagFieldViewModel TitleField => _standard[TagFields.Title];

    /// <summary>Interpret.</summary>
    public TagFieldViewModel ArtistField => _standard[TagFields.Artist];

    /// <summary>Album.</summary>
    public TagFieldViewModel AlbumField => _standard[TagFields.Album];

    /// <summary>Albuminterpret.</summary>
    public TagFieldViewModel AlbumArtistField => _standard[TagFields.AlbumArtist];

    /// <summary>Komponist.</summary>
    public TagFieldViewModel ComposerField => _standard[TagFields.Composer];

    /// <summary>Genre.</summary>
    public TagFieldViewModel GenreField => _standard[TagFields.Genre];

    /// <summary>Jahr.</summary>
    public TagFieldViewModel YearField => _standard[TagFields.Year];

    /// <summary>Tracknummer.</summary>
    public TagFieldViewModel TrackField => _standard[TagFields.Track];

    /// <summary>Anzahl Tracks.</summary>
    public TagFieldViewModel TrackCountField => _standard[TagFields.TrackCount];

    /// <summary>Discnummer.</summary>
    public TagFieldViewModel DiscField => _standard[TagFields.Disc];

    /// <summary>Anzahl Discs.</summary>
    public TagFieldViewModel DiscCountField => _standard[TagFields.DiscCount];

    /// <summary>Kommentar.</summary>
    public TagFieldViewModel CommentField => _standard[TagFields.Comment];

    /// <summary>
    /// Hinzugefügte und in den markierten Dateien vorhandene weitere Felder.
    /// </summary>
    public ObservableCollection<TagFieldViewModel> ExtraFields { get; } = [];

    /// <summary>
    /// Einträge der Genre-Liste.
    /// </summary>
    public ObservableCollection<GenreEntry> GenreEntries { get; } = [];

    /// <summary>
    /// Vorschläge für „Feld hinzufügen“ (gängige Felder, die noch nicht angezeigt werden).
    /// </summary>
    public ObservableCollection<string> FieldSuggestions { get; } = [];

    /// <summary>
    /// Eingabe für „Feld hinzufügen“.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddFieldCommand))]
    public partial string? NewFieldInput { get; set; }

    /// <summary>
    /// Vorgemerkt wird immer für die markierten Dateien.
    /// </summary>
    protected override IReadOnlyList<string> StageFiles => Context.SelectedFiles;

    /// <summary>
    /// Anzahl der markierten Dateien.
    /// </summary>
    public int SelectedCount => Context.SelectedFiles.Count;

    /// <summary>
    /// Lädt die Werte der markierten Dateien (geänderte Felder behalten ihre Eingabe).
    /// </summary>
    public override void Refresh()
    {
        var tags = Context.SelectedFiles.Select(Context.CurrentTags).ToList();
        SyncExtraFields(tags);
        foreach (var field in _standard.Values.Concat(ExtraFields))
        {
            field.Load(tags.Select(tag => tag.Get(field.Key)).ToList());
        }

        RebuildGenres();
        RebuildSuggestions();
        OnPropertyChanged(nameof(SelectedCount));
        Validate();
        base.Refresh();
    }

    /// <summary>
    /// Übernimmt ein Genre aus der Liste.
    /// </summary>
    /// <param name="option">Das Genre.</param>
    [RelayCommand]
    private void ApplyGenre(GenreOption option) => GenreField.Value = option.Name;

    /// <summary>
    /// Entfernt ein eigenes Genre aus der Liste.
    /// </summary>
    /// <param name="option">Das Genre.</param>
    [RelayCommand]
    private void RemoveGenre(GenreOption option)
    {
        Context.Library.RemoveGenre(option.Name);
        RebuildGenres();
    }

    /// <summary>
    /// Nimmt ein Feld dauerhaft ins Formular auf (E131).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddField))]
    private void AddField()
    {
        if (TagFieldNames.ToKey(NewFieldInput) is { } key && !TagFields.Standard.Contains(key))
        {
            Context.Library.AddField(key);
            NewFieldInput = null;
            Refresh();
        }
    }

    /// <summary>
    /// Ob „Feld hinzufügen“ möglich ist.
    /// </summary>
    /// <returns><c>true</c> bei nicht leerer Eingabe.</returns>
    private bool CanAddField() => !string.IsNullOrWhiteSpace(NewFieldInput);

    /// <summary>
    /// Blendet ein hinzugefügtes Feld aus (✕); der Tag bleibt in den Dateien.
    /// </summary>
    /// <param name="field">Das Feld.</param>
    [RelayCommand]
    private void RemoveField(TagFieldViewModel field)
    {
        Context.Library.RemoveField(field.Key);
        ExtraFields.Remove(field);
        Refresh();
    }

    /// <summary>
    /// Nimmt die Eingabe eines Felds zurück (↺).
    /// </summary>
    /// <param name="field">Das Feld.</param>
    [RelayCommand]
    private void RevertField(TagFieldViewModel field) => field.Revert();

    /// <summary>
    /// Nimmt alle Eingaben zurück („Felder zurücksetzen“).
    /// </summary>
    [RelayCommand]
    private void RevertAll()
    {
        foreach (var field in AllFields().Where(field => field.IsDirty))
        {
            field.Revert();
        }
    }

    /// <summary>
    /// Vormerken nur mit mindestens einem geänderten, gültigen Feld.
    /// </summary>
    /// <returns><c>true</c>, wenn vorgemerkt werden kann.</returns>
    protected override bool CanStage() => base.CanStage() && AllFields().Any(field => field.IsDirty);

    /// <summary>
    /// Erzeugt die Änderung aus den geänderten Feldern.
    /// </summary>
    /// <param name="files">Die markierten Dateien.</param>
    /// <returns>Die Änderung.</returns>
    protected override TagChange? CreateChange(IReadOnlyList<string> files)
    {
        var values = AllFields().Where(field => field.IsDirty).ToDictionary(field => field.Key, field => field.Value.Trim());
        return values.Count == 0 ? null : new SetFieldsChange(values);
    }

    /// <summary>
    /// Nimmt ein neues Genre in die Liste auf und lädt die neuen Werte.
    /// </summary>
    protected override void OnStaged()
    {
        if (GenreField.IsDirty && GenreField.Value.Trim().Length > 0)
        {
            Context.Library.AddGenre(GenreField.Value.Trim());
        }

        foreach (var field in AllFields())
        {
            field.IsDirty = false;
        }

        Refresh();
    }

    /// <summary>
    /// Erzeugt ein Feld und verfolgt seine Eingaben.
    /// </summary>
    /// <param name="key">Der Schlüssel.</param>
    /// <param name="isRemovable">Ob es ausgeblendet werden kann.</param>
    /// <returns>Das Feld.</returns>
    private TagFieldViewModel CreateField(string key, bool isRemovable)
    {
        var field = new TagFieldViewModel(key, isRemovable);
        field.Edited += (_, _) => Validate();
        return field;
    }

    /// <summary>
    /// Alle Felder des Formulars.
    /// </summary>
    /// <returns>Standard- und weitere Felder.</returns>
    private IEnumerable<TagFieldViewModel> AllFields() => _standard.Values.Concat(ExtraFields);

    /// <summary>
    /// Bestimmt die weiteren Felder: hinzugefügte Felder und gängige Felder, die in einer markierten Datei einen Wert
    /// haben; geänderte Felder bleiben stehen.
    /// </summary>
    /// <param name="tags">Tags der markierten Dateien.</param>
    private void SyncExtraFields(IReadOnlyList<AudioTags> tags)
    {
        var present = TagFields.Extra.Where(key => tags.Any(tag => tag.Get(key).Length > 0));
        var wanted = Context.Library.ExtraFields.Union(present).Union(ExtraFields.Where(f => f.IsDirty).Select(f => f.Key)).ToList();
        foreach (var field in ExtraFields.Where(field => !wanted.Contains(field.Key)).ToList())
        {
            ExtraFields.Remove(field);
        }

        foreach (var key in wanted.Where(key => ExtraFields.All(field => field.Key != key)))
        {
            ExtraFields.Add(CreateField(key, isRemovable: true));
        }
    }

    /// <summary>
    /// Baut die Genre-Liste auf: eigene Genres, Trennlinie, Standardgenres. Der eingegebene Wert bleibt erhalten,
    /// auch wenn die editierbare Auswahl ihren Text beim Leeren der Liste zurücksetzt (E141).
    /// </summary>
    private void RebuildGenres()
    {
        var value = GenreField.Value;
        FillGenres();
        GenreField.Value = value;
    }

    /// <summary>
    /// Füllt die Genre-Liste neu.
    /// </summary>
    private void FillGenres()
    {
        GenreEntries.Clear();
        foreach (var genre in Context.Library.CustomGenres)
        {
            GenreEntries.Add(new GenreOption(genre, IsCustom: true));
        }

        if (GenreEntries.Count > 0)
        {
            GenreEntries.Add(new GenreSeparator());
        }

        foreach (var genre in Genres.Standard)
        {
            GenreEntries.Add(new GenreOption(genre, IsCustom: false));
        }
    }

    /// <summary>
    /// Baut die Vorschläge für „Feld hinzufügen“ auf.
    /// </summary>
    private void RebuildSuggestions()
    {
        FieldSuggestions.Clear();
        foreach (var key in TagFields.Extra.Where(key => ExtraFields.All(field => field.Key != key)))
        {
            FieldSuggestions.Add(TagFieldNames.Get(key));
        }
    }

    /// <summary>
    /// Prüft die Zahlenfelder.
    /// </summary>
    private void Validate()
    {
        var invalid = AllFields().FirstOrDefault(field => !field.IsValid);
        ValidationMessage = invalid is null ? null : CoreText.TagNumberInvalid(invalid.Label);
        StageCommand.NotifyCanExecuteChanged();
    }
}
