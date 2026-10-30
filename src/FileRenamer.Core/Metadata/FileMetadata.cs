namespace FileRenamer.Core.Metadata;

/// <summary>
/// Art einer Mediendatei, abgeleitet aus der Dateiendung.
/// </summary>
public enum MediaKind
{
    /// <summary>Weder Bild noch Audio.</summary>
    None,

    /// <summary>Bilddatei (JPEG, TIFF, HEIC, PNG, RAW).</summary>
    Image,

    /// <summary>Audiodatei (MP3, FLAC, M4A).</summary>
    Audio,
}

/// <summary>
/// Aus EXIF, IPTC, XMP oder PNG-Text gelesene Bildinformationen. Nicht vorhandene Werte sind <c>null</c>.
/// </summary>
public sealed record ImageMetadata
{
    /// <summary>Aufnahmedatum (DateTimeOriginal → DateTimeDigitized → DateTime).</summary>
    public DateTime? DateTaken { get; init; }

    /// <summary>Kamerahersteller.</summary>
    public string? Make { get; init; }

    /// <summary>Kameramodell.</summary>
    public string? Model { get; init; }

    /// <summary>Objektiv.</summary>
    public string? Lens { get; init; }

    /// <summary>ISO-Empfindlichkeit.</summary>
    public int? Iso { get; init; }

    /// <summary>Blendenzahl, z. B. 4.0.</summary>
    public double? FNumber { get; init; }

    /// <summary>Belichtungszeit in Sekunden.</summary>
    public double? ExposureTime { get; init; }

    /// <summary>Brennweite in Millimetern.</summary>
    public double? FocalLength { get; init; }

    /// <summary>Bildbreite in Pixeln.</summary>
    public int? Width { get; init; }

    /// <summary>Bildhöhe in Pixeln.</summary>
    public int? Height { get; init; }

    /// <summary>Drehung in Grad (0, 90, 180, 270) laut EXIF-Orientierung.</summary>
    public int? Orientation { get; init; }

    /// <summary>Geographische Breite in Dezimalgrad (Süden negativ).</summary>
    public double? Latitude { get; init; }

    /// <summary>Geographische Länge in Dezimalgrad (Westen negativ).</summary>
    public double? Longitude { get; init; }

    /// <summary>Höhe über dem Meeresspiegel in Metern.</summary>
    public double? Altitude { get; init; }

    /// <summary>Bildbeschreibung (EXIF → IPTC → XMP).</summary>
    public string? Description { get; init; }

    /// <summary>Ersteller (EXIF → IPTC → XMP).</summary>
    public string? Creator { get; init; }

    /// <summary>Urheberrechtsvermerk (EXIF → IPTC → XMP).</summary>
    public string? Copyright { get; init; }
}

/// <summary>
/// Aus ID3 (MP3), Vorbis Comments (FLAC) oder iTunes-Atomen (M4A) gelesene Audioinformationen.
/// </summary>
public sealed record AudioMetadata
{
    /// <summary>Interpret.</summary>
    public string? Artist { get; init; }

    /// <summary>Titel.</summary>
    public string? Title { get; init; }

    /// <summary>Album.</summary>
    public string? Album { get; init; }

    /// <summary>Album-Interpret.</summary>
    public string? AlbumArtist { get; init; }

    /// <summary>Komponist.</summary>
    public string? Composer { get; init; }

    /// <summary>Genre.</summary>
    public string? Genre { get; init; }

    /// <summary>Tracknummer.</summary>
    public int? Track { get; init; }

    /// <summary>CD-/Disknummer.</summary>
    public int? Disc { get; init; }

    /// <summary>Erscheinungsjahr.</summary>
    public int? Year { get; init; }
}

/// <summary>
/// Metadaten einer Datei: Art der Datei sowie die gelesenen Bild- bzw. Audioinformationen.
/// </summary>
/// <param name="Kind">Art der Datei laut Endung.</param>
/// <param name="Image">Bildinformationen oder <c>null</c>.</param>
/// <param name="Audio">Audioinformationen oder <c>null</c>.</param>
public sealed record FileMetadata(MediaKind Kind, ImageMetadata? Image = null, AudioMetadata? Audio = null)
{
    /// <summary>
    /// Metadaten einer Datei, die weder Bild noch Audio ist.
    /// </summary>
    public static FileMetadata None { get; } = new(MediaKind.None);
}
