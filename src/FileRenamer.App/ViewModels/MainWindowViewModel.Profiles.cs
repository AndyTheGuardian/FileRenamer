using FileRenamer.App.ViewModels.Tags;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileRenamer.Core.Preview;
using FileRenamer.App.Localization;
using FileRenamer.App.Services;
using FileRenamer.Core.Localization;
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
    /// Gewählte Sprache (E92).
    /// </summary>
    [ObservableProperty]
    public partial LanguagePreference Language { get; set; }

    /// <summary>
    /// Eigene Vorlagen für „Name aus Vorlage“ (E106); wird beim Start aus den Einstellungen erzeugt.
    /// </summary>
    public TemplateLibrary Templates { get; private set; } = new([], []);

    /// <summary>
    /// Tritt nach einem Sprachwechsel ein, wenn alle Texte des ViewModels erneuert sind;
    /// die Anwendung baut daraufhin das Hauptfenster neu auf.
    /// </summary>
    public event EventHandler? LanguageChanged;

    /// <summary>
    /// Setzt das Theme (Menü und Icons).
    /// </summary>
    /// <param name="theme">Die Auswahl.</param>
    [RelayCommand]
    private void SetTheme(ThemePreference theme) => Theme = theme;

    /// <summary>
    /// Setzt die Sprache (Menü „Ansicht → Sprache“).
    /// </summary>
    /// <param name="language">Die Auswahl.</param>
    [RelayCommand]
    private void SetLanguage(LanguagePreference language) => Language = language;

    /// <summary>
    /// Setzt das Profil zurück (E91): leert nach Rückfrage die Kette und das Profilfeld.
    /// Gespeicherte Profile und „Dateierweiterung ignorieren“ bleiben unverändert.
    /// </summary>
    /// <returns>Ein Task.</returns>
    [RelayCommand]
    private async Task ResetProfileAsync()
    {
        if (Chain.Count > 0 &&
            !await _dialogs.ConfirmAsync(Loc.T("Profile.ResetTitle"), Loc.F("Profile.ResetText", Chain.Count), Loc.T("Profile.Reset")))
        {
            return;
        }

        ClearChain();
        SetCurrentProfile(null);
    }

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
            await _dialogs.ShowMessageAsync(Loc.T("Profile.LoadTitle"), Loc.F("Profile.LoadFailed", name));
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
        var name = await _dialogs.PromptAsync(Loc.T("Profile.SaveTitle"), Loc.T("Profile.Name"), suggestion, Loc.T("Profile.SaveHint"));
        if (name is null)
        {
            return;
        }

        if (_profileStore.Exists(name) &&
            !await _dialogs.ConfirmAsync(Loc.T("Profile.OverwriteTitle"), Loc.F("Profile.OverwriteText", name), Loc.T("Profile.Overwrite")))
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
        if (!await _dialogs.ConfirmAsync(Loc.T("Profile.DeleteTitle"), Loc.F("Profile.DeleteText", name), Loc.T("Profile.Delete")))
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
        _themeService.Apply(CurrentSettings());
        SaveSettings();
    }

    /// <summary>
    /// Stellt die Sprache um, erneuert alle Texte des ViewModels, merkt sich die Auswahl
    /// und meldet den Wechsel, damit das Fenster neu aufgebaut wird (E92).
    /// </summary>
    /// <param name="value">Die Sprache.</param>
    partial void OnLanguageChanged(LanguagePreference value)
    {
        Loc.Language = CoreText.Resolve(value, CultureInfo.CurrentUICulture);
        RefreshTexts();
        SaveSettings();
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Erzeugt die Objekte mit sprachabhängigen Texten neu: Editor (mit seinen Einstellungen) und Vorschau. Die Verfahrensliste liest das neu aufgebaute Fenster selbst;
    /// eine Änderungsmeldung an das alte Fenster würde dort die Auswahl zurücksetzen.
    /// </summary>
    private void RefreshTexts()
    {
        Tags?.OnLanguageChanged();

        if (Editor is { } editor)
        {
            SetEditor(RuleCatalog.CreateEditorFor(SelectedChainItem?.Rule ?? editor.CreateRule()));
        }

        RefreshPreview();
    }

    /// <summary>
    /// Stellt Farben, Theme, Sprache und zuletzt verwendetes Profil wieder her.
    /// </summary>
    private void RestoreSettings()
    {
        var settings = _settingsStore.Load();
        // Zuerst, weil die folgenden Schritte bereits speichern
        Templates = new TemplateLibrary(settings.CustomPhotoTemplates, settings.CustomAudioTemplates);
        Templates.Changed += (_, _) => SaveSettings();
        TagLibrary = new TagLibrary(settings);
        TagLibrary.Changed += (_, _) => SaveSettings();
        foreach (var (key, columns) in settings.ColumnLayouts)
        {
            _savedColumns[key] = columns;
        }

        ConnectColumns(RenameColumnsKey, RenameColumns);
        RestoreColors(settings);
        Language = settings.Language;
        Loc.Language = CoreText.Resolve(settings.Language, CultureInfo.CurrentUICulture);
        Theme = settings.Theme;
        _themeService.Apply(CurrentSettings());
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
            await _dialogs.ShowMessageAsync(Loc.T("Profile.SaveTitle"), Loc.F("Profile.SaveFailed", exception.Message));
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
    /// Die aktuellen Einstellungen: Design, Profil, Farben (E155), Sprache, Vorlagen, Tag-Bibliothek und Spalten.
    /// </summary>
    /// <returns>Die Einstellungen.</returns>
    private AppSettings CurrentSettings() => TagLibrary.ApplyTo(new AppSettings(Theme, CurrentProfileName)
    {
        LightColors = _lightColors,
        DarkColors = _darkColors,
        ColorProfiles = _colorProfiles,
        CustomColors = _customColors,
        Language = Language,
        CustomPhotoTemplates = Templates.Photo,
        CustomAudioTemplates = Templates.Audio,
        ColumnLayouts = new Dictionary<string, IReadOnlyList<SavedColumn>>(_savedColumns),
    });

    /// <summary>
    /// Speichert die Einstellungen für den nächsten Start; Schreibfehler werden ignoriert.
    /// </summary>
    private void SaveSettings()
    {
        try
        {
            _settingsStore.Save(CurrentSettings());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Einstellungen sind Komfort; ein Fehler darf die Bedienung nicht unterbrechen.
        }
    }
}
