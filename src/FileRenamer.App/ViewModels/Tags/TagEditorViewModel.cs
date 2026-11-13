using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileRenamer.App.Services;
using FileRenamer.Core.Tagging;
using FileRenamer.Core.Tagging.Changes;

namespace FileRenamer.App.ViewModels.Tags;

/// <summary>
/// Für welche Dateien eine Funktion vorgemerkt wird („Gilt für“).
/// </summary>
public enum TagScope
{
    /// <summary>Ausgewählte Dateien (Checkbox).</summary>
    Selected,

    /// <summary>Alle Audiodateien der Liste.</summary>
    AllAudio,
}

/// <summary>
/// Was ein Editor vom Bereich „Tags“ braucht: ausgewählte und alle Audiodateien, deren Tags, Vormerken von Änderungen
/// und Dienste.
/// </summary>
public interface ITagEditorContext
{
    /// <summary>
    /// Ausgewählte Audiodateien (Checkbox) in Listenreihenfolge.
    /// </summary>
    IReadOnlyList<string> SelectedFiles { get; }

    /// <summary>
    /// Alle Audiodateien in Listenreihenfolge.
    /// </summary>
    IReadOnlyList<string> AudioFiles { get; }

    /// <summary>
    /// Dialoge und Dateiauswahl.
    /// </summary>
    IDialogService Dialogs { get; }

    /// <summary>
    /// Bildverarbeitung für Cover.
    /// </summary>
    ICoverImageProcessor Images { get; }

    /// <summary>
    /// Programmweite Einstellungen des Bereichs.
    /// </summary>
    TagLibrary Library { get; }

    /// <summary>
    /// Tags einer Datei nach den vorgemerkten Änderungen.
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <returns>Die Tags.</returns>
    AudioTags CurrentTags(string path);

    /// <summary>
    /// Tags einer Datei, wie sie auf der Platte stehen.
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <returns>Die Tags.</returns>
    AudioTags DiskTags(string path);

    /// <summary>
    /// Merkt eine Änderung vor.
    /// </summary>
    /// <param name="change">Die Änderung.</param>
    void Stage(TagChange change);
}

/// <summary>
/// Gemeinsame Basis der Editoren im Bereich „Tags“ (oben rechts). Ein Editor erzeugt beim Vormerken eine
/// <see cref="TagChange"/> für die Dateien seines Geltungsbereichs.
/// </summary>
/// <param name="function">Die Funktion des Editors.</param>
/// <param name="context">Der Bereich „Tags“.</param>
public abstract partial class TagEditorViewModel(TagFunction function, ITagEditorContext context) : ObservableObject
{
    /// <summary>
    /// Die Funktion.
    /// </summary>
    public TagFunction Function { get; } = function;

    /// <summary>
    /// Überschrift des Editors.
    /// </summary>
    public string Title => TagFunctionItem.GetTitle(Function);

    /// <summary>
    /// Der Bereich „Tags“.
    /// </summary>
    protected ITagEditorContext Context { get; } = context;

    /// <summary>
    /// Fehler in den Eingaben oder <c>null</c>.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    [NotifyCanExecuteChangedFor(nameof(StageCommand))]
    public partial string? ValidationMessage { get; set; }

    /// <summary>
    /// Ob ein Fehler vorliegt.
    /// </summary>
    public bool HasValidationMessage => ValidationMessage is not null;

    /// <summary>
    /// Für welche Dateien vorgemerkt wird.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StageCommand))]
    public partial TagScope Scope { get; set; }

    /// <summary>
    /// Die Dateien des Geltungsbereichs.
    /// </summary>
    protected IReadOnlyList<string> ScopeFiles => Scope == TagScope.AllAudio ? Context.AudioFiles : Context.SelectedFiles;

    /// <summary>
    /// Die Dateien, für die „Änderungen vormerken“ gilt (Standard: Geltungsbereich).
    /// </summary>
    protected virtual IReadOnlyList<string> StageFiles => ScopeFiles;

    /// <summary>
    /// Wird aufgerufen, wenn sich Markierung oder Vorschau geändert haben.
    /// </summary>
    public virtual void Refresh()
    {
        StageCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Berechnet Beispiel und Prüfung neu, wenn sich der Geltungsbereich ändert.
    /// </summary>
    /// <param name="value">Der neue Bereich.</param>
    partial void OnScopeChanged(TagScope value) => Refresh();

    /// <summary>
    /// Merkt die Änderung des Editors vor.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStage))]
    private void Stage()
    {
        var files = StageFiles;
        if (CreateChange(files) is { } change)
        {
            Context.Stage(change with { Files = files });
            OnStaged();
        }
    }

    /// <summary>
    /// Ob vorgemerkt werden kann: Dateien vorhanden, keine Fehler und eine sinnvolle Änderung.
    /// </summary>
    /// <returns><c>true</c>, wenn vorgemerkt werden kann.</returns>
    protected virtual bool CanStage() => StageFiles.Count > 0 && ValidationMessage is null;

    /// <summary>
    /// Erzeugt die Änderung für die Dateien.
    /// </summary>
    /// <param name="files">Die Dateien.</param>
    /// <returns>Die Änderung oder <c>null</c>, wenn nichts vorzumerken ist.</returns>
    protected abstract TagChange? CreateChange(IReadOnlyList<string> files);

    /// <summary>
    /// Wird nach dem Vormerken aufgerufen.
    /// </summary>
    protected virtual void OnStaged()
    {
    }
}
