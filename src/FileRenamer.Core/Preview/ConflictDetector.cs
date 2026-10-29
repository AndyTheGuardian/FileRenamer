using FileRenamer.Core.Files;
using FileRenamer.Core.Rules;
using FileRenamer.Core.Validation;

namespace FileRenamer.Core.Preview;

/// <summary>
/// Erkennt Konflikte der Zielnamen: ungültige Namen, doppelte Zielnamen innerhalb der Auswahl
/// und Kollisionen mit bereits vorhandenen Dateien außerhalb der Auswahl.
/// </summary>
/// <param name="fileSystem">Dateisystem für Existenzprüfungen.</param>
public sealed class ConflictDetector(IFileSystem fileSystem)
{
    /// <summary>
    /// Ermittelt die Konflikte je Datei.
    /// </summary>
    /// <param name="files">Dateien mit ihren neuen Dateinamen.</param>
    /// <returns>Je Datei die Liste der Konflikte (gleiche Reihenfolge).</returns>
    public IReadOnlyList<IReadOnlyList<RenameIssue>> Detect(IReadOnlyList<(FileEntry File, string NewFileName)> files)
    {
        var comparer = fileSystem.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var targets = files.Select(file => Path.Combine(file.File.DirectoryPath, file.NewFileName)).ToList();
        var sources = new HashSet<string>(files.Select(file => file.File.FullPath), comparer);
        var duplicates = FindDuplicates(targets, comparer);

        return files.Select((file, index) => CollectIssues(file, targets[index], sources, duplicates)).ToList();
    }

    /// <summary>
    /// Sammelt alle Konflikte einer Datei.
    /// </summary>
    /// <param name="file">Die Datei mit neuem Namen.</param>
    /// <param name="target">Zielpfad.</param>
    /// <param name="sources">Pfade aller ausgewählten Dateien (werden beim Umbenennen frei).</param>
    /// <param name="duplicates">Mehrfach vorkommende Zielpfade.</param>
    /// <returns>Die Konflikte.</returns>
    private List<RenameIssue> CollectIssues(
        (FileEntry File, string NewFileName) file, string target, HashSet<string> sources, HashSet<string> duplicates)
    {
        var issues = new List<RenameIssue>();
        var isChanged = !string.Equals(file.File.FileName, file.NewFileName, StringComparison.Ordinal);

        if (isChanged && FileNameValidator.Validate(file.NewFileName) is { } invalid)
        {
            issues.Add(RenameIssue.Conflict(invalid));
        }

        if (duplicates.Contains(target))
        {
            issues.Add(RenameIssue.Conflict("Der Zielname kommt in der Auswahl mehrfach vor."));
        }
        else if (isChanged && !sources.Contains(target) && fileSystem.Exists(target))
        {
            issues.Add(RenameIssue.Conflict("Eine Datei oder ein Ordner mit diesem Namen existiert bereits."));
        }

        return issues;
    }

    /// <summary>
    /// Ermittelt Zielpfade, die mehrfach vorkommen.
    /// </summary>
    /// <param name="targets">Alle Zielpfade.</param>
    /// <param name="comparer">Vergleich passend zur Groß-/Kleinschreibung des Dateisystems.</param>
    /// <returns>Die mehrfach vorkommenden Pfade.</returns>
    private static HashSet<string> FindDuplicates(IEnumerable<string> targets, StringComparer comparer) =>
        new(targets.GroupBy(target => target, comparer).Where(group => group.Count() > 1).Select(group => group.Key), comparer);
}
