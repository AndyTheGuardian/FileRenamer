namespace FileRenamer.Core.Naming;

/// <summary>
/// Unveränderlicher Dateiname, der aus Abschnitten mit Herkunftsinformation besteht.
/// Jede Änderung liefert eine neue Instanz; so bleibt nachvollziehbar, welches Verfahren
/// welche Zeichen erzeugt hat (Grundlage der farbigen Hervorhebung).
/// </summary>
public sealed class SegmentedName
{
    private readonly List<NameSegment> _segments;

    /// <summary>
    /// Erzeugt einen Namen aus bereits zusammengeführten Abschnitten.
    /// </summary>
    /// <param name="segments">Die Abschnitte in Reihenfolge.</param>
    private SegmentedName(List<NameSegment> segments)
    {
        _segments = segments;
        Text = string.Concat(segments.Select(segment => segment.Text));
    }

    /// <summary>
    /// Die Abschnitte des Namens in Reihenfolge.
    /// </summary>
    public IReadOnlyList<NameSegment> Segments => _segments;

    /// <summary>
    /// Der vollständige Text des Namens.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Die Länge des Namens in Zeichen.
    /// </summary>
    public int Length => Text.Length;

    /// <summary>
    /// Erzeugt einen Namen aus unverändertem Originaltext.
    /// </summary>
    /// <param name="text">Der Originaltext.</param>
    /// <returns>Ein Name mit einem einzigen Originalabschnitt (oder ohne Abschnitte bei leerem Text).</returns>
    public static SegmentedName FromText(string text) =>
        new(text.Length == 0 ? [] : [new NameSegment(text, [])]);

    /// <summary>
    /// Ersetzt einen Bereich des Namens durch neuen Text, der dem angegebenen Verfahren zugeordnet wird.
    /// Die Herkunft des ersetzten Textes bleibt am neuen Abschnitt erhalten (Überschneidung).
    /// </summary>
    /// <param name="start">Startindex des zu ersetzenden Bereichs.</param>
    /// <param name="length">Länge des zu ersetzenden Bereichs (0 = reines Einfügen).</param>
    /// <param name="replacement">Der neue Text.</param>
    /// <param name="ruleId">Id des verursachenden Verfahrens.</param>
    /// <returns>Der geänderte Name.</returns>
    public SegmentedName Replace(int start, int length, string replacement, Guid ruleId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start + length, Length);

        var removed = Slice(start, length);
        var result = Slice(0, start);

        if (replacement.Length > 0)
        {
            var sources = removed.SelectMany(segment => segment.Sources).Append(ruleId).Distinct().ToList();
            result.Add(new NameSegment(replacement, sources));
        }

        result.AddRange(Slice(start + length, Length - start - length));
        return new SegmentedName(Merge(result));
    }

    /// <summary>
    /// Fügt Text an der angegebenen Position ein.
    /// </summary>
    /// <param name="index">Einfügeposition (0 = Anfang, <see cref="Length"/> = Ende).</param>
    /// <param name="text">Der einzufügende Text.</param>
    /// <param name="ruleId">Id des verursachenden Verfahrens.</param>
    /// <returns>Der geänderte Name.</returns>
    public SegmentedName Insert(int index, string text, Guid ruleId) => Replace(index, 0, text, ruleId);

    /// <summary>
    /// Hängt unveränderten Originaltext an (z. B. die ausgeblendete Dateierweiterung).
    /// </summary>
    /// <param name="text">Der anzuhängende Originaltext.</param>
    /// <returns>Der verlängerte Name.</returns>
    public SegmentedName AppendOriginal(string text)
    {
        if (text.Length == 0)
        {
            return this;
        }

        var segments = new List<NameSegment>(_segments) { new(text, []) };
        return new SegmentedName(Merge(segments));
    }

    /// <summary>
    /// Liefert die Abschnitte, die den angegebenen Bereich abdecken (an den Rändern zugeschnitten).
    /// </summary>
    /// <param name="start">Startindex des Bereichs.</param>
    /// <param name="length">Länge des Bereichs.</param>
    /// <returns>Neue Liste der (Teil-)Abschnitte.</returns>
    private List<NameSegment> Slice(int start, int length)
    {
        var result = new List<NameSegment>();
        var end = start + length;
        var offset = 0;

        foreach (var segment in _segments)
        {
            var segmentEnd = offset + segment.Text.Length;
            var from = Math.Max(start, offset);
            var to = Math.Min(end, segmentEnd);

            if (from < to)
            {
                result.Add(segment with { Text = segment.Text[(from - offset)..(to - offset)] });
            }

            offset = segmentEnd;
        }

        return result;
    }

    /// <summary>
    /// Fasst benachbarte Abschnitte mit identischer Herkunft zusammen.
    /// </summary>
    /// <param name="segments">Die Abschnitte in Reihenfolge.</param>
    /// <returns>Die zusammengefassten Abschnitte.</returns>
    private static List<NameSegment> Merge(List<NameSegment> segments)
    {
        var merged = new List<NameSegment>(segments.Count);

        foreach (var segment in segments.Where(segment => segment.Text.Length > 0))
        {
            if (merged.Count > 0 && merged[^1].HasSameSources(segment))
            {
                merged[^1] = merged[^1] with { Text = merged[^1].Text + segment.Text };
            }
            else
            {
                merged.Add(segment);
            }
        }

        return merged;
    }
}
