using System.Globalization;
using System.Text;

namespace FileRenamer.Core.Metadata.Readers;

/// <summary>
/// Liest EXIF-Daten aus einer TIFF-Struktur (Byte-Reihenfolge, IFD0, EXIF-IFD, GPS-IFD).
/// Grundlage für JPEG, TIFF, die TIFF-basierten RAW-Formate, PNG (eXIf), HEIC und CR3.
/// </summary>
internal static class TiffParser
{
    /// <summary>
    /// Art des Verzeichnisses, mit dem das Lesen beginnt.
    /// </summary>
    public enum RootDirectory
    {
        /// <summary>IFD0 mit Verweisen auf EXIF- und GPS-IFD.</summary>
        Main,

        /// <summary>Das Verzeichnis enthält direkt EXIF-Tags (CR3: CMT2).</summary>
        Exif,

        /// <summary>Das Verzeichnis enthält direkt GPS-Tags (CR3: CMT4).</summary>
        Gps,
    }

    private const int MaxEntries = 1000;

    /// <summary>
    /// Wertet die TIFF-Struktur ab der angegebenen Position aus und ergänzt die Bildinformationen.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="tiffStart">Position des TIFF-Headers („II“ oder „MM“).</param>
    /// <param name="builder">Sammelt die Werte.</param>
    /// <param name="root">Art des ersten Verzeichnisses.</param>
    public static void Parse(ByteSource source, long tiffStart, ImageMetadataBuilder builder, RootDirectory root = RootDirectory.Main)
    {
        bool littleEndian;
        if (source.StartsWith(tiffStart, "II"))
        {
            littleEndian = true;
        }
        else if (source.StartsWith(tiffStart, "MM"))
        {
            littleEndian = false;
        }
        else
        {
            return;
        }

        var context = new Context(source, tiffStart, littleEndian, builder);
        var firstIfd = source.UInt32(tiffStart + 4, littleEndian);
        context.ReadDirectory(firstIfd, root);
    }

    /// <summary>
    /// Zustand während des Lesens einer TIFF-Struktur.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="tiffStart">Position des TIFF-Headers.</param>
    /// <param name="littleEndian">Byte-Reihenfolge.</param>
    /// <param name="builder">Sammelt die Werte.</param>
    private sealed class Context(ByteSource source, long tiffStart, bool littleEndian, ImageMetadataBuilder builder)
    {
        private readonly HashSet<uint> _visited = [];

        /// <summary>
        /// Liest ein Verzeichnis und folgt den Verweisen auf EXIF- und GPS-Verzeichnis.
        /// </summary>
        /// <param name="offset">Offset relativ zum TIFF-Header.</param>
        /// <param name="kind">Art des Verzeichnisses.</param>
        public void ReadDirectory(uint offset, RootDirectory kind)
        {
            if (offset == 0 || !_visited.Add(offset))
            {
                return;
            }

            var tags = ReadEntries(offset);
            switch (kind)
            {
                case RootDirectory.Main:
                    ApplyMain(tags);
                    break;
                case RootDirectory.Exif:
                    ApplyExif(tags);
                    break;
                default:
                    ApplyGps(tags);
                    break;
            }
        }

        /// <summary>
        /// Liest alle Einträge eines Verzeichnisses.
        /// </summary>
        /// <param name="offset">Offset relativ zum TIFF-Header.</param>
        /// <returns>Die Einträge nach Tag-Nummer.</returns>
        private Dictionary<ushort, Entry> ReadEntries(uint offset)
        {
            var start = tiffStart + offset;
            var count = Math.Min(source.UInt16(start, littleEndian), (ushort)MaxEntries);
            var entries = new Dictionary<ushort, Entry>();

            for (var index = 0; index < count; index++)
            {
                var position = start + 2 + index * 12;
                var tag = source.UInt16(position, littleEndian);
                entries.TryAdd(tag, new Entry(
                    source.UInt16(position + 2, littleEndian),
                    source.UInt32(position + 4, littleEndian),
                    position + 8));
            }

            return entries;
        }

