using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileRenamer.App.Localization;
using FileRenamer.App.Services;
using FileRenamer.Core.Profiles;
using FileRenamer.Core.Theming;

namespace FileRenamer.App.ViewModels.Settings;

/// <summary>
/// Teil der Einstellungen: Farbreihen, gemeinsame Regler für Akzentfarbe und Hintergrund, Farbkreis und Hex-Feld (E155).
/// </summary>
public sealed partial class SettingsViewModel
{
    /// <summary>Pastellfarben bei <see cref="PastelSaturation"/>; je kräftiger Farbe eine mit demselben Farbton.</summary>
    public ObservableCollection<AccentSwatchViewModel> Pastel { get; } = [];

    /// <summary>Kräftige Farben.</summary>
    public ObservableCollection<AccentSwatchViewModel> Strong { get; } = [];

    /// <summary>Neutrale Farben (95 % Schwarz, 95 % Weiß).</summary>
    public ObservableCollection<AccentSwatchViewModel> Neutral { get; } = [];

    /// <summary>Eigene Farben.</summary>
    public ObservableCollection<AccentSwatchViewModel> Custom { get; } = [];

    /// <summary>Ob es eigene Farben gibt.</summary>
    public bool HasCustomColors => Custom.Count > 0;

    /// <summary>Gewählte Akzentfarbe; <c>null</c> = Standard des Themes.</summary>
    [ObservableProperty]
    public partial string? AccentHex { get; set; }

    /// <summary>Ob der Dialog gerade die Farben des dunklen Designs bearbeitet (E155).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditedThemeHint), nameof(ResetToolTip))]
    public partial bool EditingDark { get; private set; }

    /// <summary>Hinweis, für welches Design die Farben gelten.</summary>
    public string EditedThemeHint => Loc.F("Settings.ColorsFor", Loc.T(EditingDark ? "Theme.Dark" : "Theme.Light"));

    /// <summary>Sättigung der ganzen Pastell-Reihe (10–90 %).</summary>
    public int PastelSaturation { get; private set; }

    /// <summary>Farbton des Farbstichs (0–359).</summary>
    [ObservableProperty]
    public partial double TintHue { get; set; }

    /// <summary>Stärke des Farbstichs (0–100).</summary>
    [ObservableProperty]
    public partial double TintSaturation { get; set; }

    /// <summary>Helligkeit des Hintergrunds (−25 bis +25).</summary>
    [ObservableProperty]
    public partial double Brightness { get; set; }

