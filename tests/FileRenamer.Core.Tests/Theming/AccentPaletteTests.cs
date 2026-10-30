using FileRenamer.Core.Theming;

namespace FileRenamer.Core.Tests.Theming;

/// <summary>
/// Tests der Akzentfarben-Logik (E79–E89).
/// </summary>
public sealed class AccentPaletteTests
{
    /// <summary>
    /// HSV-Sättigung: Werte wie im freigegebenen Mockup v10; Farbton und Hellwert bleiben.
    /// </summary>
    /// <param name="saturation">Sättigung in Prozent.</param>
    /// <param name="expected">Erwartete Pastellfarben.</param>
    [Theory]
    [InlineData(30, new[] { "#A2C4E7", "#A4EAD1", "#FFD1B3", "#E29EC5", "#A4B2EA" })]
    [InlineData(10, new[] { "#D0DBE7", "#D3EAE2", "#FFF0E6", "#E2CBD8", "#D3D7EA" })]
    [InlineData(90, new[] { "#177FE7", "#17EA9F", "#FF7619", "#E2178A", "#1742EA" })]
    public void Pastel_UsesAbsoluteHsvSaturation(int saturation, string[] expected)
    {
        Assert.Equal(expected, AccentPalette.Pastel(saturation).Select(swatch => swatch.Hex));
    }

    /// <summary>
    /// Die Sättigung wird auf 10–90 % begrenzt.
    /// </summary>
    [Fact]
    public void Saturation_IsClamped()
    {
        Assert.Equal(AccentPalette.Pastel(10).Select(s => s.Hex), AccentPalette.Pastel(0).Select(s => s.Hex));
        Assert.Equal(AccentPalette.Pastel(90).Select(s => s.Hex), AccentPalette.Pastel(150).Select(s => s.Hex));
    }

    /// <summary>
    /// Helle Akzentfarben erhalten dunkle Schrift, dunkle helle Schrift (E80).
    /// </summary>
    [Fact]
    public void ForegroundFor_ChoosesReadableText()
    {
        Assert.Equal("#1D2330", AccentPalette.ForegroundFor("#A4EAD1"));
        Assert.Equal("#F4F7FC", AccentPalette.ForegroundFor(AccentPalette.DefaultAccent));
        Assert.Equal("#F4F7FC", AccentPalette.ForegroundFor("#0D0D0D"));
    }

    /// <summary>
    /// 95 % Weiß im hellen bzw. 95 % Schwarz im dunklen Theme brauchen einen Rahmen (E85).
    /// </summary>
    [Fact]
    public void NeedsOutline_ForLowContrast()
    {
        Assert.True(AccentPalette.NeedsOutline("#F2F2F2", "#F3F4F6"));
        Assert.True(AccentPalette.NeedsOutline("#0D0D0D", "#1B1F27"));
        Assert.False(AccentPalette.NeedsOutline(AccentPalette.DefaultAccent, "#F3F4F6"));
        Assert.False(AccentPalette.NeedsOutline("#0D0D0D", "#F3F4F6"));
    }

    /// <summary>
    /// Hex-Prüfung im Format #RRGGBB.
    /// </summary>
    [Fact]
    public void IsValidHex()
    {
        Assert.True(AccentPalette.IsValidHex("#25A0C3"));
        Assert.True(AccentPalette.IsValidHex("#25a0c3"));
        Assert.False(AccentPalette.IsValidHex("25A0C3"));
        Assert.False(AccentPalette.IsValidHex("#25A0C"));
        Assert.False(AccentPalette.IsValidHex(null));
    }

    /// <summary>
    /// Benutzerdefinierte Farben: keine Duplikate, keine festen Farben, höchstens 10 (älteste fällt weg) (E87).
    /// </summary>
    [Fact]
    public void AddCustomColor_RulesFromE87()
    {
        IReadOnlyList<string> colors = [];
        colors = AccentPalette.AddCustomColor(colors, "#25a0c3", 30);
        colors = AccentPalette.AddCustomColor(colors, "#25A0C3", 30);
        colors = AccentPalette.AddCustomColor(colors, AccentPalette.DefaultAccent, 30);
        colors = AccentPalette.AddCustomColor(colors, "#A2C4E7", 30);

        Assert.Equal(["#25A0C3"], colors);

        for (var i = 0; i < 10; i++)
        {
            colors = AccentPalette.AddCustomColor(colors, $"#10{i:D2}00", 30);
        }

        Assert.Equal(10, colors.Count);
        Assert.Equal("#100000", colors[0]);
    }
}
