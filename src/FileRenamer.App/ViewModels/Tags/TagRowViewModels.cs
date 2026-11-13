using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FileRenamer.App.Localization;
using FileRenamer.Core.Tagging;
using FileRenamer.Core.Tagging.Changes;

namespace FileRenamer.App.ViewModels.Tags;

/// <summary>
/// Eine vorgemerkte Änderung in der Liste unten links: Checkbox (Schritt ein/aus), Farbe der Funktion, Beschreibung
/// und Anzahl der Dateien.
/// </summary>
/// <param name="change">Die Änderung.</param>
/// <param name="toggled">Wird nach dem Ein- oder Ausschalten aufgerufen.</param>
public sealed partial class PendingChangeViewModel(TagChange change, Action toggled) : ObservableObject
{
    /// <summary>
    /// Die Änderung.
    /// </summary>
    public TagChange Change { get; } = change;

    /// <summary>
    /// Die Funktion (Farbe).
    /// </summary>
    public TagFunction Function => Change.Function;

    /// <summary>
    /// Beschreibung, z. B. „Albuminterpret, Jahr“.
    /// </summary>
    public string DisplayName => Describe(Change);

    /// <summary>
    /// Anzahl der Dateien als Text, z. B. „· 4“.
    /// </summary>
    public string CountText => "· " + Change.Files.Count;

    /// <summary>
    /// Ob die Änderung aktiv ist.
    /// </summary>
    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Meldet das Ein- oder Ausschalten.
    /// </summary>
    /// <param name="value">Der neue Zustand.</param>
    partial void OnIsEnabledChanged(bool value) => toggled();

    /// <summary>
    /// Beschreibt eine Änderung in Worten.
    /// </summary>
    /// <param name="change">Die Änderung.</param>
    /// <returns>Die Beschreibung.</returns>
    private static string Describe(TagChange change) => change switch
    {
        SetFieldsChange fields => string.Join(", ", fields.Values.Keys.Select(TagFieldNames.Get)),
        FieldsFromFileNameChange fromName => Loc.F("Pending.FromName", fromName.Template),
        TrackNumberChange => Loc.T("TagFunction.TrackNumbers"),
        AddCoverChange add => Loc.F("Pending.AddCover", CoverEditorViewModel.TypeName(add.ImagesByFile.Values.FirstOrDefault()?.Type ?? CoverType.FrontCover)),
        RemoveCoverChange remove => Loc.F("Pending.RemoveCover", remove.Type is { } type ? CoverEditorViewModel.TypeName(type) : Loc.T("Pending.AllCovers")),
        SetLyricsChange => Loc.T("Pending.Lyrics"),
        _ => change.Function.ToString(),
    };
}

/// <summary>
/// Eine Zelle der Tag-Vorschau: neuer Wert, vorheriger Wert und Funktion der Änderung (Farbe) oder <c>null</c>.
/// </summary>
/// <param name="Text">Angezeigter (neuer) Wert.</param>
/// <param name="OldText">Vorheriger Wert, falls geändert.</param>
/// <param name="Source">Funktion der Änderung oder <c>null</c>, wenn unverändert.</param>
public sealed record TagCell(string Text, string? OldText, TagFunction? Source)
{
    /// <summary>
    /// Ob der Wert geändert wird.
    /// </summary>
    public bool IsChanged => Source is not null;

    /// <summary>
    /// Ob der vorherige Wert angezeigt werden kann (geändert und nicht leer).
    /// </summary>
    public bool HasOldText => IsChanged && !string.IsNullOrEmpty(OldText);

    /// <summary>
    /// Tooltip „vorher: …“ bei geänderten Werten.
    /// </summary>
    public string? Tip => IsChanged ? Loc.F("Tags.Before", string.IsNullOrEmpty(OldText) ? "–" : OldText) : null;
}

/// <summary>
/// Zeile der Tag-Vorschau (unten rechts). Nicht-Audiodateien werden ausgegraut und übersprungen.
/// </summary>
public sealed partial class TagPreviewRowViewModel : ObservableObject
{
    private readonly TagRowCallbacks _callbacks;
    private bool _isSelected;
    private bool _isMarked;

    /// <summary>
    /// Erzeugt die Zeile einer Audiodatei.
    /// </summary>
    /// <param name="preview">Die Vorschau der Datei.</param>
    /// <param name="functions">Id der Änderung → Funktion (Farbe).</param>
    /// <param name="callbacks">Anfangszustände von Auswahl und Markierung sowie Rückmeldung von Wechseln.</param>
    public TagPreviewRowViewModel(TagFilePreview preview, IReadOnlyDictionary<Guid, TagFunction> functions, TagRowCallbacks callbacks)
    {
        Path = preview.Path;
        FileName = System.IO.Path.GetFileName(preview.Path);
        IsAudio = true;
        _callbacks = callbacks;
        _isSelected = callbacks.IsSelected(Path);
        _isMarked = callbacks.IsMarked(Path);
        Hint = preview.Hints.Count == 0 ? null : string.Join(Environment.NewLine, preview.Hints);
        Track = Cell(preview, functions, TagFields.Track);
        Title = Cell(preview, functions, TagFields.Title);
        Artist = Cell(preview, functions, TagFields.Artist);
        Album = Cell(preview, functions, TagFields.Album);
        AlbumArtist = Cell(preview, functions, TagFields.AlbumArtist);
        Year = Cell(preview, functions, TagFields.Year);
        CoverSource = Source(preview, functions, TagFields.Covers);
        LyricsCell = new TagCell(preview.Result.Lyrics.Length > 0 ? "✓" : "–", null, Source(preview, functions, TagFields.Lyrics));
        Cover = preview.Result.Covers.FirstOrDefault() is { } cover ? CoverTileViewModel.TryDecode(cover.Data) : null;
    }

