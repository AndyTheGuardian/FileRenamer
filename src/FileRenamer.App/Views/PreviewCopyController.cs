using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Windows.Input;

namespace FileRenamer.App.Views;

/// <summary>
/// Reine Oberflächenmechanik zum Kopieren aus einer Vorschauliste (Umbenennen und Tags):
/// <list type="bullet">
/// <item>Strg+C (macOS: Cmd+C) kopiert den mit der Maus markierten Text einer Zelle, sonst führt es den übergebenen
/// Kopierbefehl für die markierten Zeilen aus (Umbenennen: neue Namen; Tags: Zeilen im gespeicherten Stand).</item>
/// <item>Das Kontextmenü bietet „Markierten Text kopieren“ nur an, wenn beim Öffnen Text markiert war. Der Text wird
/// beim Öffnen gemerkt, weil die Markierung verloren geht, sobald das Menü den Fokus erhält.</item>
/// </list>
/// </summary>
public sealed class PreviewCopyController
{
    private readonly ListBox _list;
    private readonly MenuItem _copySelectedTextItem;
    private readonly Func<ICommand?> _copyCommand;
    private readonly object? _copyParameter;
    private string _textAtMenuOpening = string.Empty;

    /// <summary>
    /// Verbindet die Ereignisse der Vorschauliste und des Kontextmenüs.
    /// </summary>
    /// <param name="list">Die Vorschauliste.</param>
    /// <param name="copySelectedTextItem">Der Menüeintrag „Markierten Text kopieren“.</param>
    /// <param name="copyCommand">Liefert den Befehl, den Strg+C ohne markierten Text ausführt.</param>
    /// <param name="copyParameter">Parameter des Befehls (z. B. die zu kopierenden Spalten).</param>
    public PreviewCopyController(ListBox list, MenuItem copySelectedTextItem, Func<ICommand?> copyCommand, object? copyParameter)
    {
        _list = list;
        _copySelectedTextItem = copySelectedTextItem;
        _copyCommand = copyCommand;
        _copyParameter = copyParameter;

        _list.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        _list.AddHandler(Control.ContextRequestedEvent, OnContextRequested, RoutingStrategies.Tunnel);
        _copySelectedTextItem.Click += OnCopySelectedTextClick;
    }

    /// <summary>
    /// Kopiert bei Strg+C die markierten Zeilen, sofern kein Text markiert ist; markierten Text kopiert der
    /// TextBlock selbst.
    /// </summary>
    /// <param name="sender">Die Liste.</param>
    /// <param name="e">Das Ereignis.</param>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsCopyGesture(e) || FindSelectedText(e.Source).Length > 0)
        {
            return;
        }

        var command = _copyCommand();
        if (command?.CanExecute(_copyParameter) == true)
        {
            command.Execute(_copyParameter);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Merkt beim Öffnen des Kontextmenüs den markierten Text und blendet den passenden Eintrag ein oder aus.
    /// </summary>
    /// <param name="sender">Die Liste.</param>
    /// <param name="e">Das Ereignis.</param>
    private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        _textAtMenuOpening = FindSelectedText(e.Source);
        _copySelectedTextItem.IsVisible = _textAtMenuOpening.Length > 0;
    }

    /// <summary>
    /// Kopiert den beim Öffnen des Menüs markierten Text.
    /// </summary>
    /// <param name="sender">Der Menüeintrag.</param>
    /// <param name="e">Das Ereignis.</param>
    private async void OnCopySelectedTextClick(object? sender, RoutedEventArgs e)
    {
        if (_textAtMenuOpening.Length > 0 && TopLevel.GetTopLevel(_list)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(_textAtMenuOpening);
        }
    }

    /// <summary>
    /// Prüft, ob die Taste der Kopieren-Tastenkombination der Plattform entspricht.
    /// </summary>
    /// <param name="e">Das Tastaturereignis.</param>
    /// <returns><c>true</c> bei Strg+C bzw. Cmd+C.</returns>
    private static bool IsCopyGesture(KeyEventArgs e)
    {
        var gestures = Application.Current?.PlatformSettings?.HotkeyConfiguration.Copy;
        return gestures?.Any(gesture => gesture.Matches(e)) == true;
    }

    /// <summary>
    /// Liefert den markierten Text des markierbaren TextBlocks, von dem das Ereignis ausgeht.
    /// </summary>
    /// <param name="source">Quelle des Ereignisses.</param>
    /// <returns>Der markierte Text oder ein leerer Text.</returns>
    private static string FindSelectedText(object? source)
    {
        var textBlock = source as SelectableTextBlock ?? (source as Visual)?.FindAncestorOfType<SelectableTextBlock>();
        return textBlock?.SelectedText ?? string.Empty;
    }
}
