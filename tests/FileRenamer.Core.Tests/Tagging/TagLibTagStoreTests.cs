using FileRenamer.Core.Tagging;
using FileRenamer.Core.Tests.Support;

namespace FileRenamer.Core.Tests.Tagging;

/// <summary>
/// Tests für das Lesen und Schreiben mit TagLib# an echten (kleinsten) MP3- und FLAC-Dateien.
/// </summary>
public sealed class TagLibTagStoreTests : IDisposable
{
    private static readonly CoverImage Front = new(CoverType.FrontCover, "image/jpeg", [0xFF, 0xD8, 0xFF, 0xD9]);
    private static readonly CoverImage Back = new(CoverType.BackCover, "image/png", [0x89, 0x50, 0x4E, 0x47]);
    private readonly TempFolder _folder = new();
    private readonly TagLibTagStore _store = new();

    /// <summary>
    /// Löscht den Testordner.
    /// </summary>
    public void Dispose() => _folder.Dispose();

    /// <summary>
    /// Standard- und weitere Felder, Bilder und Lyrics kommen unverändert zurück.
    /// </summary>
    /// <param name="name">Dateiname (bestimmt das Format).</param>
    [Theory]
    [InlineData("song.mp3")]
    [InlineData("song.flac")]
    public void WriteThenRead_RoundTripsAllParts(string name)
    {
        var path = CreateFile(name);
        var tags = new AudioTags(
            new Dictionary<string, string>
            {
                [TagFields.Title] = "Like A Prayer", [TagFields.Artist] = "Madonna; Prince", [TagFields.Album] = "Like a Prayer",
                [TagFields.AlbumArtist] = "Madonna", [TagFields.Genre] = "Pop", [TagFields.Year] = "1989",
                [TagFields.Track] = "1", [TagFields.TrackCount] = "11", [TagFields.Disc] = "1", [TagFields.DiscCount] = "1",
                [TagFields.Comment] = "Remaster", ["bpm"] = "120", ["publisher"] = "Sire",
            },
            [Front, Back],
            "[00:12.40]Life is a mystery",
            name.EndsWith(".mp3", StringComparison.Ordinal) ? "eng" : AudioTags.UnknownLanguage);

        _store.Write(path, tags, []);
        var read = _store.Read(path, []);

        Assert.Equal([], read.DifferingKeys(tags));
    }

    /// <summary>
    /// Benutzerdefinierte Felder werden geschrieben, gelesen und beim Leeren entfernt.
    /// </summary>
    /// <param name="name">Dateiname (bestimmt das Format).</param>
    [Theory]
    [InlineData("song.mp3")]
    [InlineData("song.flac")]
    public void CustomField_IsWrittenReadAndRemoved(string name)
    {
        var path = CreateFile(name);
        var key = TagFields.Custom("mood");

        _store.Write(path, AudioTags.Empty.With(key, "fröhlich"), ["MOOD"]);
        Assert.Equal("fröhlich", _store.Read(path, ["MOOD"]).Get(key));

        _store.Write(path, AudioTags.Empty, ["MOOD"]);
        Assert.Equal(string.Empty, _store.Read(path, ["MOOD"]).Get(key));
    }

    /// <summary>
    /// Ein leerer Wert entfernt ein Feld, andere Felder bleiben.
    /// </summary>
    [Fact]
    public void EmptyValue_RemovesOnlyThatField()
    {
        var path = CreateFile("song.mp3");
        _store.Write(path, AudioTags.Empty.With(TagFields.Title, "A").With(TagFields.Album, "B"), []);

        _store.Write(path, _store.Read(path, []).With(TagFields.Title, ""), []);
        var read = _store.Read(path, []);

        Assert.Equal(string.Empty, read.Get(TagFields.Title));
        Assert.Equal("B", read.Get(TagFields.Album));
    }

    /// <summary>
    /// MP3 wird als ID3v2.3 geschrieben (E122), ein fehlender ID3v1-Tag wird nicht angelegt.
    /// </summary>
    [Fact]
    public void Mp3_IsWrittenAsId3v23WithoutId3v1()
    {
        var path = CreateFile("song.mp3");

        _store.Write(path, AudioTags.Empty.With(TagFields.Title, "Titel"), []);
        var bytes = File.ReadAllBytes(path);

        Assert.Equal("ID3"u8.ToArray(), bytes[..3]);
        Assert.Equal(3, bytes[3]);
        Assert.NotEqual("TAG"u8.ToArray(), bytes[^128..^125]);
    }

    /// <summary>
    /// Die Sprache der Lyrics wird bei MP3 gespeichert.
    /// </summary>
    [Fact]
    public void Mp3_StoresLyricsLanguage()
    {
        var path = CreateFile("song.mp3");

        _store.Write(path, AudioTags.Empty.WithLyrics("Text", "deu"), []);

        Assert.Equal("deu", _store.Read(path, []).LyricsLanguage);
    }

    /// <summary>
    /// Unterstützt werden Audioendungen, nicht aber Bilder oder Dokumente.
    /// </summary>
    /// <param name="name">Dateiname.</param>
    /// <param name="expected">Erwartetes Ergebnis.</param>
    [Theory]
    [InlineData("a.MP3", true)]
    [InlineData("a.flac", true)]
    [InlineData("a.m4a", true)]
    [InlineData("a.opus", true)]
    [InlineData("a.wma", true)]
    [InlineData("a.jpg", false)]
    [InlineData("a.pdf", false)]
    public void IsSupported_ChecksAudioExtensions(string name, bool expected) => Assert.Equal(expected, _store.IsSupported(name));

    /// <summary>
    /// Eine beschädigte Datei führt zu einer verständlichen Ausnahme.
    /// </summary>
    [Fact]
    public void Read_CorruptFile_ThrowsTagStoreException()
    {
        var path = AudioFiles.Write(_folder.Path, "kaputt.flac", [1, 2, 3]);

        Assert.Throws<TagStoreException>(() => _store.Read(path, []));
    }

    /// <summary>
    /// Legt eine Audiodatei passend zur Endung an.
    /// </summary>
    /// <param name="name">Dateiname.</param>
    /// <returns>Der Pfad.</returns>
    private string CreateFile(string name) =>
        AudioFiles.Write(_folder.Path, name, name.EndsWith(".flac", StringComparison.Ordinal) ? AudioFiles.Flac() : AudioFiles.Mp3());
}