    /// <summary>
    /// Erzeugt die ausgegraute Zeile einer Datei, die keine Audiodatei ist.
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <param name="hint">Grund, warum die Datei übersprungen wird.</param>
    public TagPreviewRowViewModel(string path, string hint)
    {
        Path = path;
        Hint = hint;
        FileName = System.IO.Path.GetFileName(path);
        _callbacks = TagRowCallbacks.None;
        Track = Title = Artist = Album = AlbumArtist = Year = LyricsCell = new TagCell(string.Empty, null, null);
    }

    /// <summary>Pfad der Datei.</summary>
    public string Path { get; }

    /// <summary>Dateiname.</summary>
    public string FileName { get; }

    /// <summary>Ob es eine unterstützte Audiodatei ist.</summary>
    public bool IsAudio { get; }

    /// <summary>Hinweise oder <c>null</c>.</summary>
    public string? Hint { get; }

    /// <summary>Ob Hinweise vorliegen.</summary>
    public bool HasHint => Hint is not null;

    /// <summary>Tracknummer.</summary>
    public TagCell Track { get; }

    /// <summary>Titel.</summary>
    public TagCell Title { get; }

    /// <summary>Interpret.</summary>
    public TagCell Artist { get; }

    /// <summary>Album.</summary>
    public TagCell Album { get; }

    /// <summary>Albuminterpret.</summary>
    public TagCell AlbumArtist { get; }

    /// <summary>Jahr.</summary>
    public TagCell Year { get; }

    /// <summary>Lyrics vorhanden (✓) oder nicht (–).</summary>
    public TagCell LyricsCell { get; }

    /// <summary>Erstes Bild nach den Änderungen oder <c>null</c>.</summary>
    public Bitmap? Cover { get; }

    /// <summary>Funktion, die die Bilder geändert hat, oder <c>null</c>.</summary>
    public TagFunction? CoverSource { get; }

    /// <summary>Ob die Bilder geändert werden.</summary>
    public bool IsCoverChanged => CoverSource is not null;

    /// <summary>Ob ein Bild vorhanden ist.</summary>
    public bool HasCover => Cover is not null;

    /// <summary>Ob kein Bild vorhanden ist (Strich anzeigen).</summary>
    public bool HasNoCover => IsAudio && Cover is null;

    /// <summary>
    /// Ob die Datei per Checkbox ausgewählt ist (Grundlage für Formular und „Ausgewählte Dateien“, E150).
    /// Nicht-Audiodateien lassen sich nicht auswählen.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            var selected = value && IsAudio;
            if (SetProperty(ref _isSelected, selected))
            {
                _callbacks.SelectionChanged(Path, selected);
            }
        }
    }

    /// <summary>
    /// Ob die Zeile zum Kopieren markiert ist (Zeilenmarkierung, unabhängig von <see cref="IsSelected"/>).
    /// </summary>
    public bool IsMarked
    {
        get => _isMarked;
        set
        {
            var marked = value && IsAudio;
            if (SetProperty(ref _isMarked, marked))
            {
                _callbacks.MarkedChanged(Path, marked);
            }
        }
    }

    /// <summary>
    /// Bildet eine Textzelle.
    /// </summary>
    /// <param name="preview">Die Vorschau.</param>
    /// <param name="functions">Id → Funktion.</param>
    /// <param name="key">Feldschlüssel.</param>
    /// <returns>Die Zelle.</returns>
    private static TagCell Cell(TagFilePreview preview, IReadOnlyDictionary<Guid, TagFunction> functions, string key)
    {
        var source = Source(preview, functions, key);
        var text = preview.Result.Get(key);
        return new TagCell(text.Length == 0 ? "–" : text, source is null ? null : preview.Original.Get(key), source);
    }

    /// <summary>
    /// Ermittelt die Funktion, die einen Bestandteil geändert hat.
    /// </summary>
    /// <param name="preview">Die Vorschau.</param>
    /// <param name="functions">Id → Funktion.</param>
    /// <param name="key">Schlüssel.</param>
    /// <returns>Die Funktion oder <c>null</c>.</returns>
    private static TagFunction? Source(TagFilePreview preview, IReadOnlyDictionary<Guid, TagFunction> functions, string key) =>
        preview.Sources.TryGetValue(key, out var id) && functions.TryGetValue(id, out var function) ? function : null;
}

/// <summary>
/// Verbindung einer Vorschauzeile zum Bereich: liefert die Anfangszustände und nimmt Wechsel von Auswahl (Checkbox)
/// und Markierung (Zeile) entgegen.
/// </summary>
/// <param name="IsSelected">Ob eine Datei (Pfad) ausgewählt ist.</param>
/// <param name="IsMarked">Ob eine Zeile (Pfad) markiert ist.</param>
/// <param name="SelectionChanged">Meldet einen Auswahlwechsel mit Pfad und Zustand.</param>
/// <param name="MarkedChanged">Meldet einen Markierungswechsel mit Pfad und Zustand.</param>
public sealed record TagRowCallbacks(
    Func<string, bool> IsSelected, Func<string, bool> IsMarked, Action<string, bool> SelectionChanged, Action<string, bool> MarkedChanged)
{
    /// <summary>
    /// Verbindung ohne Wirkung für Zeilen, die nicht ausgewählt oder markiert werden können.
    /// </summary>
    public static TagRowCallbacks None { get; } = new(_ => false, _ => false, (_, _) => { }, (_, _) => { });
}
