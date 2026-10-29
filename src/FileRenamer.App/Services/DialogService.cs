using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;

namespace FileRenamer.App.Services;

/// <summary>
/// Implementiert die Dialoge als schlichte modale Fenster und nutzt den Speicheranbieter
/// des Hauptfensters für die Datei- und Ordnerauswahl.
/// </summary>
/// <param name="owner">Das Hauptfenster.</param>
public sealed class DialogService(Window owner) : IDialogService
{
    /// <summary>
    /// Zeigt eine Sicherheitsabfrage mit „Abbrechen“ und Bestätigung.
    /// </summary>
    /// <param name="title">Fenstertitel.</param>
    /// <param name="message">Meldungstext.</param>
    /// <param name="confirmText">Beschriftung der Bestätigungsschaltfläche.</param>
    /// <returns><c>true</c>, wenn bestätigt wurde.</returns>
    public async Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        var dialog = CreateDialog(title, CreateMessage(message));
        AddButtons(dialog, ("Abbrechen", false), (confirmText, true));
        return await dialog.ShowDialog<bool>(owner);
    }

    /// <summary>
    /// Zeigt eine Meldung mit „OK“.
    /// </summary>
    /// <param name="title">Fenstertitel.</param>
    /// <param name="message">Meldungstext.</param>
    /// <returns>Ein Task, der nach dem Schließen endet.</returns>
    public async Task ShowMessageAsync(string title, string message)
    {
        var dialog = CreateDialog(title, CreateMessage(message));
        AddButtons(dialog, ("OK", true));
        await dialog.ShowDialog<bool>(owner);
    }

    /// <summary>
    /// Fragt einen Text ab; leere Eingaben gelten als Abbruch.
    /// </summary>
    /// <param name="title">Fenstertitel.</param>
    /// <param name="label">Beschriftung des Eingabefelds.</param>
    /// <param name="initialValue">Vorbelegung.</param>
    /// <param name="hint">Erläuterung unter dem Feld.</param>
    /// <returns>Der eingegebene Text oder <c>null</c>.</returns>
    public async Task<string?> PromptAsync(string title, string label, string initialValue, string hint)
    {
        var input = new TextBox { Text = initialValue, MinWidth = 280 };
        var content = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = label },
                input,
                new TextBlock { Text = hint, Classes = { "hint" } },
            },
        };
        var dialog = CreateDialog(title, content);
        AddButtons(dialog, ("Abbrechen", false), ("Speichern", true));
        dialog.Opened += (_, _) => input.Focus();

        var confirmed = await dialog.ShowDialog<bool>(owner);
        return confirmed && !string.IsNullOrWhiteSpace(input.Text) ? input.Text.Trim() : null;
    }

    /// <summary>
    /// Öffnet die Dateiauswahl (Mehrfachauswahl).
    /// </summary>
    /// <returns>Die lokalen Pfade.</returns>
    public async Task<IReadOnlyList<string>> PickFilesAsync()
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions { Title = "Dateien hinzufügen", AllowMultiple = true });
        return files.Select(file => file.TryGetLocalPath()).OfType<string>().ToList();
    }

    /// <summary>
    /// Öffnet die Ordnerauswahl.
    /// </summary>
    /// <returns>Der lokale Pfad oder <c>null</c>.</returns>
    public async Task<string?> PickFolderAsync()
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Ordner hinzufügen", AllowMultiple = false });
        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    /// <summary>
    /// Erzeugt das Grundgerüst eines modalen Dialogs.
    /// </summary>
    /// <param name="title">Fenstertitel.</param>
    /// <param name="content">Inhalt oberhalb der Schaltflächen.</param>
    /// <returns>Das Dialogfenster.</returns>
    private static Window CreateDialog(string title, Control content) => new()
    {
        Title = title,
        SizeToContent = SizeToContent.WidthAndHeight,
        CanResize = false,
        ShowInTaskbar = false,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        MinWidth = 380,
        MaxWidth = 560,
        Content = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 18, Children = { content } },
    };

    /// <summary>
    /// Erzeugt einen umbrechenden Meldungstext.
    /// </summary>
    /// <param name="message">Der Text.</param>
    /// <returns>Das Steuerelement.</returns>
    private static TextBlock CreateMessage(string message) =>
        new() { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap };

    /// <summary>
    /// Fügt rechtsbündige Schaltflächen hinzu; die letzte ist die Standardschaltfläche.
    /// </summary>
    /// <param name="dialog">Der Dialog.</param>
    /// <param name="buttons">Beschriftung und Rückgabewert je Schaltfläche.</param>
    private static void AddButtons(Window dialog, params (string Text, bool Result)[] buttons)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var (text, result) in buttons)
        {
            var button = new Button { Content = text, MinWidth = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
            button.Click += (_, _) => dialog.Close(result);
            panel.Children.Add(button);
        }

        var primary = (Button)panel.Children[^1];
        primary.IsDefault = true;
        primary.Classes.Add("accent");
        ((StackPanel)dialog.Content!).Children.Add(panel);
    }
}
