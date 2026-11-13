using Avalonia.Controls;
using Avalonia.Interactivity;
using FileRenamer.App.ViewModels.Tags;

namespace FileRenamer.App.Views.Tags;

/// <summary>
/// Formular „Tags bearbeiten“. Der Code-Behind leitet nur die Genre-Liste weiter: Sie liegt in einem Popup, das den
/// Editor nicht als Vorfahren findet (wie bei den eigenen Vorlagen, E106).
/// </summary>
public sealed partial class EditTagsView : UserControl
{
    /// <summary>
    /// Lädt die Oberfläche.
    /// </summary>
    public EditTagsView() => InitializeComponent();

    /// <summary>
    /// Übernimmt ein gewähltes Genre in das Feld. Die Trennlinie ist nicht wählbar.
    /// </summary>
    /// <param name="sender">Die Genre-Auswahl.</param>
    /// <param name="e">Die Änderung.</param>
    private void OnGenreSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedItem: GenreOption option } && DataContext is EditTagsEditorViewModel editor)
        {
            editor.ApplyGenreCommand.Execute(option);
        }
    }

    /// <summary>
    /// Entfernt ein eigenes Genre über das ✕, ohne es zu übernehmen.
    /// </summary>
    /// <param name="sender">Die ✕-Schaltfläche.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnRemoveGenreClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: GenreOption option } && DataContext is EditTagsEditorViewModel editor)
        {
            editor.RemoveGenreCommand.Execute(option);
            e.Handled = true;
        }
    }
}
