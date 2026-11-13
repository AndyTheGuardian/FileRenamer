using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using FileRenamer.App.ViewModels.Tags;
using FileRenamer.Core.Tagging;

namespace FileRenamer.App.Views.Tags;

/// <summary>
/// Editor „Cover“. Der Code-Behind nimmt Bilder per Drag &amp; Drop auf der Ablagefläche an; die Ablage wird als
/// behandelt markiert, damit das Hauptfenster das Bild nicht als Datei in die Liste aufnimmt.
/// </summary>
public sealed partial class CoverView : UserControl
{
    /// <summary>
    /// Lädt die Oberfläche und registriert die Ablage.
    /// </summary>
    public CoverView()
    {
        InitializeComponent();
        DropZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        DropZone.AddHandler(DragDrop.DragLeaveEvent, (_, _) => DropZone.Classes.Remove("dragOver"));
        DropZone.AddHandler(DragDrop.DropEvent, OnDrop);
    }

    /// <summary>
    /// Zeigt das Kopieren-Symbol, wenn eine Bilddatei gezogen wird.
    /// </summary>
    /// <param name="sender">Die Ablagefläche.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var isImage = FindImage(e) is not null;
        e.DragEffects = isImage ? DragDropEffects.Copy : DragDropEffects.None;
        DropZone.Classes.Set("dragOver", isImage);
        e.Handled = true;
    }

    /// <summary>
    /// Fügt das abgelegte Bild ein.
    /// </summary>
    /// <param name="sender">Die Ablagefläche.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnDrop(object? sender, DragEventArgs e)
    {
        DropZone.Classes.Remove("dragOver");
        if (FindImage(e) is { } path && DataContext is CoverEditorViewModel editor)
        {
            editor.InsertFromFile(path);
        }

        e.Handled = true;
    }

    /// <summary>
    /// Liefert die erste gezogene Bilddatei.
    /// </summary>
    /// <param name="e">Ereignisdaten.</param>
    /// <returns>Der Pfad oder <c>null</c>.</returns>
    private static string? FindImage(DragEventArgs e) =>
        e.DataTransfer.TryGetFiles()?.Select(item => item.TryGetLocalPath()).OfType<string>()
            .FirstOrDefault(path => ImageFormats.Extensions.Contains(Path.GetExtension(path).ToLowerInvariant()));
}
