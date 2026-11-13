using CommunityToolkit.Mvvm.Input;
using FileRenamer.App.Localization;
using FileRenamer.Core.Tagging;

namespace FileRenamer.App.ViewModels.Tags;

/// <summary>
/// Teil des Bereichs „Tags“: Kopieren der markierten Zeilen der Tag-Vorschau in die Zwischenablage (F66, F67).
/// Markiert wird per Klick auf die Zeile, unabhängig von der Checkbox (E150).
/// Kopiert wird immer der gespeicherte Stand der Dateien, nicht die vorgemerkten Änderungen.
/// </summary>
public sealed partial class TagAreaViewModel
{
    /// <summary>
    /// Ob Zeilen markiert sind, die kopiert werden können.
    /// </summary>
    public bool HasMarkedRows => _markedPaths.Count > 0;

    /// <summary>
    /// Kopiert die markierten Zeilen mit den gewählten Spalten in Anzeigereihenfolge.
    /// </summary>
    /// <param name="columns">Die zu kopierenden Spalten.</param>
    [RelayCommand(CanExecute = nameof(HasMarkedRows))]
    private async Task CopyRowsAsync(TagCopyColumns columns)
    {
        var rows = _audioFiles.Where(_markedPaths.Contains).Select(path => new TagCopyRow(Path.GetFileName(path), DiskTags(path)));
        await _clipboard.SetTextAsync(TagListFormatter.Format(rows, columns, CopyLabels()));
    }

    /// <summary>
    /// Liefert Kopfzeile und Text für „vorhanden“ in der aktuellen Sprache.
    /// </summary>
    /// <returns>Die Beschriftungen.</returns>
    private static TagCopyLabels CopyLabels() => new(
        [
            Loc.T("TagCopy.File"), TagFieldNames.Get(TagFields.Track), TagFieldNames.Get(TagFields.Title),
            TagFieldNames.Get(TagFields.Artist), TagFieldNames.Get(TagFields.Album), TagFieldNames.Get(TagFields.AlbumArtist),
            TagFieldNames.Get(TagFields.Year), Loc.T("TagFunction.Cover"), Loc.T("TagFunction.Lyrics"),
        ],
        Loc.T("TagCopy.Present"));
}
