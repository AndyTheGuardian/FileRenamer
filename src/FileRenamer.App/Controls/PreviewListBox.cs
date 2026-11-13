using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace FileRenamer.App.Controls;

/// <summary>
/// ListBox der Vorschau, deren Zeilen markierbaren Text enthalten. Ein markierbarer TextBlock verarbeitet den
/// Mausklick selbst (Textmarkierung), sodass die Zeile normalerweise nicht markiert würde. Diese Klasse markiert
/// die Zeile in diesem Fall trotzdem – mit denselben Regeln für Strg und Umschalt wie bei einem Klick daneben.
/// </summary>
public sealed class PreviewListBox : ListBox
{
    private SelectableTextBlock? _pressedText;

    /// <summary>
    /// Meldet sich auch für bereits behandelte Mausereignisse an.
    /// </summary>
    public PreviewListBox()
    {
        AddHandler(PointerPressedEvent, OnPointerPressedOnText, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnPointerReleasedOnText, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>
    /// Verwendet die Stile einer normalen ListBox.
    /// </summary>
    protected override Type StyleKeyOverride => typeof(ListBox);

    /// <summary>
    /// Merkt sich einen Klick, den ein markierbarer TextBlock bereits behandelt hat. Die Zeile kann dann nicht
    /// selbst reagieren, weil sie den Klick nie erhalten hat.
    /// </summary>
    /// <param name="sender">Die ListBox.</param>
    /// <param name="e">Das Ereignis.</param>
    private void OnPointerPressedOnText(object? sender, PointerPressedEventArgs e)
    {
        _pressedText = e.Handled ? e.Source as SelectableTextBlock : null;
    }

    /// <summary>
    /// Markiert beim Loslassen die Zeile des zuvor angeklickten markierbaren TextBlocks (wie eine Zeile beim Loslassen
    /// der Maus). Umschalt erweitert den Bereich, Strg (macOS: Cmd) schaltet die Zeile um; ein Rechtsklick lässt eine
    /// bestehende Mehrfachmarkierung stehen, wenn die Zeile dazugehört.
    /// </summary>
    /// <param name="sender">Die ListBox.</param>
    /// <param name="e">Das Ereignis.</param>
    private void OnPointerReleasedOnText(object? sender, PointerReleasedEventArgs e)
    {
        if (_pressedText is not { } textBlock)
        {
            return;
        }

        _pressedText = null;
        var hotkeys = Application.Current?.PlatformSettings?.HotkeyConfiguration;
        var rangeModifier = e.KeyModifiers.HasFlag(hotkeys?.SelectionModifiers ?? KeyModifiers.Shift);
        var toggleModifier = e.KeyModifiers.HasFlag(hotkeys?.CommandModifiers ?? KeyModifiers.Control);
        var rightButton = e.InitialPressMouseButton == MouseButton.Right;
        // Bewusst die als veraltet markierte Variante: Mit ihr ist das Markieren per Klick, Strg und Umschalt geprüft
        // (E109). Die empfohlene Variante UpdateSelectionFromEvent wertet eigene Auslöseregeln aus und ist für diesen
        // nachgereichten Klick nicht geprüft.
#pragma warning disable CS0618
        UpdateSelectionFromEventSource(textBlock, true, rangeModifier, toggleModifier, rightButton);
#pragma warning restore CS0618
    }
}
