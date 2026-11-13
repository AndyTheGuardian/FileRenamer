using FileRenamer.Core.Preview;

namespace FileRenamer.Core.Tests.Preview;

/// <summary>
/// Tests für Reihenfolge und Breiten der Vorschauspalten (E146).
/// </summary>
public sealed class ColumnLayoutTests
{
    private static ColumnLayout Create() => new([new("file", 100), new("title", null), new("artist", null), new("year", 50)]);

    /// <summary>
    /// Feste Spalten behalten ihre Breite, die übrigen teilen sich den Rest.
    /// </summary>
    [Fact]
    public void Arrange_SplitsRemainingWidthAmongFlexibleColumns()
    {
        var slots = Create().Arrange(450);

        Assert.Equal([100d, 150d, 150d, 50d], slots.Select(slot => slot.Width));
        Assert.Equal([0d, 100d, 250d, 400d], slots.Select(slot => slot.X));
    }

    /// <summary>
    /// Ohne flexible Spalte füllt die letzte Spalte den Rest.
    /// </summary>
    [Fact]
    public void Arrange_WithoutFlexibleColumns_LastColumnFills()
    {
        var layout = Create();
        layout.SetWidth("title", 120);
        layout.SetWidth("artist", 80);

        Assert.Equal(150, layout.Arrange(450).Last().Width);
    }

    /// <summary>
    /// Flexible Spalten unterschreiten die Mindestbreite nicht.
    /// </summary>
    [Fact]
    public void Arrange_KeepsMinimumWidth()
    {
        Assert.All(Create().Arrange(100).Where(slot => slot.Key is "title" or "artist"), slot => Assert.Equal(ColumnLayout.DefaultMinWidth, slot.Width));
    }

    /// <summary>
    /// Verschieben setzt die Spalte an die Zielposition und meldet die Änderung.
    /// </summary>
    /// <param name="index">Zielposition ohne die verschobene Spalte.</param>
    /// <param name="expected">Erwartete Reihenfolge.</param>
    [Theory]
    [InlineData(0, "year,file,title,artist")]
    [InlineData(2, "file,title,year,artist")]
    [InlineData(9, "file,title,artist,year")]
    public void Move_PlacesColumnAtIndex(int index, string expected)
    {
        var layout = Create();
        var changes = 0;
        layout.Changed += (_, _) => changes++;

        layout.Move("year", index);

        Assert.Equal(expected, string.Join(",", layout.Columns.Select(column => column.Key)));
        Assert.Equal(expected == "file,title,artist,year" ? 0 : 1, changes);
    }

    /// <summary>
    /// Breiten werden gerundet und auf die Mindestbreite begrenzt; Zurücksetzen stellt den Standard her.
    /// </summary>
    [Fact]
    public void SetWidth_ClampsAndReset_RestoresDefaults()
    {
        var layout = Create();

        layout.SetWidth("title", 5);
        layout.Move("file", 3);
        Assert.Equal(ColumnLayout.DefaultMinWidth, layout.Columns.First(column => column.Key == "title").Width);
        Assert.False(layout.IsDefault);

        layout.Reset();
        Assert.True(layout.IsDefault);
        Assert.Null(layout.Columns.First(column => column.Key == "title").Width);
    }

    /// <summary>
    /// Unbekannte Spalten werden abgewiesen.
    /// </summary>
    [Fact]
    public void Move_UnknownColumn_Throws() => Assert.Throws<ArgumentException>(() => Create().Move("x", 0));

    /// <summary>
    /// Speichern merkt die Anordnung; Werkseinstellung lässt sie bestehen, „Gespeicherte wiederherstellen“ holt sie zurück.
    /// </summary>
    [Fact]
    public void Save_ResetAndRestoreSaved()
    {
        var layout = Create();
        var saves = 0;
        layout.SavedChanged += (_, _) => saves++;
        layout.Move("year", 0);
        layout.SetWidth("title", 200);

        layout.Save();
        layout.Reset();
        Assert.True(layout.IsDefault);
        Assert.True(layout.HasSaved);

        layout.RestoreSaved();
        Assert.Equal("year,file,title,artist", string.Join(",", layout.Columns.Select(column => column.Key)));
        Assert.Equal(200, layout.Columns.First(column => column.Key == "title").Width);
        Assert.Equal(1, saves);
        Assert.Equal([new SavedColumn("year", 50), new SavedColumn("file", 100), new SavedColumn("title", 200), new SavedColumn("artist", null)], layout.Saved!);
    }

    /// <summary>
    /// Ohne gespeicherte Anordnung stellt „Gespeicherte wiederherstellen“ die Werkseinstellung her.
    /// </summary>
    [Fact]
    public void RestoreSaved_WithoutSaved_RestoresDefaults()
    {
        var layout = Create();
        layout.Move("year", 0);

        layout.RestoreSaved();

        Assert.True(layout.IsDefault);
        Assert.Null(layout.Saved);
    }

    /// <summary>
    /// Eine beim Start gelesene Anordnung wird übernommen, sofern sie genau die bekannten Spalten enthält.
    /// </summary>
    [Fact]
    public void LoadSaved_AppliesMatchingLayout_IgnoresMismatch()
    {
        var layout = Create();
        layout.LoadSaved([new("artist", 90), new("title", null), new("file", 1), new("year", 50)]);
        Assert.Equal("artist,title,file,year", string.Join(",", layout.Columns.Select(column => column.Key)));
        Assert.Equal(ColumnLayout.DefaultMinWidth, layout.Columns.First(column => column.Key == "file").Width);
        Assert.True(layout.HasSaved);

        var other = Create();
        other.LoadSaved([new("artist", 90), new("title", null)]);
        Assert.True(other.IsDefault);
        Assert.False(other.HasSaved);
    }

    /// <summary>
    /// Passen die Mindestbreiten nicht hinein, ist die Gesamtbreite größer als die verfügbare (horizontal scrollen).
    /// </summary>
    [Fact]
    public void Extent_GrowsBeyondAvailableWhenMinimumWidthsDoNotFit()
    {
        var layout = Create();

        Assert.Equal(450d, layout.Extent(450));
        Assert.Equal(100d + 32 + 32 + 50, layout.Extent(120));
    }

    /// <summary>
    /// Eine geänderte sichtbare Breite wird gemeldet, eine gleiche nicht.
    /// </summary>
    [Fact]
    public void Viewport_RaisesChangedOnlyWhenDifferent()
    {
        var layout = Create();
        var changes = 0;
        layout.Changed += (_, _) => changes++;

        layout.Viewport = 300;
        layout.Viewport = 300.2;

        Assert.Equal(1, changes);
        Assert.Equal(300d, layout.Viewport);
    }
}
