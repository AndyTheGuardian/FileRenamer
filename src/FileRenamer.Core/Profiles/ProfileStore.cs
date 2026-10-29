namespace FileRenamer.Core.Profiles;

/// <summary>
/// Speichert Profile als JSON-Dateien in einem Verzeichnis. Der Dateiname wird aus dem
/// Profilnamen per URL-Kodierung gebildet, damit beliebige Profilnamen möglich sind.
/// </summary>
/// <param name="directory">Verzeichnis der Profile.</param>
public sealed class ProfileStore(string directory)
{
    private const string Extension = ".json";

    /// <summary>
    /// Liefert die Namen aller gespeicherten Profile, alphabetisch sortiert.
    /// </summary>
    /// <returns>Die Profilnamen.</returns>
    public IReadOnlyList<string> GetProfileNames()
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.GetFiles(directory, "*" + Extension)
            .Select(path => Uri.UnescapeDataString(Path.GetFileNameWithoutExtension(path)))
            .Order(StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Prüft, ob ein Profil mit diesem Namen existiert.
    /// </summary>
    /// <param name="name">Der Profilname.</param>
    /// <returns><c>true</c>, wenn es existiert.</returns>
    public bool Exists(string name) => File.Exists(GetPath(name));

    /// <summary>
    /// Speichert ein Profil (überschreibt ein gleichnamiges).
    /// </summary>
    /// <param name="profile">Das Profil.</param>
    public void Save(RenameProfile profile) => JsonFileStore.Write(GetPath(profile.Name), profile);

    /// <summary>
    /// Lädt ein Profil.
    /// </summary>
    /// <param name="name">Der Profilname.</param>
    /// <returns>Das Profil oder <c>null</c>, wenn es fehlt oder unlesbar ist.</returns>
    public RenameProfile? Load(string name) => JsonFileStore.Read<RenameProfile>(GetPath(name));

    /// <summary>
    /// Löscht ein Profil, falls vorhanden.
    /// </summary>
    /// <param name="name">Der Profilname.</param>
    public void Delete(string name) => File.Delete(GetPath(name));

    /// <summary>
    /// Bildet den Dateipfad zu einem Profilnamen.
    /// </summary>
    /// <param name="name">Der Profilname.</param>
    /// <returns>Der Pfad.</returns>
    private string GetPath(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return Path.Combine(directory, Uri.EscapeDataString(name.Trim()) + Extension);
    }
}
