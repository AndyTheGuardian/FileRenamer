namespace FileRenamer.Core.Rules;

/// <summary>
/// Schweregrad eines Befunds in der Vorschau.
/// </summary>
public enum IssueSeverity
{
    /// <summary>Hinweis; Umbenennen bleibt möglich.</summary>
    Hint,

    /// <summary>Konflikt; Umbenennen ist gesperrt.</summary>
    Conflict,
}

/// <summary>
/// Ein Hinweis oder Konflikt zu einer Datei in der Vorschau.
/// </summary>
/// <param name="Severity">Schweregrad.</param>
/// <param name="Message">Verständliche Meldung für den Anwender.</param>
/// <param name="RuleId">Id des auslösenden Verfahrens oder <c>null</c> bei allgemeinen Prüfungen.</param>
public sealed record RenameIssue(IssueSeverity Severity, string Message, Guid? RuleId = null)
{
    /// <summary>
    /// Erzeugt einen Hinweis.
    /// </summary>
    /// <param name="message">Die Meldung.</param>
    /// <param name="ruleId">Id des auslösenden Verfahrens.</param>
    /// <returns>Der Hinweis.</returns>
    public static RenameIssue Hint(string message, Guid? ruleId = null) => new(IssueSeverity.Hint, message, ruleId);

    /// <summary>
    /// Erzeugt einen Konflikt.
    /// </summary>
    /// <param name="message">Die Meldung.</param>
    /// <param name="ruleId">Id des auslösenden Verfahrens.</param>
    /// <returns>Der Konflikt.</returns>
    public static RenameIssue Conflict(string message, Guid? ruleId = null) => new(IssueSeverity.Conflict, message, ruleId);
}
