using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FileRenamer.App.Converters;

/// <summary>
/// Wandelt einen Farbwert <c>#RRGGBB</c> in einen Pinsel um (Farbflächen der Akzentauswahl).
/// </summary>
public sealed class HexToBrushConverter : IValueConverter
{
    /// <summary>
    /// Erzeugt einen Pinsel aus dem Farbwert.
    /// </summary>
    /// <param name="value">Der Farbwert.</param>
    /// <param name="targetType">Zieltyp.</param>
    /// <param name="parameter">Nicht verwendet.</param>
    /// <param name="culture">Nicht verwendet.</param>
    /// <returns>Der Pinsel oder <c>null</c>.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string hex && Color.TryParse(hex, out var color) ? new SolidColorBrush(color) : null;

    /// <summary>
    /// Nicht unterstützt.
    /// </summary>
    /// <param name="value">Wert.</param>
    /// <param name="targetType">Zieltyp.</param>
    /// <param name="parameter">Parameter.</param>
    /// <param name="culture">Kultur.</param>
    /// <returns>Keine Rückgabe.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
