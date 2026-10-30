using System.Text;

namespace FileRenamer.Core.Metadata.Readers;

/// <summary>
/// Liest ID3-Tags aus MP3-Dateien: ID3v2.2, v2.3 und v2.4 am Dateianfang, ersatzweise ID3v1/v1.1 am Dateiende.
/// </summary>
internal static class Id3Reader
{
    /// <summary>
    /// Zuordnung der ID3v2-Frames (v2.3/2.4 und v2.2) zu den Feldern.
    /// </summary>
    private static readonly Dictionary<string, Action<AudioMetadataBuilder, string>> Frames = new()
    {
        ["TPE1"] = (b, v) => b.Artist(v), ["TP1"] = (b, v) => b.Artist(v),
        ["TIT2"] = (b, v) => b.Title(v), ["TT2"] = (b, v) => b.Title(v),
        ["TALB"] = (b, v) => b.Album(v), ["TAL"] = (b, v) => b.Album(v),
        ["TPE2"] = (b, v) => b.AlbumArtist(v), ["TP2"] = (b, v) => b.AlbumArtist(v),
        ["TCOM"] = (b, v) => b.Composer(v), ["TCM"] = (b, v) => b.Composer(v),
        ["TCON"] = (b, v) => b.Genre(v), ["TCO"] = (b, v) => b.Genre(v),
        ["TRCK"] = (b, v) => b.Track(v), ["TRK"] = (b, v) => b.Track(v),
        ["TPOS"] = (b, v) => b.Disc(v), ["TPA"] = (b, v) => b.Disc(v),
        ["TDRC"] = (b, v) => b.Year(v), ["TYER"] = (b, v) => b.Year(v), ["TYE"] = (b, v) => b.Year(v),
    };

    /// <summary>
    /// Wertet die ID3-Tags einer MP3-Datei aus.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    public static void Read(ByteSource source, AudioMetadataBuilder builder)
    {
        ReadVersion2(source, builder);
        ReadVersion1(source, builder);
    }

    /// <summary>
    /// Liefert die Länge eines ID3v2-Tags am Anfang (inklusive Header), damit z. B. FLAC dahinter weiterlesen kann.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <returns>Länge in Bytes oder 0 ohne Tag.</returns>
    public static long Version2Length(ByteSource source) =>
        source.StartsWith(0, "ID3") ? 10 + SyncSafe(source.Read(6, 4)) : 0;

    /// <summary>
    /// Liest ein ID3v2-Tag.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    private static void ReadVersion2(ByteSource source, AudioMetadataBuilder builder)
    {
        if (!source.StartsWith(0, "ID3"))
        {
            return;
        }

        var header = source.Read(0, 10);
        var major = header[3];
        var flags = header[5];
        var tag = source.Read(10, SyncSafe(header.AsSpan(6, 4)));
        if (major < 4 && (flags & 0x80) != 0)
        {
            tag = RemoveUnsynchronisation(tag);
        }

        var position = (flags & 0x40) == 0 || major == 2 ? 0 : ExtendedHeaderLength(tag, major);
        while (position < tag.Length && ReadFrame(tag, ref position, major, builder))
        {
        }
    }

    /// <summary>
    /// Liest einen Frame und rückt die Position dahinter.
    /// </summary>
    /// <param name="tag">Die Tag-Daten.</param>
    /// <param name="position">Aktuelle Position.</param>
    /// <param name="major">ID3v2-Hauptversion.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    /// <returns><c>false</c> am Ende der Frames (Padding).</returns>
    private static bool ReadFrame(byte[] tag, ref int position, byte major, AudioMetadataBuilder builder)
    {
        var idLength = major == 2 ? 3 : 4;
        var headerLength = major == 2 ? 6 : 10;
        if (position + headerLength > tag.Length || tag[position] == 0)
        {
            return false;
        }

        var id = Encoding.ASCII.GetString(tag, position, idLength);
        var sizeBytes = tag.AsSpan(position + idLength, major == 2 ? 3 : 4);
        var size = major switch
        {
            2 => (sizeBytes[0] << 16) | (sizeBytes[1] << 8) | sizeBytes[2],
            4 => SyncSafe(sizeBytes),
            _ => (sizeBytes[0] << 24) | (sizeBytes[1] << 16) | (sizeBytes[2] << 8) | sizeBytes[3],
        };
        var formatFlags = major == 2 ? 0 : tag[position + 9];
        var dataStart = position + headerLength;
        if (size <= 0 || dataStart + size > tag.Length)
        {
            return false;
        }

        position = dataStart + size;
        if (Frames.TryGetValue(id, out var apply) && FrameData(tag[dataStart..position], major, formatFlags) is { } data)
        {
            apply(builder, DecodeText(data));
        }

        return true;
    }

