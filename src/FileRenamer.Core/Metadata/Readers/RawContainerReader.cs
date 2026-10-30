namespace FileRenamer.Core.Metadata.Readers;

/// <summary>
/// Liest RAW-Formate, die nicht vollständig TIFF-aufgebaut sind: Canon CR3 (ISOBMFF mit CMT-Boxen)
/// und Fujifilm RAF (Header mit eingebettetem JPEG).
/// </summary>
internal static class RawContainerReader
{
    private static readonly byte[] CanonUuid =
        [0x85, 0xC0, 0xB6, 0x87, 0x82, 0x0F, 0x11, 0xE0, 0x81, 0x11, 0xF4, 0xCE, 0x46, 0x2B, 0x6A, 0x48];

    /// <summary>
    /// Wertet eine CR3-Datei aus: CMT1 (IFD0), CMT2 (EXIF), CMT4 (GPS) sind jeweils eigene TIFF-Strukturen.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    public static void ReadCr3(ByteSource source, ImageMetadataBuilder builder)
    {
        var moov = IsoBox.ReadAll(source, 0, source.Length).FirstOrDefault(box => box.Type == "moov");
        var canon = moov?.Children(source).FirstOrDefault(box => box.Type == "uuid" && source.Read(box.Start, 16).AsSpan().SequenceEqual(CanonUuid));
        if (canon is null)
        {
            return;
        }

        var boxes = IsoBox.ReadAll(source, canon.Start + 16, canon.End);
        ParseCmt(source, boxes, "CMT2", builder, TiffParser.RootDirectory.Exif);
        ParseCmt(source, boxes, "CMT1", builder, TiffParser.RootDirectory.Main);
        ParseCmt(source, boxes, "CMT4", builder, TiffParser.RootDirectory.Gps);
    }

    /// <summary>
    /// Wertet eine RAF-Datei aus: Offset (Position 84) und Länge (88) des eingebetteten JPEG, Big Endian.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    public static void ReadRaf(ByteSource source, ImageMetadataBuilder builder)
    {
        if (!source.StartsWith(0, "FUJIFILMCCD-RAW"))
        {
            return;
        }

        var offset = source.UInt32(84, littleEndian: false);
        var length = source.UInt32(88, littleEndian: false);
        JpegReader.Read(source.Slice(offset, length), builder);
    }

    /// <summary>
    /// Wertet eine CMT-Box als TIFF-Struktur aus.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="boxes">Die Boxen im Canon-UUID-Bereich.</param>
    /// <param name="type">Boxtyp.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    /// <param name="root">Art des Verzeichnisses.</param>
    private static void ParseCmt(
        ByteSource source, List<IsoBox> boxes, string type, ImageMetadataBuilder builder, TiffParser.RootDirectory root)
    {
        if (boxes.FirstOrDefault(box => box.Type == type) is { } box)
        {
            TiffParser.Parse(source, box.Start, builder, root);
        }
    }
}
