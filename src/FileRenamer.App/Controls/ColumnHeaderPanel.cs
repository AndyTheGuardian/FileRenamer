using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using FileRenamer.App.Localization;
using FileRenamer.Core.Preview;

namespace FileRenamer.App.Controls;

/// <summary>
/// Spaltenköpfe einer Vorschauliste (E146):
/// <list type="bullet">
/// <item>Rechte Kante eines Kopfs ziehen ändert die Breite; Doppelklick auf die Kante passt sie an den Inhalt an.</item>
/// <item>Kopf ziehen verschiebt die Spalte; eine Linie in Akzentfarbe zeigt die Einfügestelle.</item>
/// <item>Das Kontextmenü speichert die Anordnung programmweit, stellt die gespeicherte wieder her oder setzt die
/// Werkseinstellung (E148).</item>
/// </list>
/// </summary>
public sealed class ColumnHeaderPanel : ColumnRowPanel, ICustomHitTest
{
    private const double EdgeTolerance = 4;
    private const double DragThreshold = 4;
    private const double AutoFitPadding = 12;

    private static readonly Cursor ResizeCursor = new(StandardCursorType.SizeWestEast);

    private ColumnSlot? _resizing;
    private ColumnSlot? _pressed;
    private Point _pressPoint;
    private bool _isDragging;
    private int _dropIndex = -1;
    private double _dropX;
    private readonly Border _dropLine = new() { IsVisible = false, IsHitTestVisible = false };

    /// <summary>
    /// Macht die ganze Fläche treffbar und legt das Kontextmenü an.
    /// </summary>
    public ColumnHeaderPanel()
    {
        Background = Brushes.Transparent;
        ClipToBounds = false;
        var save = new MenuItem { Header = Loc.T("Columns.Save") };
        save.Click += (_, _) => Columns?.Save();
        var restore = new MenuItem { Header = Loc.T("Columns.RestoreSaved") };
        restore.Click += (_, _) => Columns?.RestoreSaved();
        var reset = new MenuItem { Header = Loc.T("Columns.Reset") };
        reset.Click += (_, _) => Columns?.Reset();
        ContextMenu = new ContextMenu { Items = { save, restore, new Separator(), reset } };
        ContextMenu.Opening += (_, _) => restore.IsEnabled = Columns?.HasSaved == true;
        VisualChildren.Add(_dropLine);
    }

    /// <summary>
    /// Beginnt das Ändern der Breite oder das Verschieben; Doppelklick auf eine Kante passt die Breite an.
    /// </summary>
    /// <param name="e">Das Ereignis.</param>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || Columns is null)
        {
            return;
        }

        var point = e.GetPosition(this);
        if (EdgeAt(point.X) is { } edge)
        {
            if (e.ClickCount == 2)
            {
                AutoFit(edge.Key);
            }
            else
            {
                _resizing = edge;
                _pressPoint = point;
            }
        }
        else
        {
            _pressed = CurrentSlots().FirstOrDefault(slot => point.X >= slot.X && point.X < slot.Right);
            _pressPoint = point;
        }

