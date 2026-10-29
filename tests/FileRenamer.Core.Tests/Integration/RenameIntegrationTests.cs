using System.Globalization;
using FileRenamer.Core.Execution;
using FileRenamer.Core.Files;
using FileRenamer.Core.Preview;
using FileRenamer.Core.Rules;

namespace FileRenamer.Core.Tests.Integration;

/// <summary>
/// Integrationstests mit echten Dateien aus <c>testdata/</c>: Für jedes Verfahren wird geprüft, dass
/// die Vorschau die erwarteten Namen liefert und die tatsächliche Umbenennung genau diese Namen erzeugt.
/// </summary>
public sealed class RenameIntegrationTests : IDisposable
{
    private readonly string _workDirectory = Path.Combine(Path.GetTempPath(), "fr-integration-" + Guid.NewGuid().ToString("N"));
    private readonly PhysicalFileSystem _fileSystem = new();

    /// <summary>
    /// Text ersetzen (alle Treffer) mit Kaskade auf „Text einfügen“.
    /// </summary>
    [Fact]
    public void ReplaceText_PreviewMatchesDisk()
    {
        RenameRule[] chain = [new ReplaceTextRule { SearchText = "IMG", Replacement = "Urlaub" }];

        AssertPreviewAndDisk("01-text-ersetzen", chain,
            ["Urlaub_0001.jpg", "Urlaub_0002.jpg", "Urlaub_0003.jpg", "Urlaub_Urlaub_test.txt"]);
    }

    /// <summary>
    /// Datum mit Uhrzeit vor dem Namen.
    /// </summary>
    [Fact]
    public void AppendDate_PreviewMatchesDisk()
    {
        var directory = CopyTestData("02-datum");
        foreach (var file in Directory.GetFiles(directory))
        {
            File.SetLastWriteTime(file, new DateTime(2026, 10, 12, 14, 30, 0));
        }

        RenameRule[] chain =
        [
            new AppendDateRule
            {
                Source = DateSource.ModificationDate, IncludeTime = true, TimeFormat = "HH-mm", Culture = CultureInfo.InvariantCulture,
            },
        ];

        AssertPreviewAndDisk(directory, chain, ["2026-10-12_14-30_bericht.docx", "2026-10-12_14-30_notiz.txt"]);
    }

    /// <summary>
    /// Zähler in natürlicher Namensreihenfolge.
    /// </summary>
    [Fact]
    public void AppendCounter_PreviewMatchesDisk()
    {
        RenameRule[] chain = [new AppendCounterRule { Start = 1, Digits = 2 }];

        AssertPreviewAndDisk("03-zaehler", chain, ["datei1-01.txt", "datei10-03.txt", "datei2-02.txt"]);
    }

    /// <summary>
    /// Ziffernkette verschieben: Datei ohne Ziffern bleibt, Überlauf, letzte Ziffernkette.
    /// </summary>
    [Fact]
    public void ShiftDigitRange_PreviewMatchesDisk()
    {
        RenameRule[] chain = [new ShiftDigitRangeRule { Offset = 10 }];

        AssertPreviewAndDisk("04-ziffernkette", chain,
            ["bericht.txt", "name-011.txt", "name-012.txt", "name-301-011.txt", "name-432-012.txt", "zahl-1005.txt"]);
    }

    /// <summary>
    /// Text einfügen vom Ende des Namens (Erweiterung ignoriert).
    /// </summary>
    [Fact]
    public void InsertText_PreviewMatchesDisk()
    {
        RenameRule[] chain = [new InsertTextRule { Text = "_final", Position = 0, FromEnd = true }];

        AssertPreviewAndDisk("05-text-einfuegen", chain, ["bericht_final.docx", "kurz_final.txt"]);
    }

    /// <summary>
    /// Ringtausch und reine Groß-/Kleinschreibungsänderung werden tatsächlich ausgeführt.
    /// </summary>
    [Fact]
    public void SwapAndCaseChange_PreviewMatchesDisk()
    {
        var directory = CopyTestData("06-konflikte");
        File.Delete(Path.Combine(directory, "vorhanden-1.txt"));
        File.Delete(Path.Combine(directory, "vorhanden-2.txt"));
        RenameRule[] chain =
        [
            new ReplaceTextRule { SearchText = @"^a(?=\.txt$)", Replacement = "#", UseRegex = true },
            new ReplaceTextRule { SearchText = @"^b(?=\.txt$)", Replacement = "a", UseRegex = true },
            new ReplaceTextRule { SearchText = "#", Replacement = "b" },
            new ReplaceTextRule { SearchText = "Foto.JPG", Replacement = "foto.jpg" },
        ];

        AssertPreviewAndDisk(directory, chain, ["a.txt", "b.txt", "foto.jpg"], ignoreExtension: false, expectedRenamed: 3);
        Assert.Equal("Testdatei b.txt", File.ReadAllText(Path.Combine(directory, "a.txt")).Trim());
    }

