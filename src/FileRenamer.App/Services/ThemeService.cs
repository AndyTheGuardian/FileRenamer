using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using FileRenamer.Core.Profiles;
using FileRenamer.Core.Theming;

namespace FileRenamer.App.Services;

/// <summary>
/// Setzt Erscheinungsbild (System, Hell, Dunkel) und Akzentfarbe der Anwendung. Die Akzentfarbe wird in die
/// Fluent-Ressourcen geschrieben (Schaltflächen, Checkboxen, Auswahl) und bei jedem Theme-Wechsel neu
/// berechnet, weil Auswahlhintergrund und Rahmen bei geringem Kontrast vom Hintergrund abhängen (E79–E85).
/// </summary>
public sealed class ThemeService : IThemeService
{
    private const string LightWindow = "#F3F4F6";
    private const string DarkWindow = "#1B1F27";
    private const string LightText = "#1D2330";
    private const string DarkText = "#E4E8EF";

    private readonly Application _application;
    private string _accentHex = AccentPalette.DefaultAccent;

    /// <summary>
    /// Erzeugt den Dienst und reagiert auf Theme-Wechsel (auch durch das Betriebssystem).
    /// </summary>
    /// <param name="application">Die Anwendung.</param>
    public ThemeService(Application application)
    {
        _application = application;
        _application.ActualThemeVariantChanged += (_, _) => UpdateAccentResources();
    }

    /// <summary>
    /// Wendet die gewünschte Theme-Variante an.
    /// </summary>
    /// <param name="theme">Die Auswahl.</param>
    public void Apply(ThemePreference theme)
    {
        _application.RequestedThemeVariant = theme switch
        {
            ThemePreference.Light => ThemeVariant.Light,
            ThemePreference.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
        UpdateAccentResources();
    }

    /// <summary>
    /// Wendet eine Akzentfarbe an.
    /// </summary>
    /// <param name="hex">Die Farbe oder <c>null</c> für den Standard.</param>
    public void ApplyAccent(string? hex)
    {
        _accentHex = hex ?? AccentPalette.DefaultAccent;
        UpdateAccentResources();
    }

    /// <summary>
    /// Schreibt alle von der Akzentfarbe abhängigen Ressourcen.
    /// </summary>
    private void UpdateAccentResources()
    {
        var isDark = _application.ActualThemeVariant == ThemeVariant.Dark;
        var window = isDark ? DarkWindow : LightWindow;
        var accent = Color.Parse(_accentHex);
        var foreground = new SolidColorBrush(Color.Parse(AccentPalette.ForegroundFor(_accentHex)));
        var needsOutline = AccentPalette.NeedsOutline(_accentHex, window);
        var textColor = Color.Parse(isDark ? DarkText : LightText);
        var outline = new SolidColorBrush(needsOutline ? textColor : Colors.Transparent);

        var resources = _application.Resources;
        resources["SystemAccentColor"] = accent;
        for (var step = 1; step <= 3; step++)
        {
            resources[$"SystemAccentColorLight{step}"] = Mix(accent, Colors.White, step * 0.15);
            resources[$"SystemAccentColorDark{step}"] = Mix(accent, Colors.Black, step * 0.15);
        }

        resources["SelectionBrush"] = new SolidColorBrush(Mix(Color.Parse(window), accent, 0.24));
        resources["AccentForegroundBrush"] = foreground;
        // Markierter Text: Hintergrund in Akzentfarbe, Schrift je nach Helligkeit hell oder dunkel (E144)
        resources["TextControlSelectionHighlightColor"] = new SolidColorBrush(accent);
        resources["TextSelectionForegroundBrush"] = foreground;
        resources["AccentOutlineBrush"] = outline;
        resources["AccentSegmentBorderBrush"] = new SolidColorBrush(needsOutline ? textColor : accent);
        foreach (var state in new[] { string.Empty, "PointerOver", "Pressed" })
        {
            resources["AccentButtonForeground" + state] = foreground;
            resources["AccentButtonBorderBrush" + state] = outline;
            resources["CheckBoxCheckGlyphForegroundChecked" + state] = foreground;
        }
    }

    /// <summary>
    /// Mischt zwei Farben.
    /// </summary>
    /// <param name="from">Ausgangsfarbe.</param>
    /// <param name="to">Zielfarbe.</param>
    /// <param name="amount">Anteil der Zielfarbe (0–1).</param>
    /// <returns>Die Mischfarbe.</returns>
    private static Color Mix(Color from, Color to, double amount) => Color.FromRgb(
        (byte)Math.Round(from.R + (to.R - from.R) * amount),
        (byte)Math.Round(from.G + (to.G - from.G) * amount),
        (byte)Math.Round(from.B + (to.B - from.B) * amount));
}
