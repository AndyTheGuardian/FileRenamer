using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileRenamer.App.Localization;
using FileRenamer.App.Services;
using FileRenamer.Core.Profiles;
using FileRenamer.Core.Theming;

namespace FileRenamer.App.ViewModels.Settings;

/// <summary>
/// Einstellungsdialog (E155, übernommen aus PDF Utopia, E158): Design, Sprache, Akzentfarbe (Farbreihen Pastell,
/// Kräftig, Neutral und eigene Farben), Farbstich und Helligkeit des Hintergrunds, Farbprofile und unter „Allgemein“
/// das Verkleinern von Covern (E157). Akzent und Hintergrund teilen sich einen Satz Regler mit Farbkreis und Hex-Feld;
/// ein Umschalter wählt, was sie bearbeiten.
/// Änderungen an der Darstellung wirken sofort als Vorschau; „Abbrechen“ stellt den alten Zustand wieder her.
/// Hell und Dunkel haben eigene Farben (E155): Der Dialog bearbeitet die Farben des gerade aktiven Designs; ein
/// Wechsel des Designs im Dialog lädt dessen Farben, die bisher bearbeiteten bleiben erhalten.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private const int MinCoverPixels = 16;
    private const int MaxCoverPixels = 10000;

    private readonly IThemeService _theme;
    private readonly IDialogService _dialogs;
    private readonly AppSettings _original;
    private readonly bool _initialized;
    private bool _syncing;
    private ThemeColorSettings _light;
    private ThemeColorSettings _dark;
    private readonly List<ColorProfile> _customProfiles;
    private bool _selectingProfile;
    private ColorProfile? _lastLightProfile;
    private ColorProfile? _lastDarkProfile;

    /// <summary>Erzeugt den Dialog mit den aktuellen Einstellungen.</summary>
    /// <param name="settings">Aktuelle Einstellungen.</param>
    /// <param name="theme">Theme-Dienst für die Vorschau.</param>
    /// <param name="dialogs">Für Rückfragen, Namenseingabe und Dateiauswahl der Farbprofile.</param>
    public SettingsViewModel(AppSettings settings, IThemeService theme, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _original = settings;
        _theme = theme;
        _dialogs = dialogs;
        _light = settings.ColorsFor(dark: false);
        _dark = settings.ColorsFor(dark: true);
        _customProfiles = [.. settings.ColorProfiles];

        Themes = [new(ThemePreference.System), new(ThemePreference.Light), new(ThemePreference.Dark)];
        Languages = [new(LanguagePreference.System), new(LanguagePreference.German), new(LanguagePreference.English)];
        SelectedTheme = Themes.First(option => option.Theme == settings.Theme);
        SelectedLanguage = Languages.First(option => option.Language == settings.Language);
        EditingDark = IsDarkTheme(settings.Theme);
        LoadColors(settings.ColorsFor(EditingDark));
        foreach (var hex in settings.CustomColors)
        {
            Custom.Add(CustomSwatch(hex));
        }

        HexInput = string.Empty;
        ShrinkCovers = settings.ShrinkCovers;
        CoverMaxPixels = settings.CoverMaxPixels;
        RebuildSwatches();
        SyncSliders();
        RebuildProfiles();
        var otherDark = !EditingDark;
        SetLastProfile(otherDark, AllProfiles(otherDark).FirstOrDefault(p => p.Matches(settings.ColorsFor(otherDark))));
        _initialized = true;
    }

    /// <summary>Designs.</summary>
    public IReadOnlyList<ThemeOption> Themes { get; }

    /// <summary>Sprachen.</summary>
    public IReadOnlyList<LanguageOption> Languages { get; }

    /// <summary>Gewähltes Design.</summary>
    [ObservableProperty]
    public partial ThemeOption SelectedTheme { get; set; }

    /// <summary>Gewählte Sprache.</summary>
    [ObservableProperty]
    public partial LanguageOption SelectedLanguage { get; set; }

    /// <summary>Ob Cover beim Einfügen verkleinert werden (E157).</summary>
    [ObservableProperty]
    public partial bool ShrinkCovers { get; set; }

    /// <summary>Längste Seite verkleinerter Cover in Pixeln; leer wird beim Übernehmen ignoriert (E157).</summary>
    [ObservableProperty]
    public partial decimal? CoverMaxPixels { get; set; }

    /// <summary>Kleinste Pixelzahl für verkleinerte Cover.</summary>
    public decimal CoverMinimum => MinCoverPixels;

    /// <summary>Größte Pixelzahl für verkleinerte Cover.</summary>
    public decimal CoverMaximum => MaxCoverPixels;

    /// <summary>Die Einstellungen mit den Werten des Dialogs.</summary>
    /// <returns>Die neuen Einstellungen.</returns>
    public AppSettings ToSettings() => _original with
    {
        Theme = SelectedTheme.Theme,
        Language = SelectedLanguage.Language,
        AccentColor = null,
        LightColors = (EditingDark ? _light : CurrentColors()).Normalized(),
        DarkColors = (EditingDark ? CurrentColors() : _dark).Normalized(),
        CustomColors = Custom.Select(swatch => swatch.Hex).ToList(),
        ColorProfiles = _customProfiles.ToList(),
        ShrinkCovers = ShrinkCovers,
        CoverMaxPixels = CoverMaxPixels is { } pixels ? (int)Math.Clamp(Math.Round(pixels), MinCoverPixels, MaxCoverPixels) : _original.CoverMaxPixels,
    };

    /// <summary>Stellt nach „Abbrechen“ die alte Darstellung wieder her.</summary>
    public void Revert() => _theme.Apply(_original);

    /// <summary>
    /// Wendet eine gewählte Sprache sofort an, wie das Design (E155); setzt der Aufrufer, der das Hauptfenster neu
    /// aufbaut.
    /// </summary>
    public Action<LanguagePreference>? PreviewLanguage { get; set; }

    /// <summary>
    /// Wird ausgelöst, nachdem eine andere Sprache angewandt wurde: Der Dialog muss in der neuen Sprache neu
    /// aufgebaut werden; alle Eingaben bleiben in diesem ViewModel erhalten.
    /// </summary>
    public event EventHandler? LanguageApplied;

    /// <summary>Andere Sprache gewählt: sofort anwenden (E155).</summary>
    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (!_initialized || PreviewLanguage is null)
        {
            return;
        }

        PreviewLanguage(value.Language);
        LanguageApplied?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Anderes Design gewählt: Die bisher bearbeiteten Farben bleiben für ihr Design erhalten, Regler und Farbreihen
    /// zeigen die Farben des neuen Designs (E155). Beim Standard-Akzent folgt außerdem der Farbkreis.
    /// </summary>
    partial void OnSelectedThemeChanged(ThemeOption value)
    {
        if (!_initialized)
        {
            return;
        }

        if (EditingDark)
        {
            _dark = CurrentColors();
        }
        else
        {
            _light = CurrentColors();
        }

        // Erst anwenden: Bei „System“ steht danach fest, welches Design das Betriebssystem zeigt.
        Preview();
        var dark = IsDarkTheme(value.Theme);
        if (dark != EditingDark)
        {
            EditingDark = dark;
            LoadColors(dark ? _dark : _light);
            HexError = null;
            RebuildSwatches();
            RebuildProfiles();
        }

        SyncSliders();
    }

    private bool IsDarkTheme(ThemePreference theme) => theme switch
    {
        ThemePreference.Dark => true,
        ThemePreference.Light => false,
        _ => _theme.IsDark,
    };

    private ThemeColorSettings CurrentColors() => new()
    {
        AccentColor = AccentHex,
        PastelSaturation = PastelSaturation,
        TintHue = (int)Math.Round(TintHue),
        TintSaturation = (int)Math.Round(TintSaturation),
        Brightness = (int)Math.Round(Brightness),
    };

    private void LoadColors(ThemeColorSettings colors)
    {
        AccentHex = colors.AccentColor;
        PastelSaturation = colors.PastelSaturation;
        TintHue = colors.TintHue;
        TintSaturation = colors.TintSaturation;
        Brightness = colors.Brightness;
    }

    private void Preview()
    {
        // Während des Konstruktors sind noch nicht alle Werte gesetzt.
        if (!_initialized)
        {
            return;
        }

        _theme.Apply(ToSettings());
        SelectMatchingProfile();
    }
}
