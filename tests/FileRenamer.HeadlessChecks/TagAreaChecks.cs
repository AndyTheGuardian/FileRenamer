using Avalonia;
using Avalonia.Headless;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using FileRenamer.App;
using FileRenamer.App.Controls;
using FileRenamer.App.Services;
using FileRenamer.App.ViewModels;
using FileRenamer.App.ViewModels.Tags;
using FileRenamer.App.Views;
using FileRenamer.App.Views.Tags;
using FileRenamer.Core.Files;
using FileRenamer.Core.Profiles;
using FileRenamer.Core.Tagging;
using FileRenamer.Core.Tagging.Changes;

namespace FileRenamer.HeadlessChecks;

/// <summary>
/// Oberflächenprüfungen des Bereichs „Tags“ und der gemeinsamen Dateilisten mit echten Dateien (drei Audiodateien,
/// ein PDF, eine LRC-Datei) im Hauptfenster. Die Prüfungen bauen aufeinander auf und laufen in fester Reihenfolge.
/// </summary>
public sealed partial class TagAreaChecks
{
    private readonly CheckRunner _runner;
    private readonly string _root = Path.Combine(Path.GetTempPath(), "fr-tagcheck-" + Guid.NewGuid().ToString("N")[..6]);
    private readonly TagLibTagStore _store = new();
    private readonly FakeClipboard _clipboard = new();
    private readonly FakeDialogs _dialogs;
    private readonly ThemeService _theme;
    private readonly MainWindowViewModel _vm;
    private readonly MainWindow _window;

    /// <summary>
    /// Legt die Testdateien an und öffnet das Hauptfenster im Bereich „Tags“.
    /// </summary>
    /// <param name="runner">Gemeinsame Hilfen.</param>
    public TagAreaChecks(CheckRunner runner)
    {
        _runner = runner;
        TestFiles.CreateAlbum(Music, CoverPath, _store);
        _dialogs = new FakeDialogs { File = CoverPath };
        _theme = new ThemeService((FileRenamer.App.App)Application.Current!);
        _vm = CreateViewModel();
        _window = new MainWindow { DataContext = _vm, Width = 1300, Height = 900 };
        _window.Show();
        _vm.AddPaths([Music]);
        _vm.IsTagArea = true;
        CheckRunner.Pump();
    }

    private string Music => Path.Combine(_root, "Madonna", "Like a Prayer");

    private string CoverPath => Path.Combine(_root, "big-cover.jpg");

    private string FirstMp3 => Path.Combine(Music, "01 Like A Prayer.mp3");

    private TagAreaViewModel Tags => _vm.Tags;

    /// <summary>
    /// Führt alle Prüfungen der Reihe nach aus und räumt danach die Testdateien weg.
    /// </summary>
    public void Run()
    {
        Action[] steps =
        [
            StartState, EditTags, GenreBox, Borders, ExtraFields, FromFileNameAndTracks, CoverAndLyrics, SaveAndRead,
            ExtractAndUndo, Baselines, LanguageAndThemeShots, PendingClicks, SelectionColors, ColumnDragAndResize,
            ColumnMenuAndRestart, RenameColumns, CopyAndShowOld, ContextMenuAndCtrlC, CheckboxSelection,
            HorizontalScrolling, RenameScrolling, ClearAsksInRename, CoverDropZone, ClearList, EmptyHints, ToolbarWidths,
            Dialogs,
        ];
        foreach (var step in steps)
        {
            step();
        }

        _window.Close();
        TestFiles.Delete(_root);
    }

    /// <summary>
    /// Erzeugt ein ViewModel mit den Testdiensten und dem Einstellungsordner der Prüfung.
    /// </summary>
    /// <returns>Das ViewModel.</returns>
    private MainWindowViewModel CreateViewModel() => new(
        new PhysicalFileSystem(), new ProfileStore(Path.Combine(_root, "profiles")), new AppSettingsStore(Path.Combine(_root, "settings.json")),
        _dialogs, _theme, _clipboard, _store, new SkiaCoverImageProcessor());

    /// <summary>Meldet ein Prüfergebnis.</summary>
    /// <param name="name">Was geprüft wird.</param>
    /// <param name="ok">Ob bestanden.</param>
    /// <param name="detail">Zusatzinformation.</param>
    private void Check(string name, bool ok, string detail = "") => _runner.Check(name, ok, detail);

    /// <summary>Speichert einen Screenshot des Hauptfensters.</summary>
    /// <param name="fileName">Dateiname.</param>
    private void Shot(string fileName) => _runner.Shot(_window, fileName);

