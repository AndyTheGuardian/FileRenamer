using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;
using FileRenamer.App.ViewModels.Tags;
using FileRenamer.Core.Profiles;
using FileRenamer.Core.Tagging;
using FileRenamer.Core.Tagging.Changes;
using SkiaSharp;

namespace FileRenamer.HeadlessChecks;

/// <summary>
/// Teil der Prüfungen des Bereichs „Tags“: Speichern, Extrahieren, Widerrufen, Grundlinien, Sprache, Theme und das
/// Anklicken vorgemerkter Änderungen.
/// </summary>
public sealed partial class TagAreaChecks
{
    /// <summary>
    /// Speichert alle Änderungen und liest die Dateien zurück (MP3 und FLAC).
    /// </summary>
    private void SaveAndRead()
    {
        CheckRunner.Wait(Tags.SaveCommand.ExecuteAsync(null));
        CheckRunner.Pump();
        var read = _store.Read(FirstMp3, ["STIMMUNG"]);
        Check("Gespeichert: Titel/Track/Albuminterpret/Jahr", read.Get(TagFields.Title) == "Like A Prayer" && read.Get(TagFields.Track) == "1"
            && read.Get(TagFields.TrackCount) == "3" && read.Get(TagFields.AlbumArtist) == "Madonna" && read.Get(TagFields.Year) == "1989",
            string.Join(";", read.Fields.Select(f => f.Key + "=" + f.Value)));
        Check("Gespeichert: benutzerdefiniertes Feld", read.Get("custom:STIMMUNG") == "fröhlich");
        Check("Gespeichert: Lyrics mit Zeitstempel", read.Lyrics.StartsWith("[00:12.40]Life is a mystery", StringComparison.Ordinal), read.Lyrics);
        using (var bitmap = SKBitmap.Decode(read.Covers[0].Data))
        {
            Check("Gespeichert: Cover 1000 × 750", bitmap.Width == 1000 && bitmap.Height == 750, $"{bitmap.Width}×{bitmap.Height}");
        }

        Check("Nach dem Speichern keine vorgemerkten Änderungen", Tags.PendingChanges.Count == 0 && !Tags.CanSave);
        Check("Metadaten für Umbenennen neu gelesen (E126)", _vm.PreviewRows.Count > 0);
        var flac = _store.Read(Path.Combine(Music, "03 Love Song.flac"), []);
        Check("FLAC gespeichert", flac.Get(TagFields.Title) == "Love Song" && flac.Covers.Count == 1, flac.Get(TagFields.Title));
    }

    /// <summary>
    /// Extrahiert die Cover (gleiches Bild nur einmal) und widerruft die letzte Speicherung.
    /// </summary>
    private void ExtractAndUndo()
    {
        var cover = (CoverEditorViewModel)Show(TagFunction.Cover);
        cover.Scope = TagScope.AllAudio;
        CheckRunner.Wait(cover.ExtractCommand.ExecuteAsync(null));
        CheckRunner.Pump();
        Check("Cover extrahiert (gleiches Bild nur einmal)",
            File.Exists(Path.Combine(Music, "cover.jpg")) && !File.Exists(Path.Combine(Music, "cover (2).jpg")), _dialogs.LastMessage ?? string.Empty);
        CheckRunner.Wait(Tags.UndoSaveCommand.ExecuteAsync(null));
        CheckRunner.Pump();
        var undone = _store.Read(FirstMp3, ["STIMMUNG"]);
        Check("Widerrufen: Datei 1 wieder leer", undone.Fields.Count == 0 && undone.Covers.Count == 0 && undone.Lyrics.Length == 0,
            string.Join(";", undone.Fields.Select(f => f.Key)));
        var second = _store.Read(Path.Combine(Music, "02 Express Yourself.mp3"), []);
        Check("Widerrufen: Datei 2 wie vorher",
            second.Get(TagFields.Title) == "Express Yourself" && second.Get(TagFields.Album) == "Like a Prayer" && second.Get(TagFields.AlbumArtist).Length == 0);
    }

    /// <summary>
    /// Grundlinien (E135): In jedem Editor steht jede Beschriftung auf der ersten Textzeile des Werts daneben.
    /// </summary>
    private void Baselines()
    {
        var misaligned = new List<string>();
        foreach (var function in new[] { TagFunction.Edit, TagFunction.FromFileName, TagFunction.TrackNumbers, TagFunction.Cover, TagFunction.Lyrics })
        {
            Show(function);
            misaligned.AddRange(MisalignedLabels(function));
        }

        Check("Grundlinien Beschriftung = erste Zeile des Werts", misaligned.Count == 0, string.Join(" | ", misaligned));
    }

    /// <summary>
    /// Sucht Beschriftungen, deren Grundlinie mehr als 0,6 px von der des Werts daneben abweicht.
    /// </summary>
    /// <param name="function">Die angezeigte Funktion (für die Meldung).</param>
    /// <returns>Die Abweichungen.</returns>
    private IEnumerable<string> MisalignedLabels(TagFunction function)
    {
        var labels = _window.GetVisualDescendants().OfType<TextBlock>().Where(b => b.Classes.Contains("fieldLabel") && b.IsEffectivelyVisible && b.Parent is Grid);
        foreach (var label in labels.ToList())
        {
            var grid = (Grid)label.Parent!;
            var value = grid.Children.FirstOrDefault(c => c != label && Grid.GetRow(c) == Grid.GetRow(label) && Grid.GetColumn(c) == Grid.GetColumn(label) + 1 && c.IsVisible);
            if (value is null)
            {
                continue;
            }

            var (labelLine, valueLine) = (Baseline(label), Baseline(value));
            if (labelLine is null || valueLine is null)
            {
                yield return $"{function}:{label.Text}: ?";
            }
            else if (Math.Abs(valueLine.Value - labelLine.Value) > 0.6)
            {
                yield return $"{function}:{label.Text}={Math.Round(valueLine.Value - labelLine.Value, 1)}";
            }
        }
    }

