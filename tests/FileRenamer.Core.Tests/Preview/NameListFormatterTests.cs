using FileRenamer.Core.Preview;

namespace FileRenamer.Core.Tests.Preview;

/// <summary>
/// Tests für den Text, der beim Kopieren von Vorschauzeilen in die Zwischenablage gelegt wird.
/// </summary>
public sealed class NameListFormatterTests
{
    private static readonly NamePair[] Pairs =
    [
        new("IMG_0001", "Urlaub-001"),
        new("IMG_0002", "Urlaub-002"),
    ];

    /// <summary>
    /// Je Spaltenauswahl entsteht eine Zeile pro Datei in Anzeigereihenfolge.
    /// </summary>
    /// <param name="columns">Die Spaltenauswahl.</param>
    /// <param name="expected">Der erwartete Text.</param>
    [Theory]
    [InlineData(NameColumns.Original, "IMG_0001\nIMG_0002")]
    [InlineData(NameColumns.New, "Urlaub-001\nUrlaub-002")]
    [InlineData(NameColumns.Both, "IMG_0001\tUrlaub-001\nIMG_0002\tUrlaub-002")]
    public void Format_WritesOneLinePerFile(NameColumns columns, string expected)
    {
        var text = NameListFormatter.Format(Pairs, columns, "\n");

        Assert.Equal(expected, text);
    }

    /// <summary>
    /// Ohne Zeilentrenner wird der Zeilenumbruch des Betriebssystems verwendet; nach der letzten Zeile folgt keiner.
    /// </summary>
    [Fact]
    public void Format_UsesSystemNewLineByDefault()
    {
        var text = NameListFormatter.Format(Pairs, NameColumns.New);

        Assert.Equal("Urlaub-001" + Environment.NewLine + "Urlaub-002", text);
    }

    /// <summary>
    /// Eine einzelne Datei ergibt eine Zeile ohne Zeilentrenner, keine Datei einen leeren Text.
    /// </summary>
    [Fact]
    public void Format_HandlesSingleAndEmptyLists()
    {
        Assert.Equal("IMG_0001", NameListFormatter.Format([Pairs[0]], NameColumns.Original));
        Assert.Equal(string.Empty, NameListFormatter.Format([], NameColumns.Both));
    }
}
