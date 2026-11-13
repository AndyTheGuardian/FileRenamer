using Avalonia;
using Avalonia.Controls;
using FileRenamer.Core.Preview;

namespace FileRenamer.App.Controls;

/// <summary>
/// Ordnet die Zellen einer Vorschauzeile (oder die Spaltenköpfe) nach einem <see cref="ColumnLayout"/> an (E146).
/// Jede Zelle nennt ihre Spalte über <see cref="KeyProperty"/>; das Spaltenmodell wird über <see cref="LayoutProperty"/>
/// vom umgebenden Element geerbt, damit alle Zeilen einer Liste dieselben Spalten verwenden. Eine Zelle mit
/// <see cref="SpanRestProperty"/> (z. B. der Hinweis „keine Audiodatei“) belegt alle Spalten außer der ihres
/// Schlüssels – auf der Seite, auf der mehr Platz ist.
/// In einer horizontal scrollenden Liste (unbegrenzte Breite) verteilt die Zeile die Spalten nach der sichtbaren
/// Breite <see cref="ColumnLayout.Viewport"/>, die der Spaltenkopf setzt; passen die Spalten nicht hinein, wird die
/// Zeile breiter und die Liste scrollt (E151).
/// </summary>
public class ColumnRowPanel : Panel
{
    /// <summary>
    /// Das Spaltenmodell; wird an Kindelemente vererbt.
    /// </summary>
    public static readonly AttachedProperty<ColumnLayout?> LayoutProperty =
        AvaloniaProperty.RegisterAttached<ColumnRowPanel, Control, ColumnLayout?>("Layout", inherits: true);

    /// <summary>
    /// Schlüssel der Spalte, zu der eine Zelle gehört.
    /// </summary>
    public static readonly AttachedProperty<string?> KeyProperty =
        AvaloniaProperty.RegisterAttached<ColumnRowPanel, Control, string?>("Key");

    /// <summary>
    /// Schlüssel der Spalte, die eine übergreifende Zelle frei lässt.
    /// </summary>
    public static readonly AttachedProperty<string?> SpanRestProperty =
        AvaloniaProperty.RegisterAttached<ColumnRowPanel, Control, string?>("SpanRest");

    private ColumnLayout? _subscribed;
    private double? _scrollBasis;

    /// <summary>
    /// Ordnet neu an, wenn sich das Spaltenmodell ändert.
    /// </summary>
    static ColumnRowPanel()
    {
        LayoutProperty.Changed.AddClassHandler<ColumnRowPanel>((panel, _) => panel.Resubscribe());
        AffectsParentMeasure<ColumnRowPanel>(KeyProperty, SpanRestProperty);
    }

    /// <summary>
    /// Schneidet Zellen an den Spaltengrenzen ab.
    /// </summary>
    public ColumnRowPanel() => ClipToBounds = true;

    /// <summary>
    /// Liest das Spaltenmodell.
    /// </summary>
    /// <param name="control">Das Element.</param>
    /// <returns>Das Spaltenmodell.</returns>
    public static ColumnLayout? GetLayout(Control control) => control.GetValue(LayoutProperty);

    /// <summary>
    /// Setzt das Spaltenmodell.
    /// </summary>
    /// <param name="control">Das Element.</param>
    /// <param name="value">Das Spaltenmodell.</param>
    public static void SetLayout(Control control, ColumnLayout? value) => control.SetValue(LayoutProperty, value);

    /// <summary>
    /// Liest den Spaltenschlüssel.
    /// </summary>
    /// <param name="control">Die Zelle.</param>
    /// <returns>Der Schlüssel.</returns>
    public static string? GetKey(Control control) => control.GetValue(KeyProperty);

    /// <summary>
    /// Setzt den Spaltenschlüssel.
    /// </summary>
    /// <param name="control">Die Zelle.</param>
    /// <param name="value">Der Schlüssel.</param>
    public static void SetKey(Control control, string? value) => control.SetValue(KeyProperty, value);

    /// <summary>
    /// Liest den frei gelassenen Spaltenschlüssel.
    /// </summary>
    /// <param name="control">Die Zelle.</param>
    /// <returns>Der Schlüssel.</returns>
    public static string? GetSpanRest(Control control) => control.GetValue(SpanRestProperty);

    /// <summary>
    /// Setzt den frei gelassenen Spaltenschlüssel.
    /// </summary>
    /// <param name="control">Die Zelle.</param>
    /// <param name="value">Der Schlüssel.</param>
    public static void SetSpanRest(Control control, string? value) => control.SetValue(SpanRestProperty, value);

    /// <summary>
    /// Das geerbte Spaltenmodell.
    /// </summary>
    protected ColumnLayout? Columns => GetLayout(this);

