using Avalonia.Controls;
using Avalonia.Media;

namespace FileRenamer.App.Views;

/// <summary>
/// Schiebt die Spaltenköpfe beim horizontalen Scrollen einer Vorschauliste mit (E151). Der Kopf liegt außerhalb der
/// scrollenden Liste und wird deshalb um den horizontalen Versatz der Liste nach links verschoben; der umgebende
/// Container schneidet ihn ab.
/// </summary>
public sealed class HeaderScrollSync
{
    private readonly ListBox _list;
    private readonly Control _header;
    private readonly TranslateTransform _shift = new();

    /// <summary>
    /// Verbindet Liste und Kopf.
    /// </summary>
    /// <param name="list">Die horizontal scrollende Liste.</param>
    /// <param name="header">Der mitzuschiebende Kopf (Zeile mit Checkbox und Spaltenköpfen).</param>
    public HeaderScrollSync(ListBox list, Control header)
    {
        _list = list;
        _header = header;
        _header.RenderTransform = _shift;
        list.AddHandler(ScrollViewer.ScrollChangedEvent, OnScrollChanged);
    }

    /// <summary>
    /// Übernimmt den horizontalen Versatz der Liste (nur vom eigenen ScrollViewer, nicht von inneren Elementen).
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (e.Source is ScrollViewer viewer && viewer.TemplatedParent == _list)
        {
            _shift.X = -viewer.Offset.X;
        }
    }
}
