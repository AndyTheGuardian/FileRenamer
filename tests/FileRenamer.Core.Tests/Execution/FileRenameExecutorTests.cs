using FileRenamer.Core.Execution;
using FileRenamer.Core.Tests.Support;

namespace FileRenamer.Core.Tests.Execution;

/// <summary>
/// Tests für die zweiphasige Umbenennung mit Rücknahme.
/// </summary>
public sealed class FileRenameExecutorTests
{
    private readonly InMemoryFileSystem _fileSystem = new();

    /// <summary>
    /// Ein Ringtausch (a↔b) gelingt dank temporärer Namen.
    /// </summary>
    [Fact]
    public void Execute_Swap_Succeeds()
    {
        var a = Add("a.txt");
        var b = Add("b.txt");

        var result = new FileRenameExecutor(_fileSystem).Execute([new(a, b), new(b, a)]);

        Assert.True(result.Success);
        Assert.Equal(2, result.RenamedCount);
        Assert.Equal(2, _fileSystem.Paths.Count);
    }

    /// <summary>
    /// Eine reine Änderung der Groß-/Kleinschreibung gelingt auch ohne Unterscheidung im Dateisystem.
    /// </summary>
    [Fact]
    public void Execute_CaseOnlyRename_Succeeds()
    {
        var source = Add("Foto.JPG");
        var target = Path.Combine(RuleTestHelper.Directory, "foto.jpg");

        var result = new FileRenameExecutor(_fileSystem).Execute([new(source, target)]);

        Assert.True(result.Success);
        Assert.Equal(target, Assert.Single(_fileSystem.Paths));
    }

    /// <summary>
    /// Schlägt eine Umbenennung fehl, werden alle bereits umbenannten Dateien zurückbenannt.
    /// </summary>
    [Fact]
    public void Execute_Failure_RollsBackEverything()
    {
        var first = Add("eins.txt");
        var second = Add("zwei.txt");
        _fileSystem.FailWhenTarget = target => target.EndsWith("ZWEI.txt", StringComparison.Ordinal);

        var result = new FileRenameExecutor(_fileSystem).Execute(
        [
            new(first, Path.Combine(RuleTestHelper.Directory, "EINS.txt")),
            new(second, Path.Combine(RuleTestHelper.Directory, "ZWEI.txt")),
        ]);

        Assert.False(result.Success);
        Assert.Equal(0, result.RenamedCount);
        Assert.Empty(result.RollbackErrors);
        Assert.Equal([first, second], _fileSystem.Paths.Order());
    }

    /// <summary>
    /// Die Gegenaufträge einer erfolgreichen Umbenennung stellen die ursprünglichen Namen wieder her (E49),
    /// auch bei einem Ringtausch.
    /// </summary>
    [Fact]
    public void CreateUndoOperations_RestoresOriginalNames()
    {
        var a = Add("a.txt");
        var b = Add("b.txt");
        var c = Add("c.txt");
        var executor = new FileRenameExecutor(_fileSystem);
        var renamed = executor.Execute([new(a, b), new(b, a), new(c, Path.Combine(RuleTestHelper.Directory, "neu.txt"))]);

        var undone = executor.Execute(renamed.CreateUndoOperations());

        Assert.True(undone.Success);
        Assert.Equal(3, undone.RenamedCount);
        Assert.Equal([a, b, c], _fileSystem.Paths.Order());
    }

    /// <summary>
    /// Ist ein ursprünglicher Name inzwischen belegt, schlägt das Widerrufen fehl und ändert nichts.
    /// </summary>
    [Fact]
    public void CreateUndoOperations_OriginalNameTaken_FailsWithoutChanges()
    {
        var source = Add("alt.txt");
        var target = Path.Combine(RuleTestHelper.Directory, "neu.txt");
        var executor = new FileRenameExecutor(_fileSystem);
        var renamed = executor.Execute([new(source, target)]);
        Add("alt.txt");

        var undone = executor.Execute(renamed.CreateUndoOperations());

        Assert.False(undone.Success);
        Assert.Equal([source, target], _fileSystem.Paths.Order());
    }

    /// <summary>
    /// Aufträge ohne Änderung werden übersprungen.
    /// </summary>
    [Fact]
    public void Execute_UnchangedOperations_AreSkipped()
    {
        var path = Add("gleich.txt");

        var result = new FileRenameExecutor(_fileSystem).Execute([new(path, path)]);

        Assert.True(result.Success);
        Assert.Equal(0, result.RenamedCount);
    }

    /// <summary>
    /// Legt eine Datei im Testverzeichnis an.
    /// </summary>
    /// <param name="name">Dateiname.</param>
    /// <returns>Der Pfad.</returns>
    private string Add(string name) => _fileSystem.Add(Path.Combine(RuleTestHelper.Directory, name)).FullPath;
}
