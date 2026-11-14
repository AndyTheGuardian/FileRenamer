using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileRenamer.App.Localization;
using FileRenamer.App.Services;
using FileRenamer.Core.Profiles;
using FileRenamer.Core.Theming;

namespace FileRenamer.App.ViewModels.Settings;

/// <summary>
/// Teil der Einstellungen: Farbprofile des bearbeiteten Designs – wählen, speichern, umbenennen, löschen, exportieren
/// und importieren (E155).
/// </summary>
public sealed partial class SettingsViewModel
{
    /// <summary>Farbprofile des bearbeiteten Designs: mitgelieferte, dann eigene (E155).</summary>
    public ObservableCollection<ColorProfileViewModel> Profiles { get; } = [];

    /// <summary>
    /// Profil, dessen Farben gerade gelten; <c>null</c>, wenn die Farben zu keinem Profil passen. Die Wahl eines
    /// Profils übernimmt seine Farben.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomProfileSelected), nameof(HasProfileSelected))]
    [NotifyCanExecuteChangedFor(nameof(RenameProfileCommand), nameof(DeleteProfileCommand), nameof(ExportProfileCommand), nameof(ExportAllProfilesCommand))]
    public partial ColorProfileViewModel? SelectedProfile { get; set; }

    /// <summary>Ob ein eigenes Profil gewählt ist (umbenennen, löschen).</summary>
    public bool IsCustomProfileSelected => SelectedProfile is { IsBuiltIn: false };

    /// <summary>Ob ein Profil gewählt ist (exportieren).</summary>
    public bool HasProfileSelected => SelectedProfile is not null;

    /// <summary>Rückmeldung zu Import und Export.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProfileMessage))]
    public partial string? ProfileMessage { get; set; }

    /// <summary>Ob es eine Rückmeldung gibt.</summary>
    public bool HasProfileMessage => !string.IsNullOrEmpty(ProfileMessage);