    /// <summary>
    /// Grundlinie der ersten sichtbaren Textzeile in einem Element, in Fensterkoordinaten.
    /// </summary>
    /// <param name="visual">Das Element.</param>
    /// <returns>Die senkrechte Lage oder <c>null</c> ohne Text.</returns>
    private double? Baseline(Visual visual)
    {
        switch (visual)
        {
            case TextPresenter presenter when presenter.IsEffectivelyVisible:
                return presenter.TranslatePoint(new Point(0, presenter.TextLayout.TextLines[0].Baseline), _window)?.Y;
            case TextBlock block when block.IsEffectivelyVisible && (block.Text?.Length > 0 || block.Inlines?.Count > 0):
                return block.TranslatePoint(new Point(0, block.Padding.Top + block.TextLayout.TextLines[0].Baseline), _window)?.Y;
        }

        return visual.GetVisualChildren().Select(Baseline).FirstOrDefault(line => line is not null);
    }

    /// <summary>
    /// Sprachwechsel übersetzt die Funktionsliste; Screenshots beider Bereiche in Hell und Dunkel.
    /// </summary>
    private void LanguageAndThemeShots()
    {
        _vm.Language = LanguagePreference.English;
        CheckRunner.Pump();
        Check("Sprachwechsel: Funktionsliste englisch", Tags.FunctionEntries.OfType<TagFunctionItem>().First().DisplayName == "Edit tags");
        _vm.Language = LanguagePreference.German;
        Show(TagFunction.Edit);
        _vm.Theme = ThemePreference.Dark;
        Shot("tags-05-dark.png");
        _vm.IsTagArea = false;
        Shot("tags-08-rename-dark.png");
        _vm.Theme = ThemePreference.Light;
        Shot("tags-09-rename-light.png");
        _vm.IsTagArea = true;
        CheckRunner.Pump();
    }

    /// <summary>
    /// Klick auf eine vorgemerkte Änderung wählt ihre Dateien aus und öffnet ihre Funktion (A21) – mit echten
    /// Mausklicks, auch wiederholt; ein Klick auf ihre Checkbox schaltet sie nur aus.
    /// </summary>
    private void PendingClicks()
    {
        var audio = Tags.AudioFiles.ToList();
        var edit = (EditTagsEditorViewModel)Show(TagFunction.Edit);
        SelectOnly(audio[0], audio[1]);
        edit.CommentField.Value = "K";
        edit.StageCommand.Execute(null);
        CheckRunner.Pump();
        for (var round = 1; round <= 3; round++)
        {
            SelectOnly(audio[2]);
            ClickPending();
            var selected = Tags.SelectedFiles;
            Check($"Klick {round} auf Änderung wählt genau ihre Dateien aus", selected.Count == 2 && selected.Contains(audio[0]) && selected.Contains(audio[1])
                && Tags.Rows.Count(r => r.IsSelected) == 2, string.Join(",", selected.Select(Path.GetFileName)));
            Show(TagFunction.Lyrics);
            ClickPending();
            Check($"Klick {round}: Funktion der Änderung geöffnet", (Tags.SelectedFunction as TagFunctionItem)?.Function == TagFunction.Edit);
        }

        SelectOnly(audio[2]);
        var checkBox = PendingItem().GetVisualDescendants().OfType<CheckBox>().First();
        ClickAt(checkBox, checkBox.Bounds.Width / 2);
        Check("Klick auf Checkbox schaltet nur aus, Auswahl bleibt", !Tags.PendingChanges[^1].IsEnabled && Tags.SelectedFiles.SequenceEqual([audio[2]]),
            string.Join(",", Tags.SelectedFiles.Select(Path.GetFileName)));
        Tags.RemovePendingCommand.Execute(Tags.PendingChanges[^1]);
        CheckRunner.Pump();
    }

    /// <summary>
    /// Wählt genau die angegebenen Audiodateien aus.
    /// </summary>
    /// <param name="paths">Die Pfade.</param>
    private void SelectOnly(params string[] paths)
    {
        foreach (var row in Tags.Rows.Where(r => r.IsAudio).ToList())
        {
            row.IsSelected = paths.Contains(row.Path);
        }

        CheckRunner.Pump();
    }

    /// <summary>
    /// Zeile der zuletzt vorgemerkten Änderung.
    /// </summary>
    /// <returns>Die Zeile.</returns>
    private Control PendingItem()
    {
        var list = _window.GetVisualDescendants().OfType<ListBox>().First(l => l.Name == "PendingList");
        return (Control)list.ContainerFromIndex(Tags.PendingChanges.Count - 1)!;
    }

    /// <summary>
    /// Klickt auf den Text der zuletzt vorgemerkten Änderung.
    /// </summary>
    private void ClickPending()
    {
        var text = PendingItem().GetVisualDescendants().OfType<TextBlock>().First(b => b.IsEffectivelyVisible);
        ClickAt(text, 4);
    }
}
