using FileRenamer.Core.Metadata.Readers;

namespace FileRenamer.Core.Metadata;

/// <summary>
/// Liest die Metadaten einer Datei passend zu ihrer Endung. Fehler in beschädigten Dateien führen nie zum
/// Abbruch: Es werden dann einfach keine (oder nur die bis dahin gelesenen) Werte geliefert.
/// </summary>
public static class MetadataReader
{
    /// <summary>
    /// Liest die Metadaten einer Datei.
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <returns>Die Metadaten.</returns>
    public static FileMetadata Read(string path)
    {
        var kind = MediaTypes.GetKind(path);
        if (kind == MediaKind.None)
        {
            return FileMetadata.None;
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.RandomAccess);
            return Read(stream, Path.GetExtension(path));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new FileMetadata(kind);
        }
    }

    /// <summary>
    /// Liest die Metadaten aus einem Datenstrom.
    /// </summary>
    /// <param name="stream">Lesbarer, durchsuchbarer Datenstrom.</param>
    /// <param name="extension">Dateiendung mit Punkt, z. B. „.jpg“.</param>
    /// <returns>Die Metadaten.</returns>
    public static FileMetadata Read(Stream stream, string extension)
    {
        var kind = MediaTypes.GetKind("x" + extension);
        var source = new ByteSource(stream);
        return kind switch
        {
            MediaKind.Image => new FileMetadata(kind, Image: ReadImage(source, extension)),
            MediaKind.Audio => new FileMetadata(kind, Audio: ReadAudio(source, extension)),
            _ => FileMetadata.None,
        };
    }

    /// <summary>
    /// Wählt den passenden Bildleser.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="extension">Dateiendung.</param>
    /// <returns>Die Bildinformationen.</returns>
    private static ImageMetadata ReadImage(ByteSource source, string extension)
    {
        var builder = new ImageMetadataBuilder();
        Guard(() =>
        {
            switch (extension.ToLowerInvariant())
            {
                case ".jpg" or ".jpeg" or ".jpe":
                    JpegReader.Read(source, builder);
                    break;
                case ".png":
                    PngReader.Read(source, builder);
                    break;
                case ".heic" or ".heif":
                    HeifReader.Read(source, builder);
                    break;
                case ".cr3":
                    RawContainerReader.ReadCr3(source, builder);
                    break;
                case ".raf":
                    RawContainerReader.ReadRaf(source, builder);
                    break;
                default:
                    TiffParser.Parse(source, 0, builder);
                    break;
            }
        });
        return builder.Build();
    }

    /// <summary>
    /// Wählt den passenden Audioleser.
    /// </summary>
    /// <param name="source">Die Datenquelle.</param>
    /// <param name="extension">Dateiendung.</param>
    /// <returns>Die Audioinformationen.</returns>
    private static AudioMetadata ReadAudio(ByteSource source, string extension)
    {
        var builder = new AudioMetadataBuilder();
        Guard(() =>
        {
            switch (extension.ToLowerInvariant())
            {
                case ".flac":
                    FlacAndMp4Reader.ReadFlac(source, builder);
                    break;
                case ".m4a":
                    FlacAndMp4Reader.ReadMp4(source, builder);
                    break;
                default:
                    Id3Reader.Read(source, builder);
                    break;
            }
        });
        return builder.Build();
    }

    /// <summary>
    /// Führt einen Lesevorgang aus und ignoriert Fehler durch beschädigte oder abgeschnittene Daten.
    /// </summary>
    /// <param name="read">Der Lesevorgang.</param>
    private static void Guard(Action read)
    {
        try
        {
            read();
        }
        catch (Exception exception) when (exception is IOException or EndOfStreamException or ArgumentException
                                              or IndexOutOfRangeException or OverflowException or InvalidOperationException)
        {
            // Beschädigte Metadaten: bis hierhin gelesene Werte bleiben erhalten.
        }
    }
}