    /// <summary>Was die Regler bearbeiten.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAccentTarget), nameof(IsBackgroundTarget),
        nameof(SaturationLabel), nameof(BrightnessMinimum), nameof(BrightnessMaximum), nameof(HexHint))]
    public partial AdjustTarget Target { get; set; }

    /// <summary>Umschalter „Akzentfarbe“.</summary>
    public bool IsAccentTarget
    {
        get => Target == AdjustTarget.Accent;
        set
        {
            if (value)
            {
                Target = AdjustTarget.Accent;
            }
        }
    }

    /// <summary>Umschalter „Hintergrund“.</summary>
    public bool IsBackgroundTarget
    {
        get => Target == AdjustTarget.Background;
        set
        {
            if (value)
            {
                Target = AdjustTarget.Background;
            }
        }
    }

    /// <summary>Regler Farbton (0–359).</summary>
    [ObservableProperty]
    public partial double SliderHue { get; set; }

    /// <summary>Regler Sättigung bzw. Stärke (0–100).</summary>
    [ObservableProperty]
    public partial double SliderSaturation { get; set; }

    /// <summary>Regler Helligkeit: Akzent 0–100 (Hellwert), Hintergrund −25 bis +25.</summary>
    [ObservableProperty]
    public partial double SliderBrightness { get; set; }

    /// <summary>Zahlenfeld zum Farbton; leer wird ignoriert (E155).</summary>
    public decimal? HueInput
    {
        get => (decimal)Math.Round(SliderHue);
        set => ApplyInput(value, 0, 359, v => SliderHue = v, nameof(HueInput));
    }

    /// <summary>Zahlenfeld zu Sättigung bzw. Stärke; leer wird ignoriert (E155).</summary>
    public decimal? SaturationInput
    {
        get => (decimal)Math.Round(SliderSaturation);
        set => ApplyInput(value, SaturationMinimum, SaturationMaximum, v => SliderSaturation = v, nameof(SaturationInput));
    }

    /// <summary>Zahlenfeld zur Helligkeit; leer wird ignoriert (E155).</summary>
    public decimal? BrightnessInput
    {
        get => (decimal)Math.Round(SliderBrightness);
        set => ApplyInput(value, BrightnessMinimum, BrightnessMaximum, v => SliderBrightness = v, nameof(BrightnessInput));
    }

    /// <summary>
    /// Kleinster Wert des Sättigungsreglers: bei einer gewählten Pastellfarbe 10 %, weil der Regler dann die ganze
    /// Pastell-Reihe verschiebt (E155), sonst 0.
    /// </summary>
    public double SaturationMinimum => IsPastelSelected ? AccentPalette.MinSaturation : 0;

    /// <summary>Größter Wert des Sättigungsreglers: bei einer gewählten Pastellfarbe 90 %, sonst 100.</summary>
    public double SaturationMaximum => IsPastelSelected ? AccentPalette.MaxSaturation : 100;

    /// <summary>Ob die Regler gerade eine Pastellfarbe bearbeiten.</summary>
    public bool IsPastelSelected => Target == AdjustTarget.Accent && PastelIndex >= 0;

    /// <summary>Kleinster Wert des Helligkeitsreglers.</summary>
    public double BrightnessMinimum => Target == AdjustTarget.Accent ? 0 : ThemeTint.MinBrightness;

    /// <summary>Größter Wert des Helligkeitsreglers.</summary>
    public double BrightnessMaximum => Target == AdjustTarget.Accent ? 100 : ThemeTint.MaxBrightness;

    /// <summary>Beschriftung des Sättigungsreglers.</summary>
    public string SaturationLabel => Loc.T(Target == AdjustTarget.Accent ? "Settings.Saturation" : "Settings.TintSaturation");

    /// <summary>Beschriftung des Zurücksetzen-Knopfs.</summary>
    public string ResetLabel => Loc.T("Settings.Reset");

    /// <summary>Erklärung zum Zurücksetzen.</summary>
    public string ResetToolTip => LastProfile(EditingDark) is { } last
        ? Loc.F("Settings.ResetTip", ColorProfileViewModel.DisplayNameOf(last))
        : Loc.T("Settings.ResetTipOpening");

    /// <summary>Hinweis neben dem Hex-Feld.</summary>
    public string HexHint => Loc.T(Target == AdjustTarget.Accent ? "Settings.AccentHex" : "Settings.TintHex");

    /// <summary>Farbkreis: wirksame Akzentfarbe bzw. Farbe des Farbstichs (grau ohne Stärke).</summary>
    public string PreviewHex => Target == AdjustTarget.Accent ? EffectiveAccent : CurrentTintHex;

    /// <summary>Eingabe als Hex-Code für das gewählte Ziel.</summary>
    [ObservableProperty]
    public partial string HexInput { get; set; }

    /// <summary>Fehler beim Hex-Code.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasHexError))]
    public partial string? HexError { get; set; }

    /// <summary>Ob es einen Fehler beim Hex-Code gibt.</summary>
    public bool HasHexError => HexError is not null;

    private int PastelIndex => AccentHex is null ? -1 : Pastel.ToList().FindIndex(swatch => swatch.Hex == AccentHex);

    private string EffectiveAccent => AccentHex ?? (EditingDark ? AccentPalette.DefaultAccentDark : AccentPalette.DefaultAccent);

    private string CurrentTintHex => ThemePalette.TintColor((int)Math.Round(TintHue), (int)Math.Round(TintSaturation));

    /// <summary>Wählt eine Akzentfarbe aus den Reihen; die Regler wechseln auf „Akzentfarbe“.</summary>
    /// <param name="swatch">Die Farbe.</param>
    [RelayCommand]
    public void ChooseSwatch(AccentSwatchViewModel swatch)
    {
        ArgumentNullException.ThrowIfNull(swatch);
        Target = AdjustTarget.Accent;
        ChooseAccent(swatch.Hex);
    }

    /// <summary>
    /// „Zurücksetzen“: stellt Akzentfarbe, Pastell-Sättigung, Farbstich und Helligkeit des zuletzt gewählten Profils
    /// dieses Designs wieder her; gab es keins, die Farben beim Öffnen des Dialogs (E155).
    /// </summary>
    [RelayCommand]
    public void ResetCurrent()
    {
        LoadColors(LastProfile(EditingDark)?.Colors ?? _original.ColorsFor(EditingDark));
        HexError = null;
        RebuildSwatches();
        SyncSliders();
        Preview();
    }

    /// <summary>
    /// Übernimmt den Hex-Code: als Akzentfarbe (und in „Eigene Farben“) bzw. als Farbstich, dessen Farbton und Stärke
    /// aus der Farbe kommen; die Helligkeit des Hintergrunds bleibt.
    /// </summary>
    [RelayCommand]
    public void ApplyHex()
    {
        if (!AccentPalette.IsValidHex(HexInput))
        {
            HexError = Loc.T("Settings.AccentInvalid");
            return;
        }

        var hex = AccentPalette.Normalize(HexInput);
        if (Target == AdjustTarget.Accent)
        {
            var updated = AccentPalette.AddCustomColor(Custom.Select(swatch => swatch.Hex).ToList(), hex, AccentPalette.DefaultSaturation);
            Custom.Clear();
            foreach (var color in updated)
            {
                Custom.Add(CustomSwatch(color));
            }

            OnPropertyChanged(nameof(HasCustomColors));
            ChooseAccent(hex);
            return;
        }

        var (hue, saturation) = ThemePalette.TintFromColor(hex);
        TintHue = hue;
        TintSaturation = saturation;
        HexError = null;
        SyncSliders();
        HexInput = hex;
        Preview();
    }

    /// <summary>Entfernt eine eigene Farbe; die gewählte Akzentfarbe bleibt.</summary>
    /// <param name="swatch">Die Farbe.</param>
    [RelayCommand]
    public void RemoveCustom(AccentSwatchViewModel swatch)
    {
        Custom.Remove(swatch);
        OnPropertyChanged(nameof(HasCustomColors));
    }

    partial void OnTargetChanged(AdjustTarget value)
    {
        HexError = null;
        SyncSliders();
    }

    partial void OnSliderHueChanged(double value)
    {
        OnPropertyChanged(nameof(HueInput));
        OnSliderChanged(saturationOnly: false);
    }

    partial void OnSliderSaturationChanged(double value)
    {
        OnPropertyChanged(nameof(SaturationInput));
        OnSliderChanged(saturationOnly: true);
    }

    partial void OnSliderBrightnessChanged(double value)
    {
        OnPropertyChanged(nameof(BrightnessInput));
        OnSliderChanged(saturationOnly: false);
    }

    /// <summary>
    /// Übernimmt eine Eingabe aus einem Zahlenfeld. Ein geleertes Feld liefert <c>null</c>; dann bleibt der bisherige
    /// Wert und das Feld zeigt ihn wieder an (E155).
    /// </summary>
    private void ApplyInput(decimal? value, double minimum, double maximum, Action<double> apply, string propertyName)
    {
        if (value is { } number)
        {
            apply(Math.Clamp(Math.Round((double)number), minimum, maximum));
        }

        OnPropertyChanged(propertyName);
    }

    /// <summary>
    /// Ein Regler wurde bewegt: Wert auf das gewählte Ziel übertragen, ohne die Regler zurückzusetzen. Bei einer
    /// gewählten Pastellfarbe verschiebt die Sättigung die ganze Pastell-Reihe; die Auswahl bleibt auf derselben
    /// Pastellfarbe, statt eine neue Einzelfarbe zu erzeugen (E155).
    /// </summary>
    private void OnSliderChanged(bool saturationOnly)
    {
        if (_syncing || !_initialized)
        {
            return;
        }

        if (saturationOnly && IsPastelSelected)
        {
            var index = PastelIndex;
            PastelSaturation = AccentPalette.ClampSaturation((int)Math.Round(SliderSaturation));
            Fill(Pastel, AccentPalette.Pastel(PastelSaturation));
            ChooseAccent(Pastel[index].Hex);
            return;
        }

        if (Target == AdjustTarget.Accent)
        {
            AccentHex = AccentPalette.FromHsv(SliderHue, SliderSaturation / 100, SliderBrightness / 100);
            RebuildSwatches();
        }
        else
        {
            TintHue = SliderHue;
            TintSaturation = SliderSaturation;
            Brightness = SliderBrightness;
        }

        HexError = null;
        HexInput = PreviewHex;
        OnPropertyChanged(nameof(PreviewHex));
        Preview();
    }

    private void ChooseAccent(string? hex)
    {
        AccentHex = hex;
        HexError = null;
        RebuildSwatches();
        SyncSliders();
        Preview();
    }

    /// <summary>Stellt Regler, Farbkreis und Hex-Feld auf die Werte des gewählten Ziels.</summary>
    private void SyncSliders()
    {
        _syncing = true;
        try
        {
            // Bereich zuerst melden, damit der Regler den neuen Wert nicht auf den alten Bereich beschneidet.
            OnPropertyChanged(nameof(IsPastelSelected));
            OnPropertyChanged(nameof(SaturationMinimum));
            OnPropertyChanged(nameof(SaturationMaximum));
            if (Target == AdjustTarget.Accent)
            {
                var (hue, saturation, value) = AccentPalette.ToHsv(EffectiveAccent);
                SliderHue = Math.Round(hue);
                SliderSaturation = IsPastelSelected ? PastelSaturation : Math.Round(saturation * 100);
                SliderBrightness = Math.Round(value * 100);
            }
            else
            {
                SliderHue = TintHue;
                SliderSaturation = TintSaturation;
                SliderBrightness = Brightness;
            }

            HexInput = PreviewHex;
            OnPropertyChanged(nameof(PreviewHex));
        }
        finally
        {
            _syncing = false;
        }
    }

    private void RebuildSwatches()
    {
        Fill(Pastel, AccentPalette.Pastel(PastelSaturation));
        Fill(Strong, AccentPalette.Strong);
        Fill(Neutral, AccentPalette.Neutral);
        foreach (var swatch in Pastel.Concat(Strong).Concat(Neutral).Concat(Custom))
        {
            swatch.IsSelected = swatch.Hex == AccentHex;
        }
    }

    private static void Fill(ObservableCollection<AccentSwatchViewModel> target, IEnumerable<ColorSwatch> palette)
    {
        target.Clear();
        foreach (var swatch in palette)
        {
            target.Add(new AccentSwatchViewModel(swatch.Hex, $"{Loc.T(swatch.NameKey)} {swatch.Hex}"));
        }
    }

    private static AccentSwatchViewModel CustomSwatch(string hex) => new(hex, $"{Loc.T("Settings.AccentCustomName")} {hex}", isCustom: true);
}
