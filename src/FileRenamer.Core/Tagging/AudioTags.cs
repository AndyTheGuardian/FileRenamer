namespace FileRenamer.Core.Tagging;

/// <summary>
/// Unveränderlicher Stand der Tags einer Audiodatei: Textfelder (Schlüssel aus <see cref="TagFields"/>),
/// eingebettete Bilder und Lyrics. Leere Werte gelten als nicht vorhanden.
/// </summary>
public sealed class AudioTags : IEquatable<AudioTags>
{
    private readonly SortedDictionary<string, string> _fields;

    /// <summary>
    /// Leere Tags.
    /// </summary>
    public static AudioTags Empty { get; } = new(new Dictionary<string, string>(), [], string.Empty);

    /// <summary>
    /// Sprache unbekannter Lyrics (ID3: <c>XXX</c>).
    /// </summary>
    public const string UnknownLanguage = "XXX";

    /// <summary>
    /// Erzeugt einen Tag-Stand; leere Feldwerte werden verworfen.
    /// </summary>
    /// <param name="fields">Die Textfelder.</param>
    /// <param name="covers">Die eingebetteten Bilder.</param>
    /// <param name="lyrics">Die Lyrics (leer = keine).</param>
    /// <param name="lyricsLanguage">Sprache der Lyrics als dreistelliger Code nach ISO 639-2 (nur ID3).</param>
    public AudioTags(
        IReadOnlyDictionary<string, string> fields, IReadOnlyList<CoverImage> covers, string lyrics, string lyricsLanguage = UnknownLanguage)
    {
        _fields = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in fields)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _fields[key] = value.Trim();
            }
        }

        Covers = covers;
        Lyrics = lyrics ?? string.Empty;
        LyricsLanguage = Lyrics.Length == 0 || string.IsNullOrWhiteSpace(lyricsLanguage) ? UnknownLanguage : lyricsLanguage;
    }

    /// <summary>
    /// Die vorhandenen Textfelder.
    /// </summary>
    public IReadOnlyDictionary<string, string> Fields => _fields;

    /// <summary>
    /// Die eingebetteten Bilder in Dateireihenfolge.
    /// </summary>
    public IReadOnlyList<CoverImage> Covers { get; }

    /// <summary>
    /// Die Lyrics oder ein leerer Text.
    /// </summary>
    public string Lyrics { get; }

    /// <summary>
    /// Sprache der Lyrics (dreistelliger Code, nur für ID3 von Bedeutung).
    /// </summary>
    public string LyricsLanguage { get; }

    /// <summary>
    /// Liefert den Wert eines Felds.
    /// </summary>
    /// <param name="key">Der Schlüssel.</param>
    /// <returns>Der Wert oder ein leerer Text.</returns>
    public string Get(string key) => _fields.GetValueOrDefault(key, string.Empty);

    /// <summary>
    /// Liefert die Tags mit einem geänderten Feld.
    /// </summary>
    /// <param name="key">Der Schlüssel.</param>
    /// <param name="value">Der neue Wert; leer entfernt das Feld.</param>
    /// <returns>Die neuen Tags.</returns>
    public AudioTags With(string key, string? value) => WithFields(new Dictionary<string, string?> { [key] = value });

    /// <summary>
    /// Liefert die Tags mit mehreren geänderten Feldern.
    /// </summary>
    /// <param name="values">Schlüssel → neuer Wert; leer oder <c>null</c> entfernt das Feld.</param>
    /// <returns>Die neuen Tags.</returns>
    public AudioTags WithFields(IReadOnlyDictionary<string, string?> values)
    {
        var fields = new Dictionary<string, string>(_fields, StringComparer.Ordinal);
        foreach (var (key, value) in values)
        {
            fields[key] = value ?? string.Empty;
        }

        return new AudioTags(fields, Covers, Lyrics, LyricsLanguage);
    }

    /// <summary>
    /// Liefert die Tags mit anderen Bildern.
    /// </summary>
    /// <param name="covers">Die Bilder.</param>
    /// <returns>Die neuen Tags.</returns>
    public AudioTags WithCovers(IReadOnlyList<CoverImage> covers) => new(_fields, covers, Lyrics, LyricsLanguage);

    /// <summary>
    /// Liefert die Tags mit anderen Lyrics.
    /// </summary>
    /// <param name="lyrics">Die Lyrics; leer entfernt sie.</param>
    /// <param name="language">Die Sprache (dreistelliger Code).</param>
    /// <returns>Die neuen Tags.</returns>
    public AudioTags WithLyrics(string lyrics, string language) => new(_fields, Covers, lyrics, language);

    /// <summary>
    /// Liefert die Schlüssel aller Bestandteile, die sich gegenüber anderen Tags unterscheiden
    /// (Feldschlüssel sowie <see cref="TagFields.Covers"/> und <see cref="TagFields.Lyrics"/>).
    /// </summary>
    /// <param name="other">Die Vergleichstags.</param>
    /// <returns>Die geänderten Schlüssel.</returns>
    public IReadOnlyList<string> DifferingKeys(AudioTags other)
    {
        var keys = _fields.Keys.Union(other._fields.Keys).Where(key => Get(key) != other.Get(key)).ToList();
        if (!Covers.SequenceEqual(other.Covers))
        {
            keys.Add(TagFields.Covers);
        }

        if (Lyrics != other.Lyrics || LyricsLanguage != other.LyricsLanguage)
        {
            keys.Add(TagFields.Lyrics);
        }

        return keys;
    }

    /// <summary>
    /// Vergleicht Felder, Bilder und Lyrics.
    /// </summary>
    /// <param name="other">Die anderen Tags.</param>
    /// <returns><c>true</c> bei Gleichheit.</returns>
    public bool Equals(AudioTags? other) => other is not null && DifferingKeys(other).Count == 0;

    /// <summary>
    /// Vergleicht mit einem beliebigen Objekt.
    /// </summary>
    /// <param name="obj">Das Objekt.</param>
    /// <returns><c>true</c> bei Gleichheit.</returns>
    public override bool Equals(object? obj) => Equals(obj as AudioTags);

    /// <summary>
    /// Hashwert aus Anzahl der Felder und Bilder.
    /// </summary>
    /// <returns>Der Hashwert.</returns>
    public override int GetHashCode() => HashCode.Combine(_fields.Count, Covers.Count, Lyrics.Length);
}
