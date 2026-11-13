namespace FileRenamer.Core.Tagging;

/// <summary>
/// Bildtyp eines eingebetteten Bildes (Auswahl der ID3-Bildtypen, E117).
/// </summary>
public enum CoverType
{
    /// <summary>Vorderseite.</summary>
    FrontCover,

    /// <summary>Rückseite.</summary>
    BackCover,

    /// <summary>Booklet-Seite.</summary>
    LeafletPage,

    /// <summary>Medium (CD).</summary>
    Media,

    /// <summary>Interpret.</summary>
    Artist,

    /// <summary>Band-Logo.</summary>
    BandLogo,

    /// <summary>Sonstiges.</summary>
    Other,
}

/// <summary>
/// Ein eingebettetes Bild. Zwei Bilder sind gleich, wenn Typ, MIME-Typ und Bilddaten übereinstimmen.
/// </summary>
/// <param name="Type">Bildtyp.</param>
/// <param name="MimeType">MIME-Typ, z. B. <c>image/jpeg</c>.</param>
/// <param name="Data">Die Bilddaten.</param>
public sealed record CoverImage(CoverType Type, string MimeType, byte[] Data)
{
    /// <summary>
    /// Größe der Bilddaten in Byte.
    /// </summary>
    public int Size => Data.Length;

    /// <summary>
    /// Dateiendung passend zum MIME-Typ (mit Punkt).
    /// </summary>
    public string Extension => ImageFormats.ExtensionFor(MimeType);

    /// <summary>
    /// Vergleicht Typ, MIME-Typ und Inhalt.
    /// </summary>
    /// <param name="other">Das andere Bild.</param>
    /// <returns><c>true</c> bei Gleichheit.</returns>
    public bool Equals(CoverImage? other) =>
        other is not null && Type == other.Type && MimeType == other.MimeType && Data.AsSpan().SequenceEqual(other.Data);

    /// <summary>
    /// Hashwert aus Typ, MIME-Typ und Länge.
    /// </summary>
    /// <returns>Der Hashwert.</returns>
    public override int GetHashCode() => HashCode.Combine(Type, MimeType, Data.Length);

    /// <summary>
    /// Liefert dasselbe Bild mit anderem Typ.
    /// </summary>
    /// <param name="type">Der neue Typ.</param>
    /// <returns>Das Bild.</returns>
    public CoverImage WithType(CoverType type) => this with { Type = type };
}

/// <summary>
/// Zuordnung von Bildformaten zu MIME-Typ und Dateiendung.
/// </summary>
public static class ImageFormats
{
    /// <summary>
    /// Dateiendungen von Bildern, die eingefügt werden können.
    /// </summary>
    public static IReadOnlyList<string> Extensions { get; } = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"];

    /// <summary>
    /// Ermittelt den MIME-Typ zu einer Datei.
    /// </summary>
    /// <param name="path">Pfad oder Dateiname.</param>
    /// <returns>Der MIME-Typ; unbekannte Endungen ergeben <c>image/jpeg</c>.</returns>
    public static string MimeTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        _ => "image/jpeg",
    };

    /// <summary>
    /// Ermittelt die Dateiendung zu einem MIME-Typ.
    /// </summary>
    /// <param name="mimeType">Der MIME-Typ.</param>
    /// <returns>Die Endung mit Punkt; unbekannte Typen ergeben <c>.jpg</c>.</returns>
    public static string ExtensionFor(string mimeType) => mimeType.ToLowerInvariant() switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        "image/bmp" => ".bmp",
        _ => ".jpg",
    };
}
