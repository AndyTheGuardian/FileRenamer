using Avalonia;
using Avalonia.Headless;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using FileRenamer.App.Controls;
using FileRenamer.App.ViewModels.Tags;
using FileRenamer.App.Views.Tags;
using FileRenamer.Core.Tagging;
using FileRenamer.Core.Tagging.Changes;

namespace FileRenamer.HeadlessChecks;

/// <summary>
/// Teil der Prüfungen des Bereichs „Tags“: Kopieren, „Vorher anzeigen“, Checkboxen und Zeilenmarkierung, Zeilenhöhe
/// und horizontales Scrollen beider Vorschaulisten.
/// </summary>
public sealed partial class TagAreaChecks
{
    /// <summary>
    /// Kopieren nimmt den gespeicherten Stand (F66, F67); „Vorher anzeigen“ blendet alte Werte ein und aus, der neue
    /// Wert bleibt vollständig sichtbar (E145).
    /// </summary>
    private void CopyAndShowOld()
    {
        var edit = (EditTagsEditorViewModel)Show(TagFunction.Edit);
        Tags.AreAllSelected = true;
        foreach (var row in Tags.Rows.Where(r => r.IsAudio))
        {
            row.IsMarked = true;
        }

        edit.TitleField.Value = "NEU";
        edit.StageCommand.Execute(null);
        CheckRunner.Pump();
        Tags.CopyRowsCommand.Execute(TagCopyColumns.Tags);
        CheckRunner.Pump();
        Check("Tags kopieren: gespeicherter Stand, nicht vorgemerkt", _clipboard.Text.Contains("Express Yourself", StringComparison.Ordinal)
            && !_clipboard.Text.Contains("NEU", StringComparison.Ordinal) && !_clipboard.Text.Contains(".mp3", StringComparison.Ordinal), _clipboard.Text);
        Tags.ShowOldValues = false;
        CheckRunner.Pump();
        Check("Vorher anzeigen aus: alter Wert verborgen", !OldValueVisible());
        Tags.ShowOldValues = true;
        CheckRunner.Pump();
        Check("Vorher anzeigen an: alter Wert sichtbar", OldValueVisible());
        var newValues = _window.GetVisualDescendants().OfType<SelectableTextBlock>().Where(b => b.Text == "NEU" && b.IsEffectivelyVisible).ToList();
        Check("Vorher anzeigen: neuer Wert bleibt vollständig sichtbar", newValues.Count == 3
            && newValues.All(b => b.Bounds.Width >= b.DesiredSize.Width - 0.5 && b.TextLayout.TextLines.All(line => !line.HasCollapsed)),
            string.Join(",", newValues.Select(b => b.Bounds.Width.ToString("0"))));
        Shot("tags-11-showold.png");
        Tags.ShowOldValues = false;
        CheckRunner.Pump();
        Check("Vorher anzeigen wieder aus: verborgen", !OldValueVisible());
        Tags.CopyRowsCommand.Execute(TagCopyColumns.FileNames);
        CheckRunner.Pump();
        Check("Dateinamen kopieren", _clipboard.Text.Split('\n').Length == 3 && _clipboard.Text.StartsWith("01 Like A Prayer.mp3", StringComparison.Ordinal), _clipboard.Text);
    }

    /// <summary>
    /// Ob der durchgestrichene alte Titel der zweiten Datei sichtbar ist.
    /// </summary>
    /// <returns><c>true</c>, wenn sichtbar.</returns>
    private bool OldValueVisible() =>
        _window.GetVisualDescendants().OfType<TextBlock>().Any(b => b.Classes.Contains("oldValue") && b.Text == "Express Yourself" && b.IsEffectivelyVisible);

