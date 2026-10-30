using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FileRenamer.App.ViewModels;

namespace FileRenamer.App.Views;

/// <summary>
/// Hauptfenster. Der Code-Behind enthält nur reine Oberflächenlogik:
/// Dateien per Drag &amp; Drop annehmen; das Umsortieren der Kette übernimmt <see cref="ChainDragController"/>.
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>
    /// Initialisiert das Fenster und registriert die Drag-&amp;-Drop-Handler.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, OnFileDragOver);
        AddHandler(DragDrop.DropEvent, OnFileDrop);
        _ = new ChainDragController(ChainList, DragLayer, DragGhost, DropLine, (from, to) => ViewModel?.MoveChainItem(from, to));
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
    /// Öffnet die Farbwahl am farbigen Kreis (Menü „Ansicht → Akzentfarbe…“).
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnAccentMenuClick(object? sender, RoutedEventArgs e) => AccentButton.Flyout?.ShowAt(AccentButton);

    /// <summary>
    /// Beendet die Anwendung.
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnExitClick(object? sender, RoutedEventArgs e) => Close();
}