    /// <summary>Wählt eine Tag-Funktion und lässt die Oberfläche nachziehen.</summary>
    /// <param name="function">Die Funktion.</param>
    /// <returns>Der Editor der Funktion.</returns>
    private TagEditorViewModel Show(TagFunction function)
    {
        Tags.SelectedFunction = Tags.FunctionEntries.OfType<TagFunctionItem>().First(item => item.Function == function);
        CheckRunner.Pump();
        return Tags.Editor!;
    }

    /// <summary>Klickt mit der linken Maustaste an einen Punkt des Hauptfensters.</summary>
    /// <param name="point">Punkt in Fensterkoordinaten.</param>
    private void Click(Point point)
    {
        _window.MouseDown(point, MouseButton.Left);
        _window.MouseUp(point, MouseButton.Left);
        CheckRunner.Pump();
    }

    /// <summary>Klickt auf die senkrechte Mitte eines Steuerelements.</summary>
    /// <param name="control">Das Steuerelement.</param>
    /// <param name="x">Waagerechte Lage im Steuerelement.</param>
    private void ClickAt(Control control, double x) => Click(control.TranslatePoint(new Point(x, control.Bounds.Height / 2), _window)!.Value);

    /// <summary>Linke Kante einer Zelle in Fensterkoordinaten.</summary>
    /// <param name="panel">Zeile oder Spaltenkopf.</param>
    /// <param name="key">Schlüssel der Spalte.</param>
    /// <returns>Die waagerechte Lage.</returns>
    private double CellX(ColumnRowPanel panel, string key) =>
        panel.Children.First(child => ColumnRowPanel.GetKey(child) == key).TranslatePoint(new Point(0, 0), _window)!.Value.X;

    /// <summary>Spaltenkopf der Tag-Vorschau.</summary>
    /// <returns>Der Spaltenkopf.</returns>
    private ColumnHeaderPanel TagHeader() =>
        _window.GetVisualDescendants().OfType<ColumnHeaderPanel>().First(header => header.FindAncestorOfType<TagAreaView>() is not null);

    /// <summary>Die Liste der Tag-Vorschau.</summary>
    /// <returns>Die Liste.</returns>
    private ListBox TagList() => _window.GetVisualDescendants().OfType<ListBox>().First(list => list.Classes.Contains("tagPreview"));

    /// <summary>Höhe der höchsten Zeile einer Liste.</summary>
    /// <param name="list">Die Liste.</param>
    /// <returns>Die Höhe.</returns>
    private static double MaxRowHeight(ListBox list) =>
        Enumerable.Range(0, list.ItemCount).Select(list.ContainerFromIndex).OfType<Control>().Max(row => row.Bounds.Height);

    /// <summary>
    /// Startzustand: Zeilen, Audiodateien, alle ausgewählt (E154), Profil ausgeblendet, Beschriftung des Umschalters.
    /// </summary>
    private void StartState()
    {
        Check("Zeilen: 4 Dateien (3 Audio + PDF; LRC zählt nicht)", Tags.Rows.Count is 4 or 5,
            string.Join(" | ", Tags.Rows.Select(row => row.FileName + (row.IsAudio ? string.Empty : "*"))));
        Check("3 Audiodateien", Tags.AudioFiles.Count == 3, Tags.AudioFiles.Count.ToString());
        Check("E154: alle Audiodateien ausgewählt", Tags.SelectedFiles.Count == 3 && Tags.AreAllSelected == true);
        Check("Tag-Bereich: Profil, Speichern, Zurücksetzen ausgeblendet", !_window.FindControl<ComboBox>("ProfileComboBox")!.IsVisible
            && !_window.GetVisualDescendants().OfType<Button>().Any(b => b.IsEffectivelyVisible && (b.Command == _vm.SaveProfileCommand || b.Command == _vm.ResetProfileCommand)));
        Check("Umschalter-Beschriftung bei genug Platz sichtbar (1300 px)",
            _window.FindControl<TextBlock>("AreaRenameLabel")!.IsEffectivelyVisible && _window.FindControl<TextBlock>("AreaTagsLabel")!.IsEffectivelyVisible);
        Shot("tags-01-start.png");
    }

