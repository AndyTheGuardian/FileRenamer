using System.Text;

namespace FileRenamer.Core.Metadata.Readers;

/// <summary>
/// Liest EXIF und XMP aus HEIC/HEIF-Dateien: Die Daten liegen als „Items“, deren Typ in „iinf“
/// und deren Position in „iloc“ (jeweils in der „meta“-Box) beschrieben ist.
/// </summary>
internal static class HeifReader
{
    /// <summary>
    /// Wertet die HEIF-Struktur aus. EXIF wird vor XMP gelesen.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    public static void Read(ByteSource source, ImageMetadataBuilder builder)
    {
        var meta = IsoBox.ReadAll(source, 0, source.Length).FirstOrDefault(box => box.Type == "meta");
        if (meta is null)
        {
            return;
        }

        var children = meta.Children(source, 4);
        var items = ReadItemTypes(source, children.FirstOrDefault(box => box.Type == "iinf"));
        var locations = ReadLocations(source, children.FirstOrDefault(box => box.Type == "iloc"));

        foreach (var (id, _) in items.Where(item => item.Value.Type == "Exif"))
        {
            if (locations.TryGetValue(id, out var location))
            {
                // Exif-Item: 4 Byte Offset bis zum TIFF-Header, danach die Daten
                var tiffOffset = source.UInt32(location.Offset, littleEndian: false);
                TiffParser.Parse(source, location.Offset + 4 + tiffOffset, builder);
            }
        }

        foreach (var (id, _) in items.Where(item => item.Value.ContentType?.Contains("xml", StringComparison.OrdinalIgnoreCase) == true))
        {
            if (locations.TryGetValue(id, out var location))
            {
                XmpParser.Parse(Encoding.UTF8.GetString(source.Read(location.Offset, (int)location.Length)), builder);
            }
        }
    }

    /// <summary>
    /// Liest Typ und Inhaltstyp aller Items aus „iinf“/„infe“ (Version 2 und 3).
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="iinf">Die „iinf“-Box oder <c>null</c>.</param>
    /// <returns>Item-Id → Typ und Inhaltstyp.</returns>
    private static Dictionary<uint, ItemInfo> ReadItemTypes(ByteSource source, IsoBox? iinf)
    {
        var items = new Dictionary<uint, ItemInfo>();
        if (iinf is null)
        {
            return items;
        }

        var version = source.Read(iinf.Start, 1) is [var v] ? v : (byte)0;
        foreach (var infe in iinf.Children(source, version == 0 ? 6 : 8).Where(box => box.Type == "infe"))
        {
            var infeVersion = source.Read(infe.Start, 1) is [var iv] ? iv : (byte)0;
            if (infeVersion < 2)
            {
                continue;
            }

            var position = infe.Start + 4;
            uint id = infeVersion == 2 ? source.UInt16(position, false) : source.UInt32(position, false);
            position += (infeVersion == 2 ? 2 : 4) + 2;
            var type = Encoding.ASCII.GetString(source.Read(position, 4));
            string? contentType = null;
            if (type == "mime")
            {
                var strings = Encoding.UTF8.GetString(source.Read(position + 4, (int)(infe.End - position - 4))).Split('\0');
                contentType = strings.Length > 1 ? strings[1] : null;
            }

            items[id] = new ItemInfo(type, contentType);
        }

        return items;
    }

    /// <summary>
    /// Liest Position und Länge der Items aus „iloc“ (Versionen 0–2, erster Abschnitt je Item).
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="iloc">Die „iloc“-Box oder <c>null</c>.</param>
    /// <returns>Item-Id → Position und Länge.</returns>
    private static Dictionary<uint, ItemLocation> ReadLocations(ByteSource source, IsoBox? iloc)
    {
        var locations = new Dictionary<uint, ItemLocation>();
        if (iloc is null)
        {
            return locations;
        }

        var header = source.Read(iloc.Start, 6);
        if (header.Length < 6)
        {
            return locations;
        }

        var version = header[0];
        int offsetSize = header[4] >> 4, lengthSize = header[4] & 0x0F, baseOffsetSize = header[5] >> 4;
        var indexSize = version is 1 or 2 ? header[5] & 0x0F : 0;
        var position = iloc.Start + 6;
        var count = version < 2 ? source.UInt16(position, false) : source.UInt32(position, false);
        position += version < 2 ? 2 : 4;

        for (var item = 0; item < Math.Min(count, 10_000u) && position < iloc.End; item++)
        {
            uint id = version < 2 ? source.UInt16(position, false) : source.UInt32(position, false);
            position += version < 2 ? 2 : 4;
            position += version is 1 or 2 ? 2 : 0;   // construction_method
            position += 2;                            // data_reference_index
            var baseOffset = ReadSized(source, ref position, baseOffsetSize);
            var extentCount = source.UInt16(position, false);
            position += 2;

            for (var extent = 0; extent < extentCount; extent++)
            {
                ReadSized(source, ref position, indexSize);
                var offset = ReadSized(source, ref position, offsetSize);
                var length = ReadSized(source, ref position, lengthSize);
                if (extent == 0)
                {
                    locations[id] = new ItemLocation(baseOffset + offset, length);
                }
            }
        }

        return locations;
    }

    /// <summary>
    /// Liest eine Zahl mit 0, 4 oder 8 Byte Länge (Big Endian) und rückt die Position vor.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="position">Aktuelle Position.</param>
    /// <param name="size">Größe in Bytes.</param>
    /// <returns>Der Wert.</returns>
    private static long ReadSized(ByteSource source, ref long position, int size)
    {
        long value = size switch
        {
            4 => source.UInt32(position, false),
            8 => (long)source.UInt64BigEndian(position),
            _ => 0,
        };
        position += size is 4 or 8 ? size : 0;
        return value;
    }

    /// <summary>
    /// Typ und Inhaltstyp eines Items.
    /// </summary>
    /// <param name="Type">Itemtyp, z. B. „Exif“ oder „mime“.</param>
    /// <param name="ContentType">MIME-Typ bei „mime“-Items.</param>
    private sealed record ItemInfo(string Type, string? ContentType);

    /// <summary>
    /// Position und Länge eines Items.
    /// </summary>
    /// <param name="Offset">Absolute Position.</param>
    /// <param name="Length">Länge in Bytes.</param>
    private sealed record ItemLocation(long Offset, long Length);
}
