using FileRenamer.Core.Profiles;

namespace FileRenamer.App.Services;

/// <summary>
/// Dialoge und Dateiauswahl, entkoppelt vom ViewModel (testbar, austauschbar).
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Zeigt eine Sicherheitsabfrage.
    /// </summary>
    /// <param name="title">Fenstertitel.</param>
    /// <param name="message">Meldungstext.</param>
    /// <param name="confirmText">Beschriftung der Bestätigungsschaltfläche.</param>
    /// <returns><c>true</c>, wenn bestätigt wurde.</returns>
    Task<bool> ConfirmAsync(string title, string message, string confirmText);

    /// <summary>
    /// Zeigt eine Meldung mit nur einer Schaltfläche.
    /// </summary>
    /// <param name="title">Fenstertitel.</param>
    /// <param name="message">Meldungstext.</param>
    /// <returns>Ein Task, der nach dem Schließen endet.</returns>
    Task ShowMessageAsync(string title, string message);

    /// <summary>
    /// Fragt einen Text ab.
    /// </summary>
    /// <param name="title">Fenstertitel.</param>
    /// <param name="label">Beschriftung des Eingabefelds.</param>
    /// <param name="initialValue">Vorbelegung.</param>
    /// <param name="hint">Erläuterung unter dem Feld; leer = keine.</param>
    /// <param name="confirmText">Beschriftung der Bestätigung; <c>null</c> = „Speichern“.</param>
    /// <returns>Der eingegebene Text oder <c>null</c> bei Abbruch.</returns>
    Task<string?> PromptAsync(string title, string label, string initialValue, string hint, string? confirmText = null);

    /// <summary>
    /// Bietet mehrere Möglichkeiten zur Wahl (z. B. „Überschreiben“ oder „Beide behalten“) und „Abbrechen“.
    /// </summary>
    /// <param name="title">Fenstertitel.</param>
    /// <param name="message">Die Frage.</param>
    /// <param name="choices">Die Möglichkeiten; die erste ist hervorgehoben.</param>
    /// <returns>Position der gewählten Möglichkeit oder <c>null</c> bei Abbruch.</returns>
    Task<int?> ChooseAsync(string title, string message, IReadOnlyList<string> choices);

    /// <summary>
    /// Öffnet die Auswahl zum Speichern einer Datei.
    /// </summary>
    /// <param name="title">Fenstertitel.</param>
    /// <param name="suggestedName">Vorgeschlagener Dateiname.</param>
    /// <param name="filterName">Name des Dateityps.</param>
    /// <param name="extension">Endung des Dateityps, z. B. <c>.colorprofiles.json</c>.</param>
    /// <returns>Der lokale Pfad oder <c>null</c>.</returns>
    Task<string?> PickSaveFileAsync(string title, string suggestedName, string filterName, string extension);

    /// <summary>
    /// Zeigt die Einstellungen (E155) modal; nach einem Sprachwechsel öffnen sie sich in der neuen Sprache wieder.
    /// </summary>
    /// <param name="viewModel">Das ViewModel der Einstellungen.</param>
    /// <returns><c>true</c> bei „Übernehmen“.</returns>
    Task<bool> ShowSettingsAsync(ViewModels.Settings.SettingsViewModel viewModel);

    /// <summary>
    /// Öffnet die Auswahl für eine oder mehrere Dateien.
    /// </summary>
    /// <returns>Die lokalen Pfade.</returns>
    Task<IReadOnlyList<string>> PickFilesAsync();

    /// <summary>
    /// Öffnet die Auswahl für eine einzelne Datei mit Filter.
    /// </summary>
    /// <param name="title">Titel des Dialogs.</param>
    /// <param name="filterName">Bezeichnung des Filters, z. B. „Bilder“.</param>
    /// <param name="extensions">Erlaubte Endungen mit Punkt, z. B. <c>.jpg</c>.</param>
    /// <returns>Der lokale Pfad oder <c>null</c>.</returns>
    Task<string?> PickFileAsync(string title, string filterName, IReadOnlyList<string> extensions);

    /// <summary>
    /// Öffnet die Auswahl für einen Ordner.
    /// </summary>
    /// <returns>Der lokale Pfad oder <c>null</c>.</returns>
    Task<string?> PickFolderAsync();
}

/// <summary>
/// Setzt das Erscheinungsbild der Anwendung.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Ob gerade das dunkle Design angezeigt wird (bei „System“ das des Betriebssystems).
    /// </summary>
    bool IsDark { get; }

    /// <summary>
    /// Wendet Design und die Farben beider Designs an (E155); bei einem Wechsel des Designs gelten dessen Farben.
    /// </summary>
    /// <param name="settings">Die Einstellungen.</param>
    void Apply(AppSettings settings);
}
