namespace FileRenamer.Core.Tagging;

/// <summary>
/// Schlüssel der Tag-Felder. Standardfelder stehen immer im Formular, weitere Felder lassen sich aus
/// <see cref="Extra"/> wählen oder frei benennen (benutzerdefinierte Felder, E125).
/// </summary>
public static class TagFields
{
    /// <summary>Titel.</summary>
    public const string Title = "title";

    /// <summary>Interpret.</summary>
    public const string Artist = "artist";

    /// <summary>Album.</summary>
    public const string Album = "album";

    /// <summary>Albuminterpret.</summary>
    public const string AlbumArtist = "albumartist";

    /// <summary>Komponist.</summary>
    public const string Composer = "composer";

    /// <summary>Genre.</summary>
    public const string Genre = "genre";

    /// <summary>Jahr.</summary>
    public const string Year = "year";

    /// <summary>Tracknummer.</summary>
    public const string Track = "track";

    /// <summary>Anzahl der Tracks.</summary>
    public const string TrackCount = "trackcount";

    /// <summary>Discnummer.</summary>
    public const string Disc = "disc";

    /// <summary>Anzahl der Discs.</summary>
    public const string DiscCount = "disccount";

    /// <summary>Kommentar.</summary>
    public const string Comment = "comment";

    /// <summary>Schlüssel für die eingebetteten Bilder (nur für Herkunft und Vorschau).</summary>
    public const string Covers = "covers";

    /// <summary>Schlüssel für die Lyrics (nur für Herkunft und Vorschau).</summary>
    public const string Lyrics = "lyrics";

    /// <summary>Präfix benutzerdefinierter Felder.</summary>
    public const string CustomPrefix = "custom:";

    /// <summary>
    /// Die Standardfelder in der Reihenfolge des Formulars.
    /// </summary>
    public static IReadOnlyList<string> Standard { get; } =
        [Title, Artist, Album, AlbumArtist, Composer, Genre, Year, Track, TrackCount, Disc, DiscCount, Comment];

    /// <summary>
    /// Die gängigen weiteren Felder (Auswahl der Combobox „Feld hinzufügen“).
    /// </summary>
    public static IReadOnlyList<string> Extra { get; } =
        ["bpm", "copyright", "publisher", "isrc", "conductor", "subtitle", "grouping", "remixedby", "initialkey",
         "artistsort", "albumsort", "titlesort", "description"];

    /// <summary>
    /// Felder, die nur ganze Zahlen enthalten dürfen.
    /// </summary>
    public static IReadOnlyList<string> Numeric { get; } = [Year, Track, TrackCount, Disc, DiscCount, "bpm"];

    /// <summary>
    /// Bildet den Schlüssel eines benutzerdefinierten Felds.
    /// </summary>
    /// <param name="name">Der frei gewählte Name.</param>
    /// <returns>Der Schlüssel, z. B. <c>custom:MOOD</c>.</returns>
    public static string Custom(string name) => CustomPrefix + name.Trim().ToUpperInvariant();

    /// <summary>
    /// Prüft, ob ein Schlüssel ein benutzerdefiniertes Feld bezeichnet.
    /// </summary>
    /// <param name="key">Der Schlüssel.</param>
    /// <returns><c>true</c> bei <c>custom:…</c>.</returns>
    public static bool IsCustom(string key) => key.StartsWith(CustomPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Liefert den Namen eines benutzerdefinierten Felds ohne Präfix.
    /// </summary>
    /// <param name="key">Der Schlüssel.</param>
    /// <returns>Der Name.</returns>
    public static string CustomName(string key) => IsCustom(key) ? key[CustomPrefix.Length..] : key;
}
