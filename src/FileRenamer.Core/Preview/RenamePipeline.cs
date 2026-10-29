using FileRenamer.Core.Files;
using FileRenamer.Core.Naming;
using FileRenamer.Core.Rules;

namespace FileRenamer.Core.Preview;

/// <summary>
/// Wendet die Kette der Verfahren nacheinander auf alle Dateien an und erzeugt die Vorschau
/// inklusive Hinweisen und Konflikten. Die tatsächliche Umbenennung verwendet genau dieses Ergebnis.
/// </summary>
/// <param name="fileSystem">Dateisystem für die Konfliktprüfung.</param>
public sealed class RenamePipeline(IFileSystem fileSystem)
{
    private readonly ConflictDetector _conflictDetector = new(fileSystem);

    /// <summary>
    /// Erzeugt die Vorschau für alle Dateien (alle ausgewählt).
    /// </summary>
    /// <param name="files">Die Dateien.</param>
    /// <param name="chain">Die Verfahren in Kettenreihenfolge (deaktivierte werden übersprungen).</param>
    /// <param name="ignoreExtension">Ob die (letzte) Dateierweiterung ignoriert und ausgeblendet wird.</param>
    /// <returns>Das Vorschauergebnis.</returns>
    public PreviewResult CreatePreview(IReadOnlyList<FileEntry> files, IReadOnlyList<RenameRule> chain, bool ignoreExtension) =>
        CreatePreview(files, chain, ignoreExtension, files.Select(_ => true).ToList());

    /// <summary>
    /// Erzeugt die Vorschau. Nur ausgewählte Dateien durchlaufen die Kette (E53); abgewählte behalten
    /// ihren Namen, zählen für die Konfliktprüfung aber weiter als belegt (E54).
    /// </summary>
    /// <param name="files">Die Dateien.</param>
    /// <param name="chain">Die Verfahren in Kettenreihenfolge (deaktivierte werden übersprungen).</param>
    /// <param name="ignoreExtension">Ob die (letzte) Dateierweiterung ignoriert und ausgeblendet wird.</param>
    /// <param name="selection">Je Datei, ob sie ausgewählt ist (gleiche Reihenfolge wie <paramref name="files"/>).</param>
    /// <returns>Das Vorschauergebnis.</returns>
    public PreviewResult CreatePreview(
        IReadOnlyList<FileEntry> files, IReadOnlyList<RenameRule> chain, bool ignoreExtension, IReadOnlyList<bool> selection)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(selection.Count, files.Count, nameof(selection));

        var parts = files.Select(file => SplitName(file.FileName, ignoreExtension)).ToList();
        var names = parts.Select(part => SegmentedName.FromText(part.BaseName)).ToList();
        var issues = files.Select(_ => new List<RenameIssue>()).ToList();
        var active = Enumerable.Range(0, files.Count).Where(index => selection[index]).ToList();
        var steps = chain.Select(rule => ApplyStep(rule, files, active, names, issues)).ToList();

        var newFileNames = names.Select((name, index) => (files[index], name.Text + parts[index].Extension)).ToList();
        var conflicts = _conflictDetector.Detect(newFileNames);

        var previews = files.Select((file, index) => new FilePreview(
            file,
            parts[index].BaseName,
            names[index],
            parts[index].Extension,
            [.. issues[index], .. conflicts[index]])
        {
            IsSelected = selection[index],
        }).ToList();

        return new PreviewResult(previews, steps);
    }

    /// <summary>
    /// Wendet ein einzelnes Verfahren auf die ausgewählten Dateien an (sofern aktiviert und gültig)
    /// und aktualisiert deren Namen und Befunde.
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    /// <param name="files">Alle Dateien.</param>
    /// <param name="active">Indizes der ausgewählten Dateien.</param>
    /// <param name="names">Die aktuellen Namen aller Dateien; werden überschrieben.</param>
    /// <param name="issues">Die gesammelten Befunde je Datei; werden ergänzt.</param>
    /// <returns>Das Schrittergebnis.</returns>
    private static RuleStepResult ApplyStep(
        RenameRule rule, IReadOnlyList<FileEntry> files, IReadOnlyList<int> active, List<SegmentedName> names, List<List<RenameIssue>> issues)
    {
        var inputNames = active.Select(index => names[index].Text).ToList();
        if (!rule.IsEnabled)
        {
            return new RuleStepResult(rule.Id, inputNames, null);
        }

        var settingsError = rule.ValidateSettings();
        if (settingsError is not null)
        {
            return new RuleStepResult(rule.Id, inputNames, settingsError);
        }

        var outputs = rule.Apply(active.Select(index => new RuleInput(files[index], names[index])).ToList());
        for (var position = 0; position < outputs.Count; position++)
        {
            var index = active[position];
            names[index] = outputs[position].Name;
            issues[index].AddRange(outputs[position].Issues);
        }

        var firstConflict = outputs.SelectMany(output => output.Issues)
            .FirstOrDefault(issue => issue.Severity == IssueSeverity.Conflict);
        return new RuleStepResult(rule.Id, inputNames, firstConflict?.Message);
    }

    /// <summary>
    /// Trennt die letzte Erweiterung ab, wenn sie ignoriert werden soll. Namen wie „.bashrc“ gelten als ohne Erweiterung.
    /// </summary>
    /// <param name="fileName">Der Dateiname.</param>
    /// <param name="ignoreExtension">Ob die Erweiterung abgetrennt wird.</param>
    /// <returns>Basisname und abgetrennte Erweiterung.</returns>
    internal static (string BaseName, string Extension) SplitName(string fileName, bool ignoreExtension)
    {
        if (!ignoreExtension)
        {
            return (fileName, string.Empty);
        }

        var extension = Path.GetExtension(fileName);
        var baseName = fileName[..^extension.Length];
        return baseName.Length == 0 ? (fileName, string.Empty) : (baseName, extension);
    }
}
