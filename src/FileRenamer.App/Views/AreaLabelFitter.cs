using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace FileRenamer.App.Views;

/// <summary>
/// Blendet die Beschriftungen des Bereichsumschalters nur ein, wenn die Werkzeugleiste genug freien Platz hat (E147).
/// Maßstab ist die Breite der freien Spalte zwischen Profil und rechter Gruppe. Ein Abstand mit Puffer verhindert,
/// dass die Beschriftungen an der Grenze ständig ein- und ausgeblendet werden.
/// </summary>
public sealed class AreaLabelFitter
{
    private const double MinGap = 16;
    private const double Hysteresis = 8;
    private const double LabelSpacing = 6;

    private readonly ColumnDefinition _freeColumn;
    private readonly IReadOnlyList<TextBlock> _labels;

    /// <summary>
    /// Verbindet die Prüfung mit jedem Layoutdurchlauf der Werkzeugleiste.
    /// </summary>
    /// <param name="toolbar">Die Werkzeugleiste.</param>
    /// <param name="freeColumn">Die Spalte, die den freien Platz aufnimmt.</param>
    /// <param name="labels">Die Beschriftungen des Umschalters.</param>
    public AreaLabelFitter(Control toolbar, ColumnDefinition freeColumn, IReadOnlyList<TextBlock> labels)
    {
        _freeColumn = freeColumn;
        _labels = labels;
        toolbar.LayoutUpdated += (_, _) => Update();
    }

    /// <summary>
    /// Blendet die Beschriftungen ein oder aus.
    /// </summary>
    private void Update()
    {
        var free = _freeColumn.ActualWidth;
        var visible = _labels[0].IsVisible;
        var show = visible ? free >= MinGap : free >= LabelsWidth() + MinGap + Hysteresis;
        if (show != visible)
        {
            foreach (var label in _labels)
            {
                label.IsVisible = show;
            }
        }
    }

    /// <summary>
    /// Breite, die die Beschriftungen samt Abstand zum Symbol zusätzlich brauchen.
    /// </summary>
    /// <returns>Die Breite in Pixeln.</returns>
    private double LabelsWidth() => _labels.Sum(label => TextWidth(label) + LabelSpacing);

    /// <summary>
    /// Misst den Text einer – auch ausgeblendeten – Beschriftung.
    /// </summary>
    /// <param name="label">Die Beschriftung.</param>
    /// <returns>Die Textbreite.</returns>
    private static double TextWidth(TextBlock label)
    {
        using var layout = new TextLayout(
            label.Text ?? string.Empty, new Typeface(label.FontFamily, label.FontStyle, label.FontWeight), label.FontSize, null);
        return layout.WidthIncludingTrailingWhitespace;
    }
}
