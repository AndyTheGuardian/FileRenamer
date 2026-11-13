using CommunityToolkit.Mvvm.ComponentModel;
using FileRenamer.App.Localization;
using FileRenamer.Core.Tagging;

namespace FileRenamer.App.ViewModels.Tags;

/// <summary>
/// Ein Feld im Formular „Tags bearbeiten“. Zeigt den gemeinsamen Wert der markierten Dateien oder – bei
/// unterschiedlichen Werten – leer mit Platzhalter „&lt;verschiedene&gt;“. Eine Eingabe macht das Feld „geändert“;
/// nur geänderte Felder werden vorgemerkt.
/// </summary>
/// <param name="key">Schlüssel des Felds.</param>
/// <param name="isRemovable">Ob das Feld per ✕ ausgeblendet werden kann (hinzugefügte Felder).</param>
public sealed partial class TagFieldViewModel(string key, bool isRemovable) : ObservableObject
{
    private bool _isLoading;
    private string _loadedValue = string.Empty;
    private bool _loadedVaries;

    /// <summary>
    /// Schlüssel des Felds.
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    /// Bezeichnung.
    /// </summary>
    public string Label => TagFieldNames.Get(Key);

    /// <summary>
    /// Ob das Feld ausgeblendet werden kann.
    /// </summary>
    public bool IsRemovable { get; } = isRemovable;

    /// <summary>
    /// Ob nur ganze Zahlen erlaubt sind.
    /// </summary>
    public bool IsNumeric => TagFields.Numeric.Contains(Key);

    /// <summary>
    /// Der angezeigte bzw. eingegebene Wert.
    /// </summary>
    [ObservableProperty]
    public partial string Value { get; set; } = string.Empty;

    /// <summary>
    /// Ob die markierten Dateien unterschiedliche Werte haben (und das Feld nicht geändert ist).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Placeholder))]
    public partial bool Varies { get; set; }

    /// <summary>
    /// Ob das Feld geändert wurde.
    /// </summary>
    [ObservableProperty]
    public partial bool IsDirty { get; set; }

    /// <summary>
    /// Platzhaltertext bei unterschiedlichen Werten.
    /// </summary>
    public string? Placeholder => Varies ? Loc.T("Tags.Varies") : null;

    /// <summary>
    /// Ob der Wert gültig ist (Zahlenfelder: leer oder ganze Zahl ≥ 0).
    /// </summary>
    public bool IsValid => !IsNumeric || Value.Trim().Length == 0 || uint.TryParse(Value.Trim(), out _);

    /// <summary>
    /// Wird nach einer Eingabe ausgelöst.
    /// </summary>
    public event EventHandler? Edited;

    /// <summary>
    /// Übernimmt die Werte der markierten Dateien; ein geändertes Feld behält seine Eingabe.
    /// </summary>
    /// <param name="values">Werte der markierten Dateien.</param>
    public void Load(IReadOnlyList<string> values)
    {
        var distinct = values.Distinct(StringComparer.Ordinal).ToList();
        _loadedVaries = distinct.Count > 1;
        _loadedValue = distinct.Count == 1 ? distinct[0] : string.Empty;
        if (!IsDirty)
        {
            Restore();
        }
    }

    /// <summary>
    /// Nimmt eine Eingabe zurück (↺).
    /// </summary>
    public void Revert()
    {
        IsDirty = false;
        Restore();
        Edited?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Markiert das Feld bei einer Eingabe als geändert.
    /// </summary>
    /// <param name="value">Der neue Wert.</param>
    partial void OnValueChanged(string value)
    {
        if (_isLoading)
        {
            return;
        }

        IsDirty = true;
        Varies = false;
        OnPropertyChanged(nameof(IsValid));
        Edited?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Stellt den geladenen Wert wieder her.
    /// </summary>
    private void Restore()
    {
        _isLoading = true;
        Value = _loadedValue;
        Varies = _loadedVaries;
        _isLoading = false;
        OnPropertyChanged(nameof(IsValid));
    }
}
