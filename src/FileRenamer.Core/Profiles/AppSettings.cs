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
    /// Vergleicht auch die Listen (Farben, Vorlagen) inhaltlich.
    /// </summary>
    /// <param name="other">Die anderen Einstellungen.</param>
    /// <returns><c>true</c> bei gleichem Inhalt.</returns>
    public bool Equals(AppSettings? other) =>
        other is not null && Theme == other.Theme && LastProfileName == other.LastProfileName && AccentColor == other.AccentColor
        && PastelSaturation == other.PastelSaturation && CustomColors.SequenceEqual(other.CustomColors) && Language == other.Language
        && CustomPhotoTemplates.SequenceEqual(other.CustomPhotoTemplates) && CustomAudioTemplates.SequenceEqual(other.CustomAudioTemplates);

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
