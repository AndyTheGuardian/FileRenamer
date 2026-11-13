using FileRenamer.Core.Localization;

namespace FileRenamer.Core.Tagging;

/// <summary>
/// Liest und schreibt Tags mit der Bibliothek TagLib# (E115). MP3 wird als ID3v2.3 geschrieben (E122);
/// ein vorhandener ID3v1-Tag wird mit aktualisiert, ein fehlender nicht neu angelegt.
/// </summary>
public sealed partial class TagLibTagStore : ITagStore
{
    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".mp2", ".flac", ".m4a", ".m4b", ".aac", ".ogg", ".oga", ".opus", ".spx", ".wma", ".wav", ".aif",
        ".aiff", ".aifc", ".ape", ".wv", ".mpc", ".dsf", ".tta",
    };

    /// <summary>
    /// Stellt TagLib# auf ID3v2.3 ein.
    /// </summary>
    static TagLibTagStore()
    {
        TagLib.Id3v2.Tag.DefaultVersion = 3;
        TagLib.Id3v2.Tag.ForceDefaultVersion = true;
    }

    /// <summary>
    /// Prüft die Endung gegen die Audioformate, die TagLib# schreiben kann (E121).
    /// </summary>
    /// <param name="path">Pfad oder Dateiname.</param>
    /// <returns><c>true</c> bei einer unterstützten Endung.</returns>
    public bool IsSupported(string path) => AudioExtensions.Contains(Path.GetExtension(path));

    /// <summary>
    /// Liest Felder, Bilder und Lyrics.
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <param name="customNames">Mitzulesende benutzerdefinierte Felder.</param>
    /// <returns>Die Tags.</returns>
    public AudioTags Read(string path, IReadOnlyCollection<string> customNames)
    {
        using var file = Open(path);
        return ReadFrom(file, customNames);
    }

    /// <summary>
    /// Schreibt nur die geänderten Bestandteile und speichert die Datei.
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <param name="tags">Der gewünschte Stand.</param>
    /// <param name="customNames">Verwaltete benutzerdefinierte Felder.</param>
    public void Write(string path, AudioTags tags, IReadOnlyCollection<string> customNames)
    {
        using var file = Open(path);
        var hadId3v1 = file.TagTypesOnDisk.HasFlag(TagLib.TagTypes.Id3v1);
        var current = ReadFrom(file, customNames);
        foreach (var key in tags.DifferingKeys(current))
        {
            Apply(file, tags, key);
        }

        if (!hadId3v1)
        {
            file.RemoveTags(TagLib.TagTypes.Id3v1);
        }

        Save(file, path);
    }

    /// <summary>
    /// Öffnet eine Datei mit TagLib#.
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <returns>Die geöffnete Datei.</returns>
    private static TagLib.File Open(string path)
    {
        try
        {
            return TagLib.File.Create(path);
        }
        catch (Exception exception) when (exception is TagLib.CorruptFileException or TagLib.UnsupportedFormatException or IOException
                                              or UnauthorizedAccessException)
        {
            throw new TagStoreException(CoreText.TagReadFailed(Path.GetFileName(path), exception.Message), exception);
        }
    }

    /// <summary>
    /// Speichert eine geöffnete Datei.
    /// </summary>
    /// <param name="file">Die Datei.</param>
    /// <param name="path">Pfad (für die Meldung).</param>
    private static void Save(TagLib.File file, string path)
    {
        try
        {
            file.Save();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or TagLib.CorruptFileException
                                              or NotSupportedException)
        {
            throw new TagStoreException(CoreText.TagWriteFailed(Path.GetFileName(path), exception.Message), exception);
        }
    }

    /// <summary>
    /// Liest den Stand einer geöffneten Datei.
    /// </summary>
    /// <param name="file">Die Datei.</param>
    /// <param name="customNames">Mitzulesende benutzerdefinierte Felder.</param>
    /// <returns>Die Tags.</returns>
    private static AudioTags ReadFrom(TagLib.File file, IReadOnlyCollection<string> customNames)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in TagFields.Standard.Concat(TagFields.Extra))
        {
            fields[key] = GetField(file.Tag, key);
        }

        foreach (var name in customNames)
        {
            fields[TagFields.Custom(name)] = ReadCustom(file, name);
        }

        var covers = file.Tag.Pictures.Select(ToCover).ToList();
        return new AudioTags(fields, covers, file.Tag.Lyrics ?? string.Empty, ReadLyricsLanguage(file));
    }

    /// <summary>
    /// Überträgt einen geänderten Bestandteil in die Datei.
    /// </summary>
    /// <param name="file">Die Datei.</param>
    /// <param name="tags">Der gewünschte Stand.</param>
    /// <param name="key">Schlüssel des Bestandteils.</param>
    private static void Apply(TagLib.File file, AudioTags tags, string key)
    {
        if (key == TagFields.Covers)
        {
            file.Tag.Pictures = tags.Covers.Select(ToPicture).ToArray();
        }
        else if (key == TagFields.Lyrics)
        {
            WriteLyrics(file, tags.Lyrics, tags.LyricsLanguage);
        }
        else if (TagFields.IsCustom(key))
        {
            WriteCustom(file, TagFields.CustomName(key), tags.Get(key));
        }
        else
        {
            SetField(file.Tag, key, tags.Get(key));
        }
    }

    /// <summary>
    /// Wandelt ein TagLib-Bild um.
    /// </summary>
    /// <param name="picture">Das Bild.</param>
    /// <returns>Das Bild des Modells.</returns>
    private static CoverImage ToCover(TagLib.IPicture picture) =>
        new(ToCoverType(picture.Type), string.IsNullOrEmpty(picture.MimeType) ? "image/jpeg" : picture.MimeType, picture.Data.Data);

    /// <summary>
    /// Wandelt ein Bild des Modells in ein TagLib-Bild um.
    /// </summary>
    /// <param name="cover">Das Bild.</param>
    /// <returns>Das TagLib-Bild.</returns>
    private static TagLib.IPicture ToPicture(CoverImage cover) =>
        new TagLib.Picture(new TagLib.ByteVector(cover.Data)) { Type = ToPictureType(cover.Type), MimeType = cover.MimeType };

    /// <summary>
    /// Ordnet einen TagLib-Bildtyp zu; seltene Typen gelten als „Sonstiges“.
    /// </summary>
    /// <param name="type">Der Typ.</param>
    /// <returns>Der Bildtyp des Modells.</returns>
    private static CoverType ToCoverType(TagLib.PictureType type) => type switch
    {
        TagLib.PictureType.FrontCover => CoverType.FrontCover,
        TagLib.PictureType.BackCover => CoverType.BackCover,
        TagLib.PictureType.LeafletPage => CoverType.LeafletPage,
        TagLib.PictureType.Media => CoverType.Media,
        TagLib.PictureType.Artist or TagLib.PictureType.LeadArtist => CoverType.Artist,
        TagLib.PictureType.BandLogo => CoverType.BandLogo,
        _ => CoverType.Other,
    };

    /// <summary>
    /// Ordnet einen Bildtyp des Modells dem TagLib-Typ zu.
    /// </summary>
    /// <param name="type">Der Typ.</param>
    /// <returns>Der TagLib-Typ.</returns>
    private static TagLib.PictureType ToPictureType(CoverType type) => type switch
    {
        CoverType.FrontCover => TagLib.PictureType.FrontCover,
        CoverType.BackCover => TagLib.PictureType.BackCover,
        CoverType.LeafletPage => TagLib.PictureType.LeafletPage,
        CoverType.Media => TagLib.PictureType.Media,
        CoverType.Artist => TagLib.PictureType.Artist,
        CoverType.BandLogo => TagLib.PictureType.BandLogo,
        _ => TagLib.PictureType.Other,
    };
}
