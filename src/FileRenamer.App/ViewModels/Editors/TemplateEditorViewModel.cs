using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileRenamer.Core.Files;
using FileRenamer.Core.Metadata;
using FileRenamer.Core.Naming;
using FileRenamer.Core.Rules;
using FileRenamer.Core.Rules.Templates;

namespace FileRenamer.App.ViewModels.Editors;

/// <summary>
/// Gruppe von Platzhaltern, die als Schaltflächen angezeigt wird (E66, E78).
/// </summary>
/// <param name="Label">Bezeichnung links.</param>
/// <param name="Tokens">Platzhalter inklusive Prozentzeichen.</param>
public sealed record PlaceholderGroup(string Label, IReadOnlyList<string> Tokens);

/// <summary>
/// Eintrag der Vorlagenauswahl (E106): Vorlage oder Trennlinie.
/// </summary>
public abstract record TemplatePresetEntry
{
    /// <summary>
    /// Ob der Eintrag gewählt werden kann.
    /// </summary>
    public virtual bool IsSelectable => true;
}

/// <summary>
/// Eigene oder Beispielvorlage in der Vorlagenauswahl.
/// </summary>
/// <param name="Template">Die Vorlage.</param>
/// <param name="IsCustom">Ob es eine eigene (entfernbare) Vorlage ist.</param>
/// <param name="IsAudio">Ob sie zum Tab „Audio“ gehört.</param>
public sealed record TemplatePreset(string Template, bool IsCustom, bool IsAudio) : TemplatePresetEntry;

/// <summary>
/// Trennlinie zwischen eigenen und Beispielvorlagen.
/// </summary>
public sealed record TemplatePresetSeparator : TemplatePresetEntry
{
    /// <inheritdoc />
    public override bool IsSelectable => false;
}

/// <summary>
/// Editor für „Name aus Vorlage“ mit den Tabs „Fotos“ und „Audio“ (E76).
/// </summary>
public sealed partial class TemplateEditorViewModel() : RuleEditorViewModel(RuleKind.Template)
{
    private TemplateLibrary? _library;

    /// <summary>
    /// Beispielfoto mit Ordner und Urheber in der aktuellen Sprache (A11).
    /// </summary>
    private static FileEntry SamplePhoto => new(Path.Combine(Localization.Loc.T("Sample.Folder"), "IMG_0001.jpg"), null, new DateTime(2026, 10, 12), 0)
    {
        Metadata = new FileMetadata(MediaKind.Image, new ImageMetadata
        {
            DateTaken = new DateTime(2026, 10, 12, 14, 30, 5), Make = "Canon", Model = "Canon EOS R6", Lens = "RF24-105mm F4 L IS USM",
            Iso = 400, FNumber = 4, ExposureTime = 1 / 250.0, FocalLength = 50, Width = 6000, Height = 4000, Orientation = 0,
            Latitude = 48.13743, Longitude = 11.57549, Altitude = 519, Description = "Marienplatz",
            Creator = Localization.Loc.T("Sample.Person"), Copyright = "(c) " + Localization.Loc.T("Sample.Person"),
        }),
    };

    /// <summary>
    /// Beispiel-Audiodatei mit Ordner in der aktuellen Sprache (A11).
    /// </summary>
    private static FileEntry SampleAudio => new(Path.Combine(Localization.Loc.T("Sample.MusicFolder"), "01 like a prayer.mp3"), null, new DateTime(2026, 10, 12), 0)
    {
        Metadata = new FileMetadata(MediaKind.Audio, Audio: new AudioMetadata
        {
            Artist = "Madonna", Title = "Like A Prayer", Album = "Like A Prayer", AlbumArtist = "Madonna", Composer = "Madonna, Patrick Leonard",
            Genre = "Pop", Track = 1, Disc = 1, Year = 1989,
        }),
    };

    /// <summary>
    /// Platzhalter des Tabs „Fotos“.
    /// </summary>
    public IReadOnlyList<PlaceholderGroup> PhotoPlaceholders { get; } =
    [
        Group("Placeholder.File", Placeholders.File), Group("Placeholder.DateTime", Placeholders.PhotoDate),
        Group("Placeholder.Camera", Placeholders.PhotoCamera), Group("Placeholder.Image", Placeholders.PhotoImage),
        Group("Placeholder.Location", Placeholders.PhotoLocation), Group("Placeholder.Description", Placeholders.PhotoDescription),
    ];

    /// <summary>
    /// Platzhalter des Tabs „Audio“.
    /// </summary>
    public IReadOnlyList<PlaceholderGroup> AudioPlaceholders { get; } =
    [
        Group("Placeholder.File", Placeholders.File), Group("Placeholder.Title", Placeholders.AudioTitle),
        Group("Placeholder.Numbering", Placeholders.AudioNumbering),
    ];

