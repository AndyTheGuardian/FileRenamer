using FileRenamer.Core.Metadata;
using FileRenamer.Core.Tests.Support;

namespace FileRenamer.Core.Tests.Metadata;

/// <summary>
/// Tests der eigenen Metadaten-Leser für alle unterstützten Bild- und Audioformate (E56, E57).
/// </summary>
public sealed class MetadataReaderTests
{
    /// <summary>
    /// JPEG: alle EXIF-Werte inklusive GPS und Orientierung werden gelesen.
    /// </summary>
    [Fact]
    public void Jpeg_ReadsExifValues()
    {
        var image = ReadImage(MediaFiles.Jpeg(MediaFiles.CameraTiff()), ".jpg");

        Assert.Equal(MediaFiles.TakenAt, image.DateTaken);
        Assert.Equal("Canon", image.Make);
        Assert.Equal("Canon EOS R6", image.Model);
        Assert.Equal("RF24-105mm F4 L IS USM", image.Lens);
        Assert.Equal(400, image.Iso);
        Assert.Equal(4.0, image.FNumber);
        Assert.Equal(1 / 250.0, image.ExposureTime!.Value, 6);
        Assert.Equal(50, image.FocalLength);
        Assert.Equal((6000, 4000), (image.Width, image.Height));
        Assert.Equal(90, image.Orientation);
        Assert.Equal(48.13743, image.Latitude!.Value, 5);
        Assert.Equal(11.57549, image.Longitude!.Value, 5);
        Assert.Equal(519, image.Altitude);
    }

    /// <summary>
    /// Beschreibung, Ersteller, Copyright: zuerst EXIF, dann IPTC, dann XMP (E70).
    /// </summary>
    [Fact]
    public void Jpeg_DescriptionFields_FollowExifIptcXmpPriority()
    {
        var iptc = new Dictionary<byte, string> { [120] = "IPTC-Beschreibung", [116] = "IPTC-Copyright" };
        var image = ReadImage(MediaFiles.Jpeg(MediaFiles.CameraTiff(), iptc, xmpCreator: "XMP-Ersteller"), ".jpg");

        Assert.Equal("IPTC-Beschreibung", image.Description);
        Assert.Equal("XMP-Ersteller", image.Creator);
        Assert.Equal("(c) Max Muster", image.Copyright);
    }

    /// <summary>
    /// Ohne Aufnahmedatum gilt das Digitalisierungsdatum, danach das EXIF-Änderungsdatum (E60).
    /// </summary>
    [Fact]
    public void DateTaken_FallsBackToDigitizedThenModified()
    {
        var writer = new TiffWriter();
        var digitized = writer.Build(writer.NewIfd().Ascii(0x0132, "2026:01:03 00:00:00"), writer.NewIfd().Ascii(0x9004, "2026:01:02 00:00:00"));
        var modifiedOnly = writer.Build(writer.NewIfd().Ascii(0x0132, "2026:01:03 00:00:00"));

        Assert.Equal(new DateTime(2026, 1, 2), ReadImage(MediaFiles.Jpeg(digitized), ".jpg").DateTaken);
        Assert.Equal(new DateTime(2026, 1, 3), ReadImage(MediaFiles.Jpeg(modifiedOnly), ".jpg").DateTaken);
    }

    /// <summary>
    /// TIFF-basierte RAW-Formate werden in beiden Byte-Reihenfolgen gelesen.
    /// </summary>
    /// <param name="extension">Endung.</param>
    /// <param name="littleEndian">Byte-Reihenfolge.</param>
    [Theory]
    [InlineData(".tif", true)]
    [InlineData(".dng", true)]
    [InlineData(".nef", false)]
    [InlineData(".cr2", true)]
    [InlineData(".arw", true)]
    [InlineData(".pef", false)]
    public void TiffBasedRaw_ReadsExif(string extension, bool littleEndian)
    {
        var image = ReadImage(MediaFiles.CameraTiff(littleEndian), extension);

        Assert.Equal("Canon EOS R6", image.Model);
        Assert.Equal(MediaFiles.TakenAt, image.DateTaken);
    }

