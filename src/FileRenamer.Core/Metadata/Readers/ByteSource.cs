using System.Buffers.Binary;

namespace FileRenamer.Core.Metadata.Readers;

/// <summary>
/// Wahlfreier, lesender Zugriff auf einen Ausschnitt eines Datenstroms. Liest nur die benötigten Bereiche,
/// damit auch große RAW- oder Audiodateien schnell ausgewertet werden. Ungültige Bereiche liefern leere Daten.
/// </summary>
internal sealed class ByteSource
{
    /// <summary>
    /// Größte Datenmenge, die auf einmal gelesen wird (Schutz vor beschädigten Längenangaben).
    /// </summary>
    public const int MaxReadLength = 16 * 1024 * 1024;

    private readonly Stream _stream;
    private readonly long _origin;

    /// <summary>
    /// Erzeugt eine Quelle über den gesamten Datenstrom.
    /// </summary>
    /// <param name="stream">Lesbarer, durchsuchbarer Datenstrom.</param>
    public ByteSource(Stream stream) : this(stream, 0, stream.Length)
    {
    }

    /// <summary>
    /// Erzeugt eine Quelle über einen Ausschnitt.
    /// </summary>
    /// <param name="stream">Lesbarer, durchsuchbarer Datenstrom.</param>
    /// <param name="origin">Beginn des Ausschnitts im Datenstrom.</param>
    /// <param name="length">Länge des Ausschnitts.</param>
    private ByteSource(Stream stream, long origin, long length)
    {
        _stream = stream;
        _origin = origin;
        Length = Math.Max(0, Math.Min(length, stream.Length - origin));
    }

    /// <summary>
    /// Länge des Ausschnitts in Bytes.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// Erzeugt einen Teilausschnitt; Offsets darin beginnen bei 0.
    /// </summary>
    /// <param name="offset">Beginn relativ zu diesem Ausschnitt.</param>
    /// <param name="length">Länge (wird auf das Ende begrenzt).</param>
    /// <returns>Der Teilausschnitt.</returns>
    public ByteSource Slice(long offset, long length) =>
        new(_stream, _origin + Math.Clamp(offset, 0, Length), Math.Clamp(length, 0, Math.Max(0, Length - offset)));

    /// <summary>
    /// Liest Bytes; am Ende des Ausschnitts werden entsprechend weniger geliefert.
    /// </summary>
    /// <param name="offset">Beginn relativ zum Ausschnitt.</param>
    /// <param name="count">Gewünschte Anzahl.</param>
    /// <returns>Die gelesenen Bytes (ggf. leer).</returns>
    public byte[] Read(long offset, int count)
    {
        if (offset < 0 || offset >= Length || count <= 0)
        {
            return [];
        }

        var length = (int)Math.Min(Math.Min(count, MaxReadLength), Length - offset);
        var buffer = new byte[length];
        _stream.Position = _origin + offset;
        _stream.ReadExactly(buffer);
        return buffer;
    }

    /// <summary>
    /// Liest eine 16-Bit-Zahl ohne Vorzeichen.
    /// </summary>
    /// <param name="offset">Position.</param>
    /// <param name="littleEndian">Byte-Reihenfolge.</param>
    /// <returns>Der Wert oder 0, wenn nicht genügend Daten vorhanden sind.</returns>
    public ushort UInt16(long offset, bool littleEndian)
    {
        var bytes = Read(offset, 2);
        return bytes.Length < 2 ? (ushort)0
            : littleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(bytes) : BinaryPrimitives.ReadUInt16BigEndian(bytes);
    }

    /// <summary>
    /// Liest eine 32-Bit-Zahl ohne Vorzeichen.
    /// </summary>
    /// <param name="offset">Position.</param>
    /// <param name="littleEndian">Byte-Reihenfolge.</param>
    /// <returns>Der Wert oder 0, wenn nicht genügend Daten vorhanden sind.</returns>
    public uint UInt32(long offset, bool littleEndian)
    {
        var bytes = Read(offset, 4);
        return bytes.Length < 4 ? 0
            : littleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(bytes) : BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    /// <summary>
    /// Liest eine 64-Bit-Zahl ohne Vorzeichen (Big Endian).
    /// </summary>
    /// <param name="offset">Position.</param>
    /// <returns>Der Wert oder 0.</returns>
    public ulong UInt64BigEndian(long offset)
    {
        var bytes = Read(offset, 8);
        return bytes.Length < 8 ? 0 : BinaryPrimitives.ReadUInt64BigEndian(bytes);
    }

    /// <summary>
    /// Prüft, ob an einer Position eine bestimmte ASCII-Zeichenfolge steht.
    /// </summary>
    /// <param name="offset">Position.</param>
    /// <param name="ascii">Erwartete Zeichen.</param>
    /// <returns><c>true</c> bei Übereinstimmung.</returns>
    public bool StartsWith(long offset, string ascii) =>
        Read(offset, ascii.Length).AsSpan().SequenceEqual(System.Text.Encoding.ASCII.GetBytes(ascii));
}
