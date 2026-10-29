using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace FileRenamer.App.Converters;

/// <summary>
/// Bindet RadioButtons an einen Aufzählungswert: <c>true</c>, wenn der Wert dem Parameter entspricht;
/// beim Zurückschreiben wird der Parameter übernommen, sobald der Button ausgewählt wird.
/// </summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    /// <summary>
    /// Vergleicht den gebundenen Wert mit dem Parameter.
    /// </summary>
    /// <param name="value">Der Aufzählungswert.</param>
    /// <param name="targetType">Zieltyp (bool).</param>
    /// <param name="parameter">Der Vergleichswert.</param>
    /// <param name="culture">Kultur (nicht verwendet).</param>
    /// <returns><c>true</c> bei Gleichheit.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Equals(value, parameter);

    /// <summary>
    /// Liefert den Parameter, wenn der Button ausgewählt wurde; sonst keine Änderung.
    /// </summary>
    /// <param name="value">Ausgewählt-Zustand.</param>
    /// <param name="targetType">Zieltyp (Aufzählung).</param>
    /// <param name="parameter">Der Vergleichswert.</param>
    /// <param name="culture">Kultur (nicht verwendet).</param>
    /// <returns>Der Parameter oder <see cref="BindingOperations.DoNothing"/>.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? parameter : BindingOperations.DoNothing;
}
