using System.Globalization;
using System.Text.RegularExpressions;

namespace FileRenamer.Core.Metadata.Readers;

/// <summary>
/// Sammelt Audioinformationen; der zuerst gesetzte Wert gewinnt (z. B. ID3v2 vor ID3v1).
/// Wandelt dabei Textangaben wie „3/12“ (Track) oder „1989-11-21“ (Jahr) in Zahlen um.
/// </summary>
internal sealed partial class AudioMetadataBuilder
{
    private static readonly string[] Id3v1Genres =
    [
        "Blues", "Classic Rock", "Country", "Dance", "Disco", "Funk", "Grunge", "Hip-Hop", "Jazz", "Metal",
        "New Age", "Oldies", "Other", "Pop", "R&B", "Rap", "Reggae", "Rock", "Techno", "Industrial",
        "Alternative", "Ska", "Death Metal", "Pranks", "Soundtrack", "Euro-Techno", "Ambient", "Trip-Hop", "Vocal", "Jazz+Funk",
        "Fusion", "Trance", "Classical", "Instrumental", "Acid", "House", "Game", "Sound Clip", "Gospel", "Noise",
        "AlternRock", "Bass", "Soul", "Punk", "Space", "Meditative", "Instrumental Pop", "Instrumental Rock", "Ethnic", "Gothic",
        "Darkwave", "Techno-Industrial", "Electronic", "Pop-Folk", "Eurodance", "Dream", "Southern Rock", "Comedy", "Cult", "Gangsta",
        "Top 40", "Christian Rap", "Pop/Funk", "Jungle", "Native American", "Cabaret", "New Wave", "Psychadelic", "Rave", "Showtunes",
        "Trailer", "Lo-Fi", "Tribal", "Acid Punk", "Acid Jazz", "Polka", "Retro", "Musical", "Rock & Roll", "Hard Rock",
    ];

    private AudioMetadata _metadata = new();

    /// <summary>Setzt den Interpreten, falls noch nicht vorhanden.</summary>
    /// <param name="value">Der Wert.</param>
    public void Artist(string? value) => _metadata = _metadata with { Artist = _metadata.Artist ?? Clean(value) };

    /// <summary>Setzt den Titel, falls noch nicht vorhanden.</summary>
    /// <param name="value">Der Wert.</param>
    public void Title(string? value) => _metadata = _metadata with { Title = _metadata.Title ?? Clean(value) };

    /// <summary>Setzt das Album, falls noch nicht vorhanden.</summary>
    /// <param name="value">Der Wert.</param>
    public void Album(string? value) => _metadata = _metadata with { Album = _metadata.Album ?? Clean(value) };

    /// <summary>Setzt den Album-Interpreten, falls noch nicht vorhanden.</summary>
    /// <param name="value">Der Wert.</param>
    public void AlbumArtist(string? value) => _metadata = _metadata with { AlbumArtist = _metadata.AlbumArtist ?? Clean(value) };

    /// <summary>Setzt den Komponisten, falls noch nicht vorhanden.</summary>
    /// <param name="value">Der Wert.</param>
    public void Composer(string? value) => _metadata = _metadata with { Composer = _metadata.Composer ?? Clean(value) };

    /// <summary>Setzt das Genre; ID3-Verweise wie „(17)“ werden in den Namen übersetzt.</summary>
    /// <param name="value">Der Wert.</param>
    public void Genre(string? value) => _metadata = _metadata with { Genre = _metadata.Genre ?? ResolveGenre(Clean(value)) };

    /// <summary>Setzt die Tracknummer aus Text wie „3“ oder „3/12“.</summary>
    /// <param name="value">Der Wert.</param>
    public void Track(string? value) => _metadata = _metadata with { Track = _metadata.Track ?? LeadingNumber(value) };

    /// <summary>Setzt die Tracknummer.</summary>
    /// <param name="value">Der Wert.</param>
    public void Track(int? value) => _metadata = _metadata with { Track = _metadata.Track ?? (value > 0 ? value : null) };