    /// <summary>
    /// Die letzte Umbenennung lässt sich auf der Platte vollständig widerrufen (E49).
    /// </summary>
    [Fact]
    public void Undo_RestoresOriginalNamesOnDisk()
    {
        var directory = CopyTestData("04-ziffernkette");
        var original = Directory.GetFiles(directory).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToList();
        var preview = new RenamePipeline(_fileSystem).CreatePreview(ReadFiles(directory), [new ShiftDigitRangeRule { Offset = 10 }], true);
        var executor = new FileRenameExecutor(_fileSystem);

        var renamed = executor.Execute(preview.CreateOperations());
        var undone = executor.Execute(renamed.CreateUndoOperations());

        Assert.True(undone.Success, undone.ErrorMessage);
        Assert.Equal(original, Directory.GetFiles(directory).Select(Path.GetFileName).Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// Kollision mit einer vorhandenen Datei sperrt das Umbenennen; auf der Platte ändert sich nichts.
    /// </summary>
    [Fact]
    public void ExistingTarget_BlocksRename()
    {
        var directory = CopyTestData("06-konflikte");
        var files = ReadFiles(directory).Where(file => file.FileName == "vorhanden-1.txt").ToList();

        var preview = new RenamePipeline(_fileSystem).CreatePreview(files, [new ShiftDigitRangeRule { Offset = 1 }], true);

        Assert.True(preview.Files[0].HasConflict);
        Assert.False(preview.CanRename);
    }

    /// <summary>
    /// Räumt das Arbeitsverzeichnis auf.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_workDirectory))
        {
            Directory.Delete(_workDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Prüft Vorschau und tatsächliche Umbenennung gegen die erwarteten Namen.
    /// </summary>
    /// <param name="testDataOrDirectory">Name des Testdatenordners oder bereits kopiertes Verzeichnis.</param>
    /// <param name="chain">Die Verfahren.</param>
    /// <param name="expectedNames">Erwartete Dateinamen (alphabetisch sortiert, ordinal).</param>
    /// <param name="ignoreExtension">Ob die Erweiterung ignoriert wird.</param>
    /// <param name="expectedRenamed">Erwartete Anzahl umbenannter Dateien oder <c>null</c> für „egal“.</param>
    private void AssertPreviewAndDisk(
        string testDataOrDirectory, RenameRule[] chain, string[] expectedNames, bool ignoreExtension = true, int? expectedRenamed = null)
    {
        var directory = Directory.Exists(testDataOrDirectory) ? testDataOrDirectory : CopyTestData(testDataOrDirectory);
        var preview = new RenamePipeline(_fileSystem).CreatePreview(ReadFiles(directory), chain, ignoreExtension);

        Assert.Equal(expectedNames, preview.Files.Select(file => file.NewFileName).Order(StringComparer.Ordinal));
        Assert.True(preview.CanRename, string.Join("; ", preview.Files.SelectMany(file => file.Issues).Select(i => i.Message)));

        var result = new FileRenameExecutor(_fileSystem).Execute(preview.CreateOperations());

        Assert.True(result.Success, result.ErrorMessage);
        if (expectedRenamed is { } renamed)
        {
            Assert.Equal(renamed, result.RenamedCount);
        }

        Assert.Equal(expectedNames, Directory.GetFiles(directory).Select(Path.GetFileName).Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// Kopiert einen Testdatenordner in ein frisches Arbeitsverzeichnis.
    /// </summary>
    /// <param name="name">Name des Ordners unter <c>testdata/</c>.</param>
    /// <returns>Pfad der Kopie.</returns>
    private string CopyTestData(string name)
    {
        var source = Path.Combine(AppContext.BaseDirectory, "testdata", name);
        var target = Path.Combine(_workDirectory, name);
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        }

        return target;
    }

    /// <summary>
    /// Liest alle Dateien eines Verzeichnisses.
    /// </summary>
    /// <param name="directory">Das Verzeichnis.</param>
    /// <returns>Die Dateibeschreibungen.</returns>
    private List<FileEntry> ReadFiles(string directory) =>
        _fileSystem.GetFilesInDirectory(directory).Select(_fileSystem.ReadFileEntry).ToList();
}
