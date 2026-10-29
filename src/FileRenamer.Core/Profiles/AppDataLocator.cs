namespace FileRenamer.Core.Profiles;

/// <summary>
/// Ermittelt die plattformüblichen Ablageorte für Profile und Einstellungen:
/// Windows <c>%APPDATA%\FileRenamer</c>, Linux <c>~/.config/FileRenamer</c>,
/// macOS <c>~/Library/Application Support/FileRenamer</c>.
/// </summary>
public static class AppDataLocator
{
    private const string ApplicationFolderName = "FileRenamer";

    /// <summary>
    /// Liefert das Datenverzeichnis der Anwendung.
    /// </summary>
    /// <returns>Der Verzeichnispfad (wird nicht angelegt).</returns>
    public static string GetApplicationDirectory()
    {
        var folder = OperatingSystem.IsMacOS()
            ? Environment.SpecialFolder.LocalApplicationData
            : Environment.SpecialFolder.ApplicationData;
        return Path.Combine(Environment.GetFolderPath(folder), ApplicationFolderName);
    }

    /// <summary>
    /// Liefert das Verzeichnis der Profile.
    /// </summary>
    /// <returns>Der Verzeichnispfad.</returns>
    public static string GetProfileDirectory() => Path.Combine(GetApplicationDirectory(), "profiles");

    /// <summary>
    /// Liefert den Pfad der Einstellungsdatei.
    /// </summary>
    /// <returns>Der Dateipfad.</returns>
    public static string GetSettingsFile() => Path.Combine(GetApplicationDirectory(), "settings.json");
}
