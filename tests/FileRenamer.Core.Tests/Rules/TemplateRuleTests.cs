using System.Globalization;
using FileRenamer.Core.Files;
using FileRenamer.Core.Metadata;
using FileRenamer.Core.Rules;
using FileRenamer.Core.Tests.Support;

namespace FileRenamer.Core.Tests.Rules;

/// <summary>
/// Tests für „Name aus Vorlage“ (E61–E65, E74, E76, E77) und die EXIF-Datumsquelle (E60).
/// </summary>
public sealed class TemplateRuleTests
{
    private static readonly ImageMetadata Camera = new()
    {
        DateTaken = new DateTime(2026, 10, 12, 14, 30, 5), Make = "Canon", Model = "Canon EOS R6", FNumber = 4, ExposureTime = 1 / 250.0,
        FocalLength = 50, Iso = 400, Width = 6000, Height = 4000, Orientation = 90, Latitude = 48.137432, Longitude = -11.575491, Altitude = 519.4,
        Description = "Marienplatz", Creator = "Max Muster", Copyright = "(c) Max Muster",
    };

    private static readonly AudioMetadata Song = new()
    {
        Artist = "Madonna", Title = "Like A Prayer", Album = "Like A Prayer", AlbumArtist = "Madonna", Track = 3, Disc = 1, Year = 1989, Genre = "Pop",
    };

    /// <summary>
    /// Beispiel aus der Anforderung: „%artist% - %title%“ → „Madonna - Like A Prayer“.
    /// </summary>
    [Fact]
    public void Audio_ArtistTitle()
    {
        Assert.Equal("Madonna - Like A Prayer", Apply(new TemplateRule(), Audio("01 like a prayer.mp3")));
    }

    /// <summary>
    /// Tracknummer: Standard zweistellig, einstellbar, Format im Platzhalter hat Vorrang (E62, E76).
    /// </summary>
    [Fact]
    public void Audio_TrackDigits()
    {
        var rule = new TemplateRule { AudioTemplate = "%track% %title%" };

        Assert.Equal("03 Like A Prayer", Apply(rule, Audio("a.flac")));
        Assert.Equal("003 Like A Prayer", Apply(rule with { TrackDigits = 3 }, Audio("a.flac")));
        Assert.Equal("0003-1 Like A Prayer", Apply(rule with { AudioTemplate = "%track:0000%-%disc% %title%" }, Audio("a.m4a")));
    }

    /// <summary>
    /// Foto-Vorlage mit Datumsformat, Kamera, Belichtung, Ort und %name% (behält die Herkunft der Abschnitte).
    /// </summary>
    [Fact]
    public void Photo_AllPlaceholderKinds()
    {
        var rule = new TemplateRule { PhotoTemplate = "%name%_%model%_%date:yyyy-MM-dd%" };
        var output = new TemplateRule { PhotoTemplate = "%name%_x" }.ApplyTo(Photo("IMG_0001.jpg"));

        Assert.Equal("IMG_0001_Canon EOS R6_2026-10-12", Apply(rule, Photo("IMG_0001.jpg")));
        Assert.Equal("f4 1-250s 50mm ISO400 6000x4000 90",
            Apply(rule with { PhotoTemplate = "%fnumber% %exposure% %focallength% ISO%iso% %width%x%height% %orientation%" }, Photo("a.jpg")));
        Assert.Equal("48.13743N 11.57549W 519m", Apply(rule with { PhotoTemplate = "%lat% %lon% %alt%" }, Photo("a.jpg")));
        Assert.Equal("14-30-05 2026 10 12", Apply(rule with { PhotoTemplate = "%time% %year% %month% %day%" }, Photo("a.jpg")));
        Assert.True(output.Name.Segments[0].IsOriginal);
    }

    /// <summary>
    /// Fehlende Werte → Ersatztext mit Hinweis; ohne EXIF-Datum wird das Änderungsdatum verwendet (E63, E74).
    /// </summary>
    [Fact]
    public void Photo_MissingValues_UseReplacementAndModificationDate()
    {
        var file = RuleTestHelper.File("Bildschirmfoto.png", modified: new DateTime(2026, 10, 28)) with { Metadata = new FileMetadata(MediaKind.Image, new ImageMetadata()) };

        var output = ApplyOutput(new TemplateRule { MissingText = "Unbekannt" }, file);

        Assert.Equal("Bildschirmfoto_Unbekannt_2026-10-28", output.Name.Text);
        Assert.Equal(2, output.Issues.Count);
    }

