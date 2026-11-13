namespace FileRenamer.Core.Profiles;

/// <summary>
/// Gewünschtes Erscheinungsbild.
/// </summary>
public enum ThemePreference
{
    /// <summary>Dem Betriebssystem folgen.</summary>
    System,

    /// <summary>Hell.</summary>
    Light,

    /// <summary>Dunkel.</summary>
    Dark,
}

/// <summary>
/// Gewünschte Sprache der Oberfläche (E92).
/// </summary>
public enum LanguagePreference
{
    /// <summary>Der Systemsprache folgen (Deutsch nur bei deutschem System, sonst Englisch, E93).</summary>
    System,

    /// <summary>Deutsch.</summary>
    German,

    /// <summary>Englisch.</summary>
    English,
}

/// <summary>
/// Programmweite Einstellungen, die zwischen zwei Starts erhalten bleiben.
/// </summary>
/// <param name="Theme">Gewähltes Theme.</param>
/// <param name="LastProfileName">Zuletzt verwendetes Profil oder <c>null</c>.</param>
public sealed record AppSettings(ThemePreference Theme = ThemePreference.System, string? LastProfileName = null)
{
    /// <summary>
    /// Gewählte Akzentfarbe (<c>#RRGGBB</c>) oder <c>null</c> für den Standard (E79).
    /// </summary>
    public string? AccentColor { get; init; }

    /// <summary>
    /// Sättigung der Pastellfarben in Prozent (E83, E86, E89).
    /// </summary>
    public int PastelSaturation { get; init; } = Theming.AccentPalette.DefaultSaturation;

    /// <summary>
    /// Benutzerdefinierte Farben (E84, E87).
    /// </summary>
    public IReadOnlyList<string> CustomColors { get; init; } = [];

    /// <summary>
    /// Gewählte Sprache (E92).
    /// </summary>
    public LanguagePreference Language { get; init; } = LanguagePreference.System;

    /// <summary>
    /// Eigene Vorlagen für Fotos, neueste zuerst (E106).
    /// </summary>
    public IReadOnlyList<string> CustomPhotoTemplates { get; init; } = [];

    /// <summary>
    /// Eigene Vorlagen für Audio, neueste zuerst (E106).
    /// </summary>
    public IReadOnlyList<string> CustomAudioTemplates { get; init; } = [];

    /// <summary>
    /// Eigene Genres, neueste zuerst (E134).
    /// </summary>
    public IReadOnlyList<string> CustomGenres { get; init; } = [];

    /// <summary>
    /// Hinzugefügte Felder im Formular „Tags bearbeiten“ (Schlüssel aus <see cref="Tagging.TagFields"/>), programmweit (E131).
    /// </summary>
    public IReadOnlyList<string> ExtraTagFields { get; init; } = [];

    /// <summary>
    /// Ob Cover beim Einfügen verkleinert werden (E127).
    /// </summary>
    public bool ShrinkCovers { get; init; } = true;

    /// <summary>
    /// Höchstmaß der längsten Seite beim Verkleinern von Covern in Pixeln (E132).
    /// </summary>
    public int CoverMaxPixels { get; init; } = DefaultCoverMaxPixels;

    /// <summary>
    /// Standard für <see cref="CoverMaxPixels"/>.
    /// </summary>
    public const int DefaultCoverMaxPixels = 1000;

    /// <summary>
    /// Gespeicherte Spaltenanordnungen je Liste (Schlüssel z. B. „rename“, „tags“), E148.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<Preview.SavedColumn>> ColumnLayouts { get; init; } =
        new Dictionary<string, IReadOnlyList<Preview.SavedColumn>>();

    /// <summary>
    /// Vergleicht auch die Listen (Farben, Vorlagen) inhaltlich.
    /// </summary>
    /// <param name="other">Die anderen Einstellungen.</param>
    /// <returns><c>true</c> bei gleichem Inhalt.</returns>
    public bool Equals(AppSettings? other) =>
        other is not null && Theme == other.Theme && LastProfileName == other.LastProfileName && AccentColor == other.AccentColor
        && PastelSaturation == other.PastelSaturation && CustomColors.SequenceEqual(other.CustomColors) && Language == other.Language
        && CustomPhotoTemplates.SequenceEqual(other.CustomPhotoTemplates) && CustomAudioTemplates.SequenceEqual(other.CustomAudioTemplates)
        && CustomGenres.SequenceEqual(other.CustomGenres) && ExtraTagFields.SequenceEqual(other.ExtraTagFields)
        && ShrinkCovers == other.ShrinkCovers && CoverMaxPixels == other.CoverMaxPixels
        && SameColumnLayouts(ColumnLayouts, other.ColumnLayouts);

    /// <summary>
    /// Vergleicht gespeicherte Spaltenanordnungen inhaltlich.
    /// </summary>
    /// <param name="first">Erste Anordnungen.</param>
    /// <param name="second">Zweite Anordnungen.</param>
    /// <returns><c>true</c> bei gleichem Inhalt.</returns>
    private static bool SameColumnLayouts(
        IReadOnlyDictionary<string, IReadOnlyList<Preview.SavedColumn>> first, IReadOnlyDictionary<string, IReadOnlyList<Preview.SavedColumn>> second) =>
        first.Count == second.Count && first.All(pair => second.TryGetValue(pair.Key, out var other) && pair.Value.SequenceEqual(other));

    /// <summary>
    /// Hashwert passend zu <see cref="Equals(AppSettings?)"/>.
    /// </summary>
    /// <returns>Der Hashwert.</returns>
    public override int GetHashCode() =>
        HashCode.Combine(Theme, LastProfileName, AccentColor, PastelSaturation, CustomColors.Count, Language,
            CustomPhotoTemplates.Count, CustomAudioTemplates.Count);
}

/// <summary>
/// Liest und schreibt <see cref="AppSettings"/> als JSON-Datei.
/// </summary>
/// <param name="filePath">Pfad der Einstellungsdatei.</param>
public sealed class AppSettingsStore(string filePath)
{
    /// <summary>
    /// Lädt die Einstellungen; liefert Standardwerte, wenn keine gültige Datei vorhanden ist.
    /// </summary>
    /// <returns>Die Einstellungen.</returns>
    public AppSettings Load() => JsonFileStore.Read<AppSettings>(filePath) ?? new AppSettings();

    /// <summary>
    /// Speichert die Einstellungen.
    /// </summary>
    /// <param name="settings">Die Einstellungen.</param>
    public void Save(AppSettings settings) => JsonFileStore.Write(filePath, settings);
}
