using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace FileRenamer.App.Services;

/// <summary>
/// Schreibt über die Zwischenablage des aktuellen Hauptfensters. Das Fenster wird bei jedem Aufruf neu
/// ermittelt, weil es nach einem Sprachwechsel neu aufgebaut wird (E92).
/// </summary>
/// <param name="ownerProvider">Liefert das aktuelle Hauptfenster oder <c>null</c>.</param>
public sealed class ClipboardService(Func<TopLevel?> ownerProvider) : IClipboardService
{
    /// <summary>
    /// Legt einen Text in die Zwischenablage; ohne Fenster geschieht nichts.
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <returns>Ein Task, der nach dem Schreiben endet.</returns>
    public async Task SetTextAsync(string text)
    {
        if (ownerProvider()?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
        }
    }
}