    /// <summary>
    /// Verbotene Zeichen in Werten werden durch das Ersatzzeichen ersetzt (E64).
    /// </summary>
    [Fact]
    public void ForbiddenCharacters_AreReplaced()
    {
        var file = Audio("a.mp3") with { Metadata = new FileMetadata(MediaKind.Audio, Audio: Song with { Artist = "AC/DC", Title = "What?" }) };

        Assert.Equal("AC_DC - What_", Apply(new TemplateRule(), file));
        Assert.Equal("ACDC - What", Apply(new TemplateRule { InvalidCharacterReplacement = "" }, file));
    }

    /// <summary>
    /// Unbekannte Platzhalter (auch aus dem falschen Tab) und unzulässige Formate sind Fehler (E65).
    /// </summary>
    /// <param name="photo">Foto-Vorlage.</param>
    /// <param name="audio">Audio-Vorlage.</param>
    [Theory]
    [InlineData("%name%", "%artis% - %title%")]
    [InlineData("%name%", "%model%")]
    [InlineData("%artist%", "%title%")]
    [InlineData("%model:xx%", "%title%")]
    [InlineData("%date:HH:mm%", "%title%")]
    [InlineData("%name%", "%track:12%")]
    [InlineData("%name%/x", "%title%")]
    public void Validate_RejectsInvalidTemplates(string photo, string audio)
    {
        Assert.NotNull(new TemplateRule { PhotoTemplate = photo, AudioTemplate = audio }.ValidateSettings());
    }

    /// <summary>
    /// Andere Dateitypen bleiben mit Hinweis unverändert; eine leere Vorlage lässt ihren Dateityp unverändert (E77).
    /// </summary>
    [Fact]
    public void OtherFileTypesAndEmptyTemplates_AreUnchanged()
    {
        var pdf = new TemplateRule().ApplyTo(RuleTestHelper.File("Rechnung.pdf"));

        Assert.Equal("Rechnung.pdf", pdf.Name.Text);
        Assert.Equal(IssueSeverity.Hint, Assert.Single(pdf.Issues).Severity);
        Assert.Equal("a", Apply(new TemplateRule { AudioTemplate = "" }, Audio("a.mp3")));
    }

    /// <summary>
    /// Datumsquelle „EXIF-Aufnahmedatum“ mit Fallback auf das Änderungsdatum (E60).
    /// </summary>
    [Fact]
    public void AppendDate_ExifSource()
    {
        var rule = new AppendDateRule { Source = DateSource.ExifDate, Culture = CultureInfo.InvariantCulture };
        var withoutExif = rule.ApplyTo(RuleTestHelper.File("b.jpg", modified: new DateTime(2026, 1, 5)));

        Assert.Equal("2026-10-12_a.jpg", rule.ApplyTo(Photo("a.jpg")).Name.Text);
        Assert.Equal("2026-01-05_b.jpg", withoutExif.Name.Text);
        Assert.Single(withoutExif.Issues);
    }

    /// <summary>
    /// Erzeugt eine Fotodatei mit Kamera-Metadaten.
    /// </summary>
    /// <param name="name">Dateiname.</param>
    /// <returns>Die Datei.</returns>
    private static FileEntry Photo(string name) =>
        RuleTestHelper.File(name) with { Metadata = new FileMetadata(MediaKind.Image, Camera) };

    /// <summary>
    /// Erzeugt eine Audiodatei mit Tags.
    /// </summary>
    /// <param name="name">Dateiname.</param>
    /// <returns>Die Datei.</returns>
    private static FileEntry Audio(string name) =>
        RuleTestHelper.File(name) with { Metadata = new FileMetadata(MediaKind.Audio, Audio: Song) };

    /// <summary>
    /// Wendet die Vorlage auf den Namen ohne Endung an (wie bei „Dateierweiterung ignorieren“).
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    /// <param name="file">Die Datei.</param>
    /// <returns>Der neue Name.</returns>
    private static string Apply(TemplateRule rule, FileEntry file) => ApplyOutput(rule, file).Name.Text;

    /// <summary>
    /// Wendet die Vorlage auf den Namen ohne Endung an und liefert das vollständige Ergebnis.
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    /// <param name="file">Die Datei.</param>
    /// <returns>Das Ergebnis.</returns>
    private static RuleOutput ApplyOutput(TemplateRule rule, FileEntry file) =>
        rule.Apply([new RuleInput(file, Core.Naming.SegmentedName.FromText(Path.GetFileNameWithoutExtension(file.FileName)))])[0];
}
