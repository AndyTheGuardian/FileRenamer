using Avalonia.Controls;

namespace FileRenamer.App.Views;

/// <summary>
/// Auswahl der Akzentfarbe (Inhalt des Flyouts am farbigen Kreis); die Logik liegt im ViewModel.
/// </summary>
public sealed partial class AccentPickerView : UserControl
{
    /// <summary>
    /// Initialisiert die Ansicht.
    /// </summary>
    public AccentPickerView() => InitializeComponent();
}
