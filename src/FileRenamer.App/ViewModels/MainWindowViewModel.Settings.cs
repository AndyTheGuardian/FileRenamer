using CommunityToolkit.Mvvm.Input;
using FileRenamer.App.ViewModels.Settings;
using FileRenamer.Core.Profiles;
using FileRenamer.Core.Theming;

namespace FileRenamer.App.ViewModels;

/// <summary>
/// Teil des Hauptfenster-ViewModels: Einstellungsdialog (E155–E157) und die Farben beider Designs. Farben wirken im
/// Dialog sofort als Vorschau; erst „Übernehmen“ speichert, „Abbrechen“ stellt den vorherigen Zustand wieder her.
/// </summary>
public sealed partial class MainWindowViewModel
{
    private ThemeColorSettings _lightColors = new();
    private ThemeColorSettings _darkColors = new();
    private IReadOnlyList<ColorProfile> _colorProfiles = [];
    private IReadOnlyList<string> _customColors = [];

    /// <summary>
    /// Öffnet die Einstellungen (Zahnrad, „Datei → Einstellungen…“, „Ansicht → Akzentfarbe…“). Die Sprache wirkt
    /// wie das Design sofort; „Abbrechen“ stellt beides wieder her.
    /// </summary>
    /// <returns>Ende des Dialogs.</returns>
    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var originalLanguage = Language;
        var settings = new SettingsViewModel(CurrentSettings(), _themeService, _dialogs)
        {
            PreviewLanguage = language => Language = language,
        };
        if (!await _dialogs.ShowSettingsAsync(settings))
        {
            settings.Revert();
            Language = originalLanguage;
            return;
        }

        ApplySettings(settings.ToSettings());
    }

    /// <summary>
    /// Übernimmt die im Dialog gewählten Einstellungen, wendet sie an und speichert sie.
    /// </summary>
    /// <param name="chosen">Die gewählten Einstellungen.</param>
    private void ApplySettings(AppSettings chosen)
    {
        RestoreColors(chosen);
        TagLibrary.SetCoverShrinking(chosen.ShrinkCovers, chosen.CoverMaxPixels);
        Language = chosen.Language;
        Theme = chosen.Theme;
        _themeService.Apply(CurrentSettings());
        SaveSettings();
    }

    /// <summary>
    /// Übernimmt Farben beider Designs, eigene Farbprofile und eigene Farben aus Einstellungen; ungültige Werte entfallen.
    /// </summary>
    /// <param name="settings">Die Einstellungen.</param>
    private void RestoreColors(AppSettings settings)
    {
        _lightColors = settings.ColorsFor(dark: false);
        _darkColors = settings.ColorsFor(dark: true);
        _colorProfiles = ColorProfile.NormalizeCustom(settings.ColorProfiles);
        _customColors = (settings.CustomColors ?? []).Where(AccentPalette.IsValidHex).Select(AccentPalette.Normalize).Distinct()
            .TakeLast(AccentPalette.MaxCustomColors).ToList();
    }
}
