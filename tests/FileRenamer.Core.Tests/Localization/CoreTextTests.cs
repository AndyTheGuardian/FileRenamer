using System.Globalization;
using FileRenamer.Core.Localization;
using FileRenamer.Core.Profiles;
using FileRenamer.Core.Validation;

// Die Sprache der Meldungen ist programmweit; parallel laufende Tests mit deutschen Erwartungen
// dürfen den kurzzeitigen Wechsel auf Englisch nicht sehen.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace FileRenamer.Core.Tests.Localization;

/// <summary>
/// Tests der Sprachwahl für die Meldungen (E92, E93). Jeder Test stellt Deutsch wieder her,
/// damit andere Tests mit deutschen Erwartungen nicht beeinflusst werden.
/// </summary>
public sealed class CoreTextTests : IDisposable
{
    /// <summary>
    /// „System“ ergibt Deutsch nur bei deutscher Systemsprache, sonst Englisch (E93).
    /// </summary>
    /// <param name="culture">Die Systemkultur.</param>
    /// <param name="expected">Erwartete Sprache.</param>
    [Theory]
    [InlineData("de-DE", UiLanguage.German)]
    [InlineData("de-AT", UiLanguage.German)]
    [InlineData("en-US", UiLanguage.English)]
    [InlineData("fr-FR", UiLanguage.English)]
    public void Resolve_System_FollowsCulture(string culture, UiLanguage expected) =>
        Assert.Equal(expected, CoreText.Resolve(LanguagePreference.System, new CultureInfo(culture)));

    /// <summary>
    /// Eine feste Auswahl gilt unabhängig von der Systemsprache.
    /// </summary>
    [Fact]
    public void Resolve_FixedChoice_IgnoresCulture()
    {
        Assert.Equal(UiLanguage.English, CoreText.Resolve(LanguagePreference.English, new CultureInfo("de-DE")));
        Assert.Equal(UiLanguage.German, CoreText.Resolve(LanguagePreference.German, new CultureInfo("en-US")));
    }

    /// <summary>
    /// Meldungen erscheinen in der eingestellten Sprache.
    /// </summary>
    [Fact]
    public void Messages_FollowLanguage()
    {
        CoreText.Language = UiLanguage.English;
        Assert.Equal("The file name is empty.", FileNameValidator.Validate(string.Empty));
        Assert.Equal("Unknown", CoreText.Unknown);

        CoreText.Language = UiLanguage.German;
        Assert.Equal("Der Dateiname ist leer.", FileNameValidator.Validate(string.Empty));
    }

    /// <summary>
    /// Stellt Deutsch wieder her.
    /// </summary>
    public void Dispose() => CoreText.Language = UiLanguage.German;
}