    /// <summary>
    /// Kontextmenü der Tag-Vorschau mit vier aktiven Kopiereinträgen, markierbare Zellen, Strg+C mit Kopfzeile.
    /// </summary>
    private void ContextMenuAndCtrlC()
    {
        var list = TagList();
        list.ContextMenu!.Open(list);
        CheckRunner.Pump();
        var copyItems = list.ContextMenu.Items.OfType<MenuItem>().Count(m => m.Command == Tags.CopyRowsCommand && m.Command.CanExecute(m.CommandParameter));
        list.ContextMenu.Close();
        CheckRunner.Pump();
        Check("Tag-Vorschau ist PreviewListBox mit Kontextmenü (4 Kopiereinträge, aktiv)", list is PreviewListBox && copyItems == 4, copyItems.ToString());
        var cells = list.GetVisualDescendants().OfType<SelectableTextBlock>().Count();
        Check("Zellen markierbar (SelectableTextBlock)", cells > 10, cells.ToString());
        _clipboard.Text = string.Empty;
        list.ContainerFromIndex(1)!.Focus();
        CheckRunner.Pump();
        _window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);
        CheckRunner.Pump();
        Check("Strg+C: Zeilen mit Kopfzeile", _clipboard.Text.StartsWith("Datei\tTrack\tTitel", StringComparison.Ordinal) && _clipboard.Text.Split('\n').Length == 4,
            _clipboard.Text.Split('\n')[0]);
    }

    /// <summary>
    /// Checkboxen in der Tag-Vorschau (E150, E153): kleines Kästchen, niedrige Zeilen, gesperrt bei Nicht-Audiodateien;
    /// Checkbox wählt aus, Zeilenklick markiert nur zum Kopieren; Kopf-Checkbox wählt alle bzw. keine.
    /// </summary>
    private void CheckboxSelection()
    {
        var list = TagList();
        var audio = AudioRowIndexes();
        var box = list.ContainerFromIndex(audio[0])!.GetVisualDescendants().OfType<CheckBox>().First(c => c.Classes.Contains("fileCheck"));
        var square = box.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "NormalRectangle");
        Check("Tag-Zeile hat Checkbox, Kästchen 14 px", square is not null && Math.Abs(square.Bounds.Width - 14) < 0.1, $"{square?.Bounds.Width}");
        Check("Tag-Zeilen niedrig (≤ 24 px)", MaxRowHeight(list) <= 24, MaxRowHeight(list).ToString("0.0"));
        var pdf = list.ContainerFromIndex(Tags.Rows.ToList().FindIndex(r => !r.IsAudio))!.GetVisualDescendants().OfType<CheckBox>().First();
        Check("Nicht-Audiodatei: Checkbox gesperrt", !pdf.IsEffectivelyEnabled);
        foreach (var row in Tags.Rows)
        {
            row.IsMarked = false;
        }

        Tags.AreAllSelected = false;
        CheckRunner.Pump();
        Check("Kopf-Checkbox aus: keine Datei ausgewählt", Tags.SelectedFiles.Count == 0 && Tags.AreAllSelected == false);
        ClickAt(box, box.Bounds.Width / 2);
        Check("Checkbox-Klick wählt Datei aus, markiert die Zeile nicht", Tags.SelectedFiles.Count == 1 && !Tags.Rows[audio[0]].IsMarked && Tags.AreAllSelected == null,
            $"{Tags.SelectedFiles.Count} marked={Tags.Rows[audio[0]].IsMarked}");
        ClickAt((Control)list.ContainerFromIndex(audio[1])!, 300);
        Check("Zeilen-Klick markiert, Auswahl bleibt", Tags.Rows[audio[1]].IsMarked && Tags.SelectedFiles.Count == 1, $"{Tags.Rows[audio[1]].IsMarked} {Tags.SelectedFiles.Count}");
        Tags.CopyRowsCommand.Execute(TagCopyColumns.FileNames);
        CheckRunner.Pump();
        Check("Kopieren nimmt markierte Zeilen, nicht ausgewählte", _clipboard.Text.Trim() == Tags.Rows[audio[1]].FileName, _clipboard.Text);
        Tags.AreAllSelected = true;
        CheckRunner.Pump();
        Check("Kopf-Checkbox an: alle Audiodateien", Tags.SelectedFiles.Count == 3 && Tags.AreAllSelected == true);
    }

    /// <summary>
    /// Positionen der Audiodateien in der Tag-Vorschau.
    /// </summary>
    /// <returns>Die Positionen.</returns>
    private List<int> AudioRowIndexes() => Tags.Rows.Select((row, index) => (row, index)).Where(x => x.row.IsAudio).Select(x => x.index).ToList();

    /// <summary>
    /// Horizontales Scrollen der Tag-Vorschau (E151): breite Spalte scrollt, Kopf läuft bündig mit und bleibt treffbar.
    /// </summary>
    private void HorizontalScrolling()
    {
        var list = TagList();
        var scroll = list.GetVisualDescendants().OfType<ScrollViewer>().First();
        var header = TagHeader();
        Tags.Columns.SetWidth("file", 900);
        CheckRunner.Pump();
        Check("Breite Spalte: Liste scrollt horizontal", scroll.Extent.Width > scroll.Viewport.Width + 100, $"{scroll.Extent.Width:0}/{scroll.Viewport.Width:0}");
        scroll.Offset = new Vector(400, 0);
        CheckRunner.Pump();
        var row = list.ContainerFromIndex(AudioRowIndexes()[0])!.GetVisualDescendants().OfType<ColumnRowPanel>().First();
        Check("Kopf scrollt mit: Spalten bündig", Math.Abs(CellX(row, "title") - CellX(header, "title")) < 0.6 && Math.Abs(CellX(row, "lyrics") - CellX(header, "lyrics")) < 0.6,
            $"{CellX(row, "title"):0}/{CellX(header, "title"):0}");
        var lyrics = header.Children.First(child => ColumnRowPanel.GetKey(child) == "lyrics");
        var hit = _window.GetVisualsAt(lyrics.TranslatePoint(new Point(10, lyrics.Bounds.Height / 2), _window)!.Value)
            .FirstOrDefault(v => v is IInputElement { IsHitTestVisible: true });
        Check("Gescrollter Kopf jenseits der Breite treffbar", hit is not null && (hit == header || hit.GetVisualAncestors().Contains(header)) && lyrics.Bounds.X > header.Bounds.Width,
            $"{hit?.GetType().Name} x={lyrics.Bounds.X:0} w={header.Bounds.Width:0}");
        Shot("tags-17-hscroll.png");
        Tags.Columns.Reset();
        CheckRunner.Pump();
        Check("Standard-Spalten: kein horizontaler Überstand", scroll.Extent.Width <= scroll.Viewport.Width + 0.5, $"{scroll.Extent.Width:0}/{scroll.Viewport.Width:0}");
    }

    /// <summary>
    /// Rename-Liste: niedrige Zeilen, horizontales Scrollen mit mitlaufendem Kopf.
    /// </summary>
    private void RenameScrolling()
    {
        _vm.IsTagArea = false;
        CheckRunner.Pump();
        var list = _window.GetVisualDescendants().OfType<ListBox>().First(l => l.Name == "PreviewList" && l.IsEffectivelyVisible && l.Classes.Contains("preview"));
        Check("Rename-Zeilen niedrig (≤ 24 px)", MaxRowHeight(list) <= 24, MaxRowHeight(list).ToString("0.0"));
        var scroll = list.GetVisualDescendants().OfType<ScrollViewer>().First();
        _vm.RenameColumns.SetWidth("original", 1400);
        CheckRunner.Pump();
        Check("Rename: breite Spalte scrollt horizontal", scroll.Extent.Width > scroll.Viewport.Width + 100, $"{scroll.Extent.Width:0}/{scroll.Viewport.Width:0}");
        scroll.Offset = new Vector(300, 0);
        CheckRunner.Pump();
        var row = list.ContainerFromIndex(0)!.GetVisualDescendants().OfType<ColumnRowPanel>().First();
        var header = RenameHeader();
        Check("Rename: Kopf scrollt mit", Math.Abs(CellX(row, "new") - CellX(header, "new")) < 0.6, $"{CellX(row, "new"):0}/{CellX(header, "new"):0}");
        Shot("tags-18-rename-hscroll.png");
        _vm.RenameColumns.Reset();
        _vm.IsTagArea = true;
        CheckRunner.Pump();
    }
}
