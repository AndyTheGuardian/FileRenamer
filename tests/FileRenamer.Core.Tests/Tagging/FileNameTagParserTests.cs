using FileRenamer.Core.Tagging;

namespace FileRenamer.Core.Tests.Tagging;

/// <summary>
/// Tests für „Tags aus Dateinamen“.
/// </summary>
public sealed class FileNameTagParserTests
{
    private static readonly string Folder = Path.Combine(Path.GetTempPath(), "Madonna", "Like a Prayer");

    /// <summary>
    /// Beispiel aus dem Mockup: Track und Titel; führende Nullen entfallen.
    /// </summary>
    [Fact]
    public void Parse_TrackAndTitle()
    {
        var values = FileNameTagParser.Parse("%track% %title%", Path.Combine(Folder, "01 Like A Prayer.mp3"), false);

        Assert.NotNull(values);
        Assert.Equal("1", values[TagFields.Track]);
        Assert.Equal("Like A Prayer", values[TagFields.Title]);
    }

    /// <summary>
    /// Trennzeichen im Titel stören nicht, solange die Vorlage eindeutig ist.
    /// </summary>
    [Fact]
    public void Parse_ArtistDashTitle_KeepsDashInTitle()
    {
        var values = FileNameTagParser.Parse("%artist% - %title%", Path.Combine(Folder, "Madonna - Till Death - Live.mp3"), false);

        Assert.NotNull(values);
        Assert.Equal("Madonna", values[TagFields.Artist]);
        Assert.Equal("Till Death - Live", values[TagFields.Title]);
    }

    /// <summary>
    /// Ordnerteile der Vorlage werden mit den übergeordneten Ordnern verglichen.
    /// </summary>
    [Fact]
    public void Parse_WithFolders()
    {
        var values = FileNameTagParser.Parse("%artist%/%album%/%track% %title%", Path.Combine(Folder, "02 Express Yourself.flac"), false);

        Assert.NotNull(values);
        Assert.Equal("Madonna", values[TagFields.Artist]);
        Assert.Equal("Like a Prayer", values[TagFields.Album]);
        Assert.Equal("2", values[TagFields.Track]);
    }

    /// <summary>
    /// <c>%skip%</c> überspringt einen Teil, Unterstriche gelten auf Wunsch als Leerzeichen.
    /// </summary>
    [Fact]
    public void Parse_SkipAndUnderscores()
    {
        var values = FileNameTagParser.Parse("%skip% - %title%", Path.Combine(Folder, "CD1_03 - Love_Song.mp3"), true);

        Assert.NotNull(values);
        Assert.Equal("Love Song", values[TagFields.Title]);
        Assert.Single(values);
    }

    /// <summary>
    /// Passt der Name nicht (z. B. keine Ziffern für %track%), ist das Ergebnis <c>null</c>.
    /// </summary>
    [Fact]
    public void Parse_NoMatch_ReturnsNull() =>
        Assert.Null(FileNameTagParser.Parse("%track% %title%", Path.Combine(Folder, "Like A Prayer.mp3"), false));

    /// <summary>
    /// Vorlagen ohne oder mit unbekanntem Platzhalter sind ungültig.
    /// </summary>
    /// <param name="template">Die Vorlage.</param>
    [Theory]
    [InlineData("")]
    [InlineData("nur Text")]
    [InlineData("%artis% - %title%")]
    public void Validate_RejectsInvalidTemplates(string template) => Assert.NotNull(FileNameTagParser.Validate(template));
}
