using FileRenamer.Core.Execution;
using FileRenamer.Core.Rules;

namespace FileRenamer.Core.Preview;

/// <summary>
/// Ergebnis eines Kettenschritts: welche Namen das Verfahren als Eingabe hatte und ob es fehlerhaft ist.
/// </summary>
/// <param name="RuleId">Id des Verfahrens.</param>
/// <param name="InputNames">Namen an dieser Stelle der Kette (Grundlage für Eingabebegrenzungen).</param>
/// <param name="Error">Fehlermeldung für den Ketteneintrag oder <c>null</c>.</param>
public sealed record RuleStepResult(Guid RuleId, IReadOnlyList<string> InputNames, string? Error);

/// <summary>
/// Gesamtergebnis der Vorschau für alle Dateien und alle Kettenschritte.
/// </summary>
/// <param name="Files">Vorschau je Datei in Eingabereihenfolge.</param>
/// <param name="Steps">Ergebnis je Verfahren in Kettenreihenfolge.</param>
public sealed record PreviewResult(IReadOnlyList<FilePreview> Files, IReadOnlyList<RuleStepResult> Steps)
{
    /// <summary>
    /// Leeres Ergebnis ohne Dateien.
    /// </summary>
    public static PreviewResult Empty { get; } = new([], []);

    /// <summary>
    /// Anzahl der Dateien, deren Name sich ändert.
    /// </summary>
    public int ChangedCount => Files.Count(file => file.IsChanged);

    /// <summary>
    /// Anzahl der Dateien mit Konflikt.
    /// </summary>
    public int ConflictCount => Files.Count(file => file.HasConflict);

    /// <summary>
    /// Anzahl der Dateien mit Hinweis.
    /// </summary>
    public int HintCount => Files.Count(file => file.HasHint);

    /// <summary>
    /// Ob ein Konflikt oder ein fehlerhaftes Verfahren das Umbenennen sperrt.
    /// </summary>
    public bool HasBlockingIssues => ConflictCount > 0 || Steps.Any(step => step.Error is not null);

    /// <summary>
    /// Ob umbenannt werden darf: keine Sperre und mindestens eine Änderung.
    /// </summary>
    public bool CanRename => !HasBlockingIssues && ChangedCount > 0;

    /// <summary>
    /// Liefert das Ergebnis des Kettenschritts zu einem Verfahren.
    /// </summary>
    /// <param name="ruleId">Id des Verfahrens.</param>
    /// <returns>Das Schrittergebnis oder <c>null</c>.</returns>
    public RuleStepResult? FindStep(Guid ruleId) => Steps.FirstOrDefault(step => step.RuleId == ruleId);

    /// <summary>
    /// Erzeugt die Umbenennungsaufträge für alle geänderten Dateien.
    /// </summary>
    /// <returns>Die Aufträge.</returns>
    public IReadOnlyList<RenameOperation> CreateOperations() =>
        Files.Where(file => file.IsChanged)
            .Select(file => new RenameOperation(file.File.FullPath, file.TargetPath))
            .ToList();

    /// <summary>
    /// Liefert die erste Konfliktmeldung, die ein bestimmtes Verfahren ausgelöst hat.
    /// </summary>
    /// <param name="ruleId">Id des Verfahrens.</param>
    /// <returns>Die Meldung oder <c>null</c>.</returns>
    public string? FindFirstConflict(Guid ruleId) =>
        Files.SelectMany(file => file.Issues)
            .FirstOrDefault(issue => issue.RuleId == ruleId && issue.Severity == IssueSeverity.Conflict)?.Message;
}
