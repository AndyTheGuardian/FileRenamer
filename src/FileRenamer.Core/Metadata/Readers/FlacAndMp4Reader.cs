using System.Buffers.Binary;
using System.Text;

namespace FileRenamer.Core.Metadata.Readers;

/// <summary>
/// Liest Audio-Tags aus FLAC (Vorbis Comments) und M4A (iTunes-Atome in moov/udta/meta/ilst).
/// Beide Formate werden auf dieselben Platzhalter wie ID3 abgebildet (E57).
/// </summary>
internal static class FlacAndMp4Reader
{
    /// <summary>
    /// Zuordnung der Vorbis-Comment-Schlüssel zu den Feldern.
    /// </summary>
    private static readonly Dictionary<string, Action<AudioMetadataBuilder, string>> VorbisFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ARTIST"] = (b, v) => b.Artist(v),
            ["TITLE"] = (b, v) => b.Title(v),
            ["ALBUM"] = (b, v) => b.Album(v),
            ["ALBUMARTIST"] = (b, v) => b.AlbumArtist(v),
            ["ALBUM ARTIST"] = (b, v) => b.AlbumArtist(v),
            ["COMPOSER"] = (b, v) => b.Composer(v),
            ["GENRE"] = (b, v) => b.Genre(v),
            ["TRACKNUMBER"] = (b, v) => b.Track(v),
            ["DISCNUMBER"] = (b, v) => b.Disc(v),
            ["DATE"] = (b, v) => b.Year(v),
            ["YEAR"] = (b, v) => b.Year(v),
        };

    /// <summary>
    /// Zuordnung der iTunes-Atome zu den Textfeldern.
    /// </summary>
    private static readonly Dictionary<string, Action<AudioMetadataBuilder, string>> Mp4TextFields = new()
    {
        ["©ART"] = (b, v) => b.Artist(v),
        ["©nam"] = (b, v) => b.Title(v),
        ["©alb"] = (b, v) => b.Album(v),
        ["aART"] = (b, v) => b.AlbumArtist(v),
        ["©wrt"] = (b, v) => b.Composer(v),
        ["©gen"] = (b, v) => b.Genre(v),
        ["©day"] = (b, v) => b.Year(v),
    };

    /// <summary>
    /// Wertet eine FLAC-Datei aus (ein vorangestelltes ID3v2-Tag wird übersprungen).
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    public static void ReadFlac(ByteSource source, AudioMetadataBuilder builder)
    {
        var position = Id3Reader.Version2Length(source);
        if (!source.StartsWith(position, "fLaC"))
        {
            return;
        }

        position += 4;
        var isLast = false;
        while (!isLast && position + 4 <= source.Length)
        {
            var header = source.Read(position, 4);
            isLast = (header[0] & 0x80) != 0;
            var type = header[0] & 0x7F;
            var length = (header[1] << 16) | (header[2] << 8) | header[3];
            if (type == 4)
            {
                ReadVorbisComments(source.Read(position + 4, length), builder);
            }

            position += 4 + length;
        }
    }

    /// <summary>
    /// Wertet eine M4A-Datei aus.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    public static void ReadMp4(ByteSource source, AudioMetadataBuilder builder)
    {
        var ilst = IsoBox.Find(source, IsoBox.ReadAll(source, 0, source.Length), "moov", "udta", "meta", "ilst");
        if (ilst is null)
        {
            return;
        }

        foreach (var item in ilst.Children(source))
        {
            var data = item.Children(source).FirstOrDefault(box => box.Type == "data");
            if (data is null || data.Length < 8)
            {
                continue;
            }

            var payload = source.Read(data.Start + 8, (int)(data.Length - 8));
            ApplyMp4Item(item.Type, payload, builder);
        }
    }

    /// <summary>
    /// Übernimmt ein iTunes-Atom.
    /// </summary>
    /// <param name="type">Atomtyp, z. B. „©ART“ oder „trkn“.</param>
    /// <param name="payload">Nutzdaten des „data“-Atoms (ohne Typ- und Länderfeld).</param>
    /// <param name="builder">Sammelt die Werte.</param>
    private static void ApplyMp4Item(string type, byte[] payload, AudioMetadataBuilder builder)
    {
        if (Mp4TextFields.TryGetValue(type, out var apply))
        {
            apply(builder, Encoding.UTF8.GetString(payload));
        }
        else if (type is "trkn" or "disk" && payload.Length >= 4)
        {
            int number = BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(2, 2));
            if (type == "trkn")
            {
                builder.Track(number);
            }
            else
            {
                builder.Disc(number);
            }
        }
        else if (type == "gnre" && payload.Length >= 2)
        {
            builder.Genre(AudioMetadataBuilder.GenreName(BinaryPrimitives.ReadUInt16BigEndian(payload) - 1));
        }
    }

    /// <summary>
    /// Wertet einen Vorbis-Comment-Block aus (Längenangaben Little Endian, Einträge „SCHLÜSSEL=Wert“ in UTF-8).
    /// </summary>
    /// <param name="block">Die Blockdaten.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    private static void ReadVorbisComments(byte[] block, AudioMetadataBuilder builder)
    {
        if (block.Length < 8)
        {
            return;
        }

        var position = 4 + (int)BinaryPrimitives.ReadUInt32LittleEndian(block);
        if (position + 4 > block.Length)
        {
            return;
        }

        var count = BinaryPrimitives.ReadUInt32LittleEndian(block.AsSpan(position));
        position += 4;
        for (var index = 0; index < count && position + 4 <= block.Length; index++)
        {
            var length = (int)BinaryPrimitives.ReadUInt32LittleEndian(block.AsSpan(position));
            position += 4;
            if (length < 0 || position + length > block.Length)
            {
                return;
            }

            var entry = Encoding.UTF8.GetString(block, position, length);
            position += length;
            var separator = entry.IndexOf('=');
            if (separator > 0 && VorbisFields.TryGetValue(entry[..separator], out var apply))
            {
                apply(builder, entry[(separator + 1)..]);
            }
        }
    }
}
