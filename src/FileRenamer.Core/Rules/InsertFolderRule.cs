using FileRenamer.Core.Localization;
using FileRenamer.Core.Files;
using FileRenamer.Core.Validation;

namespace FileRenamer.Core.Rules;

/// <summary>
/// Verfahren „Ordnername einfügen“ (V19/E67): fügt den Namen des Ordners der Datei (Ebene 1) oder eines
/// darüberliegenden Ordners vor oder nach dem Namen ein.
/// </summary>
public sealed record InsertFolderRule : PerFileRenameRule
{
    /// <summary>
    /// Vor oder nach dem Namen.
    /// </summary>
    public AffixPosition Position { get; init; } = AffixPosition.BeforeName;

    /// <summary>
    /// Ordnerebene: 1 = Ordner der Datei, 2 = darüber, …
    /// </summary>
    public int Level { get; init; } = 1;

    /// <summary>
    /// Trennzeichen zwischen Ordnername und Name.
    /// </summary>
    public string Separator { get; init; } = "_";

    /// <inheritdoc />
    public override RuleKind Kind => RuleKind.InsertFolder;

    /// <summary>
    /// Prüft Ebene und Trennzeichen.
    /// </summary>
    /// <returns>Fehlermeldung oder <c>null</c>.</returns>
    public override string? ValidateSettings() =>
        Level < 1 ? CoreText.LevelTooSmall : FileNameValidator.DescribeInvalidCharacter(Separator);

    /// <summary>
    /// Ermittelt den Ordnernamen einer Ebene.
    /// </summary>
    /// <param name="file">Die Datei.</param>
    /// <param name="level">Die Ebene.</param>
    /// <returns>Der Ordnername oder <c>null</c>, wenn die Ebene nicht existiert.</returns>
    public static string? GetFolderName(FileEntry file, int level)
    {
        var directory = file.DirectoryPath;
        for (var step = 1; step < level && !string.IsNullOrEmpty(directory); step++)
        {
            directory = Path.GetDirectoryName(directory);
        }

        var name = string.IsNullOrEmpty(directory)
            ? null
            : Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrEmpty(name) ? null : name;
    }

    /// <summary>
    /// Fügt den Ordnernamen ein; fehlt die Ebene (z. B. Laufwerkswurzel), bleibt der Name mit Hinweis unverändert.
    /// </summary>
    /// <param name="input">Die Datei mit ihrem aktuellen Namen.</param>
    /// <returns>Das Ergebnis für diese Datei.</returns>
    protected override RuleOutput ApplyToFile(RuleInput input) =>
        GetFolderName(input.File, Level) is { } folder
            ? RuleOutput.Changed(Affix.Add(input.Name, folder, Separator, Position, Id))
            : RuleOutput.Unchanged(input.Name, RenameIssue.Hint(CoreText.FolderLevelMissing(Level), Id));
}
