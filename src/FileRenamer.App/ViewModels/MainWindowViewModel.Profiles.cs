using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileRenamer.App.Services;
using FileRenamer.Core.Profiles;

namespace FileRenamer.App.ViewModels;

/// <summary>
/// Teil des Hauptfenster-ViewModels: Profile und Theme. Ein Profil wird nur durch Auswahl aus der Liste
/// geladen; „Speichern“ öffnet immer den Dialog, vorbelegt mit dem eingetippten bzw. aktuellen Namen (E48).
/// </summary>
public sealed partial class MainWindowViewModel
{
    private readonly ProfileStore _profileStore;
    private readonly AppSettingsStore _settingsStore;
    private readonly IThemeService _themeService;

    /// <summary>
    /// Namen der gespeicherten Profile.
    /// </summary>
    public ObservableCollection<string> ProfileNames { get; } = [];

    /// <summary>
    /// Zuletzt geladenes bzw. gespeichertes Profil oder <c>null</c>.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteProfileCommand))]
    public partial string? CurrentProfileName { get; set; }

    /// <summary>
    /// Text im editierbaren Profilfeld (Vorbelegung für „Speichern“).
    /// </summary>
    [ObservableProperty]
    public partial string? ProfileNameInput { get; set; }

    /// <summary>
    /// Gewähltes Theme.
    /// </summary>
    [ObservableProperty]
    public partial ThemePreference Theme { get; set; }

    /// <summary>
    /// Setzt das Theme (Menü und Icons).
    /// </summary>
    /// <param name="theme">Die Auswahl.</param>
    [RelayCommand]
    private void SetTheme(ThemePreference theme) => Theme = theme;

    /// <summary>
    /// Lädt ein Profil und ersetzt Kette und Erweiterungseinstellung.
    /// </summary>
    /// <param name="name">Der Profilname.</param>
    /// <returns>Ein Task.</returns>
    [RelayCommand]
    private async Task LoadProfileAsync(string name)
    {
        if (_profileStore.Load(name) is not { } profile)
        {
            await _dialogs.ShowMessageAsync("Profil laden", $"Das Profil „{name}“ konnte nicht gelesen werden.");
            return;
        }

        ApplyProfile(profile);
        SetCurrentProfile(name);
    }

    /// <summary>
    /// Speichert die aktuelle Kette als Profil; der Name wird im Dialog bestätigt oder geändert.
    /// </summary>
    /// <returns>Ein Task.</returns>
    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        var suggestion = string.IsNullOrWhiteSpace(ProfileNameInput) ? CurrentProfileName ?? string.Empty : ProfileNameInput.Trim();
        var name = await _dialogs.PromptAsync("Profil speichern", "Name", suggestion,
            "Gespeichert werden die Verfahren, ihre Parameter, die Reihenfolge und „Dateierweiterung ignorieren“. " +
            "Mit einem neuen Namen legen Sie ein weiteres Profil an.");
        if (name is null)
        {
            return;
        }

        if (_profileStore.Exists(name) &&
            !await _dialogs.ConfirmAsync("Profil überschreiben", $"Das Profil „{name}“ existiert bereits. Überschreiben?", "Überschreiben"))
        {
            return;
        }

        if (await TrySaveProfileAsync(name))
        {
            SyncProfileNames();
            SetCurrentProfile(name);
        }
    }

    /// <summary>
    /// Löscht das aktuelle Profil nach Rückfrage.
    /// </summary>
    /// <returns>Ein Task.</returns>
    [RelayCommand(CanExecute = nameof(HasCurrentProfile))]
    private async Task DeleteProfileAsync()
    {
        var name = CurrentProfileName!;
        if (!await _dialogs.ConfirmAsync("Profil löschen", $"Das Profil „{name}“ wirklich löschen?", "Löschen"))
        {
            return;
        }

        _profileStore.Delete(name);
        SyncProfileNames();
        SetCurrentProfile(null);
    }

    /// <summary>
    /// Ob ein Profil geladen bzw. gespeichert ist.
    /// </summary>
    /// <returns><c>true</c>, wenn ein aktuelles Profil existiert.</returns>
    private bool HasCurrentProfile() => CurrentProfileName is not null;

    /// <summary>
    /// Wendet das Theme an und merkt es sich.
    /// </summary>
    /// <param name="value">Das Theme.</param>
    partial void OnThemeChanged(ThemePreference value)
    {
        _themeService.Apply(value);
        SaveSettings();
    }

    /// <summary>
    /// Stellt Theme und zuletzt verwendetes Profil wieder her.
    /// </summary>
    private void RestoreSettings()
    {
        var settings = _settingsStore.Load();
        Theme = settings.Theme;
        _themeService.Apply(settings.Theme);
        SyncProfileNames();
        if (settings.LastProfileName is { } last && _profileStore.Load(last) is { } profile)
        {
            ApplyProfile(profile);
            SetCurrentProfile(last);
        }
    }

    /// <summary>
    /// Ersetzt Kette und Erweiterungseinstellung durch die eines Profils.
    /// </summary>
    /// <param name="profile">Das Profil.</param>
    private void ApplyProfile(RenameProfile profile)
    {
        ClearChain();
        IgnoreExtension = profile.IgnoreExtension;
        foreach (var rule in profile.Rules)
        {
            Chain.Add(new ChainItemViewModel(rule));
        }
    }

    /// <summary>
    /// Merkt sich das aktuelle Profil, zeigt seinen Namen im Profilfeld und speichert die Einstellungen.
    /// </summary>
    /// <param name="name">Der Profilname oder <c>null</c>.</param>
    private void SetCurrentProfile(string? name)
    {
        CurrentProfileName = name;
        ProfileNameInput = name;
        SaveSettings();
    }

    /// <summary>
    /// Speichert das aktuelle Profil und meldet Schreibfehler.
    /// </summary>
    /// <param name="name">Der Profilname.</param>
    /// <returns><c>true</c> bei Erfolg.</returns>
    private async Task<bool> TrySaveProfileAsync(string name)
    {
        try
        {
            _profileStore.Save(new RenameProfile(name, IgnoreExtension, Chain.Select(item => item.Rule).ToList()));
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await _dialogs.ShowMessageAsync("Profil speichern", $"Das Profil konnte nicht gespeichert werden: {exception.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gleicht die Profilliste mit der Ablage ab, ohne sie zu leeren (die Eingabe im Profilfeld bleibt erhalten).
    /// </summary>
    private void SyncProfileNames()
    {
        var stored = _profileStore.GetProfileNames();
        foreach (var removed in ProfileNames.Except(stored).ToList())
        {
            ProfileNames.Remove(removed);
        }

        for (var index = 0; index < stored.Count; index++)
        {
            if (!ProfileNames.Contains(stored[index]))
            {
                ProfileNames.Insert(index, stored[index]);
            }
        }
    }

    /// <summary>
    /// Speichert Theme und Profil für den nächsten Start; Schreibfehler werden ignoriert.
    /// </summary>
    private void SaveSettings()
    {
        try
        {
            _settingsStore.Save(new AppSettings(Theme, CurrentProfileName));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Einstellungen sind Komfort; ein Fehler darf die Bedienung nicht unterbrechen.
        }
    }
}
