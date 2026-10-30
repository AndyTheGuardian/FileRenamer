using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileRenamer.App.Localization;
using FileRenamer.Core.Theming;

namespace FileRenamer.App.ViewModels;

/// <summary>
/// Eine Farbfläche der Akzentauswahl.
/// </summary>
/// <param name="hex">Farbwert <c>#RRGGBB</c>.</param>
/// <param name="name">Anzeigename (Tooltip).</param>
/// <param name="isCustom">Ob die Farbe benutzerdefiniert (entfernbar) ist.</param>
public sealed partial class SwatchViewModel(string hex, string name, bool isCustom = false) : ObservableObject
{
    /// <summary>Farbwert.</summary>
    public string Hex { get; } = hex;

    /// <summary>Tooltip mit Name und Farbwert.</summary>
    public string ToolTip { get; } = $"{name} {hex}";

    /// <summary>Ob die Farbe entfernt werden kann.</summary>
    public bool IsCustom { get; } = isCustom;

    /// <summary>Ob die Farbe die aktuelle Akzentfarbe ist.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>
/// ViewModel der Akzentfarbwahl (E79–E89): Pastellfarben mit einstellbarer HSV-Sättigung, kräftige und neutrale
/// Farben, benutzerdefinierte Farben, eigener Hex-Code und „Standard“. Jede Änderung wird gemeldet,
/// damit das Hauptfenster-ViewModel sie anwendet und speichert.
/// </summary>
public sealed partial class AccentColorViewModel : ObservableObject
{
    /// <summary>
    /// Erzeugt die Farbwahl mit gespeicherten Werten.
    /// </summary>
    /// <param name="accentHex">Gespeicherte Akzentfarbe oder <c>null</c> für den Standard.</param>
    /// <param name="saturation">Gespeicherte Pastell-Sättigung.</param>
    /// <param name="customColors">Gespeicherte benutzerdefinierte Farben.</param>
    public AccentColorViewModel(string? accentHex, int saturation, IEnumerable<string> customColors)
    {
        AccentHex = accentHex is not null && AccentPalette.IsValidHex(accentHex) ? AccentPalette.Normalize(accentHex) : null;
        Saturation = AccentPalette.ClampSaturation(saturation);
        Strong = Create(AccentPalette.Strong);
        Neutral = Create(AccentPalette.Neutral);
        foreach (var hex in customColors.Where(AccentPalette.IsValidHex).Select(AccentPalette.Normalize).Distinct().TakeLast(AccentPalette.MaxCustomColors))
        {
            Custom.Add(new SwatchViewModel(hex, Loc.T("Accent.CustomName"), isCustom: true));
        }

        RebuildPastel();
        HexInput = CurrentHex;
        MarkSelection();
    }

    /// <summary>
    /// Wird nach jeder Änderung von Akzentfarbe, Sättigung oder benutzerdefinierten Farben ausgelöst.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>Pastellfarben bei der aktuellen Sättigung.</summary>
    public ObservableCollection<SwatchViewModel> Pastel { get; } = [];

    /// <summary>Kräftige Farben.</summary>
    public IReadOnlyList<SwatchViewModel> Strong { get; }

    /// <summary>Neutrale Farben.</summary>
    public IReadOnlyList<SwatchViewModel> Neutral { get; }

    /// <summary>Benutzerdefinierte Farben.</summary>
    public ObservableCollection<SwatchViewModel> Custom { get; } = [];

    /// <summary>Ob benutzerdefinierte Farben vorhanden sind.</summary>
    public bool HasCustomColors => Custom.Count > 0;

    /// <summary>Gewählte Akzentfarbe oder <c>null</c> für den Standard.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentHex))]
    public partial string? AccentHex { get; set; }

    /// <summary>Wirksame Akzentfarbe.</summary>
    public string CurrentHex => AccentHex ?? AccentPalette.DefaultAccent;

    /// <summary>Pastell-Sättigung in Prozent (10–90).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SliderValue), nameof(SaturationInput), nameof(SaturationByteText))]
    public partial int Saturation { get; set; }

    /// <summary>Wert für den Schieberegler.</summary>
    public double SliderValue
    {
        get => Saturation;
        set => SetSaturation((int)Math.Round(value));
    }

    /// <summary>Wert für das Zahlenfeld.</summary>
    public decimal? SaturationInput
    {
        get => Saturation;
        set => SetSaturation(value is { } number ? (int)Math.Round(number) : AccentPalette.DefaultSaturation);
    }

