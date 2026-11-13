using CommunityToolkit.Mvvm.Input;
using FileRenamer.Core.Preview;

namespace FileRenamer.App.ViewModels;

/// <summary>
/// Teil des Hauptfenster-ViewModels: Markieren von Vorschauzeilen und Kopieren ihrer Namen in die Zwischenablage.
/// Die Markierung dient nur dem Kopieren und ist unabhängig von der Dateiauswahl per Checkbox (E52).
/// Markierte Dateien werden über ihren Pfad gemerkt, damit die Markierung das Neuberechnen der Vorschau übersteht.
/// </summary>
public sealed partial class MainWindowViewModel
{
    private readonly HashSet<string> _markedPaths = new(StringComparer.Ordinal);

    /// <summary>
    /// Ob mindestens eine Vorschauzeile markiert ist.
    /// </summary>
    public bool HasMarkedRows => _markedPaths.Count > 0;

    /// <summary>
    /// Kopiert die Namen der markierten Zeilen in Anzeigereihenfolge und so, wie sie angezeigt werden
    /// (bei ignorierter Erweiterung also ohne Endung).
    /// </summary>
    /// <param name="columns">Die zu kopierenden Spalten.</param>
    /// <returns>Ein Task, der nach dem Kopieren endet.</returns>
    [RelayCommand(CanExecute = nameof(HasMarkedRows))]
    private async Task CopyNamesAsync(NameColumns columns)
    {
        var pairs = PreviewRows
            .Where(row => row.IsMarked)
            .Select(row => new NamePair(row.OriginalName, row.NewName));
        await _clipboard.SetTextAsync(NameListFormatter.Format(pairs, columns));
    }

    /// <summary>
    /// Übernimmt die Markierung einer Zeile.
    /// </summary>
    /// <param name="fullPath">Pfad der Datei.</param>
    /// <param name="isMarked">Neuer Zustand.</param>
    private void SetRowMarked(string fullPath, bool isMarked)
    {
        var changed = isMarked ? _markedPaths.Add(fullPath) : _markedPaths.Remove(fullPath);
        if (changed)
        {
            NotifyMarkedRowsChanged();
        }
    }

    /// <summary>
    /// Vergisst Markierungen von Dateien, die nicht mehr in der Liste stehen (entfernt oder umbenannt).
    /// </summary>
    private void PruneMarkedRows()
    {
        var current = _files.Select(file => file.FullPath).ToHashSet(StringComparer.Ordinal);
        var removed = _markedPaths.RemoveWhere(path => !current.Contains(path));
        if (removed > 0)
        {
            NotifyMarkedRowsChanged();
        }
    }

    /// <summary>
    /// Meldet eine geänderte Markierung an Oberfläche und Kopierbefehl.
    /// </summary>
    private void NotifyMarkedRowsChanged()
    {
        OnPropertyChanged(nameof(HasMarkedRows));
        CopyNamesCommand.NotifyCanExecuteChanged();
    }
}
