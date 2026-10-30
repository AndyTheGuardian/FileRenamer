using System.Text;

namespace FileRenamer.Core.Metadata.Readers;

/// <summary>
/// Liest EXIF (APP1), XMP (APP1), IPTC (APP13) und die Bildmaße (SOF) aus einer JPEG-Datei.
/// Wird auch für das in RAF-Dateien eingebettete JPEG verwendet.
/// </summary>
internal static class JpegReader
{
    private const string ExifHeader = "Exif\0\0";
    private const string XmpHeader = "http://ns.adobe.com/xap/1.0/\0";
    private const string PhotoshopHeader = "Photoshop 3.0\0";

    /// <summary>
    /// Wertet die JPEG-Segmente bis zum Bildbeginn (SOS) aus. EXIF wird vor IPTC und XMP gelesen (E70).
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    public static void Read(ByteSource source, ImageMetadataBuilder builder)
    {
        if (source.UInt16(0, littleEndian: false) != 0xFFD8)
        {
            return;
        }

        var segments = ReadSegments(source);
        foreach (var segment in segments.Where(segment => segment.Marker == 0xE1 && source.StartsWith(segment.Start, ExifHeader)))
        {
            TiffParser.Parse(source, segment.Start + ExifHeader.Length, builder);
        }

        foreach (var segment in segments.Where(segment => segment.Marker == 0xED && source.StartsWith(segment.Start, PhotoshopHeader)))
        {
            ReadPhotoshopResources(source.Read(segment.Start + PhotoshopHeader.Length, segment.Length - PhotoshopHeader.Length), builder);
        }

        foreach (var segment in segments.Where(segment => segment.Marker == 0xE1 && source.StartsWith(segment.Start, XmpHeader)))
        {
            var bytes = source.Read(segment.Start + XmpHeader.Length, segment.Length - XmpHeader.Length);
            XmpParser.Parse(Encoding.UTF8.GetString(bytes), builder);
        }

        ApplyFrameSize(source, segments, builder);
    }

    /// <summary>
    /// Liest die Liste der Segmente bis zum Bildbeginn.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <returns>Marker, Datenbeginn und Datenlänge je Segment.</returns>
    private static List<Segment> ReadSegments(ByteSource source)
    {
        var segments = new List<Segment>();
        long position = 2;

        while (position + 4 <= source.Length && segments.Count < 500)
        {
            if (source.Read(position, 1) is not [0xFF])
            {
                break;
            }

            var marker = source.Read(position + 1, 1)[0];
            if (marker is 0xDA or 0xD9)
            {
                break;
            }

            var length = source.UInt16(position + 2, littleEndian: false);
            if (length < 2)
            {
                break;
            }

            segments.Add(new Segment(marker, position + 4, length - 2));
            position += 2 + length;
        }

        return segments;
    }

    /// <summary>
    /// Sucht in Photoshop-Ressourcen (8BIM) den IPTC-Block (Ressource 0x0404).
    /// </summary>
    /// <param name="data">Die Ressourcendaten.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    private static void ReadPhotoshopResources(byte[] data, ImageMetadataBuilder builder)
    {
        var position = 0;
        while (position + 12 <= data.Length && Encoding.ASCII.GetString(data, position, 4) == "8BIM")
        {
            var id = (data[position + 4] << 8) | data[position + 5];
            var nameLength = data[position + 6];
            var namePadded = (nameLength + 2) & ~1;
            var sizePosition = position + 6 + namePadded;
            if (sizePosition + 4 > data.Length)
            {
                return;
            }

            var size = (data[sizePosition] << 24) | (data[sizePosition + 1] << 16) | (data[sizePosition + 2] << 8) | data[sizePosition + 3];
            var dataStart = sizePosition + 4;
            if (size < 0 || dataStart + size > data.Length)
            {
                return;
            }

            if (id == 0x0404)
            {
                IptcParser.Parse(data[dataStart..(dataStart + size)], builder);
            }

            position = dataStart + ((size + 1) & ~1);
        }
    }

    /// <summary>
    /// Übernimmt die Bildmaße aus dem Frame-Header (SOF), falls EXIF keine liefert.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="segments">Die Segmente.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    private static void ApplyFrameSize(ByteSource source, List<Segment> segments, ImageMetadataBuilder builder)
    {
        var frame = segments.FirstOrDefault(segment => segment.Marker is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC));
        if (frame is not null)
        {
            builder.Merge(new ImageMetadata
            {
                Height = source.UInt16(frame.Start + 1, littleEndian: false),
                Width = source.UInt16(frame.Start + 3, littleEndian: false),
            });
        }
    }

    /// <summary>
    /// Ein JPEG-Segment.
    /// </summary>
    /// <param name="Marker">Markerbyte (z. B. 0xE1 für APP1).</param>
    /// <param name="Start">Beginn der Nutzdaten.</param>
    /// <param name="Length">Länge der Nutzdaten.</param>
    private sealed record Segment(byte Marker, long Start, int Length);
}
