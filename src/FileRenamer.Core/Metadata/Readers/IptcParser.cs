using System.Text;

namespace FileRenamer.Core.Metadata.Readers;

/// <summary>
/// Liest Bildbeschreibung, Ersteller und Copyright aus IPTC-IIM-Daten (Datensätze 2:120, 2:80, 2:116).
/// </summary>
internal static class IptcParser
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, throwOnInvalidBytes: true);

    /// <summary>
    /// Wertet einen IPTC-Block aus.
    /// </summary>
    /// <param name="data">Die IPTC-Daten.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    public static void Parse(byte[] data, ImageMetadataBuilder builder)
    {
        string? description = null, creator = null, copyright = null;
        var position = 0;

        while (position + 5 <= data.Length && data[position] == 0x1C)
        {
            var record = data[position + 1];
            var dataset = data[position + 2];
            var length = (data[position + 3] << 8) | data[position + 4];
            if ((length & 0x8000) != 0 || position + 5 + length > data.Length)
            {
                break;
            }

            if (record == 2)
            {
                var value = Decode(data.AsSpan(position + 5, length));
                switch (dataset)
                {
                    case 120: description ??= value; break;
                    case 80: creator ??= value; break;
                    case 116: copyright ??= value; break;
                }
            }

            position += 5 + length;
        }

        builder.Merge(new ImageMetadata { Description = description, Creator = creator, Copyright = copyright });
    }

    /// <summary>
    /// Dekodiert einen Text als UTF-8, ersatzweise als ISO-8859-1.
    /// </summary>
    /// <param name="bytes">Die Bytes.</param>
    /// <returns>Der Text.</returns>
    private static string Decode(ReadOnlySpan<byte> bytes)
    {
        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }
}