    /// <summary>Setzt die Disknummer aus Text wie „1“ oder „1/2“.</summary>
    /// <param name="value">Der Wert.</param>
    public void Disc(string? value) => _metadata = _metadata with { Disc = _metadata.Disc ?? LeadingNumber(value) };

    /// <summary>Setzt die Disknummer.</summary>
    /// <param name="value">Der Wert.</param>
    public void Disc(int? value) => _metadata = _metadata with { Disc = _metadata.Disc ?? (value > 0 ? value : null) };

    /// <summary>Setzt das Jahr aus Text wie „1989“ oder „1989-11-21“.</summary>
    /// <param name="value">Der Wert.</param>
    public void Year(string? value) => _metadata = _metadata with { Year = _metadata.Year ?? ParseYear(value) };

    /// <summary>Liefert den Genre-Namen zu einer ID3v1-Genre-Nummer.</summary>
    /// <param name="index">Die Nummer.</param>
    /// <returns>Der Name oder <c>null</c>.</returns>
    public static string? GenreName(int index) => index >= 0 && index < Id3v1Genres.Length ? Id3v1Genres[index] : null;

    /// <summary>
    /// Liefert die gesammelten Audioinformationen.
    /// </summary>
    /// <returns>Die Audioinformationen.</returns>
    public AudioMetadata Build() => _metadata;

    /// <summary>
    /// Entfernt Leerraum und Nullzeichen; leere Texte gelten als nicht vorhanden.
    /// </summary>
    /// <param name="value">Der Text.</param>
    /// <returns>Der bereinigte Text oder <c>null</c>.</returns>
    private static string? Clean(string? value)
    {
        var cleaned = value?.Replace("\0", string.Empty).Trim();
        return string.IsNullOrEmpty(cleaned) ? null : cleaned;
    }

    /// <summary>
    /// Liest die führende Zahl („3/12“ → 3).
    /// </summary>
    /// <param name="value">Der Text.</param>
    /// <returns>Die Zahl oder <c>null</c>.</returns>
    private static int? LeadingNumber(string? value)
    {
        var match = value is null ? null : LeadingDigits().Match(value);
        return match is { Success: true } && int.TryParse(match.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0
            ? number
            : null;
    }

    /// <summary>
    /// Liest ein vierstelliges Jahr am Anfang des Texts.
    /// </summary>
    /// <param name="value">Der Text.</param>
    /// <returns>Das Jahr oder <c>null</c>.</returns>
    private static int? ParseYear(string? value)
    {
        var match = value is null ? null : YearPattern().Match(value);
        return match is { Success: true } ? int.Parse(match.Value, CultureInfo.InvariantCulture) : null;
    }

    /// <summary>
    /// Übersetzt ID3-Genreverweise: „(17)“ oder „17“ → „Rock“, „(17)Rock“ → „Rock“.
    /// </summary>
    /// <param name="value">Der Genre-Text.</param>
    /// <returns>Der Genre-Name.</returns>
    private static string? ResolveGenre(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var match = GenreReference().Match(value);
        if (!match.Success)
        {
            return value;
        }

        var rest = value[match.Length..].Trim();
        return rest.Length > 0 ? rest : GenreName(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)) ?? value;
    }

    /// <summary>Führende Ziffern.</summary>
    /// <returns>Der reguläre Ausdruck.</returns>
    [GeneratedRegex(@"^\s*\d+")]
    private static partial Regex LeadingDigits();

    /// <summary>Vierstelliges Jahr am Anfang.</summary>
    /// <returns>Der reguläre Ausdruck.</returns>
    [GeneratedRegex(@"^\d{4}")]
    private static partial Regex YearPattern();

    /// <summary>ID3-Genreverweis „(n)“ oder reine Zahl.</summary>
    /// <returns>Der reguläre Ausdruck.</returns>
    [GeneratedRegex(@"^\(?(\d{1,3})\)?(?=$|\D)")]
    private static partial Regex GenreReference();
}
