using Avalonia;
using Avalonia.Headless;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using FileRenamer.App.Services;
using FileRenamer.App.ViewModels.Tags;
using FileRenamer.App.Views.Tags;
using FileRenamer.Core.Profiles;
using FileRenamer.Core.Tagging.Changes;

namespace FileRenamer.HeadlessChecks;

/// <summary>
/// Teil der Prüfungen des Bereichs „Tags“: Liste leeren mit Rückfrage, Hinweis in der leeren Liste, Ablagefläche der
/// Cover, Beschriftung der Werkzeugleiste bei verschiedenen Breiten und eigene Dialoge.
/// </summary>
public sealed partial class TagAreaChecks
{
    private Button? _clearButton;

    /// <summary>
    /// Auch im Bereich „Umbenennen“ fragt „Liste leeren“ bei vorgemerkten Tag-Änderungen nach (F68).
    /// </summary>
    private void ClearAsksInRename()
    {
        _vm.IsTagArea = false;
        CheckRunner.Pump();
        var countBefore = _vm.PreviewRows.Count;
        _dialogs.ConfirmResult = false;
        _dialogs.ConfirmCount = 0;
        _vm.ClearFilesCommand.Execute(null);
        CheckRunner.Pump();
        Check("Umbenennen: Leeren fragt bei vorgemerkten Tag-Änderungen",
            _dialogs.ConfirmCount == 1 && _vm.PreviewRows.Count == countBefore && Tags.PendingChanges.Count == 1);
        _vm.IsTagArea = true;
        Tags.DiscardCommand.Execute(null);
        CheckRunner.Pump();
    }

    /// <summary>
    /// Die Ablagefläche der Cover nimmt auch neben dem Text Bilder an (Hintergrund treffbar).
    /// </summary>
    private void CoverDropZone()
    {
        Show(TagFunction.Cover);
        var dropZone = _window.GetVisualDescendants().OfType<Border>().First(b => b.Name == "DropZone");
        var hit = _window.GetVisualsAt(dropZone.TranslatePoint(new Point(6, 6), _window)!.Value).FirstOrDefault(v => v is IInputElement { IsHitTestVisible: true });
        Check("Cover-Ablagefläche in der Ecke treffbar", hit == dropZone || hit?.GetVisualAncestors().Contains(dropZone) == true, hit?.GetType().Name ?? "null");
    }

    /// <summary>
    /// „Liste leeren“ der Tag-Vorschau: abgelehnte Rückfrage ändert nichts, bestätigte leert Liste und Änderungen.
    /// </summary>
    private void ClearList()
    {
        _clearButton = _window.GetVisualDescendants().OfType<Button>()
            .FirstOrDefault(b => b.IsEffectivelyVisible && b.Command == _vm.ClearFilesCommand && b.FindAncestorOfType<TagAreaView>() is not null);
        Check("Tag-Vorschau: „Liste leeren“ sichtbar", _clearButton is not null);
        Shot("tags-06-clearbar.png");
        var edit = (EditTagsEditorViewModel)Show(TagFunction.Edit);
        edit.TitleField.Value = "X";
        edit.StageCommand.Execute(null);
        CheckRunner.Pump();
        var filesBefore = _vm.PreviewRows.Count;
        _dialogs.ConfirmResult = false;
        _dialogs.ConfirmCount = 0;
        _clearButton!.Command!.Execute(null);
        CheckRunner.Pump();
        Check("Leeren abgelehnt: Liste und Änderung bleiben", _dialogs.ConfirmCount == 1 && _vm.PreviewRows.Count == filesBefore && Tags.PendingChanges.Count == 1);
        _dialogs.ConfirmResult = true;
        _clearButton.Command.Execute(null);
        CheckRunner.Pump();
        Check("Leeren bestätigt: Liste leer, Änderung verworfen", _vm.PreviewRows.Count == 0 && Tags.PendingChanges.Count == 0 && Tags.Rows.Count == 0);
    }

    /// <summary>
    /// Leere Listen (E152): Hinweis in der Liste, „Liste leeren“ darüber, keine Ablagefläche mehr.
    /// </summary>
    private void EmptyHints()
    {
        var listTop = TagList().TranslatePoint(new Point(0, 0), _window)!.Value.Y;
        var buttonTop = _clearButton!.TranslatePoint(new Point(0, 0), _window)!.Value.Y;
        var hint = VisibleEmptyHint();
        Check("Leere Tag-Liste: Hinweis in der Liste, „Liste leeren“ darüber",
            hint is not null && hint.TranslatePoint(new Point(0, 0), _window)!.Value.Y > listTop && buttonTop < listTop, $"btn={buttonTop:0} list={listTop:0}");
        Check("Keine Ablagefläche mehr",
            !_window.GetVisualDescendants().OfType<Border>().Any(b => b.Background == Application.Current!.FindResource("DropHintBrush") && b.IsEffectivelyVisible));
        Shot("tags-19-empty.png");
        _vm.IsTagArea = false;
        CheckRunner.Pump();
        Check("Leere Rename-Liste: Hinweis sichtbar", VisibleEmptyHint() is not null);
        Shot("tags-20-rename-empty.png");
        _vm.IsTagArea = true;
        CheckRunner.Pump();
    }

