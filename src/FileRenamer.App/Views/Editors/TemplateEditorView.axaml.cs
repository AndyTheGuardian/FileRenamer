using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FileRenamer.App.ViewModels.Editors;

namespace FileRenamer.App.Views.Editors;

/// <summary>
/// Ansicht von „Name aus Vorlage“. Der Code-Behind fügt angeklickte Platzhalter an der Cursorposition
/// des zugehörigen Vorlagenfelds ein (reine Oberflächenmechanik, E66) und leitet das ✕ eigener Vorlagen weiter.
/// </summary>
public sealed partial class TemplateEditorView : UserControl
{
    /// <summary>
    /// Initialisiert die Ansicht.
    /// </summary>
    public TemplateEditorView() => InitializeComponent();

    /// <summary>
    /// Fügt den Platzhalter der Schaltfläche in das Vorlagenfeld des aktuellen Tabs ein.
    /// </summary>
    /// <param name="sender">Die Platzhalter-Schaltfläche.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnPlaceholderClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Content: string token } button ||
            button.FindAncestorOfType<ItemsControl>(includeSelf: false) is not { } groupList)
        {
            return;
        }

        var outerList = groupList.FindAncestorOfType<ItemsControl>(includeSelf: false);
        if (outerList?.Tag is TextBox target)
        {
            Insert(target, token);
        }
    }

    /// <summary>
    /// Übernimmt eine in der Vorlagenauswahl gewählte Vorlage und setzt die Auswahl danach zurück, damit wieder
    /// „Vorlage wählen…“ erscheint und dieselbe Vorlage erneut gewählt werden kann. Das Zurücksetzen wird
    /// nachgelagert, weil die ComboBox eine Änderung während ihres eigenen Auswahlereignisses nicht übernimmt.
    /// </summary>
    /// <param name="sender">Die Vorlagenauswahl.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnPresetSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedItem: TemplatePreset preset } comboBox && DataContext is TemplateEditorViewModel editor)
        {
            editor.ApplyPresetCommand.Execute(preset);
            Dispatcher.UIThread.Post(() => comboBox.SelectedItem = null);
        }
    }

    /// <summary>
    /// Entfernt eine eigene Vorlage über das ✕ in der Vorlagenauswahl (E106). Per Ereignis statt Bindung,
    /// weil die aufgeklappte Liste in einem eigenen Popup liegt und dort den Editor nicht als Vorfahren findet.
    /// </summary>
    /// <param name="sender">Die ✕-Schaltfläche.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnRemovePresetClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: TemplatePreset preset } && DataContext is TemplateEditorViewModel editor)
        {
            editor.RemoveTemplateCommand.Execute(preset);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Ersetzt die Auswahl bzw. fügt an der Cursorposition ein und setzt den Cursor dahinter.
    /// </summary>
    /// <param name="target">Das Vorlagenfeld.</param>
    /// <param name="token">Der Platzhalter.</param>
    private static void Insert(TextBox target, string token)
    {
        var text = target.Text ?? string.Empty;
        var start = Math.Clamp(Math.Min(target.SelectionStart, target.SelectionEnd), 0, text.Length);
        var end = Math.Clamp(Math.Max(target.SelectionStart, target.SelectionEnd), 0, text.Length);
        if (start == end)
        {
            start = end = Math.Clamp(target.CaretIndex, 0, text.Length);
        }

        target.Text = text[..start] + token + text[end..];
        target.CaretIndex = start + token.Length;
        target.Focus();
    }
}
