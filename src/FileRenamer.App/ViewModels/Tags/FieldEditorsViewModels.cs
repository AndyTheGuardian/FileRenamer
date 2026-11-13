using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileRenamer.App.Localization;
using FileRenamer.Core.Tagging;
using FileRenamer.Core.Tagging.Changes;

namespace FileRenamer.App.ViewModels.Tags;

/// <summary>
/// Editor „Tags aus Dateinamen“: Vorlage mit Platzhaltern, Beispiel an der ersten Datei des Geltungsbereichs.
/// </summary>
public sealed partial class FromFileNameEditorViewModel : TagEditorViewModel
{
    /// <summary>
    /// Erzeugt den Editor mit der Vorlage aus dem Mockup.
    /// </summary>
    /// <param name="context">Der Bereich „Tags“.</param>
    public FromFileNameEditorViewModel(ITagEditorContext context)
        : base(TagFunction.FromFileName, context) => Refresh();

    /// <summary>
    /// Beispielvorlagen.
    /// </summary>
    public IReadOnlyList<string> Presets { get; } =
        ["%track% %title%", "%track% - %artist% - %title%", "%artist% - %title%", "%artist% - %album% - %track% - %title%", "%artist%/%album%/%track% %title%"];

    /// <summary>
    /// Platzhalter als Schaltflächen (Audio, Sonstiges).
    /// </summary>
    public IReadOnlyList<string> AudioPlaceholders { get; } =
        ["%artist%", "%title%", "%album%", "%albumartist%", "%track%", "%disc%", "%year%", "%genre%", "%composer%"];

    /// <summary>
    /// Platzhalter zum Überspringen.
    /// </summary>
    public IReadOnlyList<string> OtherPlaceholders { get; } = ["%skip%"];

    /// <summary>
    /// Die Vorlage.
    /// </summary>
    [ObservableProperty]
    public partial string Template { get; set; } = "%track% %title%";

    /// <summary>
    /// Ob Unterstriche als Leerzeichen gelesen werden.
    /// </summary>
    [ObservableProperty]
    public partial bool UnderscoreAsSpace { get; set; } = true;

    /// <summary>
    /// Beispiel an der ersten Datei oder <c>null</c>.
    /// </summary>
    [ObservableProperty]
    public partial string? Example { get; set; }

    /// <summary>
    /// Prüft die Vorlage und berechnet das Beispiel neu.
    /// </summary>
    public override void Refresh()
    {
        ValidationMessage = FileNameTagParser.Validate(Template);
        Example = CreateExample();
        base.Refresh();
    }

    /// <summary>
    /// Übernimmt eine Beispielvorlage.
    /// </summary>
    /// <param name="preset">Die Vorlage.</param>
    [RelayCommand]
    private void ApplyPreset(string preset) => Template = preset;

    /// <summary>
    /// Erzeugt die Änderung.
    /// </summary>
    /// <param name="files">Die Dateien.</param>
    /// <returns>Die Änderung.</returns>
    protected override TagChange CreateChange(IReadOnlyList<string> files) => new FieldsFromFileNameChange(Template.Trim(), UnderscoreAsSpace);

    /// <summary>
    /// Vorlage geändert.
    /// </summary>
    /// <param name="value">Die Vorlage.</param>
    partial void OnTemplateChanged(string value) => Refresh();

    /// <summary>
    /// Option geändert.
    /// </summary>
    /// <param name="value">Der Wert.</param>
    partial void OnUnderscoreAsSpaceChanged(bool value) => Refresh();


