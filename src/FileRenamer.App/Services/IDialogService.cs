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
    /// <param name="hint">Erläuterung unter dem Feld.</param>
    /// <returns>Der eingegebene Text oder <c>null</c> bei Abbruch.</returns>
    Task<string?> PromptAsync(string title, string label, string initialValue, string hint);

    /// <summary>
    /// Öffnet die Auswahl für eine oder mehrere Dateien.
    /// </summary>
    /// <returns>Die lokalen Pfade.</returns>
    Task<IReadOnlyList<string>> PickFilesAsync();

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
    /// Wendet die gewünschte Theme-Variante an.
    /// </summary>
    /// <param name="theme">Die Auswahl.</param>
    void Apply(ThemePreference theme);

    /// <summary>
    /// Wendet eine Akzentfarbe an.
    /// </summary>
    /// <param name="hex">Die Farbe (<c>#RRGGBB</c>) oder <c>null</c> für den Standard.</param>
    void ApplyAccent(string? hex);
}
