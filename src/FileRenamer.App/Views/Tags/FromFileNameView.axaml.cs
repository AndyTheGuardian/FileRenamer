using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FileRenamer.App.ViewModels.Tags;

namespace FileRenamer.App.Views.Tags;

/// <summary>
/// Editor „Tags aus Dateinamen“. Der Code-Behind fügt Platzhalter an der Cursorposition ein und übernimmt
/// Beispielvorlagen (reine Oberflächenmechanik).
/// </summary>
public sealed partial class FromFileNameView : UserControl
{
    /// <summary>
    /// Lädt die Oberfläche.
    /// </summary>
    public FromFileNameView() => InitializeComponent();

    /// <summary>
    /// Fügt den Platzhalter an der Cursorposition ein bzw. ersetzt die Auswahl.
    /// </summary>
    /// <param name="sender">Die Platzhalter-Schaltfläche.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnPlaceholderClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Content: string token })
        {
            return;
        }

        var text = TemplateBox.Text ?? string.Empty;
        var start = Math.Clamp(Math.Min(TemplateBox.SelectionStart, TemplateBox.SelectionEnd), 0, text.Length);
        var end = Math.Clamp(Math.Max(TemplateBox.SelectionStart, TemplateBox.SelectionEnd), 0, text.Length);
        TemplateBox.Text = text[..start] + token + text[end..];
        TemplateBox.CaretIndex = start + token.Length;
        TemplateBox.Focus();
    }

    /// <summary>
    /// Übernimmt eine Beispielvorlage und setzt die Auswahl zurück, damit sie erneut gewählt werden kann.
    /// </summary>
    /// <param name="sender">Die Vorlagenauswahl.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnPresetSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedItem: string preset } comboBox && DataContext is FromFileNameEditorViewModel editor)
        {
            editor.ApplyPresetCommand.Execute(preset);
            Dispatcher.UIThread.Post(() => comboBox.SelectedItem = null);
        }
    }
}
