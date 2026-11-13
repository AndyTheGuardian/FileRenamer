using System.Globalization;

namespace FileRenamer.Core.Tagging;

/// <summary>
/// Teil von <see cref="TagLibTagStore"/>: Zuordnung der Feldschlüssel zu den Eigenschaften von TagLib#,
/// benutzerdefinierte Felder je Tag-Format und die Sprache der Lyrics. Mehrere Werte (z. B. Interpreten)
/// werden mit „; “ verbunden und beim Schreiben wieder getrennt.
/// </summary>
public sealed partial class TagLibTagStore
{
    private const string Separator = "; ";

    /// <summary>
    /// Liest ein Standard- oder weiteres Feld.
    /// </summary>
    /// <param name="tag">Der Tag.</param>
    /// <param name="key">Der Schlüssel.</param>
    /// <returns>Der Wert oder ein leerer Text.</returns>
    private static string GetField(TagLib.Tag tag, string key) => key switch
    {
        TagFields.Title => tag.Title ?? string.Empty,
        TagFields.Artist => Join(tag.Performers),
        TagFields.Album => tag.Album ?? string.Empty,
        TagFields.AlbumArtist => Join(tag.AlbumArtists),
        TagFields.Composer => Join(tag.Composers),
        TagFields.Genre => Join(tag.Genres),
        TagFields.Year => Number(tag.Year),
        TagFields.Track => Number(tag.Track),
        TagFields.TrackCount => Number(tag.TrackCount),
        TagFields.Disc => Number(tag.Disc),
        TagFields.DiscCount => Number(tag.DiscCount),
        TagFields.Comment => tag.Comment ?? string.Empty,
        _ => GetExtraField(tag, key),
    };

    /// <summary>
    /// Liest ein weiteres Feld (Auswahl „Feld hinzufügen“).
    /// </summary>
    /// <param name="tag">Der Tag.</param>
    /// <param name="key">Der Schlüssel.</param>
    /// <returns>Der Wert oder ein leerer Text.</returns>
    private static string GetExtraField(TagLib.Tag tag, string key) => key switch
    {
        "bpm" => Number(tag.BeatsPerMinute),
        "copyright" => tag.Copyright,
        "publisher" => tag.Publisher,
        "isrc" => tag.ISRC,
        "conductor" => tag.Conductor,
        "subtitle" => tag.Subtitle,
        "grouping" => tag.Grouping,
        "remixedby" => tag.RemixedBy,
        "initialkey" => tag.InitialKey,
        "artistsort" => Join(tag.PerformersSort),
        "albumsort" => tag.AlbumSort,
        "titlesort" => tag.TitleSort,
        "description" => tag.Description,
        _ => null,
    } ?? string.Empty;

    /// <summary>
    /// Schreibt ein Standard- oder weiteres Feld; ein leerer Wert entfernt es.
    /// </summary>
    /// <param name="tag">Der Tag.</param>
    /// <param name="key">Der Schlüssel.</param>
    /// <param name="value">Der Wert.</param>
    private static void SetField(TagLib.Tag tag, string key, string value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        switch (key)
        {
            case TagFields.Title: tag.Title = text; break;
            case TagFields.Artist: tag.Performers = Split(text); break;
            case TagFields.Album: tag.Album = text; break;
            case TagFields.AlbumArtist: tag.AlbumArtists = Split(text); break;
            case TagFields.Composer: tag.Composers = Split(text); break;
            case TagFields.Genre: tag.Genres = Split(text); break;
            case TagFields.Year: tag.Year = ToNumber(text); break;
            case TagFields.Track: tag.Track = ToNumber(text); break;
            case TagFields.TrackCount: tag.TrackCount = ToNumber(text); break;
            case TagFields.Disc: tag.Disc = ToNumber(text); break;
            case TagFields.DiscCount: tag.DiscCount = ToNumber(text); break;
            case TagFields.Comment: tag.Comment = text; break;
            default: SetExtraField(tag, key, text); break;
        }
    }

    /// <summary>
    /// Schreibt ein weiteres Feld.
    /// </summary>
    /// <param name="tag">Der Tag.</param>
    /// <param name="key">Der Schlüssel.</param>
    /// <param name="text">Der Wert oder <c>null</c> zum Entfernen.</param>
    private static void SetExtraField(TagLib.Tag tag, string key, string? text)
    {
        switch (key)
        {
            case "bpm": tag.BeatsPerMinute = ToNumber(text); break;
            case "copyright": tag.Copyright = text; break;
            case "publisher": tag.Publisher = text; break;
            case "isrc": tag.ISRC = text; break;
            case "conductor": tag.Conductor = text; break;
            case "subtitle": tag.Subtitle = text; break;
            case "grouping": tag.Grouping = text; break;
            case "remixedby": tag.RemixedBy = text; break;
            case "initialkey": tag.InitialKey = text; break;
            case "artistsort": tag.PerformersSort = Split(text); break;
            case "albumsort": tag.AlbumSort = text; break;
            case "titlesort": tag.TitleSort = text; break;
            case "description": tag.Description = text; break;
        }
    }

