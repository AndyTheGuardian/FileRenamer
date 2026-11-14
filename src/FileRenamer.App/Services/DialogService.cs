using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using FileRenamer.App.Localization;

namespace FileRenamer.App.Services;

/// <summary>
/// Implementiert die Dialoge als modale Fenster, die sich mit eigener Fläche und Rahmen farblich vom Hauptfenster
/// abheben; solange ein Dialog offen ist, wird das Hauptfenster abgedunkelt (V22, E149). Für die Datei- und
/// Ordnerauswahl wird der Speicheranbieter des Hauptfensters genutzt.
/// </summary>
/// <param name="owner">Das Hauptfenster.</param>
public sealed class DialogService(Window owner) : IDialogService
{
    private int _openDialogs;

    /// <summary>
    /// Das Fenster, über dem die Dialoge erscheinen; wird nach einem Sprachwechsel auf das neu aufgebaute
    /// Hauptfenster gesetzt.
    /// </summary>
    public Window Owner { get; set; } = owner;

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
        AddButtons(dialog, (Loc.T("Dialog.Cancel"), false), (confirmText, true));
        return await ShowAsync(dialog);
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
        AddButtons(dialog, (Loc.T("Dialog.Ok"), true));
        await ShowAsync(dialog);
    }

    /// <summary>
    /// Fragt einen Text ab; leere Eingaben gelten als Abbruch.
    /// </summary>
    /// <param name="title">Fenstertitel.</param>
    /// <param name="label">Beschriftung des Eingabefelds.</param>
    /// <param name="initialValue">Vorbelegung.</param>
    /// <param name="hint">Erläuterung unter dem Feld; leer = keine.</param>
    /// <param name="confirmText">Beschriftung der Bestätigung; <c>null</c> = „Speichern“.</param>
    /// <returns>Der eingegebene Text oder <c>null</c>.</returns>
    public async Task<string?> PromptAsync(string title, string label, string initialValue, string hint, string? confirmText = null)
    {
        var input = new TextBox { Text = initialValue, MinWidth = 280 };
        var content = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = label },
                input,
                new TextBlock { Text = hint, Classes = { "hint" }, IsVisible = hint.Length > 0 },
            },
        };
        var dialog = CreateDialog(title, content);
        AddButtons(dialog, (Loc.T("Dialog.Cancel"), false), (confirmText ?? Loc.T("Dialog.Save"), true));
        dialog.Opened += (_, _) => input.Focus();

        var confirmed = await ShowAsync(dialog);
        return confirmed && !string.IsNullOrWhiteSpace(input.Text) ? input.Text.Trim() : null;
    }

    /// <summary>
    /// Bietet mehrere Möglichkeiten und „Abbrechen“ zur Wahl; die erste Möglichkeit ist hervorgehoben.
    /// </summary>
    /// <param name="title">Fenstertitel.</param>
    /// <param name="message">Die Frage.</param>
    /// <param name="choices">Die Möglichkeiten.</param>
    /// <returns>Position der Wahl oder <c>null</c> bei Abbruch.</returns>
    public async Task<int?> ChooseAsync(string title, string message, IReadOnlyList<string> choices)
    {
        var dialog = CreateDialog(title, CreateMessage(message));
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        panel.Children.Add(ChoiceButton(dialog, Loc.T("Dialog.Cancel"), null));
        for (var index = choices.Count - 1; index >= 0; index--)
        {
            panel.Children.Add(ChoiceButton(dialog, choices[index], index));
        }

        var primary = (Button)panel.Children[^1];
        primary.IsDefault = true;
        primary.Classes.Add("accent");
        ((StackPanel)((Border)dialog.Content!).Child!).Children.Add(panel);
        return await WithShadeAsync(() => dialog.ShowDialog<int?>(Owner));
    }

    /// <summary>
    /// Öffnet die Auswahl zum Speichern einer Datei; überschreiben fragt nach.
    /// </summary>
    /// <param name="title">Fenstertitel.</param>
    /// <param name="suggestedName">Vorgeschlagener Dateiname.</param>
    /// <param name="filterName">Name des Dateityps.</param>
    /// <param name="extension">Endung des Dateityps.</param>
    /// <returns>Der lokale Pfad oder <c>null</c>.</returns>
    public async Task<string?> PickSaveFileAsync(string title, string suggestedName, string filterName, string extension)
    {
        var filter = new FilePickerFileType(filterName) { Patterns = ["*" + extension] };
        var file = await WithShadeAsync(() => Owner.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions { Title = title, SuggestedFileName = suggestedName, FileTypeChoices = [filter], ShowOverwritePrompt = true }));
        return file?.TryGetLocalPath();
    }

    /// <summary>
    /// Zeigt die Einstellungen modal (E155). Wechselt der Anwender die Sprache, schließt sich der Dialog ohne Ergebnis;
    /// nach dem Neuaufbau des Hauptfensters öffnet er sich mit demselben ViewModel, gleicher Größe und Lage wieder.
    /// </summary>
    /// <param name="viewModel">Das ViewModel der Einstellungen.</param>
    /// <returns><c>true</c> bei „Übernehmen“.</returns>
    public async Task<bool> ShowSettingsAsync(ViewModels.Settings.SettingsViewModel viewModel)
    {
        Views.SettingsWindow? previous = null;
        while (true)
        {
            var window = new Views.SettingsWindow { DataContext = viewModel };
            previous?.PlaceLike(window);
            var result = await WithShadeAsync(() => window.ShowDialog<bool?>(Owner));
            if (!window.ReopenRequested)
            {
                return result == true;
            }

            previous = window;
            // Erst den eingereihten Neuaufbau des Hauptfensters abwarten, dann über dem neuen Fenster öffnen
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.Background);
        }
    }

    /// <summary>
    /// Öffnet die Dateiauswahl (Mehrfachauswahl).
    /// </summary>
    /// <returns>Die lokalen Pfade.</returns>
    public async Task<IReadOnlyList<string>> PickFilesAsync()
    {
        var files = await WithShadeAsync(() => Owner.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions { Title = Loc.T("Dialog.PickFiles"), AllowMultiple = true }));
        return files.Select(file => file.TryGetLocalPath()).OfType<string>().ToList();
    }

    /// <summary>
    /// Öffnet die Auswahl für eine einzelne Datei mit Filter.
    /// </summary>
    /// <param name="title">Titel des Dialogs.</param>
    /// <param name="filterName">Bezeichnung des Filters.</param>
    /// <param name="extensions">Erlaubte Endungen mit Punkt.</param>
    /// <returns>Der lokale Pfad oder <c>null</c>.</returns>
    public async Task<string?> PickFileAsync(string title, string filterName, IReadOnlyList<string> extensions)
    {
        var filter = new FilePickerFileType(filterName) { Patterns = extensions.Select(extension => "*" + extension).ToList() };
        var files = await WithShadeAsync(() => Owner.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions { Title = title, AllowMultiple = false, FileTypeFilter = [filter] }));
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    /// <summary>
    /// Öffnet die Ordnerauswahl.
    /// </summary>
    /// <returns>Der lokale Pfad oder <c>null</c>.</returns>
    public async Task<string?> PickFolderAsync()
    {
        var folders = await WithShadeAsync(() => Owner.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = Loc.T("Dialog.PickFolder"), AllowMultiple = false }));
        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    /// <summary>
    /// Zeigt einen eigenen Dialog modal und dunkelt das Hauptfenster solange ab.
    /// </summary>
    /// <param name="dialog">Der Dialog.</param>
    /// <returns>Das Ergebnis des Dialogs.</returns>
    private Task<bool> ShowAsync(Window dialog) => WithShadeAsync(() => dialog.ShowDialog<bool>(Owner));

    /// <summary>
    /// Dunkelt das Hauptfenster ab, solange eine modale Aktion läuft (V22, E149); verschachtelte Dialoge werden gezählt.
    /// </summary>
    /// <typeparam name="T">Ergebnistyp.</typeparam>
    /// <param name="action">Die modale Aktion.</param>
    /// <returns>Ihr Ergebnis.</returns>
    private async Task<T> WithShadeAsync<T>(Func<Task<T>> action)
    {
        var shade = Owner.FindControl<Control>("DialogShade");
        _openDialogs++;
        SetShade(shade);
        try
        {
            return await action();
        }
        finally
        {
            _openDialogs--;
            SetShade(shade);
        }
    }

    /// <summary>
    /// Zeigt die Abdunkelung, solange mindestens ein Dialog offen ist.
    /// </summary>
    /// <param name="shade">Die Abdunkelung oder <c>null</c>, wenn das Fenster keine hat.</param>
    private void SetShade(Control? shade)
    {
        if (shade is not null)
        {
            shade.IsVisible = _openDialogs > 0;
        }
    }

    /// <summary>
    /// Erzeugt einen Dialog mit eigener Fläche und Rahmen; Inhalt und Schaltflächen stehen untereinander.
    /// </summary>
    /// <param name="title">Titel des Dialogs (Titelleiste).</param>
    /// <param name="content">Inhalt.</param>
    /// <returns>Der Dialog.</returns>
    private static Window CreateDialog(string title, Control content) => new()
    {
        Title = title,
        SizeToContent = SizeToContent.WidthAndHeight,
        CanResize = false,
        ShowInTaskbar = false,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        MinWidth = 380,
        MaxWidth = 560,
        Background = Brush("DialogBackgroundBrush"),
        Content = new Border
        {
            BorderBrush = Brush("DialogBorderBrush"),
            BorderThickness = new Thickness(1),
            Child = new StackPanel { Margin = new Thickness(20), Spacing = 18, Children = { content } },
        },
    };

    /// <summary>
    /// Holt einen Pinsel aus den Ressourcen des aktuellen Themes.
    /// </summary>
    /// <param name="key">Ressourcenschlüssel.</param>
    /// <returns>Der Pinsel oder <c>null</c>.</returns>
    private static IBrush? Brush(string key) =>
        Application.Current is { } app && app.TryFindResource(key, app.ActualThemeVariant, out var value) ? value as IBrush : null;

    /// <summary>
    /// Erzeugt eine Schaltfläche, die den Dialog mit einem Ergebnis schließt.
    /// </summary>
    /// <param name="dialog">Der Dialog.</param>
    /// <param name="text">Beschriftung.</param>
    /// <param name="result">Ergebnis; <c>null</c> = Abbrechen.</param>
    /// <returns>Die Schaltfläche.</returns>
    private static Button ChoiceButton(Window dialog, string text, int? result)
    {
        var button = new Button { Content = text, MinWidth = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
        button.Click += (_, _) => dialog.Close(result);
        return button;
    }

    /// <summary>
    /// Erzeugt einen umbrechenden Meldungstext.
    /// </summary>
    /// <param name="message">Der Text.</param>
    /// <returns>Das Steuerelement.</returns>
    private static TextBlock CreateMessage(string message) =>
        new() { Text = message, TextWrapping = TextWrapping.Wrap };

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
        ((StackPanel)((Border)dialog.Content!).Child!).Children.Add(panel);
    }
}
