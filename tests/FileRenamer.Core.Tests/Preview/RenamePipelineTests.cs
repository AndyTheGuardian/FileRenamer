using FileRenamer.Core.Files;
using FileRenamer.Core.Preview;
using FileRenamer.Core.Rules;
using FileRenamer.Core.Tests.Support;

namespace FileRenamer.Core.Tests.Preview;

/// <summary>
/// Tests für die Vorschau: Kaskade, Erweiterung, Deaktivierung, Schrittinformationen und Konflikte.
/// </summary>
public sealed class RenamePipelineTests
{
    private readonly InMemoryFileSystem _fileSystem = new();

    /// <summary>
    /// Die Verfahren werden in Kettenreihenfolge nacheinander angewandt (kaskadierend).
    /// </summary>
    [Fact]
    public void CreatePreview_AppliesRulesInOrder()
    {
        var files = AddFiles("IMG_0001.jpg", "IMG_0002.jpg");
        RenameRule[] chain =
        [
            new ReplaceTextRule { SearchText = "IMG", Replacement = "Urlaub" },
            new ShiftDigitRangeRule { Offset = 10 },
            new InsertTextRule { Text = "x-" },
        ];

        var result = CreatePipeline().CreatePreview(files, chain, ignoreExtension: true);

        Assert.Equal(["x-Urlaub_0011.jpg", "x-Urlaub_0012.jpg"], result.Files.Select(file => file.NewFileName));
        Assert.True(result.CanRename);
    }

    /// <summary>
    /// Wird die Erweiterung nicht ignoriert, wirken die Verfahren auch auf sie.
    /// </summary>
    [Fact]
    public void CreatePreview_ExtensionNotIgnored_RulesAffectExtension()
    {
        var files = AddFiles("a.jpg");
        RenameRule[] chain = [new ReplaceTextRule { SearchText = "jpg", Replacement = "jpeg" }];

        Assert.Equal("a.jpeg", CreatePipeline().CreatePreview(files, chain, ignoreExtension: false).Files[0].NewFileName);
        Assert.Equal("a.jpg", CreatePipeline().CreatePreview(files, chain, ignoreExtension: true).Files[0].NewFileName);
    }

    /// <summary>
    /// Bei ignorierter Erweiterung wird nur die letzte Endung abgetrennt und nicht angezeigt.
    /// </summary>
    [Fact]
    public void CreatePreview_IgnoreExtension_HidesOnlyLastExtension()
    {
        var preview = CreatePipeline().CreatePreview(AddFiles("archiv.tar.gz"), [], ignoreExtension: true).Files[0];

        Assert.Equal("archiv.tar", preview.OriginalDisplayName);
        Assert.Equal(".gz", preview.OriginalExtension);
        Assert.False(preview.IsExtensionChanged);
    }

    /// <summary>
    /// Deaktivierte Verfahren werden übersprungen (Rückgängig in der Vorschau).
    /// </summary>
    [Fact]
    public void CreatePreview_DisabledRule_IsSkipped()
    {
        var files = AddFiles("a.txt");
        RenameRule[] chain = [new InsertTextRule { Text = "x", IsEnabled = false }];

        var result = CreatePipeline().CreatePreview(files, chain, ignoreExtension: true);

        Assert.Equal("a.txt", result.Files[0].NewFileName);
        Assert.False(result.CanRename);
    }

    /// <summary>
    /// Die Eingabenamen je Schritt spiegeln die vorherigen Verfahren wider (Grundlage für E41).
    /// </summary>
    [Fact]
    public void CreatePreview_StepInputNames_ReflectPreviousRules()
    {
        var files = AddFiles("bild.jpg");
        var insertDigits = new InsertTextRule { Text = "-1-2", FromEnd = true };
        var shift = new ShiftDigitRangeRule();

        var result = CreatePipeline().CreatePreview(files, [insertDigits, shift], ignoreExtension: true);

        Assert.Equal(["bild"], result.FindStep(insertDigits.Id)!.InputNames);
        Assert.Equal(["bild-1-2"], result.FindStep(shift.Id)!.InputNames);
        Assert.Equal(2, ShiftDigitRangeRule.GetMaxRunNumber(result.FindStep(shift.Id)!.InputNames));
    }

    /// <summary>
    /// Ein Verfahren mit ungültigen Parametern wird nicht angewandt und sperrt das Umbenennen.
    /// </summary>
    [Fact]
    public void CreatePreview_InvalidSettings_BlocksRename()
    {
        var files = AddFiles("a.txt");
        var rule = new ReplaceTextRule { SearchText = "(", UseRegex = true };

        var result = CreatePipeline().CreatePreview(files, [rule], ignoreExtension: true);

        Assert.NotNull(result.FindStep(rule.Id)!.Error);
        Assert.False(result.CanRename);
    }

    /// <summary>
    /// Eine nachträglich ungültige Ziffernketten-Nr. markiert den Schritt als fehlerhaft und die Zeile als Konflikt (E40).
    /// </summary>
    [Fact]
    public void CreatePreview_RunNumberNotAvailable_MarksStepAndRow()
    {
        var files = AddFiles("a-1-2.txt", "b-3.txt");
        var rule = new ShiftDigitRangeRule { UseLastRun = false, RunNumber = 2 };

        var result = CreatePipeline().CreatePreview(files, [rule], ignoreExtension: true);

        Assert.NotNull(result.FindStep(rule.Id)!.Error);
        Assert.False(result.Files[0].HasConflict);
        Assert.True(result.Files[1].HasConflict);
        Assert.False(result.CanRename);
    }

