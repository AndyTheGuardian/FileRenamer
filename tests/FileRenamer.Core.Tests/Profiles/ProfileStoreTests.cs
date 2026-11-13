using FileRenamer.Core.Profiles;
using FileRenamer.Core.Rules;

namespace FileRenamer.Core.Tests.Profiles;

/// <summary>
/// Tests für das Speichern und Laden von Profilen und Einstellungen.
/// </summary>
public sealed class ProfileStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "fr-profiles-" + Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Ein Profil mit allen Verfahren übersteht Speichern und Laden unverändert (inkl. Reihenfolge und Ids).
    /// </summary>
    [Fact]
    public void SaveAndLoad_RoundTripsAllRuleTypes()
    {
        var store = new ProfileStore(_directory);
        RenameRule[] rules =
        [
            new ReplaceTextRule { SearchText = "IMG", Replacement = "Urlaub", Occurrence = MatchOccurrence.Last, UseRegex = true },
            new AppendDateRule { Source = DateSource.ModificationDate, IncludeTime = true, DateFormat = "dd.MM.yyyy" },
            new AppendCounterRule { Start = 5, Step = 2, Digits = 4, SortBy = CounterSortCriterion.Size, IsEnabled = false },
            new ShiftDigitRangeRule { Offset = -3, UseLastRun = false, RunNumber = 2 },
            new InsertTextRule { Text = "_final", Position = 2, FromEnd = true },
            new ChangeCaseRule { Mode = CaseMode.Kebab },
            new CleanCharactersRule { RemoveAccents = false },
            new RemoveRangeRule { Mode = RemoveMode.Truncate, MaxLength = 12 },
            new TemplateRule { PhotoTemplate = "%date:yyyyMMdd%_%model%", AudioTemplate = "%track% %title%", TrackDigits = 3, MissingText = "x" },
            new InsertFolderRule { Level = 2, Position = AffixPosition.AfterName },
            new ChangeExtensionRule { OnlyExtension = "jpeg", NewExtension = "jpg", Casing = ExtensionCasing.Upper },
        ];
        var profile = new RenameProfile("Urlaub: Fotos/2026?", IgnoreExtension: false, rules);

        store.Save(profile);
        var loaded = store.Load(profile.Name);

        Assert.NotNull(loaded);
        Assert.Equal(profile.Name, loaded.Name);
        Assert.False(loaded.IgnoreExtension);
        Assert.Equal(rules.Select(Normalize), loaded.Rules.Select(Normalize));
        Assert.Equal([profile.Name], store.GetProfileNames());
    }

    /// <summary>
    /// Löschen entfernt das Profil; fehlende Profile liefern <c>null</c>.
    /// </summary>
    [Fact]
    public void Delete_RemovesProfile()
    {
        var store = new ProfileStore(_directory);
        store.Save(new RenameProfile("weg", true, []));

        store.Delete("weg");

        Assert.False(store.Exists("weg"));
        Assert.Null(store.Load("weg"));
    }

    /// <summary>
    /// Einstellungen werden gespeichert; ohne Datei gelten Standardwerte.
    /// </summary>
    [Fact]
    public void AppSettingsStore_RoundTrip()
    {
        var store = new AppSettingsStore(Path.Combine(_directory, "settings.json"));

        Assert.Equal(ThemePreference.System, store.Load().Theme);

        store.Save(new AppSettings(ThemePreference.Dark, "Urlaub"));

        Assert.Equal(new AppSettings(ThemePreference.Dark, "Urlaub"), store.Load());
    }

    /// <summary>
    /// Akzentfarbe, Pastell-Sättigung und benutzerdefinierte Farben werden gespeichert (E79, E83, E84).
    /// </summary>
    [Fact]
    public void AppSettingsStore_PersistsAccentSettings()
    {
        var store = new AppSettingsStore(Path.Combine(_directory, "settings.json"));
        var settings = new AppSettings(ThemePreference.Light) { AccentColor = "#25A0C3", PastelSaturation = 45, CustomColors = ["#25A0C3", "#6B8E23"] };

        store.Save(settings);

        Assert.Equal(settings, store.Load());
        Assert.Equal(30, new AppSettingsStore(Path.Combine(_directory, "fehlt.json")).Load().PastelSaturation);
    }

    /// <summary>
    /// Gespeicherte Spaltenanordnungen bleiben samt Reihenfolge und Breiten erhalten (E148).
    /// </summary>
    [Fact]
    public void AppSettingsStore_PersistsColumnLayouts()
    {
        var store = new AppSettingsStore(Path.Combine(_directory, "settings.json"));
        var settings = new AppSettings
        {
            ColumnLayouts = new Dictionary<string, IReadOnlyList<FileRenamer.Core.Preview.SavedColumn>>
            {
                ["rename"] = [new("new", 300), new("original", null)],
            },
        };

        store.Save(settings);

        Assert.Equal(settings, store.Load());
        Assert.NotEqual(new AppSettings(), store.Load());
    }

    /// <summary>
    /// Die Sprachwahl wird gespeichert; ohne Datei gilt „System“ (E92).
    /// </summary>
    [Fact]
    public void AppSettingsStore_PersistsLanguage()
    {
        var store = new AppSettingsStore(Path.Combine(_directory, "settings.json"));

        Assert.Equal(LanguagePreference.System, store.Load().Language);

        store.Save(new AppSettings { Language = LanguagePreference.English });

        Assert.Equal(LanguagePreference.English, store.Load().Language);
    }

    /// <summary>
    /// Eigene Vorlagen werden je Tab mit Reihenfolge gespeichert; ohne Datei sind die Listen leer (E106).
    /// </summary>
    [Fact]
    public void AppSettingsStore_PersistsCustomTemplates()
    {
        var store = new AppSettingsStore(Path.Combine(_directory, "settings.json"));
        Assert.Empty(store.Load().CustomPhotoTemplates);

        var settings = new AppSettings { CustomPhotoTemplates = ["%b%", "%a%"], CustomAudioTemplates = ["%title%"] };
        store.Save(settings);

        Assert.Equal(settings, store.Load());
    }

    /// <summary>
    /// Räumt das temporäre Verzeichnis auf.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    /// <summary>
    /// Vereinheitlicht die nicht gespeicherte Kultur, damit Verfahren vergleichbar sind.
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    /// <returns>Das vergleichbare Verfahren.</returns>
    private static RenameRule Normalize(RenameRule rule) =>
        rule is AppendDateRule date ? date with { Culture = System.Globalization.CultureInfo.InvariantCulture } : rule;
}
