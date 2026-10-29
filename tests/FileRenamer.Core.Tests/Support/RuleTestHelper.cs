using FileRenamer.Core.Files;
using FileRenamer.Core.Naming;
using FileRenamer.Core.Rules;

namespace FileRenamer.Core.Tests.Support;

/// <summary>
/// Hilfsfunktionen, um Verfahren in Tests kompakt auf Namen anzuwenden.
/// </summary>
internal static class RuleTestHelper
{
    /// <summary>
    /// Ein festes Verzeichnis für Testpfade.
    /// </summary>
    public static readonly string Directory = Path.Combine(Path.GetTempPath(), "fr-test");

    /// <summary>
    /// Erzeugt eine Dateibeschreibung zu einem Namen.
    /// </summary>
    /// <param name="fileName">Dateiname.</param>
    /// <param name="created">Erstellungsdatum.</param>
    /// <param name="modified">Änderungsdatum.</param>
    /// <param name="size">Größe.</param>
    /// <returns>Die Dateibeschreibung.</returns>
    public static FileEntry File(string fileName, DateTime? created = null, DateTime? modified = null, long size = 0) =>
        new(Path.Combine(Directory, fileName), created, modified ?? new DateTime(2026, 1, 1), size);

    /// <summary>
    /// Wendet ein Verfahren auf einen einzelnen Namen an.
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    /// <param name="name">Der Name (wird als Datei- und Ausgangsname verwendet).</param>
    /// <returns>Das Ergebnis.</returns>
    public static RuleOutput ApplyTo(this RenameRule rule, string name) => rule.ApplyTo(File(name));

    /// <summary>
    /// Wendet ein Verfahren auf eine einzelne Datei an.
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    /// <param name="file">Die Datei.</param>
    /// <returns>Das Ergebnis.</returns>
    public static RuleOutput ApplyTo(this RenameRule rule, FileEntry file) =>
        rule.Apply([new RuleInput(file, SegmentedName.FromText(file.FileName))])[0];

    /// <summary>
    /// Wendet ein Verfahren auf mehrere Namen an und liefert die neuen Texte.
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    /// <param name="files">Die Dateien.</param>
    /// <returns>Die neuen Namen in Eingabereihenfolge.</returns>
    public static IReadOnlyList<string> ApplyToAll(this RenameRule rule, params FileEntry[] files) =>
        rule.Apply(files.Select(file => new RuleInput(file, SegmentedName.FromText(file.FileName))).ToList())
            .Select(output => output.Name.Text)
            .ToList();
}
