using System.Globalization;
using Avalonia.Data.Converters;
using FileRenamer.App.ViewModels.Tags;
using FileRenamer.Core.Tagging;

namespace FileRenamer.App.Converters;

/// <summary>
/// Zeigt einen Bildtyp in Worten der aktuellen Sprache an (z. B. „Vorderseite“).
/// </summary>
public sealed class CoverTypeNameConverter : IValueConverter
{
    /// <summary>
    /// Gemeinsame Instanz für XAML.
    /// </summary>
    public static CoverTypeNameConverter Instance { get; } = new();

    /// <summary>
    /// Wandelt einen Bildtyp in seinen Namen.
    /// </summary>
    /// <param name="value">Der Bildtyp.</param>
    /// <param name="targetType">Zieltyp.</param>
    /// <param name="parameter">Nicht verwendet.</param>
    /// <param name="culture">Nicht verwendet.</param>
    /// <returns>Der Name oder ein leerer Text.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is CoverType type ? CoverEditorViewModel.TypeName(type) : string.Empty;

    /// <summary>
    /// Nicht unterstützt.
    /// </summary>
    /// <param name="value">Der Wert.</param>
    /// <param name="targetType">Zieltyp.</param>
    /// <param name="parameter">Nicht verwendet.</param>
    /// <param name="culture">Nicht verwendet.</param>
    /// <returns>Nie.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
