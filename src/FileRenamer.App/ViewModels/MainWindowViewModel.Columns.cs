using FileRenamer.Core.Preview;

namespace FileRenamer.App.ViewModels;

/// <summary>
/// Teil des Hauptfenster-ViewModels: Spalten der Rename-Liste und der Tag-Vorschau (E146). Änderungen gelten bis zum
/// Programmende, auch über einen Sprachwechsel (Neuaufbau des Fensters) hinweg; gespeicherte Anordnungen stehen
/// programmweit in den Einstellungen (E148).
/// </summary>
public sealed partial class MainWindowViewModel
{
    private const string RenameColumnsKey = "rename";
    private const string TagColumnsKey = "tags";

    /// <summary>
    /// Gespeicherte Anordnungen je Liste; Quelle für das Schreiben der Einstellungen, auch bevor alle Listen bestehen.
    /// </summary>
    private readonly Dictionary<string, IReadOnlyList<SavedColumn>> _savedColumns = new(StringComparer.Ordinal);

    /// <summary>
    /// Spalten „Aktueller Name“ und „Neuer Name“; beide teilen sich standardmäßig den Platz.
    /// </summary>
    public ColumnLayout RenameColumns { get; } = new([new("original", null, 80), new("new", null, 80)]);

    /// <summary>
    /// Übernimmt die gespeicherte Anordnung einer Liste und speichert die Einstellungen, sobald sie neu gespeichert wird.
    /// </summary>
    /// <param name="key">Schlüssel der Liste in den Einstellungen.</param>
    /// <param name="layout">Die Spalten der Liste.</param>
    private void ConnectColumns(string key, ColumnLayout layout)
    {
        layout.LoadSaved(_savedColumns.GetValueOrDefault(key));
        layout.SavedChanged += (_, _) =>
        {
            _savedColumns[key] = layout.Saved!;
            SaveSettings();
        };
    }
}