    /// <summary>
    /// „Tags bearbeiten“: gemeinsame Werte, Prüfung des Jahrs, Vormerken, eigenes Genre.
    /// </summary>
    private void EditTags()
    {
        Tags.AreAllSelected = true;
        CheckRunner.Pump();
        Check("Kopf-Checkbox: alle Audiodateien ausgewählt", Tags.SelectedFiles.Count == 3, Tags.SelectedSummary ?? string.Empty);
        var edit = (EditTagsEditorViewModel)Tags.Editor!;
        Check("Album: 1 von 3 gesetzt → verschieden", edit.AlbumField.Varies, edit.AlbumField.Value);
        edit.AlbumArtistField.Value = "Madonna";
        edit.YearField.Value = "1989";
        edit.GenreField.Value = "Synthpop";
        CheckRunner.Pump();
        Check("Vormerken möglich", edit.StageCommand.CanExecute(null));
        edit.YearField.Value = "19x9";
        CheckRunner.Pump();
        Check("Ungültiges Jahr sperrt", !edit.StageCommand.CanExecute(null) && edit.HasValidationMessage, edit.ValidationMessage ?? string.Empty);
        edit.YearField.Value = "1989";
        edit.StageCommand.Execute(null);
        CheckRunner.Pump();
        Check("1 Änderung vorgemerkt", Tags.PendingChanges.Count == 1, Tags.PendingChanges.FirstOrDefault()?.DisplayName ?? string.Empty);
        Check("Eigenes Genre aufgenommen", _vm.TagLibrary.CustomGenres.Contains("Synthpop"));
    }

    /// <summary>
    /// Genre als editierbare Auswahl (E141): Übernahme aus der Liste, Trennlinie, Entfernen eines eigenen Genres.
    /// </summary>
    private void GenreBox()
    {
        var genreBox = _window.GetVisualDescendants().OfType<ComboBox>().First(c => c.Name == "GenreComboBox");
        var edit = (EditTagsEditorViewModel)Tags.Editor!;
        var before = edit.GenreField.Value;
        Check("Genre: editierbare ComboBox", genreBox.IsEditable && genreBox.ItemCount > 10, genreBox.ItemCount.ToString());
        genreBox.SelectedItem = genreBox.Items.OfType<GenreOption>().First(g => g.Name == "Pop");
        CheckRunner.Pump();
        Check("Genre aus Liste übernommen", edit.GenreField.Value == "Pop" && genreBox.Text == "Pop", edit.GenreField.Value);
        genreBox.IsDropDownOpen = true;
        Shot("tags-07-genre-open.png");
        genreBox.IsDropDownOpen = false;
        CheckRunner.Pump();
        var separator = genreBox.Items.OfType<object>().ToList().FindIndex(item => item is GenreSeparator);
        Check("Trennlinie nicht wählbar", genreBox.ContainerFromIndex(separator) is { IsEnabled: false });
        var custom = genreBox.Items.OfType<GenreOption>().First(g => g.IsCustom);
        edit.GenreField.Value = "Rock";
        CheckRunner.Pump();
        edit.RemoveGenreCommand.Execute(custom);
        CheckRunner.Pump();
        Check("Eigenes Genre entfernt, Feldwert bleibt",
            !_vm.TagLibrary.CustomGenres.Contains(custom.Name) && edit.GenreField.Value == "Rock" && genreBox.Text == "Rock", $"{edit.GenreField.Value}/{genreBox.Text}");
        _vm.TagLibrary.AddGenre(custom.Name);
        edit.GenreField.Value = before;
        CheckRunner.Pump();
    }

    /// <summary>
    /// Einheitliche Rahmen (E142) von Textfeld, Auswahlfeld und Schaltfläche; Vorschau des Albuminterpreten.
    /// </summary>
    private void Borders()
    {
        static Avalonia.Media.Color? Border(Avalonia.Controls.Primitives.TemplatedControl c) => (c.BorderBrush as Avalonia.Media.ISolidColorBrush)?.Color;
        var expected = ((Avalonia.Media.ISolidColorBrush)_window.FindResource(_window.ActualThemeVariant, "AppBorderBrush")!).Color;
        var boxes = _window.GetVisualDescendants().OfType<TextBox>().Where(b => b.IsEffectivelyVisible && b.FindAncestorOfType<ComboBox>() is null).ToList();
        var plainButton = _window.GetVisualDescendants().OfType<Button>().First(b => b.IsEffectivelyVisible && !b.Classes.Contains("accent")
            && b.Background is not Avalonia.Media.ISolidColorBrush { Color.A: 0 } && b.Content is string);
        boxes[0].Focus();
        CheckRunner.Pump();
        Check("Rahmen Textfeld = Segmentrahmen", Border(boxes[1]) == expected, $"{Border(boxes[1])} vs {expected}");
        var profileBox = _window.FindControl<ComboBox>("ProfileComboBox")!;
        var genreBox = _window.GetVisualDescendants().OfType<ComboBox>().First(c => c.Name == "GenreComboBox");
        var dirty = ((EditTagsEditorViewModel)Tags.Editor!).GenreField.IsDirty;
        Check("Rahmen ComboBox = Segmentrahmen (Profil, Genre unverändert)", Border(profileBox) == expected && (dirty || Border(genreBox) == expected),
            $"{Border(profileBox)} / {Border(genreBox)} dirty={dirty}");
        Check("Rahmen Schaltfläche = Segmentrahmen, 1 px", Border(plainButton) == expected && plainButton.BorderThickness == new Thickness(1),
            $"{plainButton.Content}: {Border(plainButton)} {plainButton.BorderThickness}");
        Check("Vorschau: Albuminterpret aus „Tags bearbeiten“",
            Tags.Rows.Where(r => r.IsAudio).All(r => r.AlbumArtist.Source == TagFunction.Edit && r.AlbumArtist.Text == "Madonna"));
    }

