using FileRenamer.App.Services;
using FileRenamer.App.ViewModels.Settings;

namespace FileRenamer.HeadlessChecks;

/// <summary>
/// Zwischenablage für die Prüfungen: merkt sich den zuletzt kopierten Text.
/// </summary>
public sealed class FakeClipboard : IClipboardService
{
    /// <summary>Zuletzt kopierter Text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <inheritdoc />
    public Task SetTextAsync(string text)
    {
        Text = text;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Dialoge für die Prüfungen: Rückfragen mit vorgegebener Antwort, Meldungen auf der Konsole, Dateiauswahl liefert
/// <see cref="File"/>.
/// </summary>
public sealed class FakeDialogs : IDialogService
{
    /// <summary>Datei, die die Dateiauswahl liefert.</summary>
    public string? File { get; set; }

    /// <summary>Zuletzt gezeigte Meldung.</summary>
    public string? LastMessage { get; private set; }

    /// <summary>Antwort auf Rückfragen.</summary>
    public bool ConfirmResult { get; set; } = true;

    /// <summary>Anzahl gestellter Rückfragen.</summary>
    public int ConfirmCount { get; set; }

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        ConfirmCount++;
        return Task.FromResult(ConfirmResult);
    }

    /// <inheritdoc />
    public Task ShowMessageAsync(string title, string message)
    {
        LastMessage = message;
        Console.WriteLine($"  [Meldung] {title}: {message.Replace("\n", " ", StringComparison.Ordinal)}");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> PromptAsync(string title, string label, string initialValue, string hint, string? confirmText = null) =>
        Task.FromResult<string?>(null);

    /// <inheritdoc />
    public Task<int?> ChooseAsync(string title, string message, IReadOnlyList<string> choices) => Task.FromResult<int?>(null);

    /// <inheritdoc />
    public Task<string?> PickSaveFileAsync(string title, string suggestedName, string filterName, string extension) =>
        Task.FromResult<string?>(null);

    /// <inheritdoc />
    public Task<bool> ShowSettingsAsync(SettingsViewModel viewModel) => Task.FromResult(false);

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> PickFilesAsync() => Task.FromResult<IReadOnlyList<string>>([]);

    /// <inheritdoc />
    public Task<string?> PickFileAsync(string title, string filterName, IReadOnlyList<string> extensions) => Task.FromResult(File);

    /// <inheritdoc />
    public Task<string?> PickFolderAsync() => Task.FromResult<string?>(null);
}
