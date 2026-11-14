using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FileRenamer.App.ViewModels;
using FileRenamer.Core.Preview;

namespace FileRenamer.App.Views;

/// <summary>
/// Hauptfenster. Der Code-Behind enthält nur reine Oberflächenlogik:
/// Dateien per Drag &amp; Drop annehmen; das Umsortieren der Kette übernimmt <see cref="ChainDragController"/>.
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>
    /// Initialisiert das Fenster, registriert die Drag-&amp;-Drop-Handler und verbindet die Hilfsklassen der Oberfläche.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, OnFileDragOver);
        AddHandler(DragDrop.DropEvent, OnFileDrop);
        _ = new ChainDragController(ChainList, DragLayer, DragGhost, DropLine, (from, to) => ViewModel?.MoveChainItem(from, to));
        _ = new PreviewCopyController(PreviewList, CopySelectedTextItem, () => ViewModel?.CopyNamesCommand, NameColumns.New);
        _ = new HeaderScrollSync(PreviewList, RenameHeader);
        _ = new AreaLabelFitter(Toolbar, Toolbar.ColumnDefinitions[4], [AreaRenameLabel, AreaTagsLabel]);
    }

    /// <summary>
    /// Das ViewModel des Fensters.
    /// </summary>
    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    /// <summary>
    /// Zeigt beim Ziehen von Dateien das Kopieren-Symbol.
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnFileDragOver(object? sender, DragEventArgs e) =>
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;

    /// <summary>
    /// Übernimmt abgelegte Dateien und Ordner.
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnFileDrop(object? sender, DragEventArgs e)
    {
        var paths = e.DataTransfer.TryGetFiles()?.Select(item => item.TryGetLocalPath()).OfType<string>().ToList();
        if (paths is { Count: > 0 })
        {
            ViewModel?.AddPaths(paths);
        }
    }

    /// <summary>
    /// Lädt ein Profil, wenn es in der aufgeklappten Liste gewählt wurde. Bloßes Eintippen lädt nichts.
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnProfileDropDownClosed(object? sender, EventArgs e)
    {
        if (ProfileComboBox.SelectedItem is string name && ViewModel is { } viewModel && name != viewModel.CurrentProfileName)
        {
            viewModel.LoadProfileCommand.Execute(name);
        }
    }

    /// <summary>
    /// Öffnet die Profilauswahl der Toolbar (Menü „Profil laden…“).
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnLoadProfileClick(object? sender, RoutedEventArgs e)
    {
        ProfileComboBox.Focus();
        ProfileComboBox.IsDropDownOpen = true;
    }

    /// <summary>
    /// Beendet die Anwendung.
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnExitClick(object? sender, RoutedEventArgs e) => Close();
}