    /// <summary>
    /// Lage der Spalten bei der aktuellen Breite.
    /// </summary>
    /// <returns>Die Spalten oder eine leere Liste.</returns>
    protected IReadOnlyList<ColumnSlot> CurrentSlots() => Columns?.Arrange(Bounds.Width) ?? [];

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Resubscribe();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Unsubscribe();
    }

    /// <summary>
    /// Misst jede Zelle mit der Breite ihrer Spalte.
    /// </summary>
    /// <param name="availableSize">Verfügbarer Platz.</param>
    /// <returns>Gewünschte Größe.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        _scrollBasis = double.IsInfinity(availableSize.Width) ? ScrollBasis() : null;
        var basis = _scrollBasis ?? availableSize.Width;
        var slots = Columns?.Arrange(basis) ?? [];
        var width = Extent(slots, basis);
        var height = 0d;
        foreach (var child in Children)
        {
            child.Measure(new Size(SlotFor(child, slots, width).Width, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(_scrollBasis is null ? availableSize.Width : width, height);
    }

    /// <summary>
    /// Ordnet jede Zelle in ihrer Spalte an.
    /// </summary>
    /// <param name="finalSize">Endgültige Größe.</param>
    /// <returns>Die verwendete Größe.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var slots = Columns?.Arrange(_scrollBasis ?? finalSize.Width) ?? [];
        var width = Math.Max(finalSize.Width, Extent(slots, 0));
        foreach (var child in Children)
        {
            var slot = SlotFor(child, slots, width);
            child.Arrange(new Rect(slot.X, 0, slot.Width, finalSize.Height));
        }

        return finalSize;
    }

    /// <summary>
    /// Wird nach einer Änderung des Spaltenmodells aufgerufen.
    /// </summary>
    protected virtual void OnColumnsChanged()
    {
        InvalidateMeasure();
        InvalidateArrange();
    }

    /// <summary>
    /// Liefert den Bereich einer Zelle: ihre Spalte, bei übergreifenden Zellen den größeren Teil neben der frei
    /// gelassenen Spalte; Zellen ohne bekannte Spalte erhalten keine Breite.
    /// </summary>
    /// <param name="child">Die Zelle.</param>
    /// <param name="slots">Die Spalten.</param>
    /// <param name="width">Gesamtbreite.</param>
    /// <returns>Der Bereich.</returns>
    private static ColumnSlot SlotFor(Control child, IReadOnlyList<ColumnSlot> slots, double width)
    {
        if (GetSpanRest(child) is { } excluded && slots.FirstOrDefault(slot => slot.Key == excluded) is { } gap)
        {
            var right = width - gap.Right;
            return gap.X >= right ? new ColumnSlot(string.Empty, 0, gap.X) : new ColumnSlot(string.Empty, gap.Right, right);
        }

        return slots.FirstOrDefault(slot => slot.Key == GetKey(child)) ?? new ColumnSlot(string.Empty, 0, 0);
    }

    /// <summary>
    /// Breite bei unbegrenztem Platz: die Summe der festen und Mindestbreiten.
    /// </summary>
    /// <returns>Die Breite.</returns>
    /// <summary>
    /// Gesamtbreite der Spalten, mindestens die Ausgangsbreite.
    /// </summary>
    /// <param name="slots">Die Spalten.</param>
    /// <param name="basis">Ausgangsbreite.</param>
    /// <returns>Die Breite.</returns>
    private static double Extent(IReadOnlyList<ColumnSlot> slots, double basis) => Math.Max(basis, slots.Count == 0 ? 0 : slots[^1].Right);

    /// <summary>
    /// Breite, nach der eine Zeile mit unbegrenztem Platz ihre Spalten verteilt: die sichtbare Breite aus dem
    /// Spaltenmodell, solange keine bekannt ist die Summe der Mindestbreiten.
    /// </summary>
    /// <returns>Die Breite.</returns>
    private double ScrollBasis() => Columns is { Viewport: > 0 } columns ? columns.Viewport : MinimumWidth();

    private double MinimumWidth() => Columns?.Columns.Sum(column => column.Width ?? column.MinWidth) ?? 0;

    /// <summary>
    /// Meldet sich beim aktuellen Spaltenmodell an.
    /// </summary>
    private void Resubscribe()
    {
        Unsubscribe();
        if (VisualRoot is not null && Columns is { } columns)
        {
            _subscribed = columns;
            columns.Changed += OnLayoutChanged;
        }

        OnColumnsChanged();
    }

    /// <summary>
    /// Meldet sich vom Spaltenmodell ab.
    /// </summary>
    private void Unsubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.Changed -= OnLayoutChanged;
            _subscribed = null;
        }
    }

    /// <summary>
    /// Reagiert auf eine Änderung des Spaltenmodells.
    /// </summary>
    /// <param name="sender">Das Spaltenmodell.</param>
    /// <param name="e">Leer.</param>
    private void OnLayoutChanged(object? sender, EventArgs e) => OnColumnsChanged();
}
