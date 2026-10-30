using System.Buffers.Binary;
using System.Text;

namespace FileRenamer.Core.Tests.Support;

/// <summary>
/// Erzeugt minimale, aber strukturell korrekte Foto- und Audiodateien mit Metadaten für Tests
/// (JPEG, PNG, HEIC, CR3, RAF, TIFF-basierte RAW, MP3/ID3, FLAC, M4A).
/// </summary>
internal static class MediaFiles
{
    /// <summary>Beispielwerte der Kamera-Metadaten.</summary>
    public static readonly DateTime TakenAt = new(2026, 10, 12, 14, 30, 5);

    /// <summary>
    /// TIFF-Struktur mit typischen EXIF-Daten: Canon EOS R6, f/4, 1/250 s, ISO 400, 50 mm, Orientierung 6 (90°),
    /// GPS 48°08'14.748"N 11°34'31.764"E, 519 m, Copyright.
    /// </summary>
    /// <param name="littleEndian">Byte-Reihenfolge.</param>
    /// <returns>Die TIFF-Bytes.</returns>
    public static byte[] CameraTiff(bool littleEndian = true)
    {
        var writer = new TiffWriter(littleEndian);
        var main = writer.NewIfd().Ascii(0x010F, "Canon").Ascii(0x0110, "Canon EOS R6").Short(0x0112, 6)
            .Ascii(0x0132, "2026:10:13 09:00:00").Ascii(0x8298, "(c) Max Muster");
        var exif = writer.NewIfd().Rational(0x829A, (1, 250)).Rational(0x829D, (4, 1)).Short(0x8827, 400)
            .Ascii(0x9003, "2026:10:12 14:30:05").Ascii(0x9004, "2026:10:12 14:30:06").Rational(0x920A, (50, 1))
            .Long(0xA002, 6000).Long(0xA003, 4000).Ascii(0xA434, "RF24-105mm F4 L IS USM");
        var gps = writer.NewIfd().Ascii(1, "N").Rational(2, (48, 1), (8, 1), (14748, 1000))
            .Ascii(3, "E").Rational(4, (11, 1), (34, 1), (31764, 1000)).Byte(5, 0).Rational(6, (519, 1));
        return writer.Build(main, exif, gps);
    }

    /// <summary>
    /// JPEG mit EXIF, optional IPTC und XMP.
    /// </summary>
    /// <param name="tiff">EXIF-TIFF oder <c>null</c>.</param>
    /// <param name="iptc">IPTC-Datensätze (Dataset → Text) oder <c>null</c>.</param>
    /// <param name="xmpCreator">Ersteller im XMP oder <c>null</c>.</param>
    /// <returns>Die JPEG-Bytes.</returns>
    public static byte[] Jpeg(byte[]? tiff, IReadOnlyDictionary<byte, string>? iptc = null, string? xmpCreator = null)
    {
        var bytes = new List<byte> { 0xFF, 0xD8 };
        if (tiff is not null)
        {
            bytes.AddRange(Segment(0xE1, [.. Encoding.ASCII.GetBytes("Exif\0\0"), .. tiff]));
        }

        if (iptc is not null)
        {
            bytes.AddRange(Segment(0xED, Photoshop(Iptc(iptc))));
        }

        if (xmpCreator is not null)
        {
            var xmp = "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">" +
                      "<rdf:Description xmlns:dc=\"http://purl.org/dc/elements/1.1/\"><dc:creator><rdf:Seq><rdf:li>" + xmpCreator +
                      "</rdf:li></rdf:Seq></dc:creator><dc:description><rdf:Alt><rdf:li xml:lang=\"x-default\">XMP-Beschreibung</rdf:li></rdf:Alt></dc:description>" +
                      "</rdf:Description></rdf:RDF></x:xmpmeta>";
            bytes.AddRange(Segment(0xE1, [.. Encoding.ASCII.GetBytes("http://ns.adobe.com/xap/1.0/\0"), .. Encoding.UTF8.GetBytes(xmp)]));
        }

        bytes.AddRange(Segment(0xC0, [8, 0x00, 0x20, 0x00, 0x40, 3, 1, 0x22, 0, 2, 0x11, 1, 3, 0x11, 1]));
        bytes.AddRange([0xFF, 0xDA, 0x00, 0x02, 0xFF, 0xD9]);
        return [.. bytes];
    }