    /// <summary>
    /// Liest ein benutzerdefiniertes Feld aus dem ersten Tag-Format, das solche Felder kennt.
    /// </summary>
    /// <param name="file">Die Datei.</param>
    /// <param name="name">Der Feldname.</param>
    /// <returns>Der Wert oder ein leerer Text.</returns>
    private static string ReadCustom(TagLib.File file, string name)
    {
        if (file.GetTag(TagLib.TagTypes.Id3v2) is TagLib.Id3v2.Tag id3)
        {
            return TagLib.Id3v2.UserTextInformationFrame.Get(id3, name, false)?.Text.FirstOrDefault() ?? string.Empty;
        }

        if (file.GetTag(TagLib.TagTypes.Xiph) is TagLib.Ogg.XiphComment xiph)
        {
            return xiph.GetFirstField(name) ?? string.Empty;
        }

        if (file.GetTag(TagLib.TagTypes.Apple) is TagLib.Mpeg4.AppleTag apple)
        {
            return apple.GetDashBox("com.apple.iTunes", name) ?? string.Empty;
        }

        if (file.GetTag(TagLib.TagTypes.Asf) is TagLib.Asf.Tag asf)
        {
            return asf.GetDescriptorString(name) ?? string.Empty;
        }

        return file.GetTag(TagLib.TagTypes.Ape) is TagLib.Ape.Tag ape ? ape.GetItem(name)?.ToString() ?? string.Empty : string.Empty;
    }

    /// <summary>
    /// Schreibt ein benutzerdefiniertes Feld in das passende Tag-Format (ID3 TXXX, Xiph-Kommentar, iTunes-Feld,
    /// ASF-Deskriptor oder APE-Eintrag); ein leerer Wert entfernt es.
    /// </summary>
    /// <param name="file">Die Datei.</param>
    /// <param name="name">Der Feldname.</param>
    /// <param name="value">Der Wert.</param>
    private static void WriteCustom(TagLib.File file, string name, string value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (file.GetTag(TagLib.TagTypes.Id3v2) is TagLib.Id3v2.Tag id3)
        {
            WriteId3Custom(id3, name, text);
        }
        else if (file.GetTag(TagLib.TagTypes.Xiph) is TagLib.Ogg.XiphComment xiph)
        {
            xiph.SetField(name, text is null ? [] : [text]);
        }
        else if (file.GetTag(TagLib.TagTypes.Apple) is TagLib.Mpeg4.AppleTag apple)
        {
            apple.SetDashBox("com.apple.iTunes", name, text ?? string.Empty);
        }
        else if (file.GetTag(TagLib.TagTypes.Asf) is TagLib.Asf.Tag asf)
        {
            asf.SetDescriptorString(text ?? string.Empty, name);
        }
        else if (file.GetTag(TagLib.TagTypes.Ape) is TagLib.Ape.Tag ape)
        {
            ape.SetValue(name, text);
        }
    }

    /// <summary>
    /// Schreibt oder entfernt einen TXXX-Frame.
    /// </summary>
    /// <param name="id3">Der ID3v2-Tag.</param>
    /// <param name="name">Beschreibung des Frames.</param>
    /// <param name="text">Der Wert oder <c>null</c>.</param>
    private static void WriteId3Custom(TagLib.Id3v2.Tag id3, string name, string? text)
    {
        var frame = TagLib.Id3v2.UserTextInformationFrame.Get(id3, name, text is not null);
        if (frame is null)
        {
            return;
        }

        if (text is null)
        {
            id3.RemoveFrame(frame);
        }
        else
        {
            frame.Text = [text];
        }
    }

    /// <summary>
    /// Schreibt die Lyrics und setzt bei ID3 die Sprache des USLT-Frames.
    /// </summary>
    /// <param name="file">Die Datei.</param>
    /// <param name="lyrics">Die Lyrics (leer = entfernen).</param>
    /// <param name="language">Dreistelliger Sprachcode.</param>
    private static void WriteLyrics(TagLib.File file, string lyrics, string language)
    {
        file.Tag.Lyrics = string.IsNullOrEmpty(lyrics) ? null : lyrics;
        if (!string.IsNullOrEmpty(lyrics) && file.GetTag(TagLib.TagTypes.Id3v2) is TagLib.Id3v2.Tag id3)
        {
            foreach (var frame in id3.GetFrames<TagLib.Id3v2.UnsynchronisedLyricsFrame>())
            {
                frame.Language = language;
            }
        }
    }

    /// <summary>
    /// Liest die Sprache der Lyrics (nur ID3, sonst unbekannt).
    /// </summary>
    /// <param name="file">Die Datei.</param>
    /// <returns>Der Sprachcode.</returns>
    private static string ReadLyricsLanguage(TagLib.File file) =>
        file.GetTag(TagLib.TagTypes.Id3v2) is TagLib.Id3v2.Tag id3
            ? id3.GetFrames<TagLib.Id3v2.UnsynchronisedLyricsFrame>().FirstOrDefault()?.Language ?? AudioTags.UnknownLanguage
            : AudioTags.UnknownLanguage;

    /// <summary>
    /// Verbindet mehrere Werte.
    /// </summary>
    /// <param name="values">Die Werte.</param>
    /// <returns>Der verbundene Text.</returns>
    private static string Join(string[]? values) => values is null ? string.Empty : string.Join(Separator, values.Where(v => !string.IsNullOrWhiteSpace(v)));

    /// <summary>
    /// Trennt einen Text an „;“ in mehrere Werte.
    /// </summary>
    /// <param name="text">Der Text oder <c>null</c>.</param>
    /// <returns>Die Werte (leer bei <c>null</c>).</returns>
    private static string[] Split(string? text) =>
        text is null ? [] : text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Formatiert eine Zahl; 0 bedeutet „nicht vorhanden“.
    /// </summary>
    /// <param name="value">Die Zahl.</param>
    /// <returns>Der Text oder ein leerer Text.</returns>
    private static string Number(uint value) => value == 0 ? string.Empty : value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Wandelt einen Text in eine Zahl um; ungültige oder leere Texte ergeben 0 („nicht vorhanden“).
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <returns>Die Zahl.</returns>
    private static uint ToNumber(string? text) =>
        uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : 0;
}
