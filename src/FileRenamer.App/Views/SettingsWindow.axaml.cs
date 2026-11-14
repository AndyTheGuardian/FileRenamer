using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FileRenamer.App.ViewModels.Settings;

namespace FileRenamer.App.Views;

/// <summary>
/// Fenster der Einstellungen (E155). Der Code-Behind enthält nur Oberflächenlogik: Schließen mit Ergebnis, Umgang mit
/// geleerten Zahlenfeldern und das erneute Öffnen nach einem Sprachwechsel an gleicher Stelle.
/// </summary>
public sealed partial class SettingsWindow : Window
{
    /// <summary>
    /// Lädt die Oberfläche und verbindet die Behandlung der Zahlenfelder.
    /// </summary>
    public SettingsWindow()
    {
        InitializeComponent();
        // Ein geleertes Zahlenfeld bleibt beim Tippen leer; verlässt man es, zeigt es wieder den gültigen Wert.
        AddHandler(InputElement.LostFocusEvent, OnFieldLostFocus, RoutingStrategies.Bubble);
        // Enter in einem Zahlenfeld übernimmt nur den Wert und schließt den Dialog nicht über „Übernehmen“.
        SliderGrid.AddHandler(KeyDownEvent, OnNumberFieldKeyDown, RoutingStrategies.Bubble);
    }

    /// <summary>
    /// Ob der Dialog wegen eines Sprachwechsels geschlossen wurde und neu geöffnet werden soll.
    /// </summary>
    public bool ReopenRequested { get; private set; }

    /// <summary>
    /// Größe beim Schließen (für das erneute Öffnen).
    /// </summary>
    public Size ClosedSize { get; private set; }

    /// <summary>
    /// Lage beim Schließen (für das erneute Öffnen).
    /// </summary>
    public PixelPoint ClosedPosition { get; private set; }

    /// <summary>
    /// Gibt einem neu geöffneten Fenster Größe und Lage dieses (geschlossenen) Fensters.
    /// </summary>
    /// <param name="window">Das neue Fenster.</param>
    public void PlaceLike(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.SizeToContent = SizeToContent.Manual;
        window.Width = ClosedSize.Width;
        window.Height = ClosedSize.Height;
        if (ClosedPosition != default)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Position = ClosedPosition;
        }

        window.Opened += (_, _) => window.Activate();
    }

    /// <summary>
    /// Merkt sich Größe und Lage beim Schließen.
    /// </summary>
    /// <param name="e">Ereignisdaten.</param>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        ClosedSize = ClientSize;
        ClosedPosition = Position;
    }

    /// <summary>
    /// Schließt den Dialog ohne Ergebnis, sobald eine andere Sprache angewandt wurde; er öffnet sich danach neu.
    /// </summary>
    /// <param name="e">Ereignisdaten.</param>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.LanguageApplied += (_, _) =>
            {
                ReopenRequested = true;
                Dispatcher.UIThread.Post(() => Close(null));
            };
        }
    }

    /// <summary>
    /// Verhindert, dass Enter in einem Zahlenfeld den Standardknopf auslöst.
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private static void OnNumberFieldKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && e.Source is Control source && (source as NumericUpDown ?? source.FindAncestorOfType<NumericUpDown>()) is not null)
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Zeigt in einem geleerten Zahlenfeld beim Verlassen wieder den gültigen Wert.
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private static void OnFieldLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Control source && (source as NumericUpDown ?? source.FindAncestorOfType<NumericUpDown>()) is { Value: null } field)
        {
            BindingOperations.GetBindingExpressionBase(field, NumericUpDown.ValueProperty)?.UpdateTarget();
        }
    }

    /// <summary>
    /// „Übernehmen“.
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnSave(object? sender, RoutedEventArgs e) => Close(true);

    /// <summary>
    /// „Abbrechen“.
    /// </summary>
    /// <param name="sender">Quelle.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
