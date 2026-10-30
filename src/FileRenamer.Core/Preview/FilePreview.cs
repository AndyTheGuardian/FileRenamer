using FileRenamer.Core.Files;
using FileRenamer.Core.Naming;
using FileRenamer.Core.Rules;

namespace FileRenamer.Core.Preview;

/// <summary>
/// Vorschau einer Datei: ursprünglicher und neuer Name samt Herkunftsabschnitten und Befunden.
/// </summary>
/// <param name="File">Die Datei.</param>
/// <param name="OriginalDisplayName">Ursprünglicher Name, wie er angezeigt wird (ohne ausgeblendete Erweiterung).</param>
/// <param name="NewDisplayName">Neuer Name mit Abschnitten (ohne ausgeblendete Erweiterung).</param>
/// <param name="NewExtension">Neue ausgeblendete Erweiterung mit Abschnitten (leer, wenn die Erweiterung nicht ignoriert wird).</param>
/// <param name="OriginalExtension">Ursprüngliche ausgeblendete Erweiterung (z. B. „.jpg“) oder leer.</param>
/// <param name="Issues">Hinweise und Konflikte.</param>
public sealed record FilePreview(
    FileEntry File,
    string OriginalDisplayName,
    SegmentedName NewDisplayName,
    SegmentedName NewExtension,
    string OriginalExtension,
    IReadOnlyList<RenameIssue> Issues)
{
    /// <summary>
    /// Ob die Datei ausgewählt ist und damit die Kette durchläuft (E52).
    /// </summary>
    public bool IsSelected { get; init; } = true;

    /// <summary>
    /// Vollständiger neuer Dateiname inklusive Erweiterung.
    /// </summary>
    public string NewFileName => NewDisplayName.Text + NewExtension.Text;

    /// <summary>
    /// Ob sich die ausgeblendete Erweiterung geändert hat; sie wird dann in der Vorschau eingeblendet (E73).
    /// </summary>
    public bool IsExtensionChanged => !string.Equals(NewExtension.Text, OriginalExtension, StringComparison.Ordinal);

    /// <summary>
    /// Vollständiger neuer Pfad.
    /// </summary>
    public string TargetPath => Path.Combine(File.DirectoryPath, NewFileName);

    /// <summary>
    /// Ob sich der Name ändert (Groß-/Kleinschreibung zählt).
    /// </summary>
    public bool IsChanged => !string.Equals(NewFileName, File.FileName, StringComparison.Ordinal);

    /// <summary>
    /// Ob mindestens ein Konflikt vorliegt.
    /// </summary>
    public bool HasConflict => Issues.Any(issue => issue.Severity == IssueSeverity.Conflict);

    /// <summary>
    /// Ob mindestens ein Hinweis vorliegt.
    /// </summary>
    public bool HasHint => Issues.Any(issue => issue.Severity == IssueSeverity.Hint);
}
