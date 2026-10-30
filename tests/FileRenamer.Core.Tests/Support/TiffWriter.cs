using System.Buffers.Binary;
using System.Text;

namespace FileRenamer.Core.Tests.Support;

/// <summary>
/// Schreibt kleine TIFF-Strukturen (IFD0 mit optionalem EXIF- und GPS-Verzeichnis) für Tests.
/// </summary>
/// <param name="littleEndian">Byte-Reihenfolge („II“ bzw. „MM“).</param>
internal sealed class TiffWriter(bool littleEndian = true)
{
    /// <summary>
    /// Erzeugt ein leeres Verzeichnis mit der Byte-Reihenfolge dieses Writers.
    /// </summary>
    /// <returns>Das Verzeichnis.</returns>
    public Ifd NewIfd() => new(littleEndian);

    /// <summary>
    /// Setzt die TIFF-Struktur zusammen; Verweise auf EXIF- und GPS-Verzeichnis werden automatisch eingetragen.
    /// </summary>
    /// <param name="main">Erstes Verzeichnis.</param>
    /// <param name="exif">EXIF-Verzeichnis oder <c>null</c>.</param>
    /// <param name="gps">GPS-Verzeichnis oder <c>null</c>.</param>
    /// <returns>Die TIFF-Bytes.</returns>
    public byte[] Build(Ifd main, Ifd? exif = null, Ifd? gps = null)
    {
        if (exif is not null)
        {
            main.Long(0x8769, 0);
        }

        if (gps is not null)
        {
            main.Long(0x8825, 0);
        }

        var mainOffset = 8;
        var exifOffset = mainOffset + main.Size;
        var gpsOffset = exifOffset + (exif?.Size ?? 0);
        if (exif is not null)
        {
            main.Long(0x8769, (uint)exifOffset);
        }

        if (gps is not null)
        {
            main.Long(0x8825, (uint)gpsOffset);
        }

        var bytes = new List<byte>(Encoding.ASCII.GetBytes(littleEndian ? "II" : "MM"));
        bytes.AddRange(U16(42));
        bytes.AddRange(U32((uint)mainOffset));
        bytes.AddRange(main.Serialize(mainOffset));
        bytes.AddRange(exif?.Serialize(exifOffset) ?? []);
        bytes.AddRange(gps?.Serialize(gpsOffset) ?? []);
        return [.. bytes];
    }

    /// <summary>
    /// 16-Bit-Zahl in der Byte-Reihenfolge des Writers.
    /// </summary>
    /// <param name="value">Der Wert.</param>
    /// <returns>Die Bytes.</returns>
    private byte[] U16(ushort value) => Ifd.Encode16(value, littleEndian);

    /// <summary>
    /// 32-Bit-Zahl in der Byte-Reihenfolge des Writers.
    /// </summary>
    /// <param name="value">Der Wert.</param>
    /// <returns>Die Bytes.</returns>
    private byte[] U32(uint value) => Ifd.Encode32(value, littleEndian);

    /// <summary>
    /// Ein TIFF-Verzeichnis mit Einträgen.
    /// </summary>
    /// <param name="littleEndian">Byte-Reihenfolge.</param>
    internal sealed class Ifd(bool littleEndian)
    {
        private readonly SortedDictionary<ushort, (ushort Type, uint Count, byte[] Data)> _entries = [];

        /// <summary>
        /// Größe des Verzeichnisses inklusive ausgelagerter Werte.
        /// </summary>
        public int Size => 2 + _entries.Count * 12 + 4 + _entries.Values.Where(e => e.Data.Length > 4).Sum(e => Pad(e.Data.Length));

        /// <summary>Fügt einen ASCII-Wert hinzu.</summary>
        /// <param name="tag">Tag.</param>
        /// <param name="value">Text.</param>
        /// <returns>Dieses Verzeichnis.</returns>
        public Ifd Ascii(ushort tag, string value)
        {
            var data = Encoding.UTF8.GetBytes(value + "\0");
            _entries[tag] = (2, (uint)data.Length, data);
            return this;
        }