    /// <summary>
    /// Speichert die aktuellen Farben als eigenes Profil des bearbeiteten Designs. Ein gleichnamiges eigenes Profil wird
    /// nach Rückfrage ersetzt; mitgelieferte bleiben unverändert, der Name bekommt dann eine Nummer (E155).
    /// </summary>
    /// <returns>Ende der Eingabe.</returns>
    [RelayCommand]
    public async Task SaveProfileAsAsync()
    {
        var initial = SelectedProfile is { IsBuiltIn: false } own ? own.DisplayName : string.Empty;
        var name = ColorProfile.CleanName(await _dialogs.PromptAsync(
            Loc.T("Settings.ProfileSaveAsTitle"), Loc.T("Settings.ProfileName"), initial, string.Empty, Loc.T("Settings.ProfileSave")));
        if (name.Length == 0)
        {
            return;
        }

        var existing = _customProfiles.FindIndex(p => p.IsDark == EditingDark && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        var profile = new ColorProfile(name, EditingDark, CurrentColors().Normalized());
        if (existing >= 0)
        {
            if (!await _dialogs.ConfirmAsync(Loc.T("Settings.ProfileSaveAsTitle"), Loc.F("Settings.ProfileOverwrite", _customProfiles[existing].Name), Loc.T("Settings.ProfileOverwriteConfirm")))
            {
                return;
            }

            profile = profile with { Name = _customProfiles[existing].Name };
            _customProfiles[existing] = profile;
        }
        else
        {
            if (CustomProfileLimitReached())
            {
                return;
            }

            profile = profile with { Name = ColorProfile.UniqueName(BuiltInNames(EditingDark), name) };
            _customProfiles.Add(profile);
        }

        ProfileMessage = null;
        RebuildProfiles(profile);
    }

    /// <summary>Benennt das gewählte eigene Profil um; der Name muss im Design eindeutig sein.</summary>
    /// <returns>Ende der Eingabe.</returns>
    [RelayCommand(CanExecute = nameof(IsCustomProfileSelected))]
    public async Task RenameProfileAsync()
    {
        if (SelectedProfile is not { IsBuiltIn: false } selected)
        {
            return;
        }

        var name = ColorProfile.CleanName(await _dialogs.PromptAsync(
            Loc.T("Settings.ProfileRenameTitle"), Loc.T("Settings.ProfileName"), selected.DisplayName, string.Empty, Loc.T("Settings.ProfileRenameConfirm")));
        if (name.Length == 0 || name == selected.Profile.Name)
        {
            return;
        }

        var index = _customProfiles.IndexOf(selected.Profile);
        var taken = TakenNames(EditingDark).Where(n => !string.Equals(n, selected.Profile.Name, StringComparison.OrdinalIgnoreCase));
        var renamed = selected.Profile with { Name = ColorProfile.UniqueName(taken, name) };
        _customProfiles[index] = renamed;
        RebuildProfiles(renamed);
    }

    /// <summary>Löscht das gewählte eigene Profil nach Rückfrage; die Farben bleiben eingestellt.</summary>
    /// <returns>Ende der Rückfrage.</returns>
    [RelayCommand(CanExecute = nameof(IsCustomProfileSelected))]
    public async Task DeleteProfileAsync()
    {
        if (SelectedProfile is not { IsBuiltIn: false } selected
            || !await _dialogs.ConfirmAsync(Loc.T("Settings.ProfileDeleteTitle"), Loc.F("Settings.ProfileDeleteMessage", selected.DisplayName), Loc.T("Settings.ProfileDelete")))
        {
            return;
        }

        _customProfiles.Remove(selected.Profile);
        RebuildProfiles();
    }

    /// <summary>Exportiert das gewählte Profil in eine Datei (E155), auch ein mitgeliefertes (E155).</summary>
    /// <returns>Ende des Exports.</returns>
    [RelayCommand(CanExecute = nameof(HasProfileSelected))]
    public Task ExportProfileAsync() =>
        SelectedProfile is { } selected ? ExportAsync([selected.Profile], selected.DisplayName) : Task.CompletedTask;

    /// <summary>Ob es eigene Profile gibt (zum Exportieren).</summary>
    public bool HasCustomProfiles => _customProfiles.Count > 0;

    /// <summary>Exportiert alle eigenen Profile beider Designs in eine Datei (E155); mitgelieferte hat jede Installation (E155).</summary>
    /// <returns>Ende des Exports.</returns>
    [RelayCommand(CanExecute = nameof(HasCustomProfiles))]
    public Task ExportAllProfilesAsync() => ExportAsync([.. _customProfiles], Loc.T("Settings.ProfileFileAll"));

    /// <summary>Exportiert alle Profile beider Designs, mitgelieferte und eigene, in eine Datei (E155).</summary>
    /// <returns>Ende des Exports.</returns>
    [RelayCommand]
    public Task ExportEveryProfileAsync() =>
        ExportAsync([.. BuiltInColorProfiles.Light, .. BuiltInColorProfiles.Dark, .. _customProfiles], Loc.T("Settings.ProfileFileEvery"));

    /// <summary>
    /// Liest Profile aus einer Datei und fügt sie als eigene Profile hinzu; gleiche Namen bekommen eine Nummer.
    /// Die eingestellten Farben ändern sich nicht.
    /// </summary>
    /// <returns>Ende des Imports.</returns>
    [RelayCommand]
    public async Task ImportProfilesAsync()
    {
        var path = await _dialogs.PickFileAsync(Loc.T("Settings.ProfileImportTitle"), Loc.T("Settings.ProfileFileType"), [ColorProfileFile.Extension]);
        if (path is null)
        {
            return;
        }

        ColorProfileReadResult read;
        try
        {
            read = ColorProfileFile.Read(await File.ReadAllTextAsync(path));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ColorProfileFormatException)
        {
            ProfileMessage = Loc.F("Settings.ProfileImportFailed", exception.Message);
            return;
        }

        // Unveränderte mitgelieferte Profile (z. B. aus „Alle Profile exportieren“) gibt es schon; sie entfallen (E155).
        var incoming = read.Profiles.Where(profile => !IsBuiltInCopy(profile)).ToList();

        // Gleichnamige eigene Profile: überschreiben oder beide behalten (E155). Mitgelieferte werden nie überschrieben.
        var conflicts = incoming.Where(profile => FindCustom(profile) >= 0).Select(profile => profile.Name).Distinct().ToList();
        var overwrite = false;
        if (conflicts.Count > 0)
        {
            var choice = await _dialogs.ChooseAsync(
                Loc.T("Settings.ProfileImportTitle"),
                Loc.F("Settings.ProfileImportConflict", string.Join(", ", conflicts)),
                [Loc.T("Settings.ProfileImportOverwrite"), Loc.T("Settings.ProfileImportKeepBoth")]);
            if (choice is null)
            {
                return;
            }

            overwrite = choice == 0;
        }

        var imported = 0;
        var replaced = 0;
        foreach (var profile in incoming)
        {
            var existing = overwrite ? FindCustom(profile) : -1;
            if (existing >= 0)
            {
                _customProfiles[existing] = profile with { Name = _customProfiles[existing].Name };
                replaced++;
                continue;
            }

            if (_customProfiles.Count >= ColorProfile.MaxCustomProfiles)
            {
                continue;
            }

            _customProfiles.Add(profile with { Name = ColorProfile.UniqueName(TakenNames(profile.IsDark), profile.Name) });
            imported++;
        }

        var skipped = read.Skipped + incoming.Count - imported - replaced;
        ProfileMessage = (replaced, skipped) switch
        {
            (0, 0) => Loc.F("Settings.ProfileImported", imported),
            (0, _) => Loc.F("Settings.ProfileImportedSkipped", imported, skipped),
            (_, 0) => Loc.F("Settings.ProfileImportedReplaced", imported, replaced),
            _ => Loc.F("Settings.ProfileImportedReplacedSkipped", imported, replaced, skipped),
        };
        RebuildProfiles();
    }

    /// <summary>Ob ein Profil einem mitgelieferten gleicht: gleiches Design, gleicher angezeigter Name, gleiche Farben.</summary>
    private static bool IsBuiltInCopy(ColorProfile profile) =>
        BuiltInColorProfiles.For(profile.IsDark).Any(builtIn =>
            string.Equals(ColorProfileViewModel.DisplayNameOf(builtIn), profile.Name, StringComparison.OrdinalIgnoreCase)
            && builtIn.Matches(profile.Colors));

    /// <summary>Position des gleichnamigen eigenen Profils im selben Design, sonst −1.</summary>
    private int FindCustom(ColorProfile profile) =>
        _customProfiles.FindIndex(p => p.IsDark == profile.IsDark && string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Ein Profil wurde gewählt: seine Farben gelten ab sofort als Vorschau.</summary>
    partial void OnSelectedProfileChanged(ColorProfileViewModel? value)
    {
        if (value is not null)
        {
            SetLastProfile(EditingDark, value.Profile);
        }

        if (_selectingProfile || !_initialized || value is null)
        {
            return;
        }

        LoadColors(value.Profile.Colors);
        HexError = null;
        RebuildSwatches();
        SyncSliders();
        Preview();
    }

    /// <summary>Füllt die Profilauswahl für das bearbeitete Design und wählt das passende Profil.</summary>
    /// <param name="preferred">Profil, das gewählt werden soll, wenn seine Farben gelten (z. B. gerade gespeichert).</param>
    private void RebuildProfiles(ColorProfile? preferred = null)
    {
        _selectingProfile = true;
        try
        {
            Profiles.Clear();
            foreach (var profile in BuiltInColorProfiles.For(EditingDark).Concat(_customProfiles.Where(p => p.IsDark == EditingDark)))
            {
                Profiles.Add(new ColorProfileViewModel(profile));
            }
        }
        finally
        {
            _selectingProfile = false;
        }

        OnPropertyChanged(nameof(HasCustomProfiles));
        ExportAllProfilesCommand.NotifyCanExecuteChanged();
        SelectMatchingProfile(preferred);
    }

    /// <summary>
    /// Wählt das Profil, dessen Farben gelten: das bisher gewählte, wenn es noch passt, sonst das erste passende
    /// eigene, sonst das erste passende mitgelieferte; ohne Treffer keines.
    /// </summary>
    private void SelectMatchingProfile(ColorProfile? preferred = null)
    {
        var colors = CurrentColors();
        var match = Profiles.FirstOrDefault(p => preferred is not null && p.Profile == preferred && p.Profile.Matches(colors))
            ?? (SelectedProfile is { } current && Profiles.Contains(current) && current.Profile.Matches(colors) ? current : null)
            ?? Profiles.Where(p => !p.IsBuiltIn).Concat(Profiles.Where(p => p.IsBuiltIn)).FirstOrDefault(p => p.Profile.Matches(colors));
        _selectingProfile = true;
        try
        {
            SelectedProfile = match;
        }
        finally
        {
            _selectingProfile = false;
        }
    }

    private async Task ExportAsync(IReadOnlyList<ColorProfile> profiles, string suggestedName)
    {
        var path = await _dialogs.PickSaveFileAsync(Loc.T("Settings.ProfileExportTitle"), suggestedName + ColorProfileFile.Extension,
            Loc.T("Settings.ProfileFileType"), "*" + ColorProfileFile.Extension);
        if (path is null)
        {
            return;
        }

        try
        {
            var named = profiles.Select(p => p with { Name = ColorProfileViewModel.DisplayNameOf(p) });
            await File.WriteAllTextAsync(path, ColorProfileFile.Write(named));
            ProfileMessage = Loc.F("Settings.ProfileExported", profiles.Count, Path.GetFileName(path));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ProfileMessage = Loc.F("Settings.ProfileExportFailed", exception.Message);
        }
    }

    private bool CustomProfileLimitReached()
    {
        if (_customProfiles.Count < ColorProfile.MaxCustomProfiles)
        {
            return false;
        }

        ProfileMessage = Loc.F("Settings.ProfileLimit", ColorProfile.MaxCustomProfiles);
        return true;
    }

    private ColorProfile? LastProfile(bool dark) => dark ? _lastDarkProfile : _lastLightProfile;

    private void SetLastProfile(bool dark, ColorProfile? profile)
    {
        if (profile is null)
        {
            return;
        }

        if (dark)
        {
            _lastDarkProfile = profile;
        }
        else
        {
            _lastLightProfile = profile;
        }

        OnPropertyChanged(nameof(ResetToolTip));
    }

    private IEnumerable<ColorProfile> AllProfiles(bool dark) =>
        BuiltInColorProfiles.For(dark).Concat(_customProfiles.Where(p => p.IsDark == dark));

    private static IEnumerable<string> BuiltInNames(bool dark) => BuiltInColorProfiles.For(dark).Select(ColorProfileViewModel.DisplayNameOf);

    private IEnumerable<string> TakenNames(bool dark) =>
        BuiltInNames(dark).Concat(_customProfiles.Where(p => p.IsDark == dark).Select(p => p.Name));
}