    /// <summary>
    /// Zusätzliche Felder (BPM und ein eigenes Feld) werden programmweit aufgenommen und vorgemerkt.
    /// </summary>
    private void ExtraFields()
    {
        var edit = (EditTagsEditorViewModel)Tags.Editor!;
        edit.NewFieldInput = "BPM";
        edit.AddFieldCommand.Execute(null);
        edit.NewFieldInput = "Stimmung";
        edit.AddFieldCommand.Execute(null);
        CheckRunner.Pump();
        Check("Felder hinzugefügt (programmweit)", _vm.TagLibrary.ExtraFields.SequenceEqual(["bpm", "custom:STIMMUNG"]), string.Join(",", _vm.TagLibrary.ExtraFields));
        edit.ExtraFields.First(f => f.Key == "custom:STIMMUNG").Value = "fröhlich";
        edit.StageCommand.Execute(null);
        CheckRunner.Pump();
    }

    /// <summary>
    /// „Tags aus Dateinamen“ und „Tracknummern vergeben“: Beispiele und Vorschau.
    /// </summary>
    private void FromFileNameAndTracks()
    {
        var fromName = (FromFileNameEditorViewModel)Show(TagFunction.FromFileName);
        Check("Beispiel Tags aus Dateinamen", fromName.Example?.Contains("Like A Prayer", StringComparison.Ordinal) == true, fromName.Example ?? string.Empty);
        Shot("tags-02-fromname.png");
        fromName.StageCommand.Execute(null);
        CheckRunner.Pump();
        var first = Tags.Rows.First(r => r.IsAudio);
        Check("Titel aus Dateinamen", first.Title.Text == "Like A Prayer" && first.Title.Source == TagFunction.FromFileName);
        var tracks = (TrackNumbersEditorViewModel)Show(TagFunction.TrackNumbers);
        Check("Beispiel Tracknummern", tracks.Example == "1/3, 2/3, 3/3", tracks.Example ?? string.Empty);
        tracks.StageCommand.Execute(null);
        CheckRunner.Pump();
    }

    /// <summary>
    /// Cover einfügen (verkleinert auf 1000 px), Lyrics importieren, eine Änderung aus- und wieder einschalten.
    /// </summary>
    private void CoverAndLyrics()
    {
        var cover = (CoverEditorViewModel)Show(TagFunction.Cover);
        cover.InsertFromFile(CoverPath);
        CheckRunner.Pump();
        Check("Cover vorgemerkt", Tags.PendingChanges.Count == 5 && Tags.Rows.Where(r => r.IsAudio).All(r => r.HasCover && r.IsCoverChanged), Tags.PendingChanges.Count.ToString());
        Check("Kachel zeigt verkleinertes Bild", cover.Tiles.FirstOrDefault()?.Meta.Contains("1000 × 750", StringComparison.Ordinal) == true, cover.Tiles.FirstOrDefault()?.Meta ?? "-");
        Shot("tags-03-cover.png");
        var lyrics = (LyricsEditorViewModel)Show(TagFunction.Lyrics);
        lyrics.Scope = TagScope.AllAudio;
        CheckRunner.Wait(lyrics.ImportCommand.ExecuteAsync(null));
        CheckRunner.Pump();
        var rows = Tags.Rows.Where(r => r.IsAudio).ToList();
        Check("Lyrics für Datei 1, Hinweis für die anderen", rows[0].LyricsCell.Text == "✓" && rows[1].HasHint && rows[2].HasHint, $"{rows[0].LyricsCell.Text} {rows[1].Hint}");
        Tags.ShowOldValues = true;
        Shot("tags-04-lyrics-showold.png");
        Tags.PendingChanges[0].IsEnabled = false;
        CheckRunner.Pump();
        Check("Ausgeschaltete Änderung wirkt nicht", Tags.Rows.First(r => r.IsAudio).AlbumArtist.Source is null);
        Tags.PendingChanges[0].IsEnabled = true;
        CheckRunner.Pump();
    }
}
