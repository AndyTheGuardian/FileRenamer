using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FileRenamer.App.ViewModels;

namespace FileRenamer.App.Views;

/// <summary>
/// Hauptfenster. Der Code-Behind enthält nur reine Oberflächenlogik:
/// Dateien per Drag &amp; Drop annehmen und Ketteneinträge per Ziehen umsortieren.
/// </summary>
public sealed partial class MainWindow : Window
{
    private int _dragIndex = -1;

    /// <summary>
    /// Initialisiert das Fenster und registriert die Drag-&amp;-Drop-Handler.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, OnFileDragOver);
        AddHandler(DragDrop.DropEvent, OnFileDrop);
        ChainList.AddHandler(PointerPressedEvent, OnChainPointerPressed, RoutingStrategies.Tunnel);
        ChainList.AddHandler(PointerReleasedEvent, OnChainPointerReleased, RoutingStrategies.Tunnel);
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
    /// Beginnt das Verschieben, wenn der Griff eines Ketteneintrags gedrückt wird.
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnChainPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not TextBlock { DataContext: ChainItemViewModel item } handle || !handle.Classes.Contains("dragHandle"))
        {
            return;
        }

        _dragIndex = ViewModel?.Chain.IndexOf(item) ?? -1;
        e.Pointer.Capture(ChainList);
        e.Handled = true;
    }

    /// <summary>
    /// Schließt das Verschieben ab und setzt den Eintrag an die Position unter dem Mauszeiger.
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnChainPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragIndex < 0)
        {
            return;
        }

        ViewModel?.MoveChainItem(_dragIndex, FindChainIndexAt(e.GetPosition(ChainList).Y));
        _dragIndex = -1;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    /// <summary>
    /// Ermittelt den Index des Ketteneintrags an einer vertikalen Position (begrenzt auf erste/letzte Position).
    /// </summary>
    /// <param name="y">Y-Koordinate relativ zur Liste.</param>
    /// <returns>Der Index.</returns>
    private int FindChainIndexAt(double y)
    {
        var count = ChainList.ItemCount;
        for (var index = 0; index < count; index++)
        {
            if (ChainList.ContainerFromIndex(index) is not { } container ||
                container.TranslatePoint(new Point(0, 0), ChainList) is not { } origin)
            {
                continue;
            }

            if (y < origin.Y + container.Bounds.Height)
            {
                return index;
            }
        }

        return Math.Max(count - 1, 0);
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
