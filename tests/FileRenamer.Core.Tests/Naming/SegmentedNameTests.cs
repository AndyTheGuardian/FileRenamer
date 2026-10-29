using FileRenamer.Core.Naming;

namespace FileRenamer.Core.Tests.Naming;

/// <summary>
/// Tests für <see cref="SegmentedName"/>: Ersetzen, Einfügen und Herkunft der Abschnitte.
/// </summary>
public sealed class SegmentedNameTests
{
    private static readonly Guid RuleA = Guid.NewGuid();
    private static readonly Guid RuleB = Guid.NewGuid();

    /// <summary>
    /// Ein ersetzter Bereich wird dem Verfahren zugeordnet, der Rest bleibt Originaltext.
    /// </summary>
    [Fact]
    public void Replace_SplitsSegmentsAndTracksSource()
    {
        var name = SegmentedName.FromText("IMG_0001").Replace(0, 3, "Urlaub", RuleA);

        Assert.Equal("Urlaub_0001", name.Text);
        Assert.Collection(name.Segments,
            segment => { Assert.Equal("Urlaub", segment.Text); Assert.Equal([RuleA], segment.Sources); },
            segment => { Assert.Equal("_0001", segment.Text); Assert.True(segment.IsOriginal); });
    }

    /// <summary>
    /// Ersetzt ein zweites Verfahren Text des ersten, sind beide als Herkunft vermerkt (Überschneidung).
    /// </summary>
    [Fact]
    public void Replace_OverlappingRules_KeepsBothSources()
    {
        var name = SegmentedName.FromText("abc")
            .Replace(0, 3, "x123", RuleA)
            .Replace(1, 3, "999", RuleB);

        Assert.Equal("x999", name.Text);
        Assert.Equal([RuleA, RuleB], name.Segments[^1].Sources);
    }

    /// <summary>
    /// Bei Überschneidungen hat das ausgewählte Verfahren Vorrang, sonst das zuletzt angewandte.
    /// </summary>
    [Fact]
    public void GetHighlightSource_PrefersSelectedRule()
    {
        var segment = new NameSegment("x", [RuleA, RuleB]);

        Assert.Equal(RuleA, segment.GetHighlightSource(RuleA));
        Assert.Equal(RuleB, segment.GetHighlightSource(null));
        Assert.Equal(RuleB, segment.GetHighlightSource(Guid.NewGuid()));
        Assert.Null(new NameSegment("y", []).GetHighlightSource(RuleA));
    }

    /// <summary>
    /// Einfügen am Anfang und Ende funktioniert, benachbarte gleiche Herkunft wird zusammengefasst.
    /// </summary>
    [Fact]
    public void Insert_AtBothEnds_MergesSameSources()
    {
        var name = SegmentedName.FromText("name")
            .Insert(4, "-1", RuleA)
            .Insert(6, "0", RuleA)
            .Insert(0, ">", RuleB);

        Assert.Equal(">name-10", name.Text);
        Assert.Equal(3, name.Segments.Count);
        Assert.Equal("-10", name.Segments[^1].Text);
    }

    /// <summary>
    /// Ungültige Bereiche werden abgewiesen.
    /// </summary>
    [Fact]
    public void Replace_OutOfRange_Throws()
    {
        var name = SegmentedName.FromText("abc");

        Assert.Throws<ArgumentOutOfRangeException>(() => name.Replace(2, 2, "x", RuleA));
        Assert.Throws<ArgumentOutOfRangeException>(() => name.Replace(-1, 0, "x", RuleA));
    }
}
