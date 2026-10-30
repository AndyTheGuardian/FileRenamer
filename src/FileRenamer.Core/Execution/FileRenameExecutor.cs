using FileRenamer.Core.Files;
using FileRenamer.Core.Localization;

namespace FileRenamer.Core.Execution;

/// <summary>
/// Ein Umbenennungsauftrag.
/// </summary>
/// <param name="SourcePath">Bisheriger Pfad.</param>
/// <param name="TargetPath">Neuer Pfad.</param>
public sealed record RenameOperation(string SourcePath, string TargetPath)
{
    /// <summary>
    /// Liefert den Gegenauftrag, der diese Umbenennung rückgängig macht.
    /// </summary>
    /// <returns>Der umgekehrte Auftrag.</returns>
    public RenameOperation Invert() => new(TargetPath, SourcePath);
}

/// <summary>
/// Ergebnis einer Umbenennung.
/// </summary>
/// <param name="Success">Ob alle Dateien umbenannt wurden.</param>
/// <param name="ExecutedOperations">Tatsächlich ausgeführte Aufträge (leer bei Fehler, da zurückbenannt wird).</param>
/// <param name="ErrorMessage">Fehlermeldung oder <c>null</c>.</param>
/// <param name="RollbackErrors">Fehler beim Zurückbenennen (sollten leer sein).</param>
public sealed record RenameExecutionResult(
    bool Success, IReadOnlyList<RenameOperation> ExecutedOperations, string? ErrorMessage, IReadOnlyList<string> RollbackErrors)
{
    /// <summary>
    /// Anzahl umbenannter Dateien.
    /// </summary>
    public int RenamedCount => ExecutedOperations.Count;

    /// <summary>
    /// Liefert die Aufträge, mit denen sich diese Umbenennung widerrufen lässt.
    /// </summary>
    /// <returns>Die umgekehrten Aufträge.</returns>
    public IReadOnlyList<RenameOperation> CreateUndoOperations() =>
        ExecutedOperations.Select(operation => operation.Invert()).ToList();
}

/// <summary>
/// Führt Umbenennungen nach dem Alles-oder-nichts-Prinzip aus. Phase 1 benennt alle Dateien in
/// eindeutige temporäre Namen um, Phase 2 in die Zielnamen. Dadurch funktionieren Ringtausch
/// (a→b, b→a) und reine Änderungen der Groß-/Kleinschreibung. Bei einem Fehler wird alles zurückbenannt.
/// </summary>
/// <param name="fileSystem">Das Dateisystem.</param>
public sealed class FileRenameExecutor(IFileSystem fileSystem)
{
    /// <summary>
    /// Führt die Aufträge aus. Aufträge ohne Namensänderung werden übersprungen.
    /// </summary>
    /// <param name="operations">Die Aufträge.</param>
    /// <returns>Das Ergebnis.</returns>
    public RenameExecutionResult Execute(IReadOnlyList<RenameOperation> operations)
    {
        var pending = operations
            .Where(operation => !string.Equals(operation.SourcePath, operation.TargetPath, StringComparison.Ordinal))
            .ToList();
        var journal = new Stack<(string From, string To)>();

        try
        {
            var temporaryPaths = pending.Select(operation => CreateTemporaryPath(operation.SourcePath)).ToList();
            MoveAll(pending.Select((operation, index) => (operation.SourcePath, temporaryPaths[index])), journal);
            MoveAll(pending.Select((operation, index) => (temporaryPaths[index], operation.TargetPath)), journal);
            return new RenameExecutionResult(true, pending, null, []);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            var rollbackErrors = RollBack(journal);
            return new RenameExecutionResult(false, [], exception.Message, rollbackErrors);
        }
    }

    /// <summary>
    /// Führt eine Folge von Verschiebungen aus und protokolliert jede erfolgreiche im Journal.
    /// </summary>
    /// <param name="moves">Die Verschiebungen.</param>
    /// <param name="journal">Journal für die Rücknahme.</param>
    private void MoveAll(IEnumerable<(string From, string To)> moves, Stack<(string From, string To)> journal)
    {
        foreach (var (from, to) in moves)
        {
            fileSystem.MoveFile(from, to);
            journal.Push((from, to));
        }
    }

    /// <summary>
    /// Nimmt alle protokollierten Verschiebungen in umgekehrter Reihenfolge zurück.
    /// </summary>
    /// <param name="journal">Das Journal.</param>
    /// <returns>Meldungen zu fehlgeschlagenen Rücknahmen.</returns>
    private List<string> RollBack(Stack<(string From, string To)> journal)
    {
        var errors = new List<string>();
        while (journal.TryPop(out var move))
        {
            try
            {
                fileSystem.MoveFile(move.To, move.From);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                errors.Add(CoreText.RollbackFailed(move.To, move.From, exception.Message));
            }
        }

        return errors;
    }

    /// <summary>
    /// Erzeugt einen freien, eindeutigen temporären Pfad im selben Verzeichnis.
    /// </summary>
    /// <param name="sourcePath">Pfad der Ausgangsdatei.</param>
    /// <returns>Der temporäre Pfad.</returns>
    private string CreateTemporaryPath(string sourcePath)
    {
        var directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
        string candidate;
        do
        {
            candidate = Path.Combine(directory, $".filerenamer-{Guid.NewGuid():N}.tmp");
        }
        while (fileSystem.Exists(candidate));

        return candidate;
    }
}
