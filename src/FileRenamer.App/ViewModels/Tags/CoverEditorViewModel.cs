using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileRenamer.App.Localization;
using FileRenamer.Core.Tagging;
using FileRenamer.Core.Tagging.Changes;

namespace FileRenamer.App.ViewModels.Tags;

/// <summary>
/// Woher ein einzufügendes Cover stammt.
/// </summary>
public enum CoverSource
{
    /// <summary>Gewählte Bilddatei (gilt für alle Dateien des Geltungsbereichs).</summary>
    File,

    /// <summary>Bild aus dem Ordner jeder Audiodatei (cover, folder, front).</summary>
    Folder,
}

/// <summary>
/// Kachel eines eingebetteten Bildes mit Vorschaubild, Typ und Angaben zu Format, Maßen und Größe.
/// </summary>
public sealed partial class CoverTileViewModel : ObservableObject
{
    /// <summary>
    /// Erzeugt die Kachel; nicht lesbare Bilder erhalten kein Vorschaubild.
    /// </summary>
    /// <param name="cover">Das Bild.</param>
    public CoverTileViewModel(CoverImage cover)
    {
        Cover = cover;
        Thumbnail = TryDecode(cover.Data);
        var format = cover.Extension.TrimStart('.').ToUpperInvariant().Replace("JPG", "JPEG", StringComparison.Ordinal);
        var size = Thumbnail is { } bitmap ? $" · {bitmap.PixelSize.Width} × {bitmap.PixelSize.Height}" : string.Empty;
        Meta = $"{format}{size} · {Math.Max(1, cover.Size / 1024).ToString(CultureInfo.CurrentCulture)} KB";
    }

    /// <summary>
    /// Das Bild.
    /// </summary>
    public CoverImage Cover { get; }

    /// <summary>
    /// Das Vorschaubild oder <c>null</c>.
    /// </summary>
    public Bitmap? Thumbnail { get; }

    /// <summary>
    /// Bildtyp in Worten.
    /// </summary>
    public string Caption => CoverEditorViewModel.TypeName(Cover.Type);

    /// <summary>
    /// Format, Maße und Größe.
    /// </summary>
    public string Meta { get; }

    /// <summary>
    /// Ob die Kachel gewählt ist.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// Dekodiert Bilddaten.
    /// </summary>
    /// <param name="data">Die Daten.</param>
    /// <returns>Das Bild oder <c>null</c>.</returns>
    public static Bitmap? TryDecode(byte[] data)
    {
        try
        {
            using var stream = new MemoryStream(data);
            return new Bitmap(stream);
        }
        catch (Exception)
        {
            // Beschädigte oder nicht unterstützte Bilder werden ohne Vorschau angezeigt.
            return null;
        }
    }
}

/// <summary>
/// Editor „Cover“: zeigt die Bilder der ersten markierten Datei, fügt Bilder ein (Datei, Drag &amp; Drop oder aus dem
/// Ordner, optional verkleinert), entfernt den gewählten Bildtyp und extrahiert Bilder als Dateien (E117, E127).
/// Einfügen und Entfernen werden vorgemerkt; Extrahieren schreibt sofort und nutzt die Bilder, wie sie in den Dateien stehen.
/// </summary>
public sealed partial class CoverEditorViewModel : TagEditorViewModel
{
    private static readonly string[] FolderImageNames = ["cover", "folder", "front"];

    /// <summary>
    /// Erzeugt den Editor. Das Verkleinern beim Einfügen stellen die Einstellungen ein (E157).
    /// </summary>
    /// <param name="context">Der Bereich „Tags“.</param>
    public CoverEditorViewModel(ITagEditorContext context)
        : base(TagFunction.Cover, context)
    {
        Refresh();
    }

    /// <summary>
    /// Die wählbaren Bildtypen.
    /// </summary>
    public IReadOnlyList<CoverType> Types { get; } = Enum.GetValues<CoverType>();

    /// <summary>
    /// Kacheln der ersten markierten Datei.
    /// </summary>
    public ObservableCollection<CoverTileViewModel> Tiles { get; } = [];

    /// <summary>
    /// Name der Datei, deren Bilder gezeigt werden.
    /// </summary>
    [ObservableProperty]
    public partial string? FocusFileName { get; set; }

    /// <summary>
    /// Die gewählte Kachel.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedCommand))]
    public partial CoverTileViewModel? SelectedTile { get; set; }

    /// <summary>
    /// Quelle beim Einfügen.
    /// </summary>
    [ObservableProperty]
    public partial CoverSource Source { get; set; }