        e.Pointer.Capture(this);
        e.Handled = true;
    }

    /// <summary>
    /// Ändert die Breite, verschiebt die Einfügelinie oder zeigt über einer Kante den passenden Mauszeiger.
    /// </summary>
    /// <param name="e">Das Ereignis.</param>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var point = e.GetPosition(this);
        if (_resizing is { } resizing)
        {
            Columns?.SetWidth(resizing.Key, resizing.Width + point.X - _pressPoint.X);
        }
        else if (_pressed is not null)
        {
            UpdateDrag(point);
        }
        else
        {
            Cursor = EdgeAt(point.X) is null ? Cursor.Default : ResizeCursor;
        }
    }

    /// <summary>
    /// Schließt Breitenänderung oder Verschieben ab.
    /// </summary>
    /// <param name="e">Das Ereignis.</param>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDragging && _pressed is { } pressed && _dropIndex >= 0)
        {
            Columns?.Move(pressed.Key, _dropIndex);
        }

        EndInteraction(e.Pointer);
    }

    /// <summary>
    /// Bricht ab, wenn die Maus verloren geht.
    /// </summary>
    /// <param name="e">Das Ereignis.</param>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _resizing = null;
        _pressed = null;
        _isDragging = false;
        _dropIndex = -1;
        ShowDropLine(false);
    }

    /// <summary>
    /// Trifft die ganze Kopfzeile, auch Köpfe jenseits des rechten Rands, die beim horizontalen Scrollen sichtbar
    /// werden (E151).
    /// </summary>
    /// <param name="point">Punkt in eigenen Koordinaten.</param>
    /// <returns>Ob der Punkt getroffen ist.</returns>
    public bool HitTest(Point point) =>
        point.X >= 0 && point.Y >= 0 && point.Y < Bounds.Height && point.X < (Columns?.Extent(Bounds.Width) ?? Bounds.Width);

    /// <summary>
    /// Misst auch die Einfügelinie.
    /// </summary>
    /// <param name="availableSize">Verfügbarer Platz.</param>
    /// <returns>Gewünschte Größe.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        _dropLine.Measure(availableSize);
        return base.MeasureOverride(availableSize);
    }

    /// <summary>
    /// Meldet die sichtbare Breite an das Spaltenmodell (für horizontal scrollende Zeilen, E151), ordnet die Köpfe an
    /// – bei zu schmalem Platz auch über den rechten Rand hinaus – und setzt die Einfügelinie an die Einfügestelle.
    /// </summary>
    /// <param name="finalSize">Endgültige Größe.</param>
    /// <returns>Die verwendete Größe.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Columns is { } columns)
        {
            columns.Viewport = finalSize.Width;
        }

        var size = base.ArrangeOverride(finalSize);
        var right = Columns?.Extent(finalSize.Width) ?? finalSize.Width;
        var x = Math.Clamp(_dropX - 1, 0, Math.Max(0, right - 2));
        _dropLine.Arrange(new Rect(x, 0, 2, finalSize.Height));
        return size;
    }

    /// <summary>
    /// Startet das Verschieben nach einer kleinen Mausbewegung und bestimmt die Einfügestelle.
    /// </summary>
    /// <param name="point">Mausposition.</param>
    private void UpdateDrag(Point point)
    {
        _isDragging |= Math.Abs(point.X - _pressPoint.X) > DragThreshold;
        if (!_isDragging || _pressed is not { } pressed)
        {
            return;
        }

        var others = CurrentSlots().Where(slot => slot.Key != pressed.Key).ToList();
        _dropIndex = others.Count(slot => slot.X + slot.Width / 2 < point.X);
        _dropX = _dropIndex == 0 ? others.FirstOrDefault()?.X ?? 0 : others[_dropIndex - 1].Right;
        ShowDropLine(true);
    }

    /// <summary>
    /// Setzt die Breite einer Spalte auf die breiteste sichtbare Zelle samt Kopf.
    /// </summary>
    /// <param name="key">Schlüssel der Spalte.</param>
    private void AutoFit(string key)
    {
        if (Columns is not { } columns || TopLevel.GetTopLevel(this) is not Visual root)
        {
            return;
        }

        var cells = root.GetVisualDescendants().OfType<ColumnRowPanel>()
            .Where(panel => GetLayout(panel) == columns)
            .SelectMany(panel => panel.Children.Where(child => GetKey(child) == key && child.IsVisible));
        var width = 0d;
        foreach (var cell in cells)
        {
            cell.Measure(Size.Infinity);
            width = Math.Max(width, cell.DesiredSize.Width);
            cell.InvalidateMeasure();
        }

        columns.SetWidth(key, Math.Ceiling(width) + AutoFitPadding);
    }

    /// <summary>
    /// Sucht die Spalte, deren rechte Kante an der Position liegt.
    /// </summary>
    /// <param name="x">Waagerechte Position.</param>
    /// <returns>Die Spalte oder <c>null</c>.</returns>
    private ColumnSlot? EdgeAt(double x) => CurrentSlots().FirstOrDefault(slot => Math.Abs(slot.Right - x) <= EdgeTolerance);

    /// <summary>
    /// Beendet die laufende Bedienung.
    /// </summary>
    /// <param name="pointer">Die Maus.</param>
    private void EndInteraction(IPointer pointer)
    {
        _resizing = null;
        _pressed = null;
        _isDragging = false;
        _dropIndex = -1;
        pointer.Capture(null);
        ShowDropLine(false);
    }

    /// <summary>
    /// Zeigt oder verbirgt die Einfügelinie in Akzentfarbe.
    /// </summary>
    /// <param name="visible">Ob die Linie sichtbar ist.</param>
    private void ShowDropLine(bool visible)
    {
        if (visible && _dropLine.Background is null)
        {
            var color = this.TryFindResource("SystemAccentColor", ActualThemeVariant, out var value) && value is Color accent ? accent : Colors.DodgerBlue;
            _dropLine.Background = new SolidColorBrush(color);
        }
        else if (!visible)
        {
            _dropLine.Background = null;
        }

        _dropLine.IsVisible = visible;
        InvalidateArrange();
    }
}
