using Avalonia;
using Avalonia.Styling;
using FileRenamer.Core.Profiles;

namespace FileRenamer.App.Services;

/// <summary>
/// Setzt das Erscheinungsbild der Anwendung (System, Hell, Dunkel).
/// </summary>
/// <param name="application">Die Anwendung.</param>
public sealed class ThemeService(Application application) : IThemeService
{
    /// <summary>
    /// Wendet die gewünschte Theme-Variante an.
    /// </summary>
    /// <param name="theme">Die Auswahl.</param>
    public void Apply(ThemePreference theme) =>
        application.RequestedThemeVariant = theme switch
        {
            ThemePreference.Light => ThemeVariant.Light,
            ThemePreference.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
}
