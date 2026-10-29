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
/// Programmweite Einstellungen, die zwischen zwei Starts erhalten bleiben.
/// </summary>
/// <param name="Theme">Gewähltes Theme.</param>
/// <param name="LastProfileName">Zuletzt verwendetes Profil oder <c>null</c>.</param>
public sealed record AppSettings(ThemePreference Theme = ThemePreference.System, string? LastProfileName = null);

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
