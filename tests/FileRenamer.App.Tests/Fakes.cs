using FileRenamer.App.Services;
using FileRenamer.App.ViewModels.Settings;
using FileRenamer.Core.Profiles;
using FileRenamer.Core.Theming;

namespace FileRenamer.App.Tests;

/// <summary>
/// Dialoge für Tests: liefert vorgegebene Antworten und merkt sich Meldungen, Rückfragen und Auswahlfragen.
/// </summary>
internal sealed class FakeDialogs : IDialogService
{
    /// <summary>Ergebnis der Einstellungen.</summary>
    public bool SettingsResult { get; set; }

    /// <summary>Wird beim Öffnen der Einstellungen aufgerufen.</summary>
    public Action<SettingsViewModel>? EditSettings { get; set; }

    /// <summary>Gezeigte Meldungen.</summary>
    public List<string> Messages { get; } = [];

    /// <summary>Antwort auf Rückfragen.</summary>
    public bool ConfirmResult { get; set; } = true;

    /// <summary>Gestellte Rückfragen.</summary>
    public List<string> Confirmations { get; } = [];

    /// <summary>Antwort auf Texteingaben.</summary>
    public string? PromptAnswer { get; set; }

    /// <summary>Gewählte Datei zum Öffnen.</summary>
    public string? OpenFile { get; set; }

    /// <summary>Gewählte Datei zum Speichern.</summary>
    public string? SaveFile { get; set; }

    /// <summary>Antwort auf Auswahlfragen.</summary>
    public int? ChoiceAnswer { get; set; }

    /// <summary>Gestellte Auswahlfragen.</summary>
    public List<string> Choices { get; } = [];

    /// <inheritdoc />
    public Task ShowMessageAsync(string title, string message)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        Confirmations.Add(message);
        return Task.FromResult(ConfirmResult);
    }

    /// <inheritdoc />
    public Task<int?> ChooseAsync(string title, string message, IReadOnlyList<string> choices)
    {
        Choices.Add(message);
        return Task.FromResult(ChoiceAnswer);
    }

    /// <inheritdoc />
    public Task<string?> PromptAsync(string title, string label, string initialValue, string hint, string? confirmText = null) =>
        Task.FromResult(PromptAnswer);

    /// <inheritdoc />
    public Task<string?> PickFileAsync(string title, string filterName, IReadOnlyList<string> extensions) => Task.FromResult(OpenFile);

    /// <inheritdoc />
    public Task<string?> PickSaveFileAsync(string title, string suggestedName, string filterName, string extension) => Task.FromResult(SaveFile);

    /// <inheritdoc />
    public Task<bool> ShowSettingsAsync(SettingsViewModel viewModel)
    {
        EditSettings?.Invoke(viewModel);
        return Task.FromResult(SettingsResult);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> PickFilesAsync() => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<string?> PickFolderAsync() => throw new NotSupportedException();
}

/// <summary>
/// Theme-Dienst für Tests: merkt sich jede Anwendung; „System“ zeigt <see cref="SystemDark"/>.
/// </summary>
internal sealed class FakeTheme : IThemeService
{
    /// <summary>Angewandte Designs mit Akzent und Farbstich.</summary>
    public List<(ThemePreference Theme, string? Accent, ThemeTint Tint)> Applied { get; } = [];

    /// <summary>Ob das Betriebssystem das dunkle Design zeigt.</summary>
    public bool SystemDark { get; set; }

    /// <inheritdoc />
    public bool IsDark { get; private set; }

    /// <inheritdoc />
    public void Apply(AppSettings settings)
    {
        IsDark = settings.Theme switch
        {
            ThemePreference.Dark => true,
            ThemePreference.Light => false,
            _ => SystemDark,
        };
        var colors = settings.ColorsFor(IsDark);
        Applied.Add((settings.Theme, colors.AccentColor, colors.Tint));
    }
}