    /// <summary>
    /// Bildtyp beim Einfügen.
    /// </summary>
    [ObservableProperty]
    public partial CoverType InsertType { get; set; }

    /// <summary>
    /// Ob ein Bild desselben Typs ersetzt wird.
    /// </summary>
    [ObservableProperty]
    public partial bool ReplaceSameType { get; set; } = true;

    /// <summary>
    /// Ob beim Extrahieren alle Bildtypen gespeichert werden (sonst nur der gewählte).
    /// </summary>
    [ObservableProperty]
    public partial bool ExtractAllTypes { get; set; }

    /// <summary>
    /// Ob in einen gewählten Ordner extrahiert wird (sonst in den Ordner der Datei).
    /// </summary>
    [ObservableProperty]
    public partial bool ExtractToChosenFolder { get; set; }

    /// <summary>
    /// Dateiname beim Extrahieren (ohne Endung, mit Platzhaltern).
    /// </summary>
    [ObservableProperty]
    public partial string ExtractFileName { get; set; } = "cover";

    /// <summary>
    /// Ob gleiche Bilder nur einmal gespeichert werden.
    /// </summary>
    [ObservableProperty]
    public partial bool SkipDuplicates { get; set; } = true;

    /// <summary>
    /// Beispiel für den Zielpfad beim Extrahieren.
    /// </summary>
    [ObservableProperty]
    public partial string? ExtractExample { get; set; }

    /// <summary>
    /// Liefert den Namen eines Bildtyps.
    /// </summary>
    /// <param name="type">Der Typ.</param>
    /// <returns>Der Name.</returns>
    public static string TypeName(CoverType type) => Loc.T("CoverType." + type);

    /// <summary>
    /// Zeigt die Bilder der ersten markierten Datei (nach den vorgemerkten Änderungen).
    /// </summary>
    public override void Refresh()
    {
        var focus = Context.SelectedFiles.FirstOrDefault();
        FocusFileName = focus is null ? null : Path.GetFileName(focus);
        var selectedType = SelectedTile?.Cover.Type;
        Tiles.Clear();
        foreach (var cover in focus is null ? [] : Context.CurrentTags(focus).Covers)
        {
            Tiles.Add(new CoverTileViewModel(cover));
        }

        SelectTile(Tiles.FirstOrDefault(tile => tile.Cover.Type == selectedType) ?? Tiles.FirstOrDefault());
        UpdateExtractExample();
        InsertCommand.NotifyCanExecuteChanged();
        ExtractCommand.NotifyCanExecuteChanged();
        RemoveSelectedCommand.NotifyCanExecuteChanged();
        base.Refresh();
    }

    /// <summary>
    /// Wählt eine Kachel.
    /// </summary>
    /// <param name="tile">Die Kachel oder <c>null</c>.</param>
    [RelayCommand]
    private void SelectTile(CoverTileViewModel? tile)
    {
        foreach (var existing in Tiles)
        {
            existing.IsSelected = existing == tile;
        }

        SelectedTile = tile;
    }

    /// <summary>
    /// Fügt ein Bild ein: aus einer gewählten Datei oder aus dem Ordner jeder Audiodatei.
    /// </summary>
    /// <returns>Ein Task.</returns>
    [RelayCommand(CanExecute = nameof(CanInsert))]
    private async Task InsertAsync()
    {
        if (Source == CoverSource.Folder)
        {
            StageImages(ScopeFiles.Select(path => (path, image: FindFolderImage(path))).Where(pair => pair.image is not null)
                .ToDictionary(pair => pair.path, pair => pair.image!));
            return;
        }

        var picked = await Context.Dialogs.PickFileAsync(Loc.T("Cover.PickTitle"), Loc.T("Cover.ImageFilter"), ImageFormats.Extensions);
        if (picked is not null)
        {
            InsertFromFile(picked);
        }
    }

    /// <summary>
    /// Fügt ein Bild aus einer Datei für alle Dateien des Geltungsbereichs ein (auch per Drag &amp; Drop).
    /// </summary>
    /// <param name="imagePath">Pfad der Bilddatei.</param>
    public void InsertFromFile(string imagePath)
    {
        if (CanInsert() && LoadImage(imagePath) is { } image)
        {
            StageImages(ScopeFiles.ToDictionary(path => path, _ => image));
        }
    }

    /// <summary>
    /// Ob eingefügt werden kann.
    /// </summary>
    /// <returns><c>true</c> mit Dateien und gültigem Höchstmaß.</returns>
    private bool CanInsert() => ScopeFiles.Count > 0 && ValidationMessage is null;