        /// <summary>Fügt einen SHORT-Wert hinzu.</summary>
        /// <param name="tag">Tag.</param>
        /// <param name="value">Wert.</param>
        /// <returns>Dieses Verzeichnis.</returns>
        public Ifd Short(ushort tag, ushort value)
        {
            _entries[tag] = (3, 1, Encode16(value, littleEndian));
            return this;
        }

        /// <summary>Fügt einen LONG-Wert hinzu.</summary>
        /// <param name="tag">Tag.</param>
        /// <param name="value">Wert.</param>
        /// <returns>Dieses Verzeichnis.</returns>
        public Ifd Long(ushort tag, uint value)
        {
            _entries[tag] = (4, 1, Encode32(value, littleEndian));
            return this;
        }

        /// <summary>Fügt einen BYTE-Wert hinzu.</summary>
        /// <param name="tag">Tag.</param>
        /// <param name="value">Wert.</param>
        /// <returns>Dieses Verzeichnis.</returns>
        public Ifd Byte(ushort tag, byte value)
        {
            _entries[tag] = (1, 1, [value]);
            return this;
        }

        /// <summary>Fügt RATIONAL-Werte hinzu.</summary>
        /// <param name="tag">Tag.</param>
        /// <param name="values">Zähler/Nenner-Paare.</param>
        /// <returns>Dieses Verzeichnis.</returns>
        public Ifd Rational(ushort tag, params (uint Numerator, uint Denominator)[] values)
        {
            var data = values.SelectMany(v => Encode32(v.Numerator, littleEndian).Concat(Encode32(v.Denominator, littleEndian))).ToArray();
            _entries[tag] = (5, (uint)values.Length, data);
            return this;
        }

        /// <summary>
        /// Schreibt das Verzeichnis für eine Zielposition.
        /// </summary>
        /// <param name="offset">Position des Verzeichnisses relativ zum TIFF-Header.</param>
        /// <returns>Die Bytes.</returns>
        public byte[] Serialize(int offset)
        {
            var head = new List<byte>(Encode16((ushort)_entries.Count, littleEndian));
            var data = new List<byte>();
            var dataOffset = offset + 2 + _entries.Count * 12 + 4;

            foreach (var (tag, (type, count, value)) in _entries)
            {
                head.AddRange(Encode16(tag, littleEndian));
                head.AddRange(Encode16(type, littleEndian));
                head.AddRange(Encode32(count, littleEndian));
                if (value.Length <= 4)
                {
                    head.AddRange(value.Concat(new byte[4 - value.Length]));
                }
                else
                {
                    head.AddRange(Encode32((uint)(dataOffset + data.Count), littleEndian));
                    data.AddRange(value);
                    data.AddRange(new byte[Pad(value.Length) - value.Length]);
                }
            }

            head.AddRange(new byte[4]);
            return [.. head, .. data];
        }

        /// <summary>16-Bit-Zahl kodieren.</summary>
        /// <param name="value">Wert.</param>
        /// <param name="little">Little Endian.</param>
        /// <returns>Bytes.</returns>
        public static byte[] Encode16(ushort value, bool little)
        {
            var bytes = new byte[2];
            if (little) { BinaryPrimitives.WriteUInt16LittleEndian(bytes, value); } else { BinaryPrimitives.WriteUInt16BigEndian(bytes, value); }
            return bytes;
        }

        /// <summary>32-Bit-Zahl kodieren.</summary>
        /// <param name="value">Wert.</param>
        /// <param name="little">Little Endian.</param>
        /// <returns>Bytes.</returns>
        public static byte[] Encode32(uint value, bool little)
        {
            var bytes = new byte[4];
            if (little) { BinaryPrimitives.WriteUInt32LittleEndian(bytes, value); } else { BinaryPrimitives.WriteUInt32BigEndian(bytes, value); }
            return bytes;
        }

        /// <summary>Auf gerade Länge auffüllen.</summary>
        /// <param name="length">Länge.</param>
        /// <returns>Gerade Länge.</returns>
        private static int Pad(int length) => (length + 1) & ~1;
    }
}
