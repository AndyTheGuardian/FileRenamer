using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FileRenamer.App.Services;
using FileRenamer.App.ViewModels;
using FileRenamer.App.ViewModels.Settings;
using FileRenamer.App.ViewModels.Tags;
using FileRenamer.App.Views;
using FileRenamer.Core.Files;
using FileRenamer.Core.Profiles;
using FileRenamer.Core.Tagging;
using FileRenamer.Core.Tagging.Changes;
using FileRenamer.Core.Theming;

namespace FileRenamer.HeadlessChecks;

/// <summary>
/// Oberflächenprüfungen der Einstellungen (E155–E157) mit dem echten <see cref="DialogService"/>: Zahnrad und Menü,
/// Vorschau eines Farbprofils, Abbrechen, Übernehmen und Speichern; dazu die Auswahl aller Audiodateien (E154).
/// </summary>
public sealed partial class SettingsChecks
{
    private readonly CheckRunner _runner;
    private readonly string _root = Path.Combine(Path.GetTempPath(), "fr-settings-" + Guid.NewGuid().ToString("N")[..6]);
    private readonly MainWindow _window = new() { Width = 1300, Height = 900 };
    private readonly MainWindowViewModel _vm;

    /// <summary>
    /// Legt Testdateien an und öffnet das Hauptfenster mit echtem Dialogdienst in Hell und Deutsch.
    /// </summary>
    /// <param name="runner">Gemeinsame Hilfen.</param>
    public SettingsChecks(CheckRunner runner)
    {
        _runner = runner;
        TestFiles.CreateSimple(Path.Combine(_root, "music"));
        _vm = new MainWindowViewModel(new PhysicalFileSystem(), new ProfileStore(Path.Combine(_root, "profiles")), new AppSettingsStore(SettingsPath),
            new DialogService(_window), new ThemeService((FileRenamer.App.App)Application.Current!), new FakeClipboard(), new TagLibTagStore(),
            new SkiaCoverImageProcessor());
        _window.DataContext = _vm;
        _window.Show();
        _vm.Theme = ThemePreference.Light;
        _vm.Language = LanguagePreference.German;
        _vm.AddPaths([Path.Combine(_root, "music")]);
        CheckRunner.Pump();
    }

    private string SettingsPath => Path.Combine(_root, "settings.json");

    /// <summary>
    /// Führt alle Prüfungen aus und räumt danach auf.
    /// </summary>
    public void Run()
    {
        SelectionAndToolbar();
        ControlHeightsInMainWindow();
        PreviewAndCancel();
        ApplyAndSave();
        _window.Close();
        TestFiles.Delete(_root);
    }

    /// <summary>Meldet ein Prüfergebnis.</summary>
    /// <param name="name">Was geprüft wird.</param>
    /// <param name="ok">Ob bestanden.</param>
    /// <param name="detail">Zusatzinformation.</param>
    private void Check(string name, bool ok, string detail = "") => _runner.Check(name, ok, detail);

    /// <summary>
    /// E154 sowie Zahnrad, Menüeinträge und Cover-Editor ohne „Verkleinern“.
    /// </summary>
    private void SelectionAndToolbar()
    {
        _vm.IsTagArea = true;
        CheckRunner.Pump();
        Check("E154: alle Audiodateien ausgewählt", _vm.Tags.SelectedFiles.Count == 3 && _vm.Tags.AreAllSelected == true, $"{_vm.Tags.SelectedFiles.Count}");
        Check("E154: Nicht-Audiodatei nicht ausgewählt", !_vm.Tags.Rows.First(r => !r.IsAudio).IsSelected);
        Check("Zahnrad vorhanden, Farbkreis entfernt",
            _window.FindControl<Button>("SettingsButton") is { IsEffectivelyVisible: true } && _window.FindControl<Button>("AccentButton") is null);
        var entries = _window.GetLogicalDescendants().OfType<MenuItem>().Count(m => m.Command == _vm.OpenSettingsCommand);
        Check("Menü: Datei → Einstellungen… und Ansicht → Akzentfarbe… öffnen die Einstellungen", entries == 2, entries.ToString());
        _vm.Tags.SelectedFunction = _vm.Tags.FunctionEntries.OfType<TagFunctionItem>().First(i => i.Function == TagFunction.Cover);
        CheckRunner.Pump();
        Check("Cover-Editor ohne „Verkleinern“", !_window.GetVisualDescendants().OfType<NumericUpDown>().Any(n => n.IsEffectivelyVisible && n.Maximum == 10000));
        _vm.IsTagArea = false;
        CheckRunner.Pump();
    }

