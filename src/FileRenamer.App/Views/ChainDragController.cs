using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FileRenamer.App.ViewModels;

namespace FileRenamer.App.Views;

/// <summary>
/// Reine Oberflächenmechanik für das Umsortieren der Kette per Drag &amp; Drop (E102, wie Mockup v10):
/// Ein Abbild des Eintrags klebt am Mauszeiger, der Eintrag selbst bleibt blass an seinem Platz,
/// eine Linie zeigt die Einfügestelle. Verschoben wird erst beim Loslassen.
/// </summary>
public sealed class ChainDragController
{
    private const string DraggingClass = "dragging";
    private const string HandleClass = "dragHandle";

    private readonly ListBox _list;
    private readonly Visual _layer;
    private readonly Control _ghost;
    private readonly Control _dropLine;
    private readonly Action<int, int> _move;

    private Control? _container;
    private Point _grabOffset;
    private int _fromIndex = -1;
    private int _targetIndex = -1;

    /// <summary>
    /// Erzeugt den Controller und registriert die Mausereignisse an der Liste.
    /// </summary>
    /// <param name="list">Die Kettenliste.</param>
    /// <param name="layer">Ebene über dem Fensterinhalt, in der Abbild und Linie liegen (Canvas).</param>
    /// <param name="ghost">Abbild des gezogenen Eintrags.</param>
    /// <param name="dropLine">Linie für die Einfügestelle.</param>
    /// <param name="move">Verschiebt einen Eintrag von Index 1 nach Index 2.</param>
    public ChainDragController(ListBox list, Visual layer, Control ghost, Control dropLine, Action<int, int> move)
    {
        _list = list;
        _layer = layer;
        _ghost = ghost;
        _dropLine = dropLine;
        _move = move;
        list.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        list.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        list.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        list.PointerCaptureLost += (_, _) => EndDrag();
    }

    /// <summary>
    /// Ob gerade gezogen wird.
    /// </summary>
    private bool IsDragging => _container is not null;

    /// <summary>
    /// Beginnt das Ziehen, wenn der Griff eines Eintrags gedrückt wird.
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not TextBlock { DataContext: ChainItemViewModel item } handle || !handle.Classes.Contains(HandleClass) ||
            _list.ContainerFromItem(item) is not { } container)
        {
            return;
        }

        _container = container;
        _fromIndex = _list.IndexFromContainer(container);
        _targetIndex = _fromIndex;
        _grabOffset = e.GetPosition(container);
        container.Classes.Add(DraggingClass);

        _ghost.DataContext = item;
        _ghost.Width = container.Bounds.Width;
        _ghost.IsVisible = true;
        PlaceGhost(e.GetPosition(_layer));

        e.Pointer.Capture(_list);
        e.Handled = true;
    }

    /// <summary>
    /// Lässt das Abbild dem Mauszeiger folgen und aktualisiert die Einfügestelle.
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!IsDragging)
        {
            return;
        }

        PlaceGhost(e.GetPosition(_layer));
        _targetIndex = FindIndexAt(e.GetPosition(_list).Y);
        PlaceDropLine();
        e.Handled = true;
    }

    /// <summary>
    /// Legt den Eintrag an der angezeigten Einfügestelle ab.
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!IsDragging)
        {
            return;
        }

        var (from, to) = (_fromIndex, _targetIndex);
        EndDrag();
        _move(from, to);
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    /// <summary>
    /// Beendet das Ziehen ohne Verschieben (auch bei Verlust der Mauserfassung).
    /// </summary>
    private void EndDrag()
    {
        _container?.Classes.Remove(DraggingClass);
        _container = null;
        _ghost.IsVisible = false;
        _ghost.DataContext = null;
        _dropLine.IsVisible = false;
    }

    /// <summary>
    /// Setzt das Abbild so, dass es an derselben Stelle am Mauszeiger hängt, an der der Eintrag gegriffen wurde.
    /// </summary>
    /// <param name="pointer">Mausposition relativ zur Ziehebene.</param>
    private void PlaceGhost(Point pointer)
    {
        Canvas.SetLeft(_ghost, pointer.X - _grabOffset.X);
        Canvas.SetTop(_ghost, pointer.Y - _grabOffset.Y);
    }

    /// <summary>
    /// Zeigt die Linie über dem Zieleintrag (nach oben ziehen) bzw. darunter (nach unten ziehen).
    /// </summary>
    private void PlaceDropLine()
    {
        if (_targetIndex == _fromIndex || _list.ContainerFromIndex(_targetIndex) is not { } target ||
            target.TranslatePoint(default, _layer) is not { } origin)
        {
            _dropLine.IsVisible = false;
            return;
        }

        var y = _targetIndex < _fromIndex ? origin.Y : origin.Y + target.Bounds.Height;
        Canvas.SetLeft(_dropLine, origin.X);
        Canvas.SetTop(_dropLine, y - _dropLine.Height / 2);
        _dropLine.Width = target.Bounds.Width;
        _dropLine.IsVisible = true;
    }

    /// <summary>
    /// Ermittelt den Index des Eintrags an einer vertikalen Position (begrenzt auf erste/letzte Position).
    /// </summary>
    /// <param name="y">Y-Koordinate relativ zur Liste.</param>
    /// <returns>Der Index.</returns>
    private int FindIndexAt(double y)
    {
        var count = _list.ItemCount;
        for (var index = 0; index < count; index++)
        {
            if (_list.ContainerFromIndex(index) is { } container &&
                container.TranslatePoint(default, _list) is { } origin &&
                y < origin.Y + container.Bounds.Height)
            {
                return index;
            }
        }

        return Math.Max(count - 1, 0);
    }
}
