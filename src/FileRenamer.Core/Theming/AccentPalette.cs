using System.Globalization;
using System.Text.RegularExpressions;

namespace FileRenamer.Core.Theming;

/// <summary>
/// Eine wählbare Farbe der Akzentauswahl.
/// </summary>
/// <param name="Hex">Farbwert im Format <c>#RRGGBB</c>.</param>
/// <param name="Name">Anzeigename.</param>
public sealed record AccentSwatch(string Hex, string Name);

/// <summary>
/// Farben und Farbberechnungen der Akzentfarbe (E79–E89): Paletten, HSV-Sättigung der Pastellfarben,
/// lesbare Schriftfarbe, Kontrastprüfung und die Liste benutzerdefinierter Farben.
/// </summary>
public static partial class AccentPalette
{
    /// <summary>Standard-Akzentfarbe (bisheriges Blau).</summary>
    public const string DefaultAccent = "#2F6FDF";

    /// <summary>Kleinste einstellbare Pastell-Sättigung in Prozent.</summary>
    public const int MinSaturation = 10;

    /// <summary>Größte einstellbare Pastell-Sättigung in Prozent.</summary>
    public const int MaxSaturation = 90;

    /// <summary>Standard-Sättigung der Pastellfarben in Prozent (E89).</summary>
    public const int DefaultSaturation = 30;

    /// <summary>Höchstzahl benutzerdefinierter Farben (E87).</summary>
    public const int MaxCustomColors = 10;

    /// <summary>Unterhalb dieses Kontrastverhältnisses erhalten Akzentflächen einen Rahmen (E85).</summary>
    public const double MinContrastRatio = 1.6;

    private const string DarkText = "#1D2330";
    private const string LightText = "#F4F7FC";

    /// <summary>
    /// Grundfarben der Pastell-Reihe (Farbton und Hellwert bleiben, die Sättigung wird eingestellt).
    /// </summary>
    public static IReadOnlyList<AccentSwatch> PastelBase =>
    [
        new("#A7C7E7", Name("Pastellblau", "Pastel blue")), new("#B5EAD7", "Mint"), new("#FFDAC1", Name("Pfirsich", "Peach")),
        new("#E2B6CF", Name("Rosé", "Rose")), new("#C7CEEA", Name("Lavendel", "Lavender")),
    ];

    /// <summary>
    /// Kräftige Farben.
    /// </summary>
    public static IReadOnlyList<AccentSwatch> Strong =>
    [
        new(DefaultAccent, Name("Blau (Standard)", "Blue (default)")), new("#1F9D55", Name("Grün", "Green")), new("#E8740C", "Orange"),
        new("#C93434", Name("Rot", "Red")), new("#8B4FD6", Name("Violett", "Violet")),
    ];

    /// <summary>
    /// Neutrale Farben (95 % Schwarz, 95 % Weiß).
    /// </summary>
    public static IReadOnlyList<AccentSwatch> Neutral =>
        [new("#0D0D0D", Name("95 % Schwarz", "95 % black")), new("#F2F2F2", Name("95 % Weiß", "95 % white"))];

    /// <summary>
    /// Farbname in der aktuellen Sprache.
    /// </summary>
    /// <param name="german">Deutscher Name.</param>
    /// <param name="english">Englischer Name.</param>
    /// <returns>Der Name.</returns>
    private static string Name(string german, string english) =>
        Localization.CoreText.Language == Localization.UiLanguage.German ? german : english;

    /// <summary>
    /// Liefert die Pastellfarben für eine Sättigung.
    /// </summary>
    /// <param name="saturationPercent">Sättigung in Prozent (wird auf 10–90 begrenzt).</param>
    /// <returns>Die Pastellfarben.</returns>
    public static IReadOnlyList<AccentSwatch> Pastel(int saturationPercent) =>
        PastelBase.Select(swatch => swatch with { Hex = WithHsvSaturation(swatch.Hex, ClampSaturation(saturationPercent)) }).ToList();

    /// <summary>
    /// Begrenzt eine Sättigung auf den erlaubten Bereich.
    /// </summary>
    /// <param name="saturationPercent">Sättigung in Prozent.</param>
    /// <returns>Sättigung zwischen 10 und 90.</returns>
    public static int ClampSaturation(int saturationPercent) => Math.Clamp(saturationPercent, MinSaturation, MaxSaturation);

    /// <summary>
    /// Setzt die Sättigung nach dem HSV-Prinzip (S = (max − min) / max; 0 % = 0, 100 % = 255).
    /// Farbton und Hellwert V (größter Kanal) bleiben erhalten.
    /// </summary>
    /// <param name="hex">Ausgangsfarbe.</param>
    /// <param name="saturationPercent">Gewünschte Sättigung in Prozent.</param>
    /// <returns>Die neue Farbe.</returns>
    public static string WithHsvSaturation(string hex, int saturationPercent)
    {
        var rgb = ToRgb(hex);
        double max = rgb.Max(), min = rgb.Min();
        var newMin = max * (1 - saturationPercent / 100.0);
        var scaled = rgb.Select(channel => max == min ? max : newMin + (channel - min) / (max - min) * (max - newMin));
        return ToHex(scaled.Select(value => (int)Math.Round(value, MidpointRounding.AwayFromZero)).ToArray());
    }

