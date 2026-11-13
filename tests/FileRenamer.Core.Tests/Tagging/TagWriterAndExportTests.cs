using FileRenamer.Core.Tagging;
using FileRenamer.Core.Tests.Support;

namespace FileRenamer.Core.Tests.Tagging;

/// <summary>
/// Tests für Speichern mit Rücknahme, Widerrufen sowie Export von Covern und Lyrics.
/// </summary>
public sealed class TagWriterAndExportTests : IDisposable
{
    private readonly TempFolder _folder = new();

    /// <summary>
    /// Löscht den Testordner.
    /// </summary>
    public void Dispose() => _folder.Dispose();

    /// <summary>
    /// Schlägt eine Datei fehl, werden die bereits geschriebenen zurückgeschrieben (alles oder nichts).
    /// </summary>
    [Fact]
    public void Save_FailureRollsBackWrittenFiles()
    {
        var store = new FakeStore { FailOn = "c" };
        var jobs = new[] { Job("a", "1"), Job("b", "2"), Job("c", "3") };

        var result = new TagWriter(store).Save(jobs, []);

        Assert.False(result.Success);
        Assert.Equal(["a:neu", "b:neu", "b:alt", "a:alt"], store.Log);
    }

    /// <summary>
    /// Unveränderte Aufträge werden übersprungen; Widerrufen kehrt die Reihenfolge und die Stände um.
    /// </summary>
    [Fact]
    public void Save_SkipsUnchanged_AndUndoReverses()
    {
        var store = new FakeStore();
        var unchanged = new TagWriteJob("x", AudioTags.Empty, AudioTags.Empty);

        var result = new TagWriter(store).Save([Job("a", "1"), unchanged, Job("b", "2")], []);
        var undo = result.CreateUndoJobs();

        Assert.True(result.Success);
        Assert.Equal(["a:neu", "b:neu"], store.Log);
        Assert.Equal(["b", "a"], undo.Select(job => job.Path));
        Assert.Equal("alt", undo[0].Target.Get(TagFields.Title));
    }

    /// <summary>
    /// Gleiche Cover werden je Ordner nur einmal gespeichert; vorhandene Dateien mit anderem Inhalt bleiben, die neue
    /// Datei erhält einen Zusatz.
    /// </summary>
    [Fact]
    public void ExportCovers_SkipsDuplicates_NeverOverwrites()
    {
        var cover = new CoverImage(CoverType.FrontCover, "image/jpeg", [1, 2, 3]);
        File.WriteAllBytes(Path.Combine(_folder.Path, "cover.jpg"), [9]);
        var files = new[] { (Path.Combine(_folder.Path, "a.mp3"), AudioTags.Empty.WithCovers([cover])), (Path.Combine(_folder.Path, "b.mp3"), AudioTags.Empty.WithCovers([cover])) };

        var result = CoverExporter.Export(files, new CoverExportOptions(null, null, "cover", SkipDuplicates: true));

        Assert.Equal((1, 1), (result.Written, result.Skipped));
        Assert.Equal([9], File.ReadAllBytes(Path.Combine(_folder.Path, "cover.jpg")));
        Assert.Equal([1, 2, 3], File.ReadAllBytes(Path.Combine(_folder.Path, "cover (2).jpg")));
    }

    /// <summary>
    /// Platzhalter im Dateinamen; verbotene Zeichen werden ersetzt.
    /// </summary>
    [Fact]
    public void CoverFileName_ReplacesPlaceholdersAndInvalidCharacters()
    {
        var tags = AudioTags.Empty.With(TagFields.Album, "AC/DC Live").With(TagFields.Artist, "AC/DC");

        var name = CoverExporter.CreateFileName("%artist% - %album%", "/m/a.mp3", tags, CoverType.FrontCover);

        Assert.Equal("AC_DC - AC_DC Live", name);
    }

    /// <summary>
    /// Lyrics: Begleitdatei wird gefunden (LRC vor TXT), Export als TXT entfernt Zeitstempel, vorhandene Dateien bleiben.
    /// </summary>
    [Fact]
    public void Lyrics_FindExportAndPlainText()
    {
        var audio = AudioFiles.Write(_folder.Path, "01 Song.mp3", AudioFiles.Mp3());
        File.WriteAllText(Path.Combine(_folder.Path, "01 Song.txt"), "alt");
        File.WriteAllText(Path.Combine(_folder.Path, "01 Song.lrc"), "[ar:Madonna]\r\n[00:12.40]Life is a mystery\r\n");
        var tags = AudioTags.Empty.WithLyrics(LyricsFiles.Read(LyricsFiles.FindSidecar(audio)!), "eng");

        var kept = LyricsFiles.Export([(audio, tags)], LyricsFormat.Txt, null, overwrite: false);
        var written = LyricsFiles.Export([(audio, tags)], LyricsFormat.Txt, null, overwrite: true);

        Assert.Equal((0, 1), (kept.Written, kept.Skipped));
        Assert.Equal(1, written.Written);
        Assert.Equal("Life is a mystery", File.ReadAllText(Path.Combine(_folder.Path, "01 Song.txt")));
    }

    /// <summary>
    /// Erzeugt einen Auftrag mit Titel „alt“ → „neu“.
    /// </summary>
    /// <param name="path">Pfad.</param>
    /// <param name="marker">Unterscheidet die Aufträge.</param>
    /// <returns>Der Auftrag.</returns>
    private static TagWriteJob Job(string path, string marker) =>
        new(path, AudioTags.Empty.With(TagFields.Title, "alt").With(TagFields.Comment, marker),
            AudioTags.Empty.With(TagFields.Title, "neu").With(TagFields.Comment, marker));

    /// <summary>
    /// Protokolliert Schreibzugriffe und schlägt auf Wunsch bei einer Datei fehl.
    /// </summary>
    private sealed class FakeStore : ITagStore
    {
        /// <summary>Pfad, bei dem das Schreiben fehlschlägt.</summary>
        public string? FailOn { get; init; }

        /// <summary>Protokoll „Pfad:Titel“.</summary>
        public List<string> Log { get; } = [];

        /// <summary>Alle Dateien gelten als unterstützt.</summary>
        /// <param name="path">Pfad.</param>
        /// <returns><c>true</c>.</returns>
        public bool IsSupported(string path) => true;

        /// <summary>Nicht benötigt.</summary>
        /// <param name="path">Pfad.</param>
        /// <param name="customNames">Namen.</param>
        /// <returns>Leere Tags.</returns>
        public AudioTags Read(string path, IReadOnlyCollection<string> customNames) => AudioTags.Empty;

        /// <summary>Protokolliert oder schlägt fehl.</summary>
        /// <param name="path">Pfad.</param>
        /// <param name="tags">Tags.</param>
        /// <param name="customNames">Namen.</param>
        public void Write(string path, AudioTags tags, IReadOnlyCollection<string> customNames)
        {
            if (path == FailOn)
            {
                throw new TagStoreException("Fehler");
            }

            Log.Add($"{path}:{tags.Get(TagFields.Title)}");
        }
    }
}
