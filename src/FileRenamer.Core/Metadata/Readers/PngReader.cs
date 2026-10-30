using System.Text;

namespace FileRenamer.Core.Metadata.Readers;

/// <summary>
/// Liest Bildmaße (IHDR), EXIF (eXIf), XMP (iTXt „XML:com.adobe.xmp“) und Textangaben (tEXt/iTXt) aus PNG-Dateien.
/// </summary>
internal static class PngReader
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// Wertet die PNG-Chunks aus. Reihenfolge der Priorität: EXIF, XMP, PNG-Text.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    public static void Read(ByteSource source, ImageMetadataBuilder builder)
    {
        if (!source.Read(0, 8).AsSpan().SequenceEqual(Signature))
        {
            return;
        }

        var chunks = ReadChunks(source);
        if (chunks.FirstOrDefault(chunk => chunk.Type == "eXIf") is { } exif)
        {
            TiffParser.Parse(source, exif.Start, builder);
        }

        var texts = chunks.Where(chunk => chunk.Type is "tEXt" or "iTXt").Select(chunk => ReadText(source, chunk)).OfType<(string Key, string Value)>().ToList();
        if (texts.FirstOrDefault(text => text.Key == "XML:com.adobe.xmp") is { Value: { } xmp })
        {
            XmpParser.Parse(xmp, builder);
        }

        builder.Merge(new ImageMetadata
        {
            Description = Lookup(texts, "Description") ?? Lookup(texts, "Title"),
            Creator = Lookup(texts, "Author"),
            Copyright = Lookup(texts, "Copyright"),
        });

        if (chunks.FirstOrDefault(chunk => chunk.Type == "IHDR") is { } header)
        {
            builder.Merge(new ImageMetadata
            {
                Width = (int)source.UInt32(header.Start, littleEndian: false),
                Height = (int)source.UInt32(header.Start + 4, littleEndian: false),
            });
        }
    }

    /// <summary>
    /// Liest die Chunk-Liste bis IDAT/IEND.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <returns>Typ, Datenbeginn und Länge je Chunk.</returns>
    private static List<Chunk> ReadChunks(ByteSource source)
    {
        var chunks = new List<Chunk>();
        long position = 8;
        while (position + 8 <= source.Length && chunks.Count < 1000)
        {
            var length = source.UInt32(position, littleEndian: false);
            var type = Encoding.ASCII.GetString(source.Read(position + 4, 4));
            if (type == "IEND" || length > ByteSource.MaxReadLength)
            {
                break;
            }

            chunks.Add(new Chunk(type, position + 8, (int)length));
            position += 12 + length;
        }

        return chunks;
    }

    /// <summary>
    /// Liest Schlüssel und Text eines tEXt- oder (unkomprimierten) iTXt-Chunks.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="chunk">Der Chunk.</param>
    /// <returns>Schlüssel und Text oder <c>null</c>.</returns>
    private static (string Key, string Value)? ReadText(ByteSource source, Chunk chunk)
    {
        var data = source.Read(chunk.Start, chunk.Length);
        var keyEnd = Array.IndexOf(data, (byte)0);
        if (keyEnd <= 0)
        {
            return null;
        }

        var key = Encoding.Latin1.GetString(data, 0, keyEnd);
        if (chunk.Type == "tEXt")
        {
            return (key, Encoding.Latin1.GetString(data, keyEnd + 1, data.Length - keyEnd - 1));
        }

        // iTXt: Kompression (1), Methode (1), Sprache\0, übersetzter Schlüssel\0, Text (UTF-8)
        if (keyEnd + 2 >= data.Length || data[keyEnd + 1] != 0)
        {
            return null;
        }

        var position = keyEnd + 3;
        for (var nulls = 0; nulls < 2 && position < data.Length; position++)
        {
            if (data[position] == 0)
            {
                nulls++;
            }
        }

        return position <= data.Length ? (key, Encoding.UTF8.GetString(data, position, data.Length - position)) : null;
    }

    /// <summary>
    /// Sucht einen Textwert nach Schlüssel.
    /// </summary>
    /// <param name="texts">Die Texte.</param>
    /// <param name="key">Der Schlüssel.</param>
    /// <returns>Der Wert oder <c>null</c>.</returns>
    private static string? Lookup(List<(string Key, string Value)> texts, string key) =>
        texts.Where(text => text.Key == key).Select(text => text.Value).FirstOrDefault();

    /// <summary>
    /// Ein PNG-Chunk.
    /// </summary>
    /// <param name="Type">Chunk-Typ.</param>
    /// <param name="Start">Beginn der Daten.</param>
    /// <param name="Length">Länge der Daten.</param>
    private sealed record Chunk(string Type, long Start, int Length);
}