    /// <summary>Sättigung auf der Skala 0–255 (HSV-Prinzip, E86).</summary>
    public string SaturationByteText => $"≙ {(int)Math.Round(Saturation * 255 / 100.0, MidpointRounding.AwayFromZero)}";

    /// <summary>Eingabefeld für einen eigenen Hex-Code.</summary>
    [ObservableProperty]
    public partial string HexInput { get; set; } = string.Empty;

    /// <summary>Fehlermeldung zum Hex-Code oder <c>null</c>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasHexError))]
    public partial string? HexError { get; set; }

    /// <summary>Ob eine Fehlermeldung angezeigt wird.</summary>
    public bool HasHexError => HexError is not null;

    /// <summary>
    /// Übernimmt eine Farbfläche als Akzentfarbe.
    /// </summary>
    /// <param name="swatch">Die Farbfläche.</param>
    [RelayCommand]
    private void Select(SwatchViewModel swatch) => SetAccent(swatch.Hex);

    /// <summary>
    /// Übernimmt den eingegebenen Hex-Code und nimmt ihn in „Benutzerdefiniert“ auf (E87).
    /// </summary>
    [RelayCommand]
    private void ApplyHex()
    {
        if (!AccentPalette.IsValidHex(HexInput))
        {
            HexError = Loc.T("Accent.HexInvalid");
            return;
        }

        var hex = AccentPalette.Normalize(HexInput);
        var updated = AccentPalette.AddCustomColor(Custom.Select(swatch => swatch.Hex).ToList(), hex, Saturation);
        Custom.Clear();
        foreach (var color in updated)
        {
            Custom.Add(new SwatchViewModel(color, Loc.T("Accent.CustomName"), isCustom: true));
        }

        OnPropertyChanged(nameof(HasCustomColors));
        SetAccent(hex);
    }

    /// <summary>
    /// Entfernt eine benutzerdefinierte Farbe.
    /// </summary>
    /// <param name="swatch">Die Farbfläche.</param>
    [RelayCommand]
    private void Remove(SwatchViewModel swatch)
    {
        Custom.Remove(swatch);
        OnPropertyChanged(nameof(HasCustomColors));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Setzt die Akzentfarbe auf den Standard zurück (E81).
    /// </summary>
    [RelayCommand]
    private void Reset() => SetAccent(null);

    /// <summary>
    /// Setzt die Akzentfarbe, aktualisiert Auswahl und Eingabefeld und meldet die Änderung.
    /// </summary>
    /// <param name="hex">Die Farbe oder <c>null</c> für den Standard.</param>
    private void SetAccent(string? hex)
    {
        AccentHex = hex;
        HexInput = CurrentHex;
        HexError = null;
        MarkSelection();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Ändert die Sättigung; eine gewählte Pastellfarbe passt sich sofort an.
    /// </summary>
    /// <param name="value">Neue Sättigung.</param>
    private void SetSaturation(int value)
    {
        var clamped = AccentPalette.ClampSaturation(value);
        if (clamped == Saturation)
        {
            OnPropertyChanged(nameof(SaturationInput));
            return;
        }

        var pastelIndex = Pastel.ToList().FindIndex(swatch => swatch.Hex == AccentHex);
        Saturation = clamped;
        RebuildPastel();
        if (pastelIndex >= 0)
        {
            SetAccent(Pastel[pastelIndex].Hex);
        }
        else
        {
            MarkSelection();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Berechnet die Pastellfarben für die aktuelle Sättigung neu.
    /// </summary>
    private void RebuildPastel()
    {
        Pastel.Clear();
        foreach (var swatch in Create(AccentPalette.Pastel(Saturation)))
        {
            Pastel.Add(swatch);
        }
    }

    /// <summary>
    /// Markiert die Farbfläche der aktuellen Akzentfarbe.
    /// </summary>
    private void MarkSelection()
    {
        foreach (var swatch in Pastel.Concat(Strong).Concat(Neutral).Concat(Custom))
        {
            swatch.IsSelected = swatch.Hex == CurrentHex;
        }
    }

    /// <summary>
    /// Erzeugt Farbflächen aus einer Palette.
    /// </summary>
    /// <param name="palette">Die Palette.</param>
    /// <returns>Die Farbflächen.</returns>
    private static List<SwatchViewModel> Create(IEnumerable<AccentSwatch> palette) =>
        palette.Select(swatch => new SwatchViewModel(swatch.Hex, swatch.Name)).ToList();
}
