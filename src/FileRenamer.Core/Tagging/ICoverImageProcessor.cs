namespace FileRenamer.Core.Tagging;

/// <summary>
/// Verkleinert Cover und ermittelt ihre Größe (Bildverarbeitung liegt in der Oberfläche, E127).
/// </summary>
public interface ICoverImageProcessor
{
    /// <summary>
    /// Verkleinert ein Bild, sodass die längste Seite höchstens <paramref name="maxPixels"/> misst. Kleinere Bilder
    /// bleiben unverändert; JPEG wird mit Qualität 90 % gespeichert, andere Formate behalten nach Möglichkeit ihr Format (E132).
    /// </summary>
    /// <param name="image">Das Bild.</param>
    /// <param name="maxPixels">Höchstmaß der längsten Seite.</param>
    /// <returns>Das (ggf. verkleinerte) Bild.</returns>
    CoverImage Shrink(CoverImage image, int maxPixels);
}
