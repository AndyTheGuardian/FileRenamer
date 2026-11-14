using Avalonia;
using Avalonia.Headless;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using FileRenamer.App.Controls;
using FileRenamer.App.ViewModels;
using FileRenamer.App.Views.Tags;
using FileRenamer.Core.Profiles;
using FileRenamer.Core.Tagging.Changes;

namespace FileRenamer.HeadlessChecks;

/// <summary>
/// Teil der Prüfungen des Bereichs „Tags“: Farbe markierten Texts sowie Spalten der Vorschaulisten (verbreitern,
/// verschieben, anpassen, Kontextmenü, Speichern und Laden).
/// </summary>
public sealed partial class TagAreaChecks
{
    private List<ColumnRowPanel> _tagRowPanels = [];

    /// <summary>
    /// Markierter Text (E144): Hintergrund in der Akzentfarbe, Schrift auf hellen Farben dunkel, auf dunklen hell.
    /// </summary>
    private void SelectionColors()
    {
        Show(TagFunction.Edit);
        var box = _window.GetVisualDescendants().OfType<TextBox>().First(b => b.IsEffectivelyVisible && b.FindAncestorOfType<ComboBox>() is null);
        var cell = _window.GetVisualDescendants().OfType<SelectableTextBlock>().First(b => b.IsEffectivelyVisible);
        foreach (var (hex, wantDark) in new[] { ("#F2E35C", true), ("#BFE3FF", true), ("#2F6FDF", false) })
        {
            ApplyAccent(hex);
            var foreground = (box.SelectionForegroundBrush as ISolidColorBrush)?.Color;
            var background = (box.SelectionBrush as ISolidColorBrush)?.Color;
            var cellForeground = (cell.SelectionForegroundBrush as ISolidColorBrush)?.Color;
            var ok = foreground is { } f && cellForeground == foreground && (wantDark ? Luminance(f) < 100 : Luminance(f) > 180) && background == Color.Parse(hex);
            Check($"Markierter Text auf {hex}: Schrift {(wantDark ? "dunkel" : "hell")}", ok, $"fg={foreground} bg={background} cell={cellForeground}");
        }

        ApplyAccent("#F2E35C");
        box.Text = "Markierter Text";
        box.Focus();
        box.SelectAll();
        Shot("tags-10-selection-light-accent.png");
        box.Text = string.Empty;
        ApplyAccent(null);
    }

    /// <summary>
    /// Wendet eine Akzentfarbe im hellen Design an, ohne die Einstellungen zu speichern.
    /// </summary>
    /// <param name="hex">Die Farbe oder <c>null</c> für den Standard.</param>
    private void ApplyAccent(string? hex)
    {
        _theme.Apply(new AppSettings { Theme = ThemePreference.Light, LightColors = new() { AccentColor = hex } });
        CheckRunner.Pump();
    }

    /// <summary>
    /// Wahrgenommene Helligkeit einer Farbe (0–255).
    /// </summary>
    /// <param name="color">Die Farbe.</param>
    /// <returns>Die Helligkeit.</returns>
    private static double Luminance(Color color) => 0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B;

    /// <summary>
    /// Ob Köpfe und Zellen aller Zeilen der Tag-Vorschau bündig stehen.
    /// </summary>
    /// <returns><c>true</c>, wenn alle sichtbaren Zellen unter ihrem Kopf beginnen.</returns>
    private bool Aligned()
    {
        var header = TagHeader();
        return _tagRowPanels.Count > 0 && Tags.Columns.Columns.All(column => _tagRowPanels
            .Where(panel => panel.Children.Any(child => ColumnRowPanel.GetKey(child) == column.Key && child.IsVisible))
            .All(panel => Math.Abs(CellX(panel, column.Key) - CellX(header, column.Key)) < 0.6));
    }

