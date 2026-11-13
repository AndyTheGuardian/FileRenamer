using FileRenamer.Core.Tagging;
using SkiaSharp;

namespace FileRenamer.App.Services;

/// <summary>
/// Verkleinert Cover mit SkiaSharp, das Avalonia ohnehin mitbringt (keine zusätzliche Abhängigkeit, E127, E132).
/// JPEG wird mit Qualität 90 % gespeichert, PNG und WebP behalten ihr Format, andere Formate werden als PNG gespeichert.
/// </summary>
public sealed class SkiaCoverImageProcessor : ICoverImageProcessor
{
    private const int JpegQuality = 90;

    /// <summary>
    /// Verkleinert das Bild, wenn seine längste Seite das Höchstmaß überschreitet; nicht lesbare Bilder bleiben unverändert.
    /// </summary>
    /// <param name="image">Das Bild.</param>
    /// <param name="maxPixels">Höchstmaß der längsten Seite.</param>
    /// <returns>Das (ggf. verkleinerte) Bild.</returns>
    public CoverImage Shrink(CoverImage image, int maxPixels)
    {
        using var bitmap = SKBitmap.Decode(image.Data);
        if (bitmap is null || Math.Max(bitmap.Width, bitmap.Height) <= maxPixels)
        {
            return image;
        }

        var scale = (double)maxPixels / Math.Max(bitmap.Width, bitmap.Height);
        var info = new SKImageInfo(Math.Max(1, (int)Math.Round(bitmap.Width * scale)), Math.Max(1, (int)Math.Round(bitmap.Height * scale)));
        using var resized = bitmap.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell));
        return resized is null ? image : Encode(image, resized);
    }

    /// <summary>
    /// Kodiert das verkleinerte Bild im passenden Format.
    /// </summary>
    /// <param name="original">Das ursprüngliche Bild (Typ und Format).</param>
    /// <param name="resized">Das verkleinerte Bild.</param>
    /// <returns>Das neue Bild.</returns>
    private static CoverImage Encode(CoverImage original, SKBitmap resized)
    {
        var (format, mimeType) = original.MimeType switch
        {
            "image/png" => (SKEncodedImageFormat.Png, "image/png"),
            "image/webp" => (SKEncodedImageFormat.Webp, "image/webp"),
            "image/jpeg" => (SKEncodedImageFormat.Jpeg, "image/jpeg"),
            _ => (SKEncodedImageFormat.Png, "image/png"),
        };
        using var data = resized.Encode(format, JpegQuality);
        return data is null ? original : new CoverImage(original.Type, mimeType, data.ToArray());
    }
}
