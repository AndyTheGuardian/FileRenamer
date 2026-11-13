namespace FileRenamer.App.ViewModels.Tags;

/// <summary>
/// Teil des Bereichs „Tags“: Auswahl der Dateien per Checkbox und Markierung von Zeilen zum Kopieren (E150), wie in
/// der Vorschau des Umbenennens.
/// </summary>
public sealed partial class TagAreaViewModel
{
    private readonly TagRowCallbacks _rowCallbacks;
    private bool? _areAllSelected = false;

    /// <summary>
    /// Zustand der Checkbox im Spaltenkopf: alle (<c>true</c>), keine (<c>false</c>) oder einige Audiodateien
    /// ausgewählt (<c>null</c>). Setzen auf <c>true</c>/<c>false</c> wählt alle Audiodateien aus bzw. ab.
    /// </summary>
    public bool? AreAllSelected
    {
        get => _areAllSelected;
        set
        {
            if (value is not { } selectAll || value == _areAllSelected)
            {
                return;
            }

            _selectedPaths.Clear();
            if (selectAll)
            {
                _selectedPaths.UnionWith(_audioFiles);
            }

            foreach (var row in Rows)
            {
                row.IsSelected = selectAll;
            }

            OnSelectionChanged();
        }
    }

    /// <summary>
    /// Übernimmt die Checkbox einer Zeile.
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <param name="isSelected">Neuer Zustand.</param>
    private void SetSelected(string path, bool isSelected)
    {
        var changed = isSelected ? _selectedPaths.Add(path) : _selectedPaths.Remove(path);
        if (changed)
        {
            OnSelectionChanged();
        }
    }

    /// <summary>
    /// Übernimmt die Markierung einer Zeile (nur für das Kopieren).
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <param name="isMarked">Neuer Zustand.</param>
    private void SetMarked(string path, bool isMarked)
    {
        var changed = isMarked ? _markedPaths.Add(path) : _markedPaths.Remove(path);
        if (changed)
        {
            CopyRowsCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Leitet den Zustand der Kopf-Checkbox aus der Anzahl ausgewählter Audiodateien ab.
    /// </summary>
    /// <param name="selectedCount">Anzahl ausgewählter Audiodateien.</param>
    private void UpdateAllSelected(int selectedCount)
    {
        bool? state = selectedCount == 0 ? false : selectedCount == _audioFiles.Count ? true : null;
        SetProperty(ref _areAllSelected, state, nameof(AreAllSelected));
    }
}