    /// <summary>
    /// Öffnet die Einstellungen, prüft Profilliste und Vorschau, Ring der gewählten Farbe und „Abbrechen“.
    /// </summary>
    private void PreviewAndCancel()
    {
        var before = CheckRunner.Resource("AppBackgroundBrush");
        Check("Standardfarben unverändert (hell)", before == "#fff3f4f6", before);
        var (task, dialog, settings) = OpenSettings();
        Check("Einstellungen geöffnet, Hauptfenster abgedunkelt", _window.FindControl<Control>("DialogShade")!.IsVisible);
        CheckControls("Einstellungen", dialog);
        Check("Profilliste: 7 mitgelieferte, Standard gewählt",
            settings.Profiles.Count == 7 && settings.SelectedProfile?.Profile.Name == "ColorProfile.Standard", settings.SelectedProfile?.DisplayName ?? "-");
        _runner.Shot(dialog, "settings-light.png");
        settings.SelectedProfile = settings.Profiles.First(p => p.Profile.Name == "ColorProfile.Fjord");
        CheckRunner.Pump();
        Check("Profil Fjord: Vorschau färbt Hintergrund", CheckRunner.Resource("AppBackgroundBrush") != before, CheckRunner.Resource("AppBackgroundBrush"));
        _runner.Shot(_window, "main-fjord-preview.png");
        settings.ChooseSwatch(settings.Strong[2]);
        CheckRunner.Pump();
        var ring = dialog.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("selected")).ToList();
        Check("Gewählte Farbe hat Ring", ring.Count == 1 && ring[0].BorderBrush is ISolidColorBrush { Color.A: > 0 }, ring.Count.ToString());
        Close(dialog, task, cancel: true);
        Check("Abbrechen: Farben zurück, Abdunkelung weg",
            CheckRunner.Resource("AppBackgroundBrush") == before && !_window.FindControl<Control>("DialogShade")!.IsVisible, CheckRunner.Resource("AppBackgroundBrush"));
    }

    /// <summary>
    /// Wählt Dunkel, das Profil „Mitternacht“ und 600 px für Cover, übernimmt und prüft die gespeicherten Einstellungen.
    /// </summary>
    private void ApplyAndSave()
    {
        var (task, dialog, settings) = OpenSettings();
        settings.SelectedTheme = settings.Themes.First(t => t.Theme == ThemePreference.Dark);
        CheckRunner.Pump();
        Check("Design Dunkel: dunkle Profile", settings.EditingDark && settings.Profiles.All(p => p.Profile.IsDark));
        CheckSegmentCorners(dialog);
        settings.SelectedProfile = settings.Profiles.First(p => p.Profile.Name == "ColorProfile.Midnight");
        settings.CoverMaxPixels = 600;
        CheckRunner.Pump();
        _runner.Shot(dialog, "settings-dark-midnight.png");
        Close(dialog, task, cancel: false);
        var saved = new AppSettingsStore(SettingsPath).Load();
        var midnight = BuiltInColorProfiles.Dark.First(p => p.Name == "ColorProfile.Midnight");
        Check("Übernehmen: Dunkel, Mitternacht und 600 px gespeichert", saved.Theme == ThemePreference.Dark && saved.DarkColors is { } dark
            && midnight.Matches(dark) && saved.CoverMaxPixels == 600 && _vm.TagLibrary.CoverMaxPixels == 600, $"{saved.Theme} {saved.CoverMaxPixels}");
        Check("Hauptfenster in Mitternacht-Farben", CheckRunner.Resource("AppBackgroundBrush") != "#ff1b1f27", CheckRunner.Resource("AppBackgroundBrush"));
        _runner.Shot(_window, "main-dark-midnight.png");
        _vm.Theme = ThemePreference.Light;
        CheckRunner.Pump();
    }

    /// <summary>
    /// Umschalter Design und Sprache: Das erste und das letzte Feld sind außen rund, auch gewählt (Hintergrund des
    /// gewählten Felds folgt der Rundung).
    /// </summary>
    /// <param name="dialog">Das Fenster der Einstellungen.</param>
    private void CheckSegmentCorners(SettingsWindow dialog)
    {
        var corners = dialog.GetVisualDescendants().OfType<ListBox>().Where(l => l.Classes.Contains("segments"))
            .SelectMany(list => new[] { (list.ContainerFromIndex(0), new CornerRadius(3, 0, 0, 3)), (list.ContainerFromIndex(list.ItemCount - 1), new CornerRadius(0, 3, 3, 0)) })
            .Select(x => (Expected: x.Item2, Actual: x.Item1?.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>().FirstOrDefault(p => p.Name == "PART_ContentPresenter")?.CornerRadius))
            .ToList();
        Check("Umschalter Design/Sprache: Außenkanten rund, auch gewählt", corners.Count == 4 && corners.All(c => c.Actual == c.Expected),
            string.Join(" ", corners.Select(c => c.Actual?.ToString() ?? "-")));
        _runner.Shot(dialog, "settings-segments-dark.png");
    }

    /// <summary>
    /// Öffnet die Einstellungen über den Befehl des Hauptfensters.
    /// </summary>
    /// <returns>Laufender Befehl, Fenster und ViewModel der Einstellungen.</returns>
    private (Task Task, SettingsWindow Window, SettingsViewModel ViewModel) OpenSettings()
    {
        var task = _vm.OpenSettingsCommand.ExecuteAsync(null);
        CheckRunner.Pump();
        var dialog = _window.OwnedWindows.OfType<SettingsWindow>().First();
        dialog.Width = 600;
        CheckRunner.Pump();
        return (task, dialog, (SettingsViewModel)dialog.DataContext!);
    }

    /// <summary>
    /// Schließt die Einstellungen über „Abbrechen“ oder „Übernehmen“ und wartet auf das Ende des Befehls.
    /// </summary>
    /// <param name="dialog">Das Fenster.</param>
    /// <param name="task">Der laufende Befehl.</param>
    /// <param name="cancel">Ob abgebrochen wird.</param>
    private static void Close(SettingsWindow dialog, Task task, bool cancel)
    {
        dialog.GetVisualDescendants().OfType<Button>().First(b => cancel ? b.IsCancel : b.IsDefault).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        CheckRunner.Pump();
        while (!task.IsCompleted)
        {
            Dispatcher.UIThread.RunJobs();
        }

        CheckRunner.Pump();
    }
}
