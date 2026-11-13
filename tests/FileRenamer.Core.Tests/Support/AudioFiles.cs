namespace FileRenamer.Core.Tests.Support;

/// <summary>
/// Erzeugt kleinste gültige Audiodateien ohne Tags, die TagLib# lesen und beschreiben kann (Stille).
/// </summary>
internal static class AudioFiles
{
    /// <summary>
    /// MP3 aus zehn stummen MPEG-1-Layer-III-Frames (128 kbit/s, 44,1 kHz).
    /// </summary>
    /// <returns>Die Dateidaten.</returns>
    public static byte[] Mp3()
    {
        var frame = new byte[417];
        frame[0] = 0xFF;
        frame[1] = 0xFB;
        frame[2] = 0x90;
        frame[3] = 0x64;
        return Enumerable.Repeat(frame, 10).SelectMany(bytes => bytes).ToArray();
    }

    /// <summary>
    /// FLAC mit STREAMINFO (44,1 kHz, Stereo, 16 Bit) ohne Audiodaten.
    /// </summary>
    /// <returns>Die Dateidaten.</returns>
    public static byte[] Flac()
    {
        var streamInfo = new byte[34];
        streamInfo[0] = 0x10;
        streamInfo[2] = 0x10;
        var packed = (44100UL << 44) | (1UL << 41) | (15UL << 36);
        for (var index = 0; index < 8; index++)
        {
            streamInfo[10 + index] = (byte)(packed >> (56 - (8 * index)));
        }

        return [.. "fLaC"u8.ToArray(), 0x80, 0, 0, 34, .. streamInfo, .. new byte[16]];
    }

    /// <summary>
    /// Schreibt eine Datei in einen Ordner.
    /// </summary>
    /// <param name="folder">Der Ordner.</param>
    /// <param name="name">Dateiname.</param>
    /// <param name="data">Die Daten.</param>
    /// <returns>Der Pfad.</returns>
    public static string Write(string folder, string name, byte[] data)
    {
        var path = Path.Combine(folder, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, data);
        return path;
    }
}

/// <summary>
/// Temporärer Ordner, der nach dem Test gelöscht wird.
/// </summary>
internal sealed class TempFolder : IDisposable
{
    /// <summary>
    /// Legt einen leeren Ordner an.
    /// </summary>
    public TempFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FileRenamerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>
    /// Pfad des Ordners.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Löscht den Ordner samt Inhalt.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
