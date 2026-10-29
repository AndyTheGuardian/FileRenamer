using FileRenamer.Core.Files;
using FileRenamer.Core.Naming;

namespace FileRenamer.Core.Rules;

/// <summary>
/// Eingabe eines Verfahrens für eine Datei: die Datei selbst und ihr Name an dieser Stelle der Kette.
/// </summary>
/// <param name="File">Die Datei mit ihren Metadaten.</param>
/// <param name="Name">Der (ggf. bereits veränderte) Name ohne ausgeblendete Erweiterung.</param>
public sealed record RuleInput(FileEntry File, SegmentedName Name);

/// <summary>
/// Ergebnis eines Verfahrens für eine Datei.
/// </summary>
/// <param name="Name">Der neue Name.</param>
/// <param name="Issues">Hinweise und Konflikte, die das Verfahren gemeldet hat.</param>
public sealed record RuleOutput(SegmentedName Name, IReadOnlyList<RenameIssue> Issues)
{
    /// <summary>
    /// Erzeugt ein Ergebnis mit geändertem Namen ohne Befunde.
    /// </summary>
    /// <param name="name">Der neue Name.</param>
    /// <returns>Das Ergebnis.</returns>
    public static RuleOutput Changed(SegmentedName name) => new(name, []);

    /// <summary>
    /// Erzeugt ein Ergebnis, das den Namen unverändert lässt.
    /// </summary>
    /// <param name="name">Der unveränderte Name.</param>
    /// <param name="issues">Optionale Befunde.</param>
    /// <returns>Das Ergebnis.</returns>
    public static RuleOutput Unchanged(SegmentedName name, params RenameIssue[] issues) => new(name, issues);
}
