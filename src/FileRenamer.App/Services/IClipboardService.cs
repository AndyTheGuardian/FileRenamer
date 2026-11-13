namespace FileRenamer.App.Services;

/// <summary>
/// Zugriff auf die Zwischenablage, entkoppelt vom ViewModel (testbar, austauschbar).
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Legt einen Text in die Zwischenablage.
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <returns>Ein Task, der nach dem Schreiben endet.</returns>
    Task SetTextAsync(string text);
}