    /// <summary>
    /// Beschreibt das Ergebnis für die erste Datei, z. B. „01 Like A Prayer.mp3 → Track 1 · Titel Like A Prayer“.
    /// </summary>
    /// <returns>Das Beispiel oder <c>null</c>.</returns>
    private string? CreateExample()
    {
        if (ValidationMessage is not null || ScopeFiles.Count == 0)
        {
            return null;
        }

        var path = ScopeFiles[0];
        var values = FileNameTagParser.Parse(Template, path, UnderscoreAsSpace);
        var result = values is null
            ? Loc.T("FromName.NoMatch")
            : string.Join(" · ", values.Select(pair => $"{TagFieldNames.Get(pair.Key)} {pair.Value}"));
        return $"{Path.GetFileName(path)} → {result}";
    }
}

/// <summary>
/// Editor „Tracknummern vergeben“.
/// </summary>
public sealed partial class TrackNumbersEditorViewModel : TagEditorViewModel
{
    /// <summary>
    /// Erzeugt den Editor mit Startwert 1 und gesetzter Anzahl der Tracks.
    /// </summary>
    /// <param name="context">Der Bereich „Tags“.</param>
    public TrackNumbersEditorViewModel(ITagEditorContext context)
        : base(TagFunction.TrackNumbers, context) => Refresh();

    /// <summary>
    /// Reihenfolge.
    /// </summary>
    [ObservableProperty]
    public partial TrackOrder Order { get; set; }

    /// <summary>
    /// Startwert.
    /// </summary>
    [ObservableProperty]
    public partial decimal? Start { get; set; } = 1;

    /// <summary>
    /// Ob „von“ auf die Anzahl der Dateien gesetzt wird.
    /// </summary>
    [ObservableProperty]
    public partial bool SetTrackCount { get; set; } = true;

    /// <summary>
    /// Ob die Discnummer gesetzt wird.
    /// </summary>
    [ObservableProperty]
    public partial bool SetDisc { get; set; }

    /// <summary>
    /// Discnummer.
    /// </summary>
    [ObservableProperty]
    public partial decimal? Disc { get; set; } = 1;

    /// <summary>
    /// Anzahl der Discs.
    /// </summary>
    [ObservableProperty]
    public partial decimal? DiscCount { get; set; } = 1;

    /// <summary>
    /// Beispiel, z. B. „1/4, 2/4, 3/4, 4/4“.
    /// </summary>
    [ObservableProperty]
    public partial string? Example { get; set; }

    /// <summary>
    /// Berechnet das Beispiel neu.
    /// </summary>
    public override void Refresh()
    {
        ValidationMessage = Start is null or < 1 ? Loc.T("Tracks.StartInvalid") : null;
        Example = ValidationMessage is null ? CreateExample() : null;
        base.Refresh();
    }

    /// <summary>
    /// Erzeugt die Änderung.
    /// </summary>
    /// <param name="files">Die Dateien.</param>
    /// <returns>Die Änderung.</returns>
    protected override TagChange CreateChange(IReadOnlyList<string> files) =>
        new TrackNumberChange((int)(Start ?? 1), Order, SetTrackCount, SetDisc ? (int?)(Disc ?? 1) : null, SetDisc ? (int?)(DiscCount ?? 1) : null);

    /// <summary>Startwert geändert.</summary>
    /// <param name="value">Der Wert.</param>
    partial void OnStartChanged(decimal? value) => Refresh();

    /// <summary>Option geändert.</summary>
    /// <param name="value">Der Wert.</param>
    partial void OnSetTrackCountChanged(bool value) => Refresh();


    /// <summary>
    /// Bildet das Beispiel für bis zu vier Dateien.
    /// </summary>
    /// <returns>Das Beispiel oder <c>null</c> ohne Dateien.</returns>
    private string? CreateExample()
    {
        var count = ScopeFiles.Count;
        if (count == 0)
        {
            return null;
        }

        var start = (int)(Start ?? 1);
        var numbers = Enumerable.Range(start, Math.Min(count, 4))
            .Select(number => SetTrackCount ? $"{number}/{count}" : number.ToString(CultureInfo.InvariantCulture));
        return string.Join(", ", numbers) + (count > 4 ? ", …" : string.Empty);
    }
}
