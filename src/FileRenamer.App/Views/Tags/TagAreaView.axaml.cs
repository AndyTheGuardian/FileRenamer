using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using FileRenamer.App.ViewModels.Tags;
using FileRenamer.Core.Tagging;

namespace FileRenamer.App.Views.Tags;

/// <summary>
/// Bereich „Tags“ (Mockup v13): Funktionen und Optionen oben, vorgemerkte Änderungen und Tag-Vorschau unten.
/// Das Kopieren aus der Tag-Vorschau übernimmt <see cref="PreviewCopyController"/>; Strg+C ohne markierten Text
/// kopiert die markierten Zeilen samt Kopfzeile im gespeicherten Stand (F67).
/// </summary>
public sealed partial class TagAreaView : UserControl
{
    /// <summary>
    /// Lädt die Oberfläche und verbindet das Kopieren sowie das Mitschieben der Spaltenköpfe.
    /// </summary>
    public TagAreaView()
    {
        InitializeComponent();
        _ = new PreviewCopyController(
            PreviewList, CopySelectedTextItem, () => (DataContext as TagAreaViewModel)?.CopyRowsCommand, TagCopyColumns.Rows);
        _ = new HeaderScrollSync(PreviewList, TagHeader);
        PendingList.AddHandler(InputElement.TappedEvent, OnPendingTapped);
        // Ein schneller zweiter Klick zählt als Doppelklick und löst kein Tapped aus.
        PendingList.AddHandler(InputElement.DoubleTappedEvent, OnPendingTapped);
    }

    /// <summary>
    /// Ein Klick auf eine vorgemerkte Änderung markiert ihre Dateien – auch ein wiederholter Klick, der die Auswahl der
    /// Liste nicht ändert. Klicks auf Checkbox und ✕ bleiben bei ihrer eigenen Funktion.
    /// </summary>
    /// <param name="sender">Die Liste.</param>
    /// <param name="e">Das Ereignis.</param>
    private void OnPendingTapped(object? sender, TappedEventArgs e)
    {
        var source = e.Source as Visual;
        if (source?.FindAncestorOfType<Button>(includeSelf: true) is not null
            || source?.FindAncestorOfType<CheckBox>(includeSelf: true) is not null
            || source?.FindAncestorOfType<ListBoxItem>(includeSelf: true)?.DataContext is not PendingChangeViewModel item
            || DataContext is not TagAreaViewModel area)
        {
            return;
        }

        area.ActivatePendingCommand.Execute(item);
    }
}