    /// <summary>
    /// Prüft das Format <c>#RRGGBB</c>.
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <returns><c>true</c>, wenn gültig.</returns>
    public static bool IsValidHex(string? text) => text is not null && HexPattern().IsMatch(text.Trim());

    /// <summary>
    /// Vereinheitlicht einen gültigen Hex-Code auf Großbuchstaben.
    /// </summary>
    /// <param name="hex">Der Hex-Code.</param>
    /// <returns>Der vereinheitlichte Hex-Code.</returns>
    public static string Normalize(string hex) => hex.Trim().ToUpperInvariant();

    /// <summary>
    /// Wählt dunkle oder helle Schrift, damit Text auf der Akzentfarbe lesbar bleibt (E80).
    /// </summary>
    /// <param name="hex">Die Akzentfarbe.</param>
    /// <returns>Die Schriftfarbe.</returns>
    public static string ForegroundFor(string hex) => RelativeLuminance(hex) > 0.45 ? DarkText : LightText;

    /// <summary>
    /// Prüft, ob sich die Akzentfarbe zu wenig vom Hintergrund abhebt und einen Rahmen braucht (E85).
    /// </summary>
    /// <param name="accentHex">Die Akzentfarbe.</param>
    /// <param name="backgroundHex">Die Hintergrundfarbe.</param>
    /// <returns><c>true</c> bei zu geringem Kontrast.</returns>
    public static bool NeedsOutline(string accentHex, string backgroundHex) =>
        ContrastRatio(accentHex, backgroundHex) < MinContrastRatio;

    /// <summary>
    /// Kontrastverhältnis zweier Farben nach WCAG (1 bis 21).
    /// </summary>
    /// <param name="firstHex">Erste Farbe.</param>
    /// <param name="secondHex">Zweite Farbe.</param>
    /// <returns>Das Kontrastverhältnis.</returns>
    public static double ContrastRatio(string firstHex, string secondHex)
    {
        var first = RelativeLuminance(firstHex);
        var second = RelativeLuminance(secondHex);
        return (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
    }

    /// <summary>
    /// Nimmt eine eigene Farbe in die benutzerdefinierte Liste auf: keine Farben aus den festen Reihen,
    /// keine Duplikate, höchstens 10 – bei mehr fällt die älteste weg (E87).
    /// </summary>
    /// <param name="customColors">Bisherige Liste.</param>
    /// <param name="hex">Neue Farbe.</param>
    /// <param name="saturationPercent">Aktuelle Pastell-Sättigung (für den Vergleich mit der Pastell-Reihe).</param>
    /// <returns>Die neue Liste.</returns>
    public static IReadOnlyList<string> AddCustomColor(IReadOnlyList<string> customColors, string hex, int saturationPercent)
    {
        var color = Normalize(hex);
        var fixedColors = Pastel(saturationPercent).Concat(Strong).Concat(Neutral).Select(swatch => swatch.Hex);
        if (fixedColors.Contains(color) || customColors.Contains(color))
        {
            return customColors;
        }

        return customColors.Append(color).TakeLast(MaxCustomColors).ToList();
    }

    /// <summary>
    /// Relative Leuchtdichte nach WCAG.
    /// </summary>
    /// <param name="hex">Die Farbe.</param>
    /// <returns>Leuchtdichte zwischen 0 und 1.</returns>
    private static double RelativeLuminance(string hex)
    {
        var channels = ToRgb(hex).Select(value => value / 255.0)
            .Select(c => c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4)).ToArray();
        return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2];
    }

    /// <summary>
    /// Zerlegt einen Hex-Code in Rot, Grün, Blau.
    /// </summary>
    /// <param name="hex">Der Hex-Code.</param>
    /// <returns>Die Kanäle (0–255).</returns>
    private static int[] ToRgb(string hex)
    {
        var value = hex.Trim().TrimStart('#');
        return [.. Enumerable.Range(0, 3).Select(i => int.Parse(value.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture))];
    }

    /// <summary>
    /// Setzt einen Hex-Code aus Kanälen zusammen.
    /// </summary>
    /// <param name="rgb">Rot, Grün, Blau (0–255).</param>
    /// <returns>Der Hex-Code.</returns>
    private static string ToHex(int[] rgb) => "#" + string.Concat(rgb.Select(c => Math.Clamp(c, 0, 255).ToString("X2", CultureInfo.InvariantCulture)));

    /// <summary>
    /// Format <c>#RRGGBB</c>.
    /// </summary>
    /// <returns>Der reguläre Ausdruck.</returns>
    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexPattern();
}