        /// <summary>
        /// Wertet IFD0 aus.
        /// </summary>
        /// <param name="tags">Die Einträge.</param>
        private void ApplyMain(Dictionary<ushort, Entry> tags)
        {
            builder.SetDateModified(ParseDate(Text(tags, 0x0132)));
            builder.Merge(new ImageMetadata
            {
                Description = Text(tags, 0x010E),
                Make = Text(tags, 0x010F),
                Model = Text(tags, 0x0110),
                Orientation = ToDegrees(Number(tags, 0x0112)),
                Creator = Text(tags, 0x013B),
                Copyright = Text(tags, 0x8298),
            });

            ReadSubDirectory(tags, 0x8769, RootDirectory.Exif);
            ReadSubDirectory(tags, 0x8825, RootDirectory.Gps);

            // Maße aus IFD0 nur als Ersatz (bei RAW-Dateien ist IFD0 oft nur eine Vorschau)
            builder.Merge(new ImageMetadata { Width = (int?)Number(tags, 0x0100), Height = (int?)Number(tags, 0x0101) });
        }

        /// <summary>
        /// Wertet das EXIF-Verzeichnis aus.
        /// </summary>
        /// <param name="tags">Die Einträge.</param>
        private void ApplyExif(Dictionary<ushort, Entry> tags)
        {
            builder.SetDateOriginal(ParseDate(Text(tags, 0x9003)));
            builder.SetDateDigitized(ParseDate(Text(tags, 0x9004)));
            builder.Merge(new ImageMetadata
            {
                ExposureTime = Rationals(tags, 0x829A).FirstOrDefault(),
                FNumber = Rationals(tags, 0x829D).FirstOrDefault(),
                Iso = (int?)Number(tags, 0x8827),
                FocalLength = Rationals(tags, 0x920A).FirstOrDefault(),
                Width = (int?)Number(tags, 0xA002),
                Height = (int?)Number(tags, 0xA003),
                Lens = Text(tags, 0xA434),
            });
        }

        /// <summary>
        /// Wertet das GPS-Verzeichnis aus.
        /// </summary>
        /// <param name="tags">Die Einträge.</param>
        private void ApplyGps(Dictionary<ushort, Entry> tags)
        {
            var latitude = ToDecimalDegrees(Rationals(tags, 2), Text(tags, 1), "S");
            var longitude = ToDecimalDegrees(Rationals(tags, 4), Text(tags, 3), "W");
            var altitude = Rationals(tags, 6).FirstOrDefault();
            if (altitude is { } meters && Number(tags, 5) == 1)
            {
                altitude = -meters;
            }

            builder.Merge(new ImageMetadata { Latitude = latitude, Longitude = longitude, Altitude = altitude });
        }

        /// <summary>
        /// Folgt einem Verweis auf ein Unterverzeichnis.
        /// </summary>
        /// <param name="tags">Die Einträge.</param>
        /// <param name="tag">Tag mit dem Offset.</param>
        /// <param name="kind">Art des Unterverzeichnisses.</param>
        private void ReadSubDirectory(Dictionary<ushort, Entry> tags, ushort tag, RootDirectory kind)
        {
            if (Number(tags, tag) is { } offset)
            {
                ReadDirectory((uint)offset, kind);
            }
        }

        /// <summary>
        /// Liest einen ASCII-Wert.
        /// </summary>
        /// <param name="tags">Die Einträge.</param>
        /// <param name="tag">Tag-Nummer.</param>
        /// <returns>Der Text oder <c>null</c>.</returns>
        private string? Text(Dictionary<ushort, Entry> tags, ushort tag)
        {
            if (!tags.TryGetValue(tag, out var entry) || entry.Type is not (2 or 7 or 1))
            {
                return null;
            }

            var bytes = ValueBytes(entry, 1);
            var end = Array.IndexOf(bytes, (byte)0);
            var text = Encoding.UTF8.GetString(bytes, 0, end < 0 ? bytes.Length : end).Trim();
            return text.Length == 0 ? null : text;
        }

        /// <summary>
        /// Liest eine Ganzzahl (BYTE, SHORT oder LONG).
        /// </summary>
        /// <param name="tags">Die Einträge.</param>
        /// <param name="tag">Tag-Nummer.</param>
        /// <returns>Der Wert oder <c>null</c>.</returns>
        private long? Number(Dictionary<ushort, Entry> tags, ushort tag)
        {
            if (!tags.TryGetValue(tag, out var entry) || entry.Count == 0)
            {
                return null;
            }

            return entry.Type switch
            {
                1 => ValueBytes(entry, 1) is [var value, ..] ? value : null,
                3 => source.UInt16(ValueOffset(entry, 2), littleEndian),
                4 or 9 => source.UInt32(ValueOffset(entry, 4), littleEndian),
                _ => null,
            };
        }

