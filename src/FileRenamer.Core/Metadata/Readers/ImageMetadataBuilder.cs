namespace FileRenamer.Core.Metadata.Readers;

/// <summary>
/// Sammelt Bildinformationen aus mehreren Quellen. Ein Wert wird nur gesetzt, wenn er noch fehlt –
/// die Reihenfolge der Leseaufrufe (EXIF → IPTC → XMP → PNG-Text) bestimmt also die Priorität (E70).
/// </summary>
internal sealed class ImageMetadataBuilder
{
    private DateTime? _dateOriginal;
    private DateTime? _dateDigitized;
    private DateTime? _dateModified;
    private ImageMetadata _metadata = new();

    /// <summary>
    /// Merkt sich das EXIF-Aufnahmedatum (DateTimeOriginal).
    /// </summary>
    /// <param name="value">Der Zeitpunkt.</param>
    public void SetDateOriginal(DateTime? value) => _dateOriginal ??= value;

    /// <summary>
    /// Merkt sich das EXIF-Digitalisierungsdatum (DateTimeDigitized).
    /// </summary>
    /// <param name="value">Der Zeitpunkt.</param>
    public void SetDateDigitized(DateTime? value) => _dateDigitized ??= value;

    /// <summary>
    /// Merkt sich das EXIF-Änderungsdatum (DateTime im IFD0).
    /// </summary>
    /// <param name="value">Der Zeitpunkt.</param>
    public void SetDateModified(DateTime? value) => _dateModified ??= value;

    /// <summary>
    /// Ergänzt fehlende Werte; bereits gesetzte bleiben erhalten.
    /// </summary>
    /// <param name="proposed">Die Werte aus einer weiteren Quelle.</param>
    public void Merge(ImageMetadata proposed)
    {
        _metadata = _metadata with
        {
            Make = _metadata.Make ?? Clean(proposed.Make),
            Model = _metadata.Model ?? Clean(proposed.Model),
            Lens = _metadata.Lens ?? Clean(proposed.Lens),
            Iso = _metadata.Iso ?? proposed.Iso,
            FNumber = _metadata.FNumber ?? proposed.FNumber,
            ExposureTime = _metadata.ExposureTime ?? proposed.ExposureTime,
            FocalLength = _metadata.FocalLength ?? proposed.FocalLength,
            Width = _metadata.Width ?? proposed.Width,
            Height = _metadata.Height ?? proposed.Height,
            Orientation = _metadata.Orientation ?? proposed.Orientation,
            Latitude = _metadata.Latitude ?? proposed.Latitude,
            Longitude = _metadata.Longitude ?? proposed.Longitude,
            Altitude = _metadata.Altitude ?? proposed.Altitude,
            Description = _metadata.Description ?? Clean(proposed.Description),
            Creator = _metadata.Creator ?? Clean(proposed.Creator),
            Copyright = _metadata.Copyright ?? Clean(proposed.Copyright),
        };
    }

    /// <summary>
    /// Erzeugt die Bildinformationen; das Aufnahmedatum folgt der Reihenfolge aus E60.
    /// </summary>
    /// <returns>Die gesammelten Bildinformationen.</returns>
    public ImageMetadata Build() => _metadata with { DateTaken = _dateOriginal ?? _dateDigitized ?? _dateModified };

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
}
