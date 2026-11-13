using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FileRenamer.App.Localization;
using FileRenamer.App.Services;
using FileRenamer.App.ViewModels;
using FileRenamer.App.Views;
using FileRenamer.Core.Files;
using FileRenamer.Core.Localization;
using FileRenamer.Core.Profiles;
using FileRenamer.Core.Tagging;

namespace FileRenamer.App;

/// <summary>
/// Avalonia-Anwendung: lädt Ressourcen, verdrahtet Dienste und öffnet das Hauptfenster.
/// Nach einem Sprachwechsel wird das Hauptfenster mit demselben ViewModel neu aufgebaut (E92).
/// </summary>
public sealed class App : Application
{
    private IClassicDesktopStyleApplicationLifetime? _desktop;
    private DialogService? _dialogs;

    /// <summary>
    /// Lädt die XAML-Ressourcen der Anwendung.
    /// </summary>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Stellt die Sprache ein und erzeugt Hauptfenster und ViewModel, sobald das Framework bereit ist.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settingsStore = new AppSettingsStore(AppDataLocator.GetSettingsFile());
            Loc.Language = CoreText.Resolve(settingsStore.Load().Language, CultureInfo.CurrentUICulture);

            var window = new MainWindow();
            _desktop = desktop;
            _dialogs = new DialogService(window);
            var viewModel = new MainWindowViewModel(
                new PhysicalFileSystem(),
                new ProfileStore(AppDataLocator.GetProfileDirectory()),
                settingsStore,
                _dialogs,
                new ThemeService(this),
                new ClipboardService(() => _desktop?.MainWindow),
                new TagLibTagStore(),
                new SkiaCoverImageProcessor());
            viewModel.LanguageChanged += OnLanguageChanged;
            window.DataContext = viewModel;
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Baut das Fenster erst nach dem laufenden Menü-Klick neu auf, damit das alte Fenster nicht
    /// während seiner eigenen Ereignisbehandlung geschlossen wird.
    /// </summary>
    /// <param name="sender">Das ViewModel.</param>
    /// <param name="e">Leer.</param>
    private void OnLanguageChanged(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() => RebuildMainWindow(sender));

    /// <summary>
    /// Ersetzt das Hauptfenster durch ein neu aufgebautes in der neuen Sprache; Größe, Lage und ViewModel bleiben.
    /// </summary>
    /// <param name="viewModel">Das ViewModel.</param>
    private void RebuildMainWindow(object? viewModel)
    {
        if (_desktop?.MainWindow is not { } oldWindow || _dialogs is null)
        {
            return;
        }

        var newWindow = new MainWindow
        {
            DataContext = viewModel,
            Width = oldWindow.Width,
            Height = oldWindow.Height,
            Position = oldWindow.Position,
            WindowStartupLocation = WindowStartupLocation.Manual,
            WindowState = oldWindow.WindowState,
        };
        _dialogs.Owner = newWindow;
        _desktop.MainWindow = newWindow;
        newWindow.Show();
        oldWindow.DataContext = null;
        oldWindow.Close();
    }
}
