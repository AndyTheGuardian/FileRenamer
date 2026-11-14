using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using FileRenamer.Core.Profiles;
using FileRenamer.Core.Theming;

namespace FileRenamer.App.Services;

/// <summary>
/// Setzt Erscheinungsbild (System, Hell, Dunkel) und die Farben des geltenden Designs (E155): Flächen, Rahmen und
/// Nebentext aus <see cref="ThemePalette"/> mit Farbstich und Helligkeit, dazu die Akzentfarbe in den
/// Fluent-Ressourcen (Schaltflächen, Checkboxen, Auswahl). Hell und Dunkel haben je eigene Farben; bei jedem Wechsel
/// des Designs (auch durch das Betriebssystem) wird neu berechnet.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private const double SelectionAmount = 0.24;
    private const double TextContrastRatio = 4.5;

    private readonly Application _application;
    private ThemeColorSettings _light = new();
    private ThemeColorSettings _dark = new();

    /// <summary>
    /// Verbindet den Dienst mit der Anwendung und setzt die Standardfarben.
    /// </summary>
    /// <param name="application">Die Anwendung.</param>
    public ThemeService(Application application)
    {
        _application = application;
        _application.ActualThemeVariantChanged += (_, _) => UpdateResources();
        UpdateResources();
    }

    /// <inheritdoc />
    public bool IsDark => _application.ActualThemeVariant == ThemeVariant.Dark;

    /// <inheritdoc />
    public void Apply(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _light = settings.ColorsFor(dark: false);
        _dark = settings.ColorsFor(dark: true);
        var variant = settings.Theme switch
        {
            ThemePreference.Light => ThemeVariant.Light,
            ThemePreference.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
        if (_application.RequestedThemeVariant != variant)
        {
            _application.RequestedThemeVariant = variant;
        }

        UpdateResources();
    }

    /// <summary>
    /// Schreibt die Farben des angezeigten Designs in die Ressourcen der Anwendung.
    /// </summary>
    private void UpdateResources()
    {
        var dark = IsDark;
        var settings = dark ? _dark : _light;
        var colors = ThemePalette.For(dark, settings.Tint);
        SetSurfaces(colors);
        SetAccent(settings.AccentColor ?? (dark ? AccentPalette.DefaultAccentDark : AccentPalette.DefaultAccent), colors);
    }

    /// <summary>
    /// Flächen, Rahmen und Nebentext, auch für Eingabefelder, Auswahllisten und Menüs der Fluent-Steuerelemente.
    /// </summary>
    /// <param name="colors">Die Farben des Designs.</param>
    private void SetSurfaces(ThemeColors colors)
    {
        Set("AppBackgroundBrush", colors.AppBackground);
        Set("PanelBackgroundBrush", colors.Panel);
        Set("InputBackgroundBrush", colors.Input);
        Set("AppBorderBrush", colors.Border);
        Set("MutedTextBrush", colors.MutedText);
        Set("DropHintBrush", colors.DropHint);
        Set("DialogBackgroundBrush", colors.DialogBackground);
        Set("DialogBorderBrush", colors.DialogBorder);
        Set("TextControlBackground", colors.Input);
        Set("TextControlBackgroundPointerOver", colors.Hover);
        Set("TextControlBackgroundFocused", colors.Input);
        Set("ComboBoxBackground", colors.Input);
        Set("ComboBoxBackgroundPointerOver", colors.Hover);
        Set("ComboBoxDropDownBackground", colors.AppBackground);
        Set("MenuFlyoutPresenterBackground", colors.AppBackground);
        Set("FlyoutPresenterBackground", colors.AppBackground);
        // Einheitliche 1-px-Rahmen (E142)
        foreach (var key in new[] { "TextControlBorderBrush", "ComboBoxBorderBrush", "ButtonBorderBrush" })
        {
            foreach (var state in new[] { string.Empty, "PointerOver", "Pressed", "Disabled" })
            {
                Set(key + state, colors.Border);
            }
        }
    }

    /// <summary>
    /// Akzentfarbe mit lesbarer Schrift, Auswahlhintergrund und – bei zu geringem Kontrast – einem satteren Rahmen
    /// (E79–E85, E144).
    /// </summary>
    /// <param name="accentHex">Die Akzentfarbe.</param>
    /// <param name="colors">Die Farben des Designs.</param>
    private void SetAccent(string accentHex, ThemeColors colors)
    {
        var needsOutline = AccentPalette.NeedsOutline(accentHex, colors.AppBackground);
        var outlineHex = needsOutline ? AccentPalette.OutlineColor(accentHex, colors.AppBackground, IsDark) ?? colors.Text : null;
        var foreground = AccentPalette.ForegroundFor(accentHex);
        var resources = _application.Resources;
        var accent = Color.Parse(accentHex);
        resources["SystemAccentColor"] = accent;
        for (var step = 1; step <= 3; step++)
        {
            resources[$"SystemAccentColorLight{step}"] = Color.Parse(AccentPalette.Mix(accentHex, "#FFFFFF", step * 0.15));
            resources[$"SystemAccentColorDark{step}"] = Color.Parse(AccentPalette.Mix(accentHex, "#000000", step * 0.15));
        }

        Set("SelectionBrush", AccentPalette.Mix(colors.AppBackground, accentHex, SelectionAmount));
        Set("AccentForegroundBrush", foreground);
        // Schrift in Akzentfarbe (Abschnittstitel der Einstellungen) nur, wenn sie lesbar ist (4,5 : 1)
        Set("AccentTextBrush", AccentPalette.ContrastRatio(accentHex, colors.AppBackground) >= TextContrastRatio ? accentHex : colors.Text);
        // Markierter Text: Hintergrund in Akzentfarbe, Schrift je nach Helligkeit hell oder dunkel (E144)
        Set("TextControlSelectionHighlightColor", accentHex);
        Set("TextSelectionForegroundBrush", foreground);
        resources["AccentOutlineBrush"] = outlineHex is null ? Brushes.Transparent : Brush(outlineHex);
        Set("AccentSegmentBorderBrush", outlineHex ?? accentHex);
        foreach (var state in new[] { string.Empty, "PointerOver", "Pressed" })
        {
            Set("AccentButtonForeground" + state, foreground);
            resources["AccentButtonBorderBrush" + state] = outlineHex is null ? Brushes.Transparent : Brush(outlineHex);
            Set("CheckBoxCheckGlyphForegroundChecked" + state, foreground);
        }
    }

    /// <summary>
    /// Setzt eine Farbressource.
    /// </summary>
    /// <param name="key">Schlüssel.</param>
    /// <param name="hex">Farbe als <c>#RRGGBB</c>.</param>
    private void Set(string key, string hex) => _application.Resources[key] = Brush(hex);

    /// <summary>
    /// Erzeugt einen einfarbigen Pinsel.
    /// </summary>
    /// <param name="hex">Farbe als <c>#RRGGBB</c>.</param>
    /// <returns>Der Pinsel.</returns>
    private static SolidColorBrush Brush(string hex) => new(Color.Parse(hex));
}
