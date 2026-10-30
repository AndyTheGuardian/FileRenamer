using FileRenamer.Core.Localization;
using System.Globalization;
using FileRenamer.Core.Validation;

namespace FileRenamer.Core.Rules.Templates;

/// <summary>
/// Katalog der Platzhalter für „Name aus Vorlage“ und deren Prüfung (E62, E65).
/// </summary>
public static class Placeholders
{
    /// <summary>
    /// Platzhalter, die in beiden Vorlagen erlaubt sind.
    /// </summary>
    public static IReadOnlyList<string> File { get; } = ["name", "ext", "folder"];

    /// <summary>
    /// Datums- und Zeitplatzhalter der Foto-Vorlage.
    /// </summary>
    public static IReadOnlyList<string> PhotoDate { get; } = ["date", "time", "year", "month", "day", "hour", "minute", "second"];

    /// <summary>
    /// Kameraplatzhalter der Foto-Vorlage.
    /// </summary>
    public static IReadOnlyList<string> PhotoCamera { get; } = ["make", "model", "lens", "iso", "fnumber", "exposure", "focallength"];

    /// <summary>
    /// Bildplatzhalter der Foto-Vorlage.
    /// </summary>
    public static IReadOnlyList<string> PhotoImage { get; } = ["width", "height", "orientation"];

    /// <summary>
    /// Ortsplatzhalter der Foto-Vorlage.
    /// </summary>
    public static IReadOnlyList<string> PhotoLocation { get; } = ["lat", "lon", "alt"];

    /// <summary>
    /// Beschreibungsplatzhalter der Foto-Vorlage.
    /// </summary>
    public static IReadOnlyList<string> PhotoDescription { get; } = ["description", "creator", "copyright"];

    /// <summary>
    /// Titelplatzhalter der Audio-Vorlage.
    /// </summary>
    public static IReadOnlyList<string> AudioTitle { get; } = ["artist", "title", "album", "albumartist", "composer", "genre"];

    /// <summary>
    /// Nummerierungsplatzhalter der Audio-Vorlage.
    /// </summary>
    public static IReadOnlyList<string> AudioNumbering { get; } = ["track", "disc", "year"];

    /// <summary>
    /// Alle in der Foto-Vorlage erlaubten Platzhalter.
    /// </summary>
    public static IReadOnlySet<string> Photo { get; } =
        new HashSet<string>([.. File, .. PhotoDate, .. PhotoCamera, .. PhotoImage, .. PhotoLocation, .. PhotoDescription]);

    /// <summary>
    /// Alle in der Audio-Vorlage erlaubten Platzhalter.
    /// </summary>
    public static IReadOnlySet<string> Audio { get; } = new HashSet<string>([.. File, .. AudioTitle, .. AudioNumbering]);

    /// <summary>
    /// Prüft eine Vorlage: nur bekannte Platzhalter, Formate nur bei Datum/Zeit (Datumsformat) sowie
    /// Track/Disc (Nullen für die Stellenzahl), keine verbotenen Zeichen im festen Text.
    /// </summary>
    /// <param name="template">Die Vorlage.</param>
    /// <param name="allowed">Erlaubte Platzhalter.</param>
    /// <param name="label">Bezeichnung für Meldungen, z. B. „Foto-Vorlage“.</param>
    /// <returns>Fehlermeldung oder <c>null</c>.</returns>
    public static string? Validate(string template, IReadOnlySet<string> allowed, string label)
    {
        var tokens = TemplateParser.Parse(template);
        var unknown = tokens.Where(token => token.Placeholder is { } name && !allowed.Contains(name)).Select(token => $"%{token.Placeholder}%").Distinct().ToList();
        if (unknown.Count > 0)
        {
            return $"{label}: {CoreText.UnknownPlaceholder(string.Join(", ", unknown))}";
        }

        foreach (var token in tokens)
        {
            var error = token.Literal is { } literal
                ? FileNameValidator.DescribeInvalidCharacter(literal.Replace("%", string.Empty))
                : ValidateFormat(token);
            if (error is not null)
            {
                return $"{label}: {error}";
            }
        }

        return null;
    }

    /// <summary>
    /// Prüft das Format eines Platzhalters.
    /// </summary>
    /// <param name="token">Der Platzhalter.</param>
    /// <returns>Fehlermeldung oder <c>null</c>.</returns>
    private static string? ValidateFormat(TemplateToken token)
    {
        if (token.Format is not { } format)
        {
            return null;
        }

        return token.Placeholder switch
        {
            "date" => DateFormatValidator.ValidateDateFormat(format, CultureInfo.CurrentCulture),
            "time" => DateFormatValidator.ValidateTimeFormat(format, CultureInfo.CurrentCulture),
            "track" or "disc" => format.Length is >= 1 and <= 9 && format.All(c => c == '0')
                ? null
                : CoreText.ZeroFormatOnly(token.Placeholder!),
            _ => CoreText.NoFormatAllowed(token.Placeholder!),
        };
    }
}
