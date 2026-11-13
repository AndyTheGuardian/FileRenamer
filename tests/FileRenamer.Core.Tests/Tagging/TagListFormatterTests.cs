using FileRenamer.Core.Tagging;

namespace FileRenamer.Core.Tests.Tagging;

/// <summary>
/// Tests für das Kopieren aus der Tag-Vorschau (F66, F67).
/// </summary>
public sealed class TagListFormatterTests
{
    private static readonly TagCopyLabels Labels =
        new(["Datei", "Track", "Titel", "Interpret", "Album", "Albuminterpret", "Jahr", "Cover", "Lyrics"], "ja");

    private static readonly TagCopyRow Full = new("01 Like A Prayer.mp3", AudioTags.Empty
        .With(TagFields.Track, "1").With(TagFields.Title, "Like A Prayer").With(TagFields.Artist, "Madonna")
        .With(TagFields.Album, "Like a Prayer").With(TagFields.AlbumArtist, "Madonna").With(TagFields.Year, "1989")
        .WithCovers([new CoverImage(CoverType.FrontCover, "image/jpeg", [1, 2, 3])])
        .WithLyrics("Life is a mystery", AudioTags.UnknownLanguage));

    private static readonly TagCopyRow Empty = new("02 Leer.mp3", AudioTags.Empty);

    /// <summary>
    /// Je Spaltenauswahl die erwartete Zeile; leere Werte bleiben leere Zellen.
    /// </summary>
    /// <param name="columns">Die Spalten.</param>
    /// <param name="expected">Der erwartete Text.</param>
    [Theory]
    [InlineData(TagCopyColumns.FileNames, "01 Like A Prayer.mp3|02 Leer.mp3")]
    [InlineData(TagCopyColumns.Tags, "1\tLike A Prayer\tMadonna\tLike a Prayer\tMadonna\t1989\tja\tja|\t\t\t\t\t\t\t")]
    [InlineData(TagCopyColumns.FileNamesAndTags, "01 Like A Prayer.mp3\t1\tLike A Prayer\tMadonna\tLike a Prayer\tMadonna\t1989\tja\tja|02 Leer.mp3\t\t\t\t\t\t\t\t")]
    public void Format_CopiesSelectedColumns(TagCopyColumns columns, string expected)
    {
        Assert.Equal(expected, TagListFormatter.Format([Full, Empty], columns, Labels, "|"));
    }

    /// <summary>
    /// „Zeilen kopieren“ beginnt mit der Kopfzeile.
    /// </summary>
    [Fact]
    public void Format_Rows_StartsWithHeader()
    {
        var text = TagListFormatter.Format([Empty], TagCopyColumns.Rows, Labels, "|");

        Assert.Equal("Datei\tTrack\tTitel\tInterpret\tAlbum\tAlbuminterpret\tJahr\tCover\tLyrics|02 Leer.mp3\t\t\t\t\t\t\t\t", text);
    }

    /// <summary>
    /// Tabulatoren und Zeilenumbrüche in Werten zerstören die Tabelle nicht.
    /// </summary>
    [Fact]
    public void Format_ReplacesTabsAndLineBreaksInValues()
    {
        var row = new TagCopyRow("a.mp3", AudioTags.Empty.With(TagFields.Title, "Eins\tZwei\r\nDrei"));

        Assert.Equal("a.mp3\t\tEins Zwei Drei\t\t\t\t\t\t", TagListFormatter.Format([row], TagCopyColumns.FileNamesAndTags, Labels));
    }
}