    /// <summary>
    /// Entfernt den gewählten Bildtyp aus den Dateien des Geltungsbereichs.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRemoveSelected))]
    private void RemoveSelected()
    {
        Context.Stage(new RemoveCoverChange(SelectedTile!.Cover.Type) { Files = ScopeFiles });
    }

    /// <summary>
    /// Ob entfernt werden kann.
    /// </summary>
    /// <returns><c>true</c> mit gewählter Kachel.</returns>
    private bool CanRemoveSelected() => SelectedTile is not null && ScopeFiles.Count > 0;

    /// <summary>
    /// Speichert die Bilder als Dateien und meldet das Ergebnis.
    /// </summary>
    /// <returns>Ein Task.</returns>
    [RelayCommand(CanExecute = nameof(CanExtract))]
    private async Task ExtractAsync()
    {
        var folder = ExtractToChosenFolder ? await Context.Dialogs.PickFolderAsync() : null;
        if (ExtractToChosenFolder && folder is null)
        {
            return;
        }

        CoverType? type = ExtractAllTypes ? null : SelectedTile?.Cover.Type ?? CoverType.FrontCover;
        var options = new CoverExportOptions(type, folder, ExtractFileName, SkipDuplicates);
        var result = CoverExporter.Export(ScopeFiles.Select(path => (path, Context.DiskTags(path))), options);
        await Context.Dialogs.ShowMessageAsync(Loc.T("Cover.ExtractTitle"), TagAreaViewModel.DescribeExport(result));
    }

    /// <summary>
    /// Ob extrahiert werden kann.
    /// </summary>
    /// <returns><c>true</c> mit Dateien.</returns>
    private bool CanExtract() => ScopeFiles.Count > 0;

    /// <summary>
    /// Nicht verwendet; der Editor merkt über eigene Befehle vor.
    /// </summary>
    /// <param name="files">Die Dateien.</param>
    /// <returns><c>null</c>.</returns>
    protected override TagChange? CreateChange(IReadOnlyList<string> files) => null;

    /// <summary>Dateiname geändert.</summary>
    /// <param name="value">Der Wert.</param>
    partial void OnExtractFileNameChanged(string value) => UpdateExtractExample();

    /// <summary>Ziel geändert.</summary>
    /// <param name="value">Der Wert.</param>
    partial void OnExtractToChosenFolderChanged(bool value) => UpdateExtractExample();

    /// <summary>
    /// Merkt das Einfügen vor.
    /// </summary>
    /// <param name="images">Pfad der Audiodatei → Bild.</param>
    private void StageImages(Dictionary<string, CoverImage> images) =>
        Context.Stage(new AddCoverChange(images, ReplaceSameType) { Files = ScopeFiles });

    /// <summary>
    /// Lädt eine Bilddatei, setzt den Typ und verkleinert sie bei Bedarf.
    /// </summary>
    /// <param name="path">Pfad der Bilddatei.</param>
    /// <returns>Das Bild oder <c>null</c>, wenn die Datei nicht lesbar ist.</returns>
    private CoverImage? LoadImage(string path)
    {
        try
        {
            var image = new CoverImage(InsertType, ImageFormats.MimeTypeFor(path), File.ReadAllBytes(path));
            return Context.Library.ShrinkCovers ? Context.Images.Shrink(image, Context.Library.CoverMaxPixels) : image;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Sucht im Ordner einer Audiodatei nach cover.*, folder.* oder front.*.
    /// </summary>
    /// <param name="audioPath">Pfad der Audiodatei.</param>
    /// <returns>Das Bild oder <c>null</c>.</returns>
    private CoverImage? FindFolderImage(string audioPath)
    {
        var folder = Path.GetDirectoryName(audioPath) ?? string.Empty;
        var candidate = FolderImageNames
            .SelectMany(name => ImageFormats.Extensions.Select(extension => Path.Combine(folder, name + extension)))
            .FirstOrDefault(File.Exists);
        return candidate is null ? null : LoadImage(candidate);
    }


    /// <summary>
    /// Bildet das Beispiel für den Zielpfad.
    /// </summary>
    private void UpdateExtractExample()
    {
        var focus = Context.SelectedFiles.FirstOrDefault();
        if (focus is null)
        {
            ExtractExample = null;
            return;
        }

        var type = SelectedTile?.Cover.Type ?? CoverType.FrontCover;
        var name = CoverExporter.CreateFileName(ExtractFileName, focus, Context.DiskTags(focus), type);
        var folder = ExtractToChosenFolder ? "…" : Path.GetFileName(Path.GetDirectoryName(focus));
        ExtractExample = Path.Combine(folder ?? string.Empty, name + (SelectedTile?.Cover.Extension ?? ".jpg"));
    }
}
