using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;

namespace FileRenamer.App.Controls;

/// <summary>
/// Richtet eine Beschriftung so aus, dass ihre erste Zeile auf derselben Grundlinie liegt wie die erste Textzeile des
/// Werts rechts daneben (E135). Der Wert ist das Element in derselben Zeile und der nächsten Spalte des umgebenden
/// Grids; in einem waagerechten StackPanel (Trennzeichen wie „/“ oder „px“) das vorherige Element, sonst das nächste. Die Beschriftung bleibt oben ausgerichtet; nach jedem Layout wird ihr oberer Rand angepasst. So passt die
/// Ausrichtung unabhängig von Steuerelement (Textfeld, Auswahl, Checkbox, Segment) und Schrift.
/// </summary>
public sealed class BaselineAlignment : AvaloniaObject
{
    private const double Tolerance = 0.25;

    /// <summary>
    /// Ob die Beschriftung an der ersten Zeile ihres Nachbarn ausgerichtet wird.
    /// </summary>
    public static readonly AttachedProperty<bool> AlignToNeighborProperty =
        AvaloniaProperty.RegisterAttached<BaselineAlignment, TextBlock, bool>("AlignToNeighbor");

    /// <summary>
    /// Meldet die Ausrichtung an bzw. ab.
    /// </summary>
    static BaselineAlignment() => AlignToNeighborProperty.Changed.AddClassHandler<TextBlock>(OnChanged);

    /// <summary>
    /// Liest den Wert.
    /// </summary>
    /// <param name="label">Die Beschriftung.</param>
    /// <returns><c>true</c>, wenn ausgerichtet wird.</returns>
    public static bool GetAlignToNeighbor(TextBlock label) => label.GetValue(AlignToNeighborProperty);

    /// <summary>
    /// Setzt den Wert.
    /// </summary>
    /// <param name="label">Die Beschriftung.</param>
    /// <param name="value">Ob ausgerichtet wird.</param>
    public static void SetAlignToNeighbor(TextBlock label, bool value) => label.SetValue(AlignToNeighborProperty, value);

    /// <summary>
    /// Verbindet bzw. trennt die Ausrichtung nach jedem Layout.
    /// </summary>
    /// <param name="label">Die Beschriftung.</param>
    /// <param name="args">Die Änderung.</param>
    private static void OnChanged(TextBlock label, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is true)
        {
            label.LayoutUpdated += OnLayoutUpdated;
        }
        else
        {
            label.LayoutUpdated -= OnLayoutUpdated;
        }
    }

    /// <summary>
    /// Verschiebt die Beschriftung um den Abstand der beiden Grundlinien.
    /// </summary>
    /// <param name="sender">Die Beschriftung.</param>
    /// <param name="e">Leer.</param>
    private static void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (sender is not TextBlock { IsEffectivelyVisible: true } label
            || FindNeighbor(label) is not { } neighbor
            || FirstBaseline(label, label) is not { } own
            || FirstBaseline(neighbor, label) is not { } target)
        {
            return;
        }

        var delta = target - own;
        if (Math.Abs(delta) > Tolerance)
        {
            var margin = label.Margin;
            label.Margin = new Thickness(margin.Left, Math.Max(0, margin.Top + delta), margin.Right, margin.Bottom);
        }
    }

    /// <summary>
    /// Sucht das Element, an dem die Beschriftung ausgerichtet wird.
    /// </summary>
    /// <param name="label">Die Beschriftung.</param>
    /// <returns>Das Element oder <c>null</c>.</returns>
    private static Control? FindNeighbor(TextBlock label) => label.Parent switch
    {
        Grid grid => FindGridNeighbor(grid, label),
        StackPanel { Orientation: Avalonia.Layout.Orientation.Horizontal } panel => FindPanelNeighbor(panel, label),
        _ => null,
    };

    /// <summary>
    /// Sucht in einem waagerechten StackPanel das vorherige sichtbare Element, sonst das nächste.
    /// </summary>
    /// <param name="panel">Das Panel.</param>
    /// <param name="label">Die Beschriftung.</param>
    /// <returns>Das Element oder <c>null</c>.</returns>
    private static Control? FindPanelNeighbor(StackPanel panel, TextBlock label)
    {
        var index = panel.Children.IndexOf(label);
        return panel.Children.Take(index).LastOrDefault(IsAnchor) ?? panel.Children.Skip(index + 1).FirstOrDefault(IsAnchor);
    }

    /// <summary>
    /// Ob ein Element als Bezug taugt: sichtbar und nicht selbst eine ausgerichtete Beschriftung.
    /// </summary>
    /// <param name="control">Das Element.</param>
    /// <returns><c>true</c>, wenn es als Bezug dient.</returns>
    private static bool IsAnchor(Control control) => control.IsVisible && !(control is TextBlock text && GetAlignToNeighbor(text));

    /// <summary>
    /// Sucht das sichtbare Element in derselben Zeile und der nächsten Spalte.
    /// </summary>
    /// <param name="grid">Das Grid.</param>
    /// <param name="label">Die Beschriftung.</param>
    /// <returns>Das Element oder <c>null</c>.</returns>
    private static Control? FindGridNeighbor(Grid grid, TextBlock label)
    {
        var row = Grid.GetRow(label);
        var column = Grid.GetColumn(label) + 1;
        return grid.Children.FirstOrDefault(child => child != label && child.IsVisible && Grid.GetRow(child) == row && Grid.GetColumn(child) == column);
    }

    /// <summary>
    /// Ermittelt die Grundlinie der ersten Textzeile in einem Element, bezogen auf die Beschriftung.
    /// </summary>
    /// <param name="visual">Das Element.</param>
    /// <param name="reference">Bezugselement für die Koordinaten.</param>
    /// <returns>Die Grundlinie oder <c>null</c>, wenn kein Text gefunden wurde.</returns>
    private static double? FirstBaseline(Visual visual, Visual reference)
    {
        switch (visual)
        {
            case TextPresenter presenter when presenter.IsEffectivelyVisible && presenter.TextLayout.TextLines.Count > 0:
                return presenter.TranslatePoint(new Point(0, presenter.TextLayout.TextLines[0].Baseline), reference)?.Y;
            case TextBlock text when text.IsEffectivelyVisible && HasText(text) && text.TextLayout.TextLines.Count > 0:
                return text.TranslatePoint(new Point(0, text.Padding.Top + text.TextLayout.TextLines[0].Baseline), reference)?.Y;
        }

        foreach (var child in visual.GetVisualChildren())
        {
            if (FirstBaseline(child, reference) is { } baseline)
            {
                return baseline;
            }
        }

        return null;
    }

    /// <summary>
    /// Prüft, ob ein TextBlock Text enthält.
    /// </summary>
    /// <param name="text">Der TextBlock.</param>
    /// <returns><c>true</c> mit Text oder Inlines.</returns>
    private static bool HasText(TextBlock text) => !string.IsNullOrEmpty(text.Text) || text.Inlines?.Count > 0;
}
