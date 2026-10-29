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