    /// <summary>
    /// Der sichtbare Hinweis einer leeren Liste.
    /// </summary>
    /// <returns>Der Hinweis oder <c>null</c>.</returns>
    private TextBlock? VisibleEmptyHint() =>
        _window.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(b => b.Classes.Contains("emptyHint") && b.IsEffectivelyVisible);

    /// <summary>
    /// Werkzeugleiste (E147): Beschriftung des Umschalters bei 1300 px und bei der Mindestbreite von 900 px.
    /// </summary>
    private void ToolbarWidths()
    {
        var label = _window.FindControl<TextBlock>("AreaRenameLabel")!;
        var freeColumn = _window.FindControl<Grid>("Toolbar")!.ColumnDefinitions[4];
        _vm.IsTagArea = false;
        CheckRunner.Pump();
        Check("Zurück zum Umbenennen: Profil sichtbar", _window.FindControl<ComboBox>("ProfileComboBox")!.IsEffectivelyVisible);
        Check("Umbenennen, 1300 px: Beschriftung sichtbar", label.IsEffectivelyVisible);
        _window.Width = 900;
        CheckRunner.Pump();
        Check("Umbenennen, 900 px (Mindestbreite): Beschriftung passt, Abstand bleibt", label.IsVisible && freeColumn.ActualWidth >= 16, $"frei={freeColumn.ActualWidth:0}");
        Shot("tags-12-rename-900.png");
        _vm.IsTagArea = true;
        CheckRunner.Pump();
        Check("Tags, 900 px: Beschriftung sichtbar (Profil ausgeblendet)", label.IsVisible);
        Shot("tags-13-tags-900.png");
        _vm.IsTagArea = false;
        _window.Width = 1300;
        CheckRunner.Pump();
        Check("Umbenennen, wieder 1300 px: Beschriftung sichtbar", label.IsVisible);
    }

    /// <summary>
    /// Eigene Dialoge (E149) mit dem echten <see cref="DialogService"/> in Hell und Dunkel: eigene Fläche und Rahmen,
    /// Hauptfenster abgedunkelt, Bestätigen liefert <c>true</c>.
    /// </summary>
    private void Dialogs()
    {
        foreach (var theme in new[] { ThemePreference.Light, ThemePreference.Dark })
        {
            _vm.Theme = theme;
            CheckRunner.Pump();
            CheckDialog(theme);
        }

        _vm.Theme = ThemePreference.Light;
        CheckRunner.Pump();
    }

    /// <summary>
    /// Öffnet eine Rückfrage im angegebenen Design und prüft Darstellung, Abdunkelung und Ergebnis.
    /// </summary>
    /// <param name="theme">Das Design.</param>
    private void CheckDialog(ThemePreference theme)
    {
        var shade = _window.FindControl<Border>("DialogShade")!;
        var task = new DialogService(_window).ConfirmAsync("Liste leeren", "Es sind 2 Tag-Änderung(en) vorgemerkt.\n\nBeim Leeren werden sie verworfen.", "Leeren und verwerfen");
        CheckRunner.Pump();
        var dialog = _window.OwnedWindows.OfType<Window>().LastOrDefault();
        Check($"Dialog ({theme}): offen, Hauptfenster abgedunkelt", dialog is not null && shade.IsVisible);
        if (dialog is null)
        {
            return;
        }

        dialog.Width = 520;
        CheckRunner.Pump();
        var background = (dialog.Background as ISolidColorBrush)?.Color;
        var window = ((ISolidColorBrush)_window.FindResource(_window.ActualThemeVariant, "AppBackgroundBrush")!).Color;
        Check($"Dialog ({theme}): eigene Fläche und Rahmen, ohne Kopf", background is not null && background != window && dialog.Content is Border { BorderThickness.Left: 1 }
            && !dialog.GetVisualDescendants().OfType<TextBlock>().Any(b => b.Text == "Liste leeren"), $"{background} vs {window}");
        _runner.Shot(dialog, $"dialog-{theme}.png");
        Shot($"dialog-shade-{theme}.png");
        dialog.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("accent")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        CheckRunner.Pump();
        Check($"Dialog ({theme}): Bestätigen liefert true, Abdunkelung weg", task.IsCompleted && task.Result && !shade.IsVisible);
    }
}
