using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileRenamer.App.Localization;
using FileRenamer.Core.Tagging;

namespace FileRenamer.App.ViewModels.Tags;

/// <summary>
/// Teil des Bereichs „Tags“: Speichern mit Sicherheitsabfrage (alles oder nichts), Verwerfen und Widerrufen der
/// letzten Speicherung (E124).
/// </summary>
public sealed partial class TagAreaViewModel
{
    private readonly TagWriter _writer;
    private TagSaveResult? _lastSave;

    /// <summary>
    /// Ob die letzte Speicherung widerrufen werden kann.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UndoSaveCommand))]
    public partial bool CanUndoSave { get; private set; }

    /// <summary>
    /// Beschreibt das Ergebnis eines Exports (Cover, Lyrics).
    /// </summary>
    /// <param name="result">Das Ergebnis.</param>
    /// <returns>Der Meldungstext.</returns>
    public static string DescribeExport(ExportResult result)
    {
        var text = Loc.F("Export.Result", result.Written, result.Skipped);
        return result.Errors.Count == 0 ? text : text + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, result.Errors);
    }

    /// <summary>
    /// Schreibt die geänderten Dateien nach einer Sicherheitsabfrage.
    /// </summary>
    /// <returns>Ein Task.</returns>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var jobs = _previews.Values.Where(preview => preview.IsChanged)
            .Select(preview => new TagWriteJob(preview.Path, preview.Original, preview.Result)).ToList();
        if (!await Dialogs.ConfirmAsync(Loc.T("TagSave.ConfirmTitle"), Loc.F("TagSave.ConfirmText", jobs.Count), Loc.T("TagSave.Confirm")))
        {
            return;
        }

        var result = _writer.Save(jobs, Library.CustomFieldNames);
        if (result.Success)
        {
            _lastSave = result;
            CanUndoSave = true;
            PendingChanges.Clear();
        }

        AfterWrite(result);
        await Dialogs.ShowMessageAsync(Loc.T("TagSave.Title"), Describe(result, "TagSave.Done"));
    }

    /// <summary>
    /// Stellt den Stand vor der letzten Speicherung wieder her (nach Sicherheitsabfrage, alles oder nichts).
    /// </summary>
    /// <returns>Ein Task.</returns>
    [RelayCommand(CanExecute = nameof(CanUndoSave))]
    private async Task UndoSaveAsync()
    {
        if (_lastSave is not { } lastSave
            || !await Dialogs.ConfirmAsync(Loc.T("TagUndo.ConfirmTitle"), Loc.F("TagUndo.ConfirmText", lastSave.Written.Count), Loc.T("TagUndo.Confirm")))
        {
            return;
        }

        var result = _writer.Save(lastSave.CreateUndoJobs(), Library.CustomFieldNames);
        if (result.Success)
        {
            _lastSave = null;
            CanUndoSave = false;
        }

        AfterWrite(result);
        await Dialogs.ShowMessageAsync(Loc.T("TagUndo.Title"), Describe(result, "TagUndo.Done"));
    }

    /// <summary>
    /// Verwirft alle vorgemerkten Änderungen.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasPendingChanges))]
    private void Discard()
    {
        PendingChanges.Clear();
        RefreshPreview();
    }

    /// <summary>
    /// Ob Änderungen vorgemerkt sind.
    /// </summary>
    /// <returns><c>true</c> mit mindestens einer Änderung.</returns>
    private bool HasPendingChanges() => PendingChanges.Count > 0;

    /// <summary>
    /// Liest die geschriebenen Dateien neu und meldet sie dem Hauptfenster (Metadaten neu lesen, E126).
    /// </summary>
    /// <param name="result">Das Ergebnis.</param>
    private void AfterWrite(TagSaveResult result)
    {
        var paths = result.Written.Select(job => job.Path).ToList();
        foreach (var path in paths)
        {
            _disk.Remove(path);
        }

        FilesWritten?.Invoke(this, paths);
        Reload();
    }

    /// <summary>
    /// Beschreibt das Ergebnis des Schreibens.
    /// </summary>
    /// <param name="result">Das Ergebnis.</param>
    /// <param name="successKey">Textschlüssel für den Erfolgsfall.</param>
    /// <returns>Der Meldungstext.</returns>
    private static string Describe(TagSaveResult result, string successKey)
    {
        if (result.Success)
        {
            return Loc.F(successKey, result.Written.Count);
        }

        var text = Loc.F("TagSave.Failed", result.Error!);
        return result.RollbackErrors.Count == 0
            ? text
            : text + Environment.NewLine + Loc.T("TagSave.RollbackFailed") + Environment.NewLine + string.Join(Environment.NewLine, result.RollbackErrors);
    }
}