    /// <summary>
    /// Doppelte Zielnamen sind Konflikte.
    /// </summary>
    [Fact]
    public void CreatePreview_DuplicateTargets_AreConflicts()
    {
        var files = AddFiles("a1.txt", "a2.txt");
        RenameRule[] chain = [new ReplaceTextRule { SearchText = "[0-9]", Replacement = "", UseRegex = true }];

        var result = CreatePipeline().CreatePreview(files, chain, ignoreExtension: true);

        Assert.All(result.Files, file => Assert.True(file.HasConflict));
        Assert.Equal(2, result.ConflictCount);
    }

    /// <summary>
    /// Ein Zielname, der als fremde Datei existiert, ist ein Konflikt; ein Ringtausch innerhalb der Auswahl nicht.
    /// </summary>
    [Fact]
    public void CreatePreview_ExistingFileOutsideSelection_IsConflict_SwapIsNot()
    {
        _fileSystem.Add(Path.Combine(RuleTestHelper.Directory, "vorhanden-2.txt"));
        var files = AddFiles("vorhanden-1.txt");
        RenameRule[] chain = [new ShiftDigitRangeRule { Offset = 1 }];

        Assert.True(CreatePipeline().CreatePreview(files, chain, ignoreExtension: true).Files[0].HasConflict);

        var swap = AddFiles("a.txt", "b.txt");
        RenameRule[] swapChain =
        [
            new ReplaceTextRule { SearchText = "a", Replacement = "#" },
            new ReplaceTextRule { SearchText = "b", Replacement = "a" },
            new ReplaceTextRule { SearchText = "#", Replacement = "b" },
        ];
        var swapResult = CreatePipeline().CreatePreview(swap, swapChain, ignoreExtension: true);

        Assert.Equal(["b.txt", "a.txt"], swapResult.Files.Select(file => file.NewFileName));
        Assert.True(swapResult.CanRename);
    }

    /// <summary>
    /// Abgewählte Dateien durchlaufen die Kette nicht; Ausgewählte werden lückenlos nummeriert (E52, E53).
    /// </summary>
    [Fact]
    public void CreatePreview_UnselectedFiles_AreExcludedFromChain()
    {
        var files = AddFiles("a.txt", "b.txt", "c.txt");

        var result = CreatePipeline().CreatePreview(files, [new AppendCounterRule()], true, [true, false, true]);

        Assert.Equal(["a-001.txt", "b.txt", "c-002.txt"], result.Files.Select(file => file.NewFileName));
        Assert.False(result.Files[1].IsSelected);
        Assert.Equal(2, result.ChangedCount);
        Assert.Equal(2, result.CreateOperations().Count);
    }

    /// <summary>
    /// Die erlaubte Ziffernketten-Nr. berücksichtigt nur ausgewählte Dateien (E53).
    /// </summary>
    [Fact]
    public void CreatePreview_UnselectedFiles_IgnoredForRunNumberLimit()
    {
        var files = AddFiles("a-1-2.txt", "b-3.txt");
        var rule = new ShiftDigitRangeRule { UseLastRun = false, RunNumber = 2 };

        var result = CreatePipeline().CreatePreview(files, [rule], true, [true, false]);

        Assert.Equal(["a-1-2"], result.FindStep(rule.Id)!.InputNames);
        Assert.Null(result.FindStep(rule.Id)!.Error);
        Assert.True(result.CanRename);
    }

    /// <summary>
    /// Der Name einer abgewählten Datei bleibt belegt: Ein ausgewählter Zielname, der ihn trifft, ist ein Konflikt (E54).
    /// </summary>
    [Fact]
    public void CreatePreview_UnselectedName_StillBlocksTarget()
    {
        var files = AddFiles("x1.txt", "x2.txt");
        RenameRule[] chain = [new ShiftDigitRangeRule { Offset = 1 }];

        var result = CreatePipeline().CreatePreview(files, chain, true, [true, false]);

        Assert.Equal("x2.txt", result.Files[0].NewFileName);
        Assert.True(result.Files[0].HasConflict);
        Assert.False(result.CanRename);
    }

    /// <summary>
    /// Die Herkunftsabschnitte stehen für die farbige Hervorhebung zur Verfügung.
    /// </summary>
    [Fact]
    public void CreatePreview_SegmentsCarryRuleIds()
    {
        var replace = new ReplaceTextRule { SearchText = "IMG", Replacement = "Urlaub" };

        var name = CreatePipeline().CreatePreview(AddFiles("IMG_1.jpg"), [replace], ignoreExtension: true).Files[0].NewDisplayName;

        Assert.Equal("Urlaub", name.Segments[0].Text);
        Assert.Equal(replace.Id, name.Segments[0].GetHighlightSource(null));
        Assert.True(name.Segments[1].IsOriginal);
    }

    /// <summary>
    /// Legt Dateien im Testverzeichnis an.
    /// </summary>
    /// <param name="names">Dateinamen.</param>
    /// <returns>Die Dateien.</returns>
    private List<FileEntry> AddFiles(params string[] names) =>
        names.Select(name => _fileSystem.Add(Path.Combine(RuleTestHelper.Directory, name))).ToList();

    /// <summary>
    /// Erzeugt die Pipeline auf dem Testdateisystem.
    /// </summary>
    /// <returns>Die Pipeline.</returns>
    private RenamePipeline CreatePipeline() => new(_fileSystem);
}
