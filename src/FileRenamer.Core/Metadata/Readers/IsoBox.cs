using System.Text;

namespace FileRenamer.Core.Metadata.Readers;

/// <summary>
/// Eine Box des ISO Base Media File Format (Grundlage von HEIC, CR3 und M4A).
/// </summary>
/// <param name="Type">Vierstelliger Boxtyp, z. B. „moov“.</param>
/// <param name="Start">Beginn der Nutzdaten (nach dem Header).</param>
/// <param name="Length">Länge der Nutzdaten.</param>
internal sealed record IsoBox(string Type, long Start, long Length)
{
    private const int MaxBoxes = 10_000;

    /// <summary>
    /// Ende der Nutzdaten.
    /// </summary>
    public long End => Start + Length;

    /// <summary>
    /// Liest die Boxen eines Bereichs (ohne Unterboxen).
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="start">Beginn des Bereichs.</param>
    /// <param name="end">Ende des Bereichs.</param>
    /// <returns>Die Boxen in Reihenfolge.</returns>
    public static List<IsoBox> ReadAll(ByteSource source, long start, long end)
    {
        var boxes = new List<IsoBox>();
        var position = start;
        end = Math.Min(end, source.Length);

        while (position + 8 <= end && boxes.Count < MaxBoxes)
        {
            long size = source.UInt32(position, littleEndian: false);
            var type = Encoding.Latin1.GetString(source.Read(position + 4, 4));
            var header = 8L;
            if (size == 1)
            {
                size = (long)source.UInt64BigEndian(position + 8);
                header = 16;
            }
            else if (size == 0)
            {
                size = end - position;
            }

            if (size < header || position + size > end)
            {
                break;
            }

            boxes.Add(new IsoBox(type, position + header, size - header));
            position += size;
        }

        return boxes;
    }

    /// <summary>
    /// Liest die Unterboxen dieser Box.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="skip">Zu überspringende Bytes am Anfang (z. B. 4 bei „FullBox“ wie „meta“).</param>
    /// <returns>Die Unterboxen.</returns>
    public List<IsoBox> Children(ByteSource source, int skip = 0) => ReadAll(source, Start + skip, End);

    /// <summary>
    /// Folgt einem Pfad von Boxtypen, z. B. „moov/udta/meta“.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="boxes">Ausgangsboxen.</param>
    /// <param name="path">Boxtypen; „meta“ wird als FullBox behandelt.</param>
    /// <returns>Die gefundene Box oder <c>null</c>.</returns>
    public static IsoBox? Find(ByteSource source, IEnumerable<IsoBox> boxes, params string[] path)
    {
        IsoBox? current = null;
        var candidates = boxes;
        foreach (var type in path)
        {
            current = candidates.FirstOrDefault(box => box.Type == type);
            if (current is null)
            {
                return null;
            }

            candidates = current.Children(source, current.Type == "meta" ? 4 : 0);
        }

        return current;
    }
}
