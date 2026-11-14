using FileRenamer.Core.Profiles;
using FileRenamer.Core.Theming;

namespace FileRenamer.Core.Tests.Theming;

/// <summary>
/// Tests der Farben je Design und der Übernahme früherer Einstellungen (E155).
/// </summary>
public sealed class ThemeColorSettingsTests
{
    /// <summary>
    /// Die frühere gemeinsame Akzentfarbe gilt für beide Designs, solange keine eigenen Farben gespeichert sind.
    /// </summary>
    [Fact]
    public void ColorsFor_OldAccentColor_AppliesToBothThemes()
    {
        var settings = new AppSettings { AccentColor = "#2f6fdf", PastelSaturation = 50 };

        Assert.Equal("#2F6FDF", settings.ColorsFor(dark: false).AccentColor);
        Assert.Equal("#2F6FDF", settings.ColorsFor(dark: true).AccentColor);
        Assert.Equal(50, settings.ColorsFor(dark: false).PastelSaturation);
        Assert.Equal(0, settings.ColorsFor(dark: true).TintSaturation);
    }

    /// <summary>
    /// Eine neue Installation beginnt mit dem Profil „Standard“.
    /// </summary>
    [Fact]
    public void ColorsFor_NewInstallation_MatchesStandardProfile()
    {
        var settings = new AppSettings();

        Assert.True(BuiltInColorProfiles.Light[0].Matches(settings.ColorsFor(dark: false)));
        Assert.True(BuiltInColorProfiles.Dark[0].Matches(settings.ColorsFor(dark: true)));
    }

    /// <summary>
    /// Gespeicherte Farben eines Designs haben Vorrang; ungültige Werte werden bereinigt.
    /// </summary>
    [Fact]
    public void ColorsFor_KeepsSavedThemeColors_AndCleansInvalidValues()
    {
        var settings = new AppSettings
        {
            AccentColor = "#123456",
            LightColors = new() { AccentColor = "#ABCDEF", TintHue = 400, TintSaturation = 120, Brightness = 99 },
            DarkColors = new() { AccentColor = "kaputt" },
        };

        var light = settings.ColorsFor(dark: false);
        Assert.Equal("#ABCDEF", light.AccentColor);
        Assert.Equal((40, 100, ThemeTint.MaxBrightness), (light.TintHue, light.TintSaturation, light.Brightness));
        Assert.Null(settings.ColorsFor(dark: true).AccentColor);
    }

    /// <summary>
    /// Farben beider Designs und eigene Profile überstehen Speichern und Laden.
    /// </summary>
    [Fact]
    public void SettingsStore_RoundTrip_KeepsThemeColorsAndProfiles()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fr-colors-{Guid.NewGuid():N}.json");
        try
        {
            var store = new AppSettingsStore(path);
            var profile = new ColorProfile("Mein Profil", true, new ThemeColorSettings { AccentColor = "#C58BD8", TintHue = 280, TintSaturation = 30, Brightness = 2 });
            store.Save(new AppSettings { ColorProfiles = [profile], LightColors = new ThemeColorSettings { TintSaturation = 40, TintHue = 100 } });

            var loaded = store.Load();

            Assert.Equal((100, 40), (loaded.ColorsFor(false).TintHue, loaded.ColorsFor(false).TintSaturation));
            Assert.True(Assert.Single(loaded.ColorProfiles).Matches(profile.Colors));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