    /// <summary>
    /// PNG: eXIf, Bildmaße aus IHDR (EXIF hat Vorrang) und Text „Author“ als Ersteller.
    /// </summary>
    [Fact]
    public void Png_ReadsExifAndText()
    {
        var withExif = ReadImage(MediaFiles.Png(MediaFiles.CameraTiff()), ".png");
        var withoutExif = ReadImage(MediaFiles.Png(null), ".png");

        Assert.Equal("Canon EOS R6", withExif.Model);
        Assert.Equal("PNG-Autor", withExif.Creator);
        Assert.Equal((640, 480), (withoutExif.Width, withoutExif.Height));
        Assert.Null(withoutExif.DateTaken);
    }

    /// <summary>
    /// HEIC: EXIF-Item über iinf/iloc.
    /// </summary>
    [Fact]
    public void Heic_ReadsExifItem()
    {
        var image = ReadImage(MediaFiles.Heic(MediaFiles.CameraTiff()), ".heic");

        Assert.Equal("Canon EOS R6", image.Model);
        Assert.Equal(MediaFiles.TakenAt, image.DateTaken);
    }

    /// <summary>
    /// CR3: CMT1, CMT2 und CMT4 (Südwesten → negative Koordinaten).
    /// </summary>
    [Fact]
    public void Cr3_ReadsCanonMetadataBoxes()
    {
        var image = ReadImage(MediaFiles.Cr3(), ".cr3");

        Assert.Equal("Canon EOS R5", image.Model);
        Assert.Equal(new DateTime(2026, 9, 30, 8, 15, 0), image.DateTaken);
        Assert.Equal(800, image.Iso);
        Assert.Equal(-33.85, image.Latitude!.Value, 5);
        Assert.Equal(-70.5, image.Longitude!.Value, 5);
    }

    /// <summary>
    /// RAF: EXIF aus dem eingebetteten JPEG.
    /// </summary>
    [Fact]
    public void Raf_ReadsEmbeddedJpeg()
    {
        var image = ReadImage(MediaFiles.Raf(MediaFiles.Jpeg(MediaFiles.CameraTiff())), ".raf");

        Assert.Equal("Canon EOS R6", image.Model);
    }

    /// <summary>
    /// Beschädigte oder fremde Daten führen nicht zum Abbruch.
    /// </summary>
    /// <param name="extension">Endung.</param>
    [Theory]
    [InlineData(".jpg")]
    [InlineData(".png")]
    [InlineData(".heic")]
    [InlineData(".cr3")]
    [InlineData(".raf")]
    [InlineData(".nef")]
    [InlineData(".mp3")]
    [InlineData(".flac")]
    [InlineData(".m4a")]
    public void CorruptData_ReturnsEmptyMetadata(string extension)
    {
        var random = new Random(42);
        var data = new byte[2048];
        random.NextBytes(data);
        data[0] = 0xFF;
        data[1] = 0xD8;

        var metadata = Read(data, extension);

        Assert.NotEqual(MediaKind.None, metadata.Kind);
    }

    /// <summary>
    /// MP3 mit ID3v2.3 (ISO-8859-1): Track „3/12“, Genre „(17)“ → „Rock“.
    /// </summary>
    [Fact]
    public void Mp3_Id3v23()
    {
        var audio = ReadAudio(MediaFiles.Mp3(3, new Dictionary<string, string>
        {
            ["TPE1"] = "Madonna", ["TIT2"] = "Like A Prayer", ["TALB"] = "Like A Prayer", ["TRCK"] = "3/12",
            ["TYER"] = "1989", ["TCON"] = "(17)", ["TPOS"] = "1/2", ["TPE2"] = "Madonna", ["TCOM"] = "Madonna, Patrick Leonard",
        }), ".mp3");

        Assert.Equal(("Madonna", "Like A Prayer", "Like A Prayer"), (audio.Artist, audio.Title, audio.Album));
        Assert.Equal((3, 1, 1989), (audio.Track!.Value, audio.Disc!.Value, audio.Year!.Value));
        Assert.Equal("Rock", audio.Genre);
        Assert.Equal("Madonna, Patrick Leonard", audio.Composer);
    }

