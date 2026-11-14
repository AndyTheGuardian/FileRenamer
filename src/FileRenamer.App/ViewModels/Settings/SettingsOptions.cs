using CommunityToolkit.Mvvm.ComponentModel;
using FileRenamer.App.Localization;
using FileRenamer.Core.Profiles;
using FileRenamer.Core.Theming;

namespace FileRenamer.App.ViewModels.Settings;

/// <summary>Wählbare Akzentfarbe im Einstellungsdialog.</summary>
/// <param name="hex">Farbwert.</param>
/// <param name="toolTip">Name und Hex-Code der Farbe.</param>
/// <param name="isCustom">Ob es eine eigene Farbe ist (entfernbar).</param>
public sealed partial class AccentSwatchViewModel(string hex, string toolTip, bool isCustom = false) : ObservableObject
{
    /// <summary>Farbwert.</summary>
    public string Hex { get; } = hex;

    /// <summary>Name und Hex-Code der Farbe.</summary>
    public string ToolTip { get; } = toolTip;

    /// <summary>Ob es eine eigene Farbe ist.</summary>
    public bool IsCustom { get; } = isCustom;

    /// <summary>Ob die Farbe gewählt ist.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>Farbprofil in der Auswahl der Einstellungen (E155).</summary>
/// <param name="profile">Das Profil.</param>
public sealed class ColorProfileViewModel(ColorProfile profile)
{
    /// <summary>Das Profil.</summary>
    public ColorProfile Profile { get; } = profile;

    /// <summary>Anzeigename; mitgelieferte Profile übersetzt.</summary>
    public string DisplayName => DisplayNameOf(Profile);

    /// <summary>Ob das Profil mitgeliefert und schreibgeschützt ist.</summary>
    public bool IsBuiltIn => Profile.IsBuiltIn;

    /// <summary>Wirksame Akzentfarbe, für den Farbpunkt.</summary>
    public string AccentHex => Profile.EffectiveAccent();

    /// <summary>Fensterhintergrund mit Farbstich und Helligkeit, für den Farbpunkt.</summary>
    public string BackgroundHex => ThemePalette.For(Profile.IsDark, Profile.Colors.Tint).AppBackground;

    /// <summary>Anzeigename eines Profils.</summary>
    /// <param name="profile">Das Profil.</param>
    /// <returns>Der Name; mitgelieferte übersetzt.</returns>
    public static string DisplayNameOf(ColorProfile profile) => profile.IsBuiltIn ? Loc.T(profile.Name) : profile.Name;

    /// <inheritdoc />
    public override string ToString() => DisplayName;
}

/// <summary>Sprache im Einstellungsdialog.</summary>
/// <param name="Language">Die Sprache.</param>
public sealed record LanguageOption(LanguagePreference Language)
{
    /// <summary>Anzeigename; Sprachen in ihrer eigenen Schreibweise.</summary>
    public string Label => Language switch
    {
        LanguagePreference.German => "Deutsch",
        LanguagePreference.English => "English",
        _ => Loc.T("Settings.LanguageSystem"),
    };

    /// <inheritdoc />
    public override string ToString() => Label;
}

/// <summary>Design im Einstellungsdialog.</summary>
/// <param name="Theme">Das Design.</param>
public sealed record ThemeOption(ThemePreference Theme)
{
    /// <summary>Anzeigename.</summary>
    public string Label => Loc.T("Theme." + Theme);

    /// <inheritdoc />
    public override string ToString() => Label;
}

/// <summary>Was die gemeinsamen Regler im Einstellungsdialog bearbeiten (E155).</summary>
public enum AdjustTarget
{
    /// <summary>Die Akzentfarbe: Farbton, Sättigung und Hellwert (HSB) absolut.</summary>
    Accent,

    /// <summary>Der Farbstich des Hintergrunds: Farbton, Stärke und Helligkeitsverschiebung (E155).</summary>
    Background,
}