    /// <summary>
    /// Bereitet die Frame-Daten auf; komprimierte oder verschlüsselte Frames werden übersprungen.
    /// </summary>
    /// <param name="data">Die Rohdaten.</param>
    /// <param name="major">ID3v2-Hauptversion.</param>
    /// <param name="formatFlags">Formatflags des Frames.</param>
    /// <returns>Die Textdaten oder <c>null</c>.</returns>
    private static byte[]? FrameData(byte[] data, byte major, int formatFlags)
    {
        if (major == 3)
        {
            return (formatFlags & 0xC0) != 0 ? null : data;
        }

        if (major == 4)
        {
            if ((formatFlags & 0x0C) != 0)
            {
                return null;
            }

            if ((formatFlags & 0x02) != 0)
            {
                data = RemoveUnsynchronisation(data);
            }

            return (formatFlags & 0x01) != 0 && data.Length >= 4 ? data[4..] : data;
        }

        return data;
    }

    /// <summary>
    /// Dekodiert einen Textframe: erstes Byte = Kodierung (0 ISO-8859-1, 1 UTF-16 mit BOM, 2 UTF-16BE, 3 UTF-8).
    /// Bei mehreren Werten (durch Nullzeichen getrennt) zählt der erste.
    /// </summary>
    /// <param name="data">Die Frame-Daten.</param>
    /// <returns>Der Text.</returns>
    private static string DecodeText(byte[] data)
    {
        if (data.Length < 2)
        {
            return string.Empty;
        }

        var text = data[0] switch
        {
            1 => DecodeUtf16WithBom(data.AsSpan(1)),
            2 => Encoding.BigEndianUnicode.GetString(data, 1, data.Length - 1),
            3 => Encoding.UTF8.GetString(data, 1, data.Length - 1),
            _ => Encoding.Latin1.GetString(data, 1, data.Length - 1),
        };
        return text.Split('\0', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
    }

    /// <summary>
    /// Dekodiert UTF-16 anhand der Byte-Order-Mark.
    /// </summary>
    /// <param name="bytes">Die Bytes inklusive BOM.</param>
    /// <returns>Der Text.</returns>
    private static string DecodeUtf16WithBom(ReadOnlySpan<byte> bytes) => bytes switch
    {
        [0xFE, 0xFF, ..] => Encoding.BigEndianUnicode.GetString(bytes[2..]),
        [0xFF, 0xFE, ..] => Encoding.Unicode.GetString(bytes[2..]),
        _ => Encoding.Unicode.GetString(bytes),
    };

    /// <summary>
    /// Liest ein ID3v1-/v1.1-Tag (letzte 128 Bytes) als Ersatz für fehlende Werte.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    private static void ReadVersion1(ByteSource source, AudioMetadataBuilder builder)
    {
        if (source.Length < 128 || !source.StartsWith(source.Length - 128, "TAG"))
        {
            return;
        }

        var tag = source.Read(source.Length - 128, 128);
        string Field(int start, int length) => Encoding.Latin1.GetString(tag, start, length).Split('\0')[0].Trim();

        builder.Title(Field(3, 30));
        builder.Artist(Field(33, 30));
        builder.Album(Field(63, 30));
        builder.Year(Field(93, 4));
        if (tag[125] == 0 && tag[126] != 0)
        {
            builder.Track(tag[126]);
        }

        builder.Genre(AudioMetadataBuilder.GenreName(tag[127]));
    }

    /// <summary>
    /// Länge des erweiterten Headers.
    /// </summary>
    /// <param name="tag">Die Tag-Daten.</param>
    /// <param name="major">ID3v2-Hauptversion.</param>
    /// <returns>Anzahl zu überspringender Bytes.</returns>
    private static int ExtendedHeaderLength(byte[] tag, byte major)
    {
        if (tag.Length < 4)
        {
            return tag.Length;
        }

        return major == 4
            ? SyncSafe(tag.AsSpan(0, 4))
            : 4 + ((tag[0] << 24) | (tag[1] << 16) | (tag[2] << 8) | tag[3]);
    }

    /// <summary>
    /// Entfernt die ID3-Unsynchronisation (0xFF 0x00 → 0xFF).
    /// </summary>
    /// <param name="data">Die Daten.</param>
    /// <returns>Die bereinigten Daten.</returns>
    private static byte[] RemoveUnsynchronisation(byte[] data)
    {
        var result = new List<byte>(data.Length);
        for (var index = 0; index < data.Length; index++)
        {
            result.Add(data[index]);
            if (data[index] == 0xFF && index + 1 < data.Length && data[index + 1] == 0x00)
            {
                index++;
            }
        }

        return [.. result];
    }

    /// <summary>
    /// Liest eine „synchsafe“ Zahl (je Byte 7 Bit).
    /// </summary>
    /// <param name="bytes">Vier Bytes.</param>
    /// <returns>Der Wert.</returns>
    private static int SyncSafe(ReadOnlySpan<byte> bytes) =>
        bytes.Length < 4 ? 0 : ((bytes[0] & 0x7F) << 21) | ((bytes[1] & 0x7F) << 14) | ((bytes[2] & 0x7F) << 7) | (bytes[3] & 0x7F);
}