        /// <summary>
        /// Liest Bruchzahlen (RATIONAL oder SRATIONAL).
        /// </summary>
        /// <param name="tags">Die Einträge.</param>
        /// <param name="tag">Tag-Nummer.</param>
        /// <returns>Die Werte als Gleitkommazahlen.</returns>
        private List<double?> Rationals(Dictionary<ushort, Entry> tags, ushort tag)
        {
            if (!tags.TryGetValue(tag, out var entry) || entry.Type is not (5 or 10))
            {
                return [];
            }

            var offset = ValueOffset(entry, 8);
            var values = new List<double?>();
            for (var index = 0; index < Math.Min(entry.Count, 8u); index++)
            {
                var numerator = source.UInt32(offset + index * 8, littleEndian);
                var denominator = source.UInt32(offset + index * 8 + 4, littleEndian);
                values.Add(denominator == 0 ? null
                    : entry.Type == 10 ? (int)numerator / (double)(int)denominator : numerator / (double)denominator);
            }

            return values;
        }

        /// <summary>
        /// Liest die Rohbytes eines Werts.
        /// </summary>
        /// <param name="entry">Der Eintrag.</param>
        /// <param name="elementSize">Größe eines Elements in Bytes.</param>
        /// <returns>Die Bytes.</returns>
        private byte[] ValueBytes(Entry entry, int elementSize) =>
            source.Read(ValueOffset(entry, elementSize), (int)Math.Min(entry.Count * (long)elementSize, 4096));

        /// <summary>
        /// Ermittelt die Position eines Werts: bis 4 Bytes direkt im Eintrag, sonst über den Offset.
        /// </summary>
        /// <param name="entry">Der Eintrag.</param>
        /// <param name="elementSize">Größe eines Elements in Bytes.</param>
        /// <returns>Die absolute Position.</returns>
        private long ValueOffset(Entry entry, int elementSize) =>
            entry.Count * (long)elementSize <= 4 ? entry.InlinePosition : tiffStart + source.UInt32(entry.InlinePosition, littleEndian);
    }

    /// <summary>
    /// Ein Verzeichniseintrag.
    /// </summary>
    /// <param name="Type">TIFF-Datentyp.</param>
    /// <param name="Count">Anzahl der Elemente.</param>
    /// <param name="InlinePosition">Position des Wert-/Offsetfelds.</param>
    private sealed record Entry(ushort Type, uint Count, long InlinePosition);

    /// <summary>
    /// Wandelt ein EXIF-Datum („2026:10:12 14:30:05“) um.
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <returns>Der Zeitpunkt oder <c>null</c>.</returns>
    internal static DateTime? ParseDate(string? text) =>
        text is not null && DateTime.TryParseExact(text.Trim(), "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value
            : null;

    /// <summary>
    /// Wandelt die EXIF-Orientierung (1–8) in eine Drehung in Grad um.
    /// </summary>
    /// <param name="orientation">EXIF-Wert.</param>
    /// <returns>0, 90, 180, 270 oder <c>null</c>.</returns>
    private static int? ToDegrees(long? orientation) => orientation switch
    {
        1 or 2 => 0,
        3 or 4 => 180,
        5 or 6 => 90,
        7 or 8 => 270,
        _ => null,
    };

    /// <summary>
    /// Rechnet Grad/Minuten/Sekunden in Dezimalgrad um.
    /// </summary>
    /// <param name="parts">Grad, Minuten, Sekunden.</param>
    /// <param name="reference">Himmelsrichtung (N/S bzw. E/W).</param>
    /// <param name="negative">Himmelsrichtung, die negativ zählt.</param>
    /// <returns>Dezimalgrad oder <c>null</c>.</returns>
    private static double? ToDecimalDegrees(List<double?> parts, string? reference, string negative)
    {
        if (parts.Count < 3 || parts.Any(part => part is null))
        {
            return null;
        }

        var value = parts[0]!.Value + parts[1]!.Value / 60 + parts[2]!.Value / 3600;
        return string.Equals(reference, negative, StringComparison.OrdinalIgnoreCase) ? -value : value;
    }
}