    /// <summary>
    /// MP3 mit ID3v2.4 (UTF-8, Datum TDRC) und ID3v2.2 (dreistellige Frames).
    /// </summary>
    [Fact]
    public void Mp3_Id3v24AndV22()
    {
        var v24 = ReadAudio(MediaFiles.Mp3(4, new Dictionary<string, string> { ["TPE1"] = "Mötley Crüe", ["TDRC"] = "1989-09-01" }), ".mp3");
        var v22 = ReadAudio(MediaFiles.Mp3(2, new Dictionary<string, string> { ["TP1"] = "Alt", ["TT2"] = "Titel" }), ".mp3");

        Assert.Equal(("Mötley Crüe", 1989), (v24.Artist, v24.Year!.Value));
        Assert.Equal(("Alt", "Titel"), (v22.Artist, v22.Title));
    }

    /// <summary>
    /// ID3v1 ergänzt nur fehlende Werte.
    /// </summary>
    [Fact]
    public void Mp3_Id3v1_FillsMissingValues()
    {
        var audio = ReadAudio(MediaFiles.Mp3(3, new Dictionary<string, string> { ["TIT2"] = "V2 Titel" }, v1: true), ".mp3");

        Assert.Equal(("V2 Titel", "V1 Artist", 7, "Pop"), (audio.Title, audio.Artist, audio.Track!.Value, audio.Genre));
    }

    /// <summary>
    /// FLAC: Vorbis Comments.
    /// </summary>
    [Fact]
    public void Flac_ReadsVorbisComments()
    {
        var audio = ReadAudio(MediaFiles.Flac("ARTIST=Madonna", "title=Like A Prayer", "TRACKNUMBER=04", "DATE=1989-03-21", "ALBUMARTIST=Madonna"), ".flac");

        Assert.Equal(("Madonna", "Like A Prayer", 4, 1989, "Madonna"), (audio.Artist, audio.Title, audio.Track!.Value, audio.Year!.Value, audio.AlbumArtist));
    }

    /// <summary>
    /// M4A: iTunes-Atome inklusive Track und Disk.
    /// </summary>
    [Fact]
    public void M4a_ReadsItunesAtoms()
    {
        var audio = ReadAudio(MediaFiles.M4a(new Dictionary<string, string>
        {
            ["©ART"] = "Madonna", ["©nam"] = "Like A Prayer", ["©alb"] = "Like A Prayer", ["©day"] = "1989-03-21T08:00:00Z",
        }), ".m4a");

        Assert.Equal(("Madonna", "Like A Prayer", 3, 1, 1989), (audio.Artist, audio.Title, audio.Track!.Value, audio.Disc!.Value, audio.Year!.Value));
    }

    /// <summary>
    /// Die Dateiart wird aus der Endung bestimmt.
    /// </summary>
    [Fact]
    public void MediaTypes_ClassifiesExtensions()
    {
        Assert.Equal(MediaKind.Image, MediaTypes.GetKind("a.RW2"));
        Assert.Equal(MediaKind.Audio, MediaTypes.GetKind("a.M4A"));
        Assert.Equal(MediaKind.None, MediaTypes.GetKind("a.pdf"));
    }

    /// <summary>
    /// Liest Metadaten aus Bytes.
    /// </summary>
    /// <param name="data">Die Dateibytes.</param>
    /// <param name="extension">Endung.</param>
    /// <returns>Die Metadaten.</returns>
    private static FileMetadata Read(byte[] data, string extension) => MetadataReader.Read(new MemoryStream(data), extension);

    /// <summary>
    /// Liest Bildinformationen.
    /// </summary>
    /// <param name="data">Die Dateibytes.</param>
    /// <param name="extension">Endung.</param>
    /// <returns>Die Bildinformationen.</returns>
    private static ImageMetadata ReadImage(byte[] data, string extension) => Read(data, extension).Image!;

    /// <summary>
    /// Liest Audioinformationen.
    /// </summary>
    /// <param name="data">Die Dateibytes.</param>
    /// <param name="extension">Endung.</param>
    /// <returns>Die Audioinformationen.</returns>
    private static AudioMetadata ReadAudio(byte[] data, string extension) => Read(data, extension).Audio!;
}