    /// <summary>
    /// Spalten (E146): Kante ziehen verbreitert, Kopf ziehen verschiebt, Doppelklick auf die Kante passt die Breite an.
    /// </summary>
    private void ColumnDragAndResize()
    {
        Show(TagFunction.Edit);
        var header = TagHeader();
        _tagRowPanels = _window.GetVisualDescendants().OfType<ColumnRowPanel>()
            .Where(p => p is not ColumnHeaderPanel && p.FindAncestorOfType<TagAreaView>() is not null).ToList();
        Check("Spalten: Layout an Zeilen vererbt, Köpfe und Zellen bündig", _tagRowPanels.Count >= 5 && Aligned(), _tagRowPanels.Count.ToString());
        var origin = header.TranslatePoint(new Point(0, 0), _window)!.Value;
        var y = origin.Y + header.Bounds.Height / 2;
        double Edge(string key) => origin.X + Tags.Columns.Arrange(header.Bounds.Width).First(slot => slot.Key == key).Right - 1;
        Drag(new Point(Edge("file"), y), new Point(Edge("file") + 60, y));
        Check("Kante ziehen: Datei-Spalte 60 px breiter", Tags.Columns.Columns.First(c => c.Key == "file").Width == 250 && Aligned(),
            $"{Tags.Columns.Columns.First(c => c.Key == "file").Width}");
        var year = Tags.Columns.Arrange(header.Bounds.Width).First(slot => slot.Key == "year");
        _window.MouseDown(new Point(origin.X + year.X + 10, y), MouseButton.Left);
        _window.MouseMove(new Point(origin.X + 40, y));
        _window.MouseMove(new Point(origin.X + 5, y));
        Shot("tags-14-column-drag.png");
        _window.MouseUp(new Point(origin.X + 5, y), MouseButton.Left);
        CheckRunner.Pump();
        Check("Kopf ziehen: Jahr an erster Stelle, Zellen folgen", Tags.Columns.Columns[0].Key == "year" && Aligned(), string.Join(",", Tags.Columns.Columns.Select(c => c.Key)));
        Shot("tags-15-columns-moved.png");
        var edge = new Point(Edge("file"), y);
        // Zwei Klicks ohne Pause dazwischen: Doppelklick
        _window.MouseDown(edge, MouseButton.Left);
        _window.MouseUp(edge, MouseButton.Left);
        Click(edge);
        var fitted = Tags.Columns.Columns.First(c => c.Key == "file").Width;
        Check("Doppelklick auf Kante: Breite an Inhalt angepasst", fitted is > 100 and < 250, $"{fitted}");
        Tags.Columns.Reset();
        CheckRunner.Pump();
        Check("Zurücksetzen: Standard, bündig", Tags.Columns.IsDefault && Aligned());
    }

    /// <summary>
    /// Zieht mit gedrückter linker Maustaste von einem Punkt zu einem anderen.
    /// </summary>
    /// <param name="from">Startpunkt.</param>
    /// <param name="to">Zielpunkt.</param>
    private void Drag(Point from, Point to)
    {
        _window.MouseDown(from, MouseButton.Left);
        _window.MouseMove(to);
        _window.MouseUp(to, MouseButton.Left);
        CheckRunner.Pump();
    }

    /// <summary>
    /// Kontextmenü der Spaltenköpfe (E148): Speichern, Standard, Gespeicherte wiederherstellen; nach einem Neustart
    /// gilt die gespeicherte Anordnung.
    /// </summary>
    private void ColumnMenuAndRestart()
    {
        var items = TagHeader().ContextMenu!.Items.OfType<MenuItem>().ToList();
        Check("Kopf-Kontextmenü: Speichern, Gespeicherte wiederherstellen, Auf Standard", items.Count == 3, string.Join(" | ", items.Select(m => m.Header)));
        Tags.Columns.Move("lyrics", 0);
        Tags.Columns.SetWidth("title", 222);
        Click(items[0]);
        Click(items[2]);
        Check("Menü „Auf Standard“: Werkseinstellung, Gespeicherte bleibt", Tags.Columns.IsDefault && Tags.Columns.HasSaved && Aligned());
        Click(items[1]);
        Check("Menü „Gespeicherte wiederherstellen“", Tags.Columns.Columns[0].Key == "lyrics" && Tags.Columns.Columns.First(c => c.Key == "title").Width == 222 && Aligned());
        var restarted = CreateViewModel();
        Check("Nach Neustart: gespeicherte Tag-Spalten geladen, Rename-Liste Standard", restarted.Tags.Columns.Columns[0].Key == "lyrics"
            && restarted.Tags.Columns.Columns.First(c => c.Key == "title").Width == 222 && restarted.RenameColumns.IsDefault);
        Tags.Columns.Reset();
        CheckRunner.Pump();
    }

    /// <summary>
    /// Löst einen Menüeintrag aus.
    /// </summary>
    /// <param name="item">Der Eintrag.</param>
    private static void Click(MenuItem item)
    {
        item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        CheckRunner.Pump();
    }

    /// <summary>
    /// Rename-Liste: getauschte Spalten bleiben mit dem Kopf bündig.
    /// </summary>
    private void RenameColumns()
    {
        _vm.IsTagArea = false;
        CheckRunner.Pump();
        var header = RenameHeader();
        var rows = _window.GetVisualDescendants().OfType<ColumnRowPanel>()
            .Where(p => p is not ColumnHeaderPanel && p.IsEffectivelyVisible && p.FindAncestorOfType<TagAreaView>() is null).ToList();
        _vm.RenameColumns.Move("new", 0);
        CheckRunner.Pump();
        Check("Rename-Liste: Spalten getauscht, bündig", rows.Count > 0
            && rows.All(p => Math.Abs(CellX(p, "new") - CellX(header, "new")) < 0.6 && CellX(p, "new") < CellX(p, "original")), rows.Count.ToString());
        Shot("tags-16-rename-swapped.png");
        _vm.RenameColumns.Reset();
        _vm.IsTagArea = true;
        CheckRunner.Pump();
    }

    /// <summary>
    /// Spaltenkopf der Rename-Liste.
    /// </summary>
    /// <returns>Der Spaltenkopf.</returns>
    private ColumnHeaderPanel RenameHeader() =>
        _window.GetVisualDescendants().OfType<ColumnHeaderPanel>().First(h => h.FindAncestorOfType<TagAreaView>() is null && h.IsEffectivelyVisible);
}