    /// <summary>
    /// PNG mit Bildmaßen, eXIf-Chunk und Text „Author“.
    /// </summary>
    /// <param name="tiff">EXIF-TIFF oder <c>null</c>.</param>
    /// <returns>Die PNG-Bytes.</returns>
    public static byte[] Png(byte[]? tiff)
    {
        var bytes = new List<byte> { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        bytes.AddRange(PngChunk("IHDR", [.. BigEndian32(640), .. BigEndian32(480), 8, 2, 0, 0, 0]));
        if (tiff is not null)
        {
            bytes.AddRange(PngChunk("eXIf", tiff));
        }

        bytes.AddRange(PngChunk("tEXt", Encoding.Latin1.GetBytes("Author\0PNG-Autor")));
        bytes.AddRange(PngChunk("IEND", []));
        return [.. bytes];
    }

    /// <summary>
    /// HEIC mit einem EXIF-Item (iinf/iloc in meta, Daten in mdat).
    /// </summary>
    /// <param name="tiff">EXIF-TIFF.</param>
    /// <returns>Die HEIC-Bytes.</returns>
    public static byte[] Heic(byte[] tiff)
    {
        var ftyp = Box("ftyp", [.. Encoding.ASCII.GetBytes("heic"), 0, 0, 0, 0, .. Encoding.ASCII.GetBytes("mif1heic")]);
        byte[] item = [.. BigEndian32(6), .. Encoding.ASCII.GetBytes("Exif\0\0"), .. tiff];
        byte[] Meta(uint offset) => Box("meta", [0, 0, 0, 0,
            .. Box("iinf", [0, 0, 0, 0, 0, 1, .. Box("infe", [2, 0, 0, 0, 0, 1, 0, 0, .. Encoding.ASCII.GetBytes("Exif"), 0])]),
            .. Box("iloc", [0, 0, 0, 0, 0x44, 0x00, 0, 1, 0, 1, 0, 0, 0, 1, .. BigEndian32(offset), .. BigEndian32((uint)item.Length)])]);
        var dataOffset = (uint)(ftyp.Length + Meta(0).Length + 8);
        return [.. ftyp, .. Meta(dataOffset), .. Box("mdat", item)];
    }

    /// <summary>
    /// Canon CR3 mit CMT1 (IFD0), CMT2 (EXIF) und CMT4 (GPS).
    /// </summary>
    /// <returns>Die CR3-Bytes.</returns>
    public static byte[] Cr3()
    {
        var writer = new TiffWriter();
        var cmt1 = writer.Build(writer.NewIfd().Ascii(0x010F, "Canon").Ascii(0x0110, "Canon EOS R5"));
        var cmt2 = writer.Build(writer.NewIfd().Ascii(0x9003, "2026:09:30 08:15:00").Short(0x8827, 800));
        var cmt4 = writer.Build(writer.NewIfd().Ascii(1, "S").Rational(2, (33, 1), (51, 1), (0, 1)).Ascii(3, "W").Rational(4, (70, 1), (30, 1), (0, 1)));
        byte[] uuid = [0x85, 0xC0, 0xB6, 0x87, 0x82, 0x0F, 0x11, 0xE0, 0x81, 0x11, 0xF4, 0xCE, 0x46, 0x2B, 0x6A, 0x48];
        return [.. Box("ftyp", Encoding.ASCII.GetBytes("crx \0\0\0\0")),
                .. Box("moov", Box("uuid", [.. uuid, .. Box("CMT1", cmt1), .. Box("CMT2", cmt2), .. Box("CMT4", cmt4)]))];
    }

    /// <summary>
    /// Fujifilm RAF: Header mit Offset/Länge des eingebetteten JPEG.
    /// </summary>
    /// <param name="jpeg">Das eingebettete JPEG.</param>
    /// <returns>Die RAF-Bytes.</returns>
    public static byte[] Raf(byte[] jpeg)
    {
        var header = new byte[100];
        Encoding.ASCII.GetBytes("FUJIFILMCCD-RAW 0201FF383501").CopyTo(header, 0);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(84), 100);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(88), (uint)jpeg.Length);
        return [.. header, .. jpeg];
    }

    /// <summary>
    /// MP3 mit ID3v2-Tag (Version 2, 3 oder 4) und optional ID3v1 am Ende.
    /// </summary>
    /// <param name="major">ID3v2-Hauptversion oder 0 für kein ID3v2.</param>
    /// <param name="frames">Frame-Id → Text.</param>
    /// <param name="v1">ID3v1-Tag anhängen (Titel „V1 Titel“, Interpret „V1 Artist“, Track 7, Genre 13 = Pop).</param>
    /// <returns>Die MP3-Bytes.</returns>
    public static byte[] Mp3(byte major, IReadOnlyDictionary<string, string> frames, bool v1 = false)
    {
        var bytes = new List<byte>();
        if (major > 0)
        {
            var body = frames.SelectMany(frame => Id3Frame(major, frame.Key, frame.Value)).ToList();
            body.AddRange(new byte[16]);
            bytes.AddRange([.. Encoding.ASCII.GetBytes("ID3"), major, 0, 0, .. SyncSafe(body.Count)]);
            bytes.AddRange(body);
        }

        bytes.AddRange([0xFF, 0xFB, 0x90, 0x00, 0, 0, 0, 0]);
        if (v1)
        {
            var tag = new byte[128];
            Encoding.ASCII.GetBytes("TAG").CopyTo(tag, 0);
            Encoding.Latin1.GetBytes("V1 Titel").CopyTo(tag, 3);
            Encoding.Latin1.GetBytes("V1 Artist").CopyTo(tag, 33);
            Encoding.Latin1.GetBytes("1999").CopyTo(tag, 93);
            tag[126] = 7;
            tag[127] = 13;
            bytes.AddRange(tag);
        }

        return [.. bytes];
    }

    /// <summary>
    /// FLAC mit STREAMINFO und Vorbis-Comment-Block.
    /// </summary>
    /// <param name="comments">Einträge „SCHLÜSSEL=Wert“.</param>
    /// <returns>Die FLAC-Bytes.</returns>
    public static byte[] Flac(params string[] comments)
    {
        var vendor = Encoding.UTF8.GetBytes("test");
        var block = new List<byte>(LittleEndian32(vendor.Length));
        block.AddRange(vendor);
        block.AddRange(LittleEndian32(comments.Length));
        foreach (var comment in comments)
        {
            var data = Encoding.UTF8.GetBytes(comment);
            block.AddRange(LittleEndian32(data.Length));
            block.AddRange(data);
        }

        return [.. Encoding.ASCII.GetBytes("fLaC"), 0x00, 0, 0, 34, .. new byte[34],
                0x84, (byte)(block.Count >> 16), (byte)(block.Count >> 8), (byte)block.Count, .. block];
    }

    /// <summary>
    /// M4A mit iTunes-Atomen (Text, Track 3 von 12, Disk 1).
    /// </summary>
    /// <param name="texts">Atomtyp (z. B. „©ART“) → Text.</param>
    /// <returns>Die M4A-Bytes.</returns>
    public static byte[] M4a(IReadOnlyDictionary<string, string> texts)
    {
        var items = texts.SelectMany(text => Box(text.Key, Box("data", [0, 0, 0, 1, 0, 0, 0, 0, .. Encoding.UTF8.GetBytes(text.Value)]))).ToList();
        items.AddRange(Box("trkn", Box("data", [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 12, 0, 0])));
        items.AddRange(Box("disk", Box("data", [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 2])));
        return [.. Box("ftyp", Encoding.ASCII.GetBytes("M4A \0\0\0\0")),
                .. Box("moov", Box("udta", Box("meta", [0, 0, 0, 0, .. Box("ilst", [.. items])])))];
    }

    /// <summary>
    /// Ein JPEG-Segment.
    /// </summary>
    /// <param name="marker">Marker.</param>
    /// <param name="data">Nutzdaten.</param>
    /// <returns>Die Bytes inklusive Marker und Länge.</returns>
    private static byte[] Segment(byte marker, byte[] data) =>
        [0xFF, marker, (byte)((data.Length + 2) >> 8), (byte)(data.Length + 2), .. data];

    /// <summary>
    /// IPTC-Datensätze im Record 2.
    /// </summary>
    /// <param name="values">Dataset → Text.</param>
    /// <returns>Die IPTC-Bytes.</returns>
    private static byte[] Iptc(IReadOnlyDictionary<byte, string> values) =>
        [.. values.SelectMany(value =>
        {
            var text = Encoding.UTF8.GetBytes(value.Value);
            return new byte[] { 0x1C, 2, value.Key, (byte)(text.Length >> 8), (byte)text.Length }.Concat(text);
        })];

    /// <summary>
    /// Photoshop-Ressourcenblock mit IPTC (8BIM 0x0404).
    /// </summary>
    /// <param name="iptc">IPTC-Daten.</param>
    /// <returns>Die APP13-Nutzdaten.</returns>
    private static byte[] Photoshop(byte[] iptc) =>
        [.. Encoding.ASCII.GetBytes("Photoshop 3.0\0"), .. Encoding.ASCII.GetBytes("8BIM"), 0x04, 0x04, 0, 0,
         .. BigEndian32((uint)iptc.Length), .. iptc, .. (iptc.Length % 2 == 1 ? new byte[1] : [])];

    /// <summary>
    /// Ein ID3v2-Textframe (ISO-8859-1 bis v2.3, UTF-8 ab v2.4).
    /// </summary>
    /// <param name="major">Hauptversion.</param>
    /// <param name="id">Frame-Id.</param>
    /// <param name="text">Text.</param>
    /// <returns>Die Frame-Bytes.</returns>
    private static byte[] Id3Frame(byte major, string id, string text)
    {
        byte[] data = major == 4 ? [3, .. Encoding.UTF8.GetBytes(text)] : [0, .. Encoding.Latin1.GetBytes(text)];
        return major switch
        {
            2 => [.. Encoding.ASCII.GetBytes(id), (byte)(data.Length >> 16), (byte)(data.Length >> 8), (byte)data.Length, .. data],
            4 => [.. Encoding.ASCII.GetBytes(id), .. SyncSafe(data.Length), 0, 0, .. data],
            _ => [.. Encoding.ASCII.GetBytes(id), .. BigEndian32((uint)data.Length), 0, 0, .. data],
        };
    }

    /// <summary>
    /// Eine ISOBMFF-Box.
    /// </summary>
    /// <param name="type">Boxtyp (Latin-1).</param>
    /// <param name="payload">Nutzdaten.</param>
    /// <returns>Die Box-Bytes.</returns>
    private static byte[] Box(string type, byte[] payload) =>
        [.. BigEndian32((uint)(payload.Length + 8)), .. Encoding.Latin1.GetBytes(type), .. payload];

    /// <summary>
    /// Ein PNG-Chunk (Prüfsumme wird nicht ausgewertet).
    /// </summary>
    /// <param name="type">Chunk-Typ.</param>
    /// <param name="data">Daten.</param>
    /// <returns>Die Chunk-Bytes.</returns>
    private static byte[] PngChunk(string type, byte[] data) =>
        [.. BigEndian32((uint)data.Length), .. Encoding.ASCII.GetBytes(type), .. data, 0, 0, 0, 0];

    /// <summary>32-Bit Big Endian.</summary>
    /// <param name="value">Wert.</param>
    /// <returns>Bytes.</returns>
    private static byte[] BigEndian32(uint value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        return bytes;
    }

    /// <summary>32-Bit Little Endian.</summary>
    /// <param name="value">Wert.</param>
    /// <returns>Bytes.</returns>
    private static byte[] LittleEndian32(int value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>„Synchsafe“-Zahl (je Byte 7 Bit).</summary>
    /// <param name="value">Wert.</param>
    /// <returns>Bytes.</returns>
    private static byte[] SyncSafe(int value) =>
        [(byte)((value >> 21) & 0x7F), (byte)((value >> 14) & 0x7F), (byte)((value >> 7) & 0x7F), (byte)(value & 0x7F)];
}
