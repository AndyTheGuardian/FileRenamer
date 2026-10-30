using FileRenamer.Core.Localization;
namespace FileRenamer.Core.Validation;

/// <summary>
/// Prüft Dateinamen plattformübergreifend nach den strengsten Regeln (Windows, macOS, Linux),
/// damit umbenannte Dateien überall gültig sind.
/// </summary>
public static class FileNameValidator
{
    /// <summary>
    /// Maximale Länge eines Dateinamens in Zeichen.
    /// </summary>
    public const int MaxLength = 255;

    private static readonly char[] ForbiddenCharacters = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    private static readonly HashSet<string> ReservedNames = new(
        ["CON", "PRN", "AUX", "NUL",
         "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
         "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"],
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Prüft einen vollständigen Dateinamen (inklusive Erweiterung).
    /// </summary>
    /// <param name="fileName">Der zu prüfende Dateiname.</param>
    /// <returns>Fehlermeldung oder <c>null</c>, wenn der Name gültig ist.</returns>
    public static string? Validate(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return CoreText.FileNameEmpty;
        }

        if (fileName is "." or "..")
        {
            return CoreText.FileNameInvalid(fileName);
        }

        return DescribeInvalidCharacter(fileName)
            ?? ValidateEnding(fileName)
            ?? ValidateReservedName(fileName)
            ?? ValidateLength(fileName);
    }

    /// <summary>
    /// Prüft einen Textbaustein (z. B. Trennzeichen oder einzufügenden Text) auf verbotene Zeichen.
    /// </summary>
    /// <param name="text">Der zu prüfende Text.</param>
    /// <returns>Fehlermeldung oder <c>null</c>, wenn kein verbotenes Zeichen enthalten ist.</returns>
    public static string? DescribeInvalidCharacter(string text)
    {
        foreach (var character in text)
        {
            if (char.IsControl(character))
            {
                return CoreText.ControlCharacter;
            }

            if (ForbiddenCharacters.Contains(character))
            {
                return CoreText.ForbiddenCharacter(character);
            }
        }

        return null;
    }

    /// <summary>
    /// Prüft, dass der Name nicht auf Punkt oder Leerzeichen endet (unter Windows unzulässig).
    /// </summary>
    /// <param name="fileName">Der Dateiname.</param>
    /// <returns>Fehlermeldung oder <c>null</c>.</returns>
    private static string? ValidateEnding(string fileName) =>
        fileName.EndsWith('.') || fileName.EndsWith(' ')
            ? CoreText.TrailingDotOrSpace
            : null;

    /// <summary>
    /// Prüft auf unter Windows reservierte Gerätenamen (auch mit Erweiterung, z. B. „CON.txt“).
    /// </summary>
    /// <param name="fileName">Der Dateiname.</param>
    /// <returns>Fehlermeldung oder <c>null</c>.</returns>
    private static string? ValidateReservedName(string fileName)
    {
        var stem = fileName.Split('.')[0].TrimEnd();
        return ReservedNames.Contains(stem) ? CoreText.ReservedName(stem) : null;
    }

    /// <summary>
    /// Prüft die maximale Länge.
    /// </summary>
    /// <param name="fileName">Der Dateiname.</param>
    /// <returns>Fehlermeldung oder <c>null</c>.</returns>
    private static string? ValidateLength(string fileName) =>
        fileName.Length > MaxLength
            ? CoreText.FileNameTooLong(fileName.Length, MaxLength)
            : null;
}
