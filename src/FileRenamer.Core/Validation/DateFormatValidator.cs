using FileRenamer.Core.Localization;
using System.Globalization;

namespace FileRenamer.Core.Validation;

/// <summary>
/// Prüft benutzerdefinierte Datums- und Uhrzeitformate (.NET-Schreibweise, z. B. <c>yyyy-MM-dd</c>)
/// darauf, ob sie formatierbar sind und einen gültigen Dateinamensbestandteil ergeben.
/// </summary>
public static class DateFormatValidator
{
    private static readonly DateTime SampleTimestamp = new(2026, 12, 31, 23, 59, 58, 987);

    /// <summary>
    /// Formatiert einen Zeitpunkt mit einem benutzerdefinierten Format. Ein einzelnes Zeichen
    /// wird als benutzerdefiniertes Format behandelt (sonst wäre z. B. „d“ das Standardformat „Kurzes Datum“).
    /// </summary>
    /// <param name="timestamp">Der Zeitpunkt.</param>
    /// <param name="format">Das Format.</param>
    /// <param name="culture">Die Kultur für Namen und Trennzeichen.</param>
    /// <returns>Der formatierte Text.</returns>
    public static string Format(DateTime timestamp, string format, CultureInfo culture) =>
        timestamp.ToString(format.Length == 1 ? "%" + format : format, culture);

    /// <summary>
    /// Prüft ein Datumsformat; es muss mindestens Jahr, Monat oder Tag enthalten.
    /// </summary>
    /// <param name="format">Das zu prüfende Format.</param>
    /// <param name="culture">Die Kultur, mit der später formatiert wird.</param>
    /// <returns>Fehlermeldung oder <c>null</c>, wenn das Format umsetzbar ist.</returns>
    public static string? ValidateDateFormat(string format, CultureInfo culture) =>
        Validate(format, culture, "yMd", CoreText.DateFormatWithoutDate);

    /// <summary>
    /// Prüft ein Uhrzeitformat; es muss mindestens Stunde, Minute oder Sekunde enthalten.
    /// </summary>
    /// <param name="format">Das zu prüfende Format.</param>
    /// <param name="culture">Die Kultur, mit der später formatiert wird.</param>
    /// <returns>Fehlermeldung oder <c>null</c>, wenn das Format umsetzbar ist.</returns>
    public static string? ValidateTimeFormat(string format, CultureInfo culture) =>
        Validate(format, culture, "Hhms", CoreText.TimeFormatWithoutTime);

    /// <summary>
    /// Gemeinsame Prüfung: nicht leer, enthält einen Pflichtbestandteil, ist formatierbar
    /// und das Ergebnis enthält keine in Dateinamen verbotenen Zeichen.
    /// </summary>
    /// <param name="format">Das zu prüfende Format.</param>
    /// <param name="culture">Die Kultur.</param>
    /// <param name="requiredSpecifiers">Mindestens eines dieser Zeichen muss enthalten sein.</param>
    /// <param name="missingSpecifierMessage">Meldung, wenn kein Pflichtbestandteil enthalten ist.</param>
    /// <returns>Fehlermeldung oder <c>null</c>.</returns>
    private static string? Validate(string format, CultureInfo culture, string requiredSpecifiers, string missingSpecifierMessage)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return CoreText.FormatMissing;
        }

        if (format.IndexOfAny(requiredSpecifiers.ToCharArray()) < 0)
        {
            return missingSpecifierMessage;
        }

        string sample;
        try
        {
            sample = Format(SampleTimestamp, format, culture);
        }
        catch (FormatException)
        {
            return CoreText.FormatInvalid;
        }

        var invalidCharacter = FileNameValidator.DescribeInvalidCharacter(sample);
        return invalidCharacter is null ? null : CoreText.FormatProduces(sample, invalidCharacter);
    }
}
