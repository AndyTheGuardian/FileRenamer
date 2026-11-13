namespace FileRenamer.Core.Tagging;

/// <summary>
/// Häufige Genres als Vorschläge der Genre-Liste. Eigene Genres verwaltet die Oberfläche (E134).
/// </summary>
public static class Genres
{
    /// <summary>
    /// Höchstzahl eigener Genres.
    /// </summary>
    public const int MaxCustomCount = 20;

    /// <summary>
    /// Die Standardliste (übliche englische Bezeichnungen, wie sie Player erwarten).
    /// </summary>
    public static IReadOnlyList<string> Standard { get; } =
        ["Pop", "Rock", "Alternative", "Blues", "Classical", "Country", "Dance", "Electronic", "Folk", "Hip-Hop", "Jazz",
         "Metal", "R&B", "Reggae", "Schlager", "Soul", "Soundtrack", "World", "Audiobook", "Podcast", "Spoken Word", "Children's"];
}
