using FileRenamer.Core.Tagging;
using FileRenamer.Core.Tagging.Changes;

namespace FileRenamer.Core.Tests.Tagging;

/// <summary>
/// Tests für die Tag-Vorschau und die vorgemerkten Änderungen.
/// </summary>
public sealed class TagPreviewPipelineTests
{
    private const string A = "/musik/01 Like A Prayer.mp3";
    private const string B = "/musik/02 Express Yourself.mp3";
    private const string C = "/musik/03 Love Song.flac";

    private static readonly IReadOnlyList<(string Path, AudioTags Tags)> Files =
    [
        (A, AudioTags.Empty.With(TagFields.Artist, "Madonna")),
        (B, AudioTags.Empty.With(TagFields.Title, "Express Yourself")),
        (C, AudioTags.Empty.With(TagFields.Title, "Love Song")),
    ];

    /// <summary>
    /// Eine Änderung gilt nur für ihre Dateien; geänderte Felder erhalten ihre Herkunft.
    /// </summary>
    [Fact]
    public void Change_AppliesOnlyToItsFiles_AndRecordsSource()
    {
        var change = new SetFieldsChange(new Dictionary<string, string> { [TagFields.AlbumArtist] = "Madonna" }) { Files = [A, B] };

        var preview = TagPreviewPipeline.Create(Files, [change]);

        Assert.Equal(change.Id, preview[0].Sources[TagFields.AlbumArtist]);
        Assert.True(preview[1].IsChanged);
        Assert.False(preview[2].IsChanged);
    }

    /// <summary>
    /// Spätere Änderungen haben Vorrang; zurückgesetzte Werte zählen nicht als Änderung.
    /// </summary>
    [Fact]
    public void LaterChange_WinsAndRevertedFieldHasNoSource()
    {
        var first = new SetFieldsChange(new Dictionary<string, string> { [TagFields.Year] = "1988", [TagFields.Artist] = "X" }) { Files = [A] };
        var second = new SetFieldsChange(new Dictionary<string, string> { [TagFields.Year] = "1989", [TagFields.Artist] = "Madonna" }) { Files = [A] };

        var preview = TagPreviewPipeline.Create(Files, [first, second])[0];

        Assert.Equal("1989", preview.Result.Get(TagFields.Year));
        Assert.Equal(second.Id, preview.Sources[TagFields.Year]);
        Assert.False(preview.Sources.ContainsKey(TagFields.Artist));
    }

    /// <summary>
    /// Tags aus Dateinamen: passende Namen werden gelesen, unpassende erhalten einen Hinweis.
    /// </summary>
    [Fact]
    public void FromFileName_ReadsMatchingNames_HintsOthers()
    {
        var files = Files.Append((Path: "/musik/Bonus.mp3", Tags: AudioTags.Empty)).ToList();
        var change = new FieldsFromFileNameChange("%track% %title%", false) { Files = files.Select(file => file.Path).ToList() };

        var preview = TagPreviewPipeline.Create(files, [change]);

        Assert.Equal("Like A Prayer", preview[0].Result.Get(TagFields.Title));
        Assert.Equal("3", preview[2].Result.Get(TagFields.Track));
        Assert.Single(preview[3].Hints);
        Assert.False(preview[3].IsChanged);
    }

    /// <summary>
    /// Tracknummern nach Titel, mit Anzahl der Tracks und Discnummer.
    /// </summary>
    [Fact]
    public void TrackNumbers_ByTitle_WithCountAndDisc()
    {
        var change = new TrackNumberChange(1, TrackOrder.Title, SetTrackCount: true, Disc: 1, DiscCount: null) { Files = [B, C] };

        var preview = TagPreviewPipeline.Create(Files, [change]);

        Assert.Equal("1", preview[1].Result.Get(TagFields.Track));
        Assert.Equal("2", preview[2].Result.Get(TagFields.Track));
        Assert.Equal("2", preview[2].Result.Get(TagFields.TrackCount));
        Assert.Equal("1", preview[2].Result.Get(TagFields.Disc));
    }

    /// <summary>
    /// Cover einfügen ersetzt denselben Typ, Entfernen löscht ihn; Lyrics fehlen für Dateien ohne Datei mit Hinweis.
    /// </summary>
    [Fact]
    public void CoverAndLyrics_Changes()
    {
        var front = new CoverImage(CoverType.FrontCover, "image/jpeg", [1]);
        var newFront = new CoverImage(CoverType.FrontCover, "image/jpeg", [2]);
        var files = new List<(string, AudioTags)> { (A, AudioTags.Empty.WithCovers([front])), (B, AudioTags.Empty) };
        var add = new AddCoverChange(new Dictionary<string, CoverImage> { [A] = newFront, [B] = newFront }, ReplaceSameType: true) { Files = [A, B] };
        var remove = new RemoveCoverChange(CoverType.FrontCover) { Files = [B] };
        var lyrics = new SetLyricsChange(new Dictionary<string, string> { [A] = "Text" }, "eng") { Files = [A, B] };

        var preview = TagPreviewPipeline.Create(files, [add, remove, lyrics]);

        Assert.Equal([newFront], preview[0].Result.Covers);
        Assert.Equal(lyrics.Id, preview[0].Sources[TagFields.Lyrics]);
        Assert.Empty(preview[1].Result.Covers);
        Assert.Single(preview[1].Hints);
    }
}
