namespace FileRenamer.App.ViewModels;

/// <summary>
/// Teil des Hauptfenster-ViewModels: Auswahl der Dateien per Checkbox (E52). Abgewählte Dateien
/// werden über ihren Pfad gemerkt; neu hinzugefügte Dateien sind daher automatisch ausgewählt.
/// </summary>
public sealed partial class MainWindowViewModel
{
    private readonly HashSet<string> _deselectedPaths = new(StringComparer.Ordinal);
    private bool? _areAllSelected = true;

    /// <summary>
    /// Zustand der Checkbox im Spaltenkopf: alle (<c>true</c>), keine (<c>false</c>) oder teilweise (<c>null</c>).
    /// Setzen auf <c>true</c>/<c>false</c> wählt alle Dateien aus bzw. ab.
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

            _deselectedPaths.Clear();
            if (!selectAll)
            {
                _deselectedPaths.UnionWith(_files.Select(file => file.FullPath));
            }

            RefreshPreview();
        }
    }

    /// <summary>
    /// Übernimmt die Checkbox einer Dateizeile und aktualisiert die Vorschau.
    /// </summary>
    /// <param name="fullPath">Pfad der Datei.</param>
    /// <param name="isSelected">Neuer Zustand.</param>
    private void SetFileSelected(string fullPath, bool isSelected)
    {
        var changed = isSelected ? _deselectedPaths.Remove(fullPath) : _deselectedPaths.Add(fullPath);
        if (changed)
        {
            RefreshPreview();
        }
    }

    /// <summary>
    /// Leitet den Zustand der Kopf-Checkbox aus der Anzahl ausgewählter Dateien ab.
    /// </summary>
    /// <param name="selectedCount">Anzahl ausgewählter Dateien.</param>
    private void UpdateAllSelected(int selectedCount)
    {
        bool? state = selectedCount == _files.Count ? true : selectedCount == 0 ? false : null;
        SetProperty(ref _areAllSelected, state, nameof(AreAllSelected));
    }
}
