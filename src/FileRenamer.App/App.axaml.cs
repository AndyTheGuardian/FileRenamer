using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FileRenamer.App.Services;
using FileRenamer.App.ViewModels;
using FileRenamer.App.Views;
using FileRenamer.Core.Files;
using FileRenamer.Core.Profiles;

namespace FileRenamer.App;

/// <summary>
/// Avalonia-Anwendung: lädt Ressourcen, verdrahtet Dienste und öffnet das Hauptfenster.
/// </summary>
public sealed class App : Application
{
    /// <summary>
    /// Lädt die XAML-Ressourcen der Anwendung.
    /// </summary>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Erzeugt Hauptfenster und ViewModel, sobald das Framework bereit ist.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            var dialogs = new DialogService(window);
            window.DataContext = new MainWindowViewModel(
                new PhysicalFileSystem(),
                new ProfileStore(AppDataLocator.GetProfileDirectory()),
                new AppSettingsStore(AppDataLocator.GetSettingsFile()),
                dialogs,
                new ThemeService(this));
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