    /// <summary>
    /// Beispielvorlagen für Fotos.
    /// </summary>
    public IReadOnlyList<string> PhotoPresets { get; } =
        ["%name%_%model%_%date:yyyy-MM-dd%", "%date:yyyy-MM-dd%_%time:HH-mm-ss%", "%date:yyyyMMdd%_%make%_%name%"];

    /// <summary>
    /// Beispielvorlagen für Audio.
    /// </summary>
    public IReadOnlyList<string> AudioPresets { get; } =
        ["%artist% - %title%", "%track% %title%", "%albumartist% - %album% - %track% - %title%"];

    /// <summary>
    /// Ablage der eigenen Vorlagen; wird vom Hauptfenster-ViewModel gesetzt (E106).
    /// Ohne Ablage zeigt die Auswahl nur die Beispielvorlagen und „Speichern“ ist gesperrt.
    /// </summary>
    public TemplateLibrary? Library
    {
        get => _library;
        set
        {
            _library = value;
            RefreshPresetLists();
        }
    }

    /// <summary>
    /// Auswahl im Tab „Fotos“: eigene Vorlagen (neueste zuerst), Trennlinie, Beispielvorlagen.
    /// </summary>
    public IReadOnlyList<TemplatePresetEntry> PhotoPresetItems => ComposePresets(isAudio: false);

    /// <summary>
    /// Auswahl im Tab „Audio“: eigene Vorlagen (neueste zuerst), Trennlinie, Beispielvorlagen.
    /// </summary>
    public IReadOnlyList<TemplatePresetEntry> AudioPresetItems => ComposePresets(isAudio: true);

    /// <summary>Vorlage für Fotos.</summary>
    [ObservableProperty]
    public partial string PhotoTemplate { get; set; } = "%name%_%model%_%date:yyyy-MM-dd%";

    /// <summary>Vorlage für Audio.</summary>
    [ObservableProperty]
    public partial string AudioTemplate { get; set; } = "%artist% - %title%";

    /// <summary>Ersatztext für fehlende Werte.</summary>
    [ObservableProperty]
    public partial string MissingText { get; set; } = Core.Localization.CoreText.Unknown;

    /// <summary>Ersatz für verbotene Zeichen.</summary>
    [ObservableProperty]
    public partial string InvalidCharacterReplacement { get; set; } = "_";

    /// <summary>Stellen der Tracknummer.</summary>
    [ObservableProperty]
    public partial decimal? TrackDigits { get; set; } = 2;

    /// <summary>Beispiel im Tab „Fotos“.</summary>
    [ObservableProperty]
    public partial string PhotoExample { get; set; } = string.Empty;

    /// <summary>Beispiel im Tab „Audio“.</summary>
    [ObservableProperty]
    public partial string AudioExample { get; set; } = string.Empty;

    /// <inheritdoc />
    public override RenameRule CreateRule() => new TemplateRule
    {
        PhotoTemplate = PhotoTemplate,
        AudioTemplate = AudioTemplate,
        MissingText = MissingText,
        InvalidCharacterReplacement = InvalidCharacterReplacement,
        TrackDigits = (int)ToLong(TrackDigits, 2),
    };

    /// <inheritdoc />
    protected override void LoadSettings(RenameRule rule)
    {
        var template = (TemplateRule)rule;
        PhotoTemplate = template.PhotoTemplate;
        AudioTemplate = template.AudioTemplate;
        MissingText = template.MissingText;
        InvalidCharacterReplacement = template.InvalidCharacterReplacement;
        TrackDigits = template.TrackDigits;
    }

    /// <inheritdoc />
    protected override bool IsSettingProperty(string? propertyName) =>
        base.IsSettingProperty(propertyName)
        && propertyName is not (nameof(PhotoExample) or nameof(AudioExample) or nameof(PhotoPresetItems) or nameof(AudioPresetItems));

