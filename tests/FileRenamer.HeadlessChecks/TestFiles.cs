using FileRenamer.Core.Tagging;
using SkiaSharp;

namespace FileRenamer.HeadlessChecks;

/// <summary>
/// Erzeugt die Testdateien der Prüfungen: kleinste gültige MP3- und FLAC-Dateien, ein PDF, eine LRC-Datei und ein
/// großes Coverbild.
/// </summary>
public static class TestFiles
{
    /// <summary>
    /// Legt das Album „Like a Prayer“ an: zwei MP3 (eine mit Tags), eine FLAC, ein PDF, Lyrics zur ersten Datei und
    /// ein Coverbild mit 1600 × 1200 Pixeln.
    /// </summary>
    /// <param name="music">Ordner des Albums.</param>
    /// <param name="coverPath">Pfad des Coverbilds.</param>
    /// <param name="store">Zum Schreiben der Ausgangs-Tags.</param>
    public static void CreateAlbum(string music, string coverPath, ITagStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        Directory.CreateDirectory(music);
        File.WriteAllBytes(Path.Combine(music, "01 Like A Prayer.mp3"), Mp3());
        File.WriteAllBytes(Path.Combine(music, "02 Express Yourself.mp3"), Mp3());
        File.WriteAllBytes(Path.Combine(music, "03 Love Song.flac"), Flac());
        File.WriteAllText(Path.Combine(music, "booklet.pdf"), "%PDF");
        File.WriteAllText(Path.Combine(music, "01 Like A Prayer.lrc"), "[00:12.40]Life is a mystery\n[00:16.10]Everyone must stand alone");
        WriteCover(coverPath);
        store.Write(Path.Combine(music, "02 Express Yourself.mp3"),
            AudioTags.Empty.With(TagFields.Title, "Express Yourself").With(TagFields.Artist, "Madonna").With(TagFields.Album, "Like a Prayer"), []);
    }

    /// <summary>
    /// Legt drei MP3-Dateien und ein PDF an.
    /// </summary>
    /// <param name="folder">Der Ordner.</param>
    public static void CreateSimple(string folder)
    {
        Directory.CreateDirectory(folder);
        foreach (var name in new[] { "01 A.mp3", "02 B.mp3", "03 C.mp3" })
        {
            File.WriteAllBytes(Path.Combine(folder, name), Mp3());
        }

        File.WriteAllText(Path.Combine(folder, "booklet.pdf"), "%PDF");
    }

    /// <summary>
    /// Löscht einen Testordner; Fehler werden ignoriert.
    /// </summary>
    /// <param name="folder">Der Ordner.</param>
    public static void Delete(string folder)
    {
        try
        {
            Directory.Delete(folder, recursive: true);
        }
        catch (IOException)
        {
            // Aufräumen ist nicht Teil der Prüfung.
        }
    }

    /// <summary>
    /// Zehn MPEG-Rahmen (MPEG-1 Layer III, 128 kbit/s, 44,1 kHz) ohne Ton.
    /// </summary>
    /// <returns>Der Dateiinhalt.</returns>
    private static byte[] Mp3()
    {
        var frame = new byte[417];
        frame[0] = 0xFF;
        frame[1] = 0xFB;
        frame[2] = 0x90;
        frame[3] = 0x64;
        return Enumerable.Repeat(frame, 10).SelectMany(bytes => bytes).ToArray();
    }

    /// <summary>
    /// FLAC-Kopf mit STREAMINFO (44,1 kHz, Stereo, 16 Bit) ohne Audiodaten.
    /// </summary>
    /// <returns>Der Dateiinhalt.</returns>
    private static byte[] Flac()
    {
        var info = new byte[34];
        info[0] = 0x10;
        info[2] = 0x10;
        var value = (44100UL << 44) | (1UL << 41) | (15UL << 36);
        for (var i = 0; i < 8; i++)
        {
            info[10 + i] = (byte)(value >> (56 - 8 * i));
        }

        return [.. "fLaC"u8.ToArray(), 0x80, 0, 0, 34, .. info, .. new byte[16]];
    }

    /// <summary>
    /// Schreibt ein einfarbiges JPEG mit 1600 × 1200 Pixeln.
    /// </summary>
    /// <param name="path">Zielpfad.</param>
    private static void WriteCover(string path)
    {
        using var bitmap = new SKBitmap(1600, 1200);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(new SKColor(0xF0, 0xA3, 0x5E));
        }

        File.WriteAllBytes(path, bitmap.Encode(SKEncodedImageFormat.Jpeg, 90).ToArray());
    }
}