    /// <summary>
    /// Berechnet die Beispiele beider Tabs (die gemeinsame Beispielzeile bleibt leer) und
    /// aktualisiert, ob „Speichern“ möglich ist.
    /// </summary>
    protected override void OnSettingsValidated()
    {
        var rule = (TemplateRule)CreateRule();
        PhotoExample = HasValidationMessage ? "–" : Describe(rule, SamplePhoto);
        AudioExample = HasValidationMessage ? "–" : Describe(rule, SampleAudio);
        SavePhotoTemplateCommand.NotifyCanExecuteChanged();
        SaveAudioTemplateCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Nimmt die Foto-Vorlage in die eigenen Vorlagen auf (E106).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSavePhotoTemplate))]
    private void SavePhotoTemplate() => SaveTemplate(isAudio: false, PhotoTemplate);

    /// <summary>
    /// Nimmt die Audio-Vorlage in die eigenen Vorlagen auf (E106).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSaveAudioTemplate))]
    private void SaveAudioTemplate() => SaveTemplate(isAudio: true, AudioTemplate);

    /// <summary>
    /// Entfernt eine eigene Vorlage.
    /// </summary>
    /// <param name="preset">Die Vorlage.</param>
    [RelayCommand]
    private void RemoveTemplate(TemplatePreset preset)
    {
        Library?.Remove(preset.IsAudio, preset.Template);
        RefreshPresetLists();
    }

    /// <summary>
    /// Ob die Foto-Vorlage gespeichert werden kann: gültig, nicht leer und noch nicht in der Liste.
    /// </summary>
    /// <returns><c>true</c>, wenn ja.</returns>
    private bool CanSavePhotoTemplate() => CanSave(isAudio: false, PhotoTemplate);

    /// <summary>
    /// Ob die Audio-Vorlage gespeichert werden kann: gültig, nicht leer und noch nicht in der Liste.
    /// </summary>
    /// <returns><c>true</c>, wenn ja.</returns>
    private bool CanSaveAudioTemplate() => CanSave(isAudio: true, AudioTemplate);

    /// <summary>
    /// Prüft, ob eine Vorlage gespeichert werden kann.
    /// </summary>
    /// <param name="isAudio"><c>true</c> für Audio, sonst Fotos.</param>
    /// <param name="template">Die Vorlage.</param>
    /// <returns><c>true</c>, wenn ja.</returns>
    private bool CanSave(bool isAudio, string template) =>
        Library is { } library && !HasValidationMessage
        && CustomTemplateList.CanAdd(library.Get(isAudio), BuiltInPresets(isAudio), template);

    /// <summary>
    /// Speichert eine Vorlage und aktualisiert Auswahl und Schaltflächen.
    /// </summary>
    /// <param name="isAudio"><c>true</c> für Audio, sonst Fotos.</param>
    /// <param name="template">Die Vorlage.</param>
    private void SaveTemplate(bool isAudio, string template)
    {
        Library?.Add(isAudio, BuiltInPresets(isAudio), template);
        RefreshPresetLists();
    }

    /// <summary>
    /// Meldet geänderte Auswahllisten und den Zustand der Speichern-Schaltflächen.
    /// </summary>
    private void RefreshPresetLists()
    {
        OnPropertyChanged(nameof(PhotoPresetItems));
        OnPropertyChanged(nameof(AudioPresetItems));
        SavePhotoTemplateCommand.NotifyCanExecuteChanged();
        SaveAudioTemplateCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Stellt die Auswahlliste eines Tabs zusammen.
    /// </summary>
    /// <param name="isAudio"><c>true</c> für Audio, sonst Fotos.</param>
    /// <returns>Eigene Vorlagen, ggf. Trennlinie, Beispielvorlagen.</returns>
    private List<TemplatePresetEntry> ComposePresets(bool isAudio)
    {
        var custom = Library?.Get(isAudio) ?? [];
        var entries = new List<TemplatePresetEntry>(custom.Select(template => new TemplatePreset(template, IsCustom: true, isAudio)));
        if (entries.Count > 0)
        {
            entries.Add(new TemplatePresetSeparator());
        }

        entries.AddRange(BuiltInPresets(isAudio).Select(template => new TemplatePreset(template, IsCustom: false, isAudio)));
        return entries;
    }

    /// <summary>
    /// Die Beispielvorlagen eines Tabs.
    /// </summary>
    /// <param name="isAudio"><c>true</c> für Audio, sonst Fotos.</param>
    /// <returns>Die Beispielvorlagen.</returns>
    private IReadOnlyList<string> BuiltInPresets(bool isAudio) => isAudio ? AudioPresets : PhotoPresets;

    /// <summary>
    /// Übernimmt eine in der Liste gewählte Vorlage in das Vorlagenfeld ihres Tabs.
    /// </summary>
    /// <param name="preset">Die Vorlage.</param>
    [RelayCommand]
    private void ApplyPreset(TemplatePreset preset)
    {
        if (preset.IsAudio)
        {
            AudioTemplate = preset.Template;
        }
        else
        {
            PhotoTemplate = preset.Template;
        }
    }

    /// <summary>
    /// Erzeugt eine Platzhaltergruppe.
    /// </summary>
    /// <param name="labelKey">Textschlüssel der Bezeichnung.</param>
    /// <param name="names">Namen ohne Prozentzeichen.</param>
    /// <returns>Die Gruppe.</returns>
    private static PlaceholderGroup Group(string labelKey, IEnumerable<string> names) =>
        new(Localization.Loc.T(labelKey), names.Select(name => $"%{name}%").ToList());

    /// <summary>
    /// Wendet die Vorlage auf eine Beispieldatei an.
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    /// <param name="file">Die Beispieldatei.</param>
    /// <returns>„alt → neu“.</returns>
    private static string Describe(TemplateRule rule, FileEntry file)
    {
        var baseName = Path.GetFileNameWithoutExtension(file.FileName);
        var extension = Path.GetExtension(file.FileName);
        var result = rule.Apply([new RuleInput(file, SegmentedName.FromText(baseName))])[0].Name.Text;
        return $"{file.FileName} → {result}{extension}";
    }
}
