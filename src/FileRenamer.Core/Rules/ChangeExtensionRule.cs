using FileRenamer.Core.Validation;

namespace FileRenamer.Core.Rules;

/// <summary>
/// Verfahren „Erweiterung ändern“ (V18/E67). Wirkt ausschließlich auf die (letzte) Endung – auch wenn
/// „Dateierweiterung ignorieren“ aktiv ist (E73). Die Pipeline übergibt dem Verfahren deshalb als „Name“
/// die Endung inklusive Punkt (z. B. „.JPEG“ oder leer).
/// </summary>
public sealed record ChangeExtensionRule : PerFileRenameRule
{
    /// <summary>
    /// Nur Dateien mit dieser Endung ändern (ohne Punkt, Groß-/Kleinschreibung egal); leer = alle.
    /// </summary>
    public string OnlyExtension { get; init; } = string.Empty;

    /// <summary>
    /// Neue Endung (ohne Punkt); leer = Endung beibehalten.
    /// </summary>
    public string NewExtension { get; init; } = string.Empty;

    /// <summary>
    /// Schreibweise der Endung.
    /// </summary>
    public ExtensionCasing Casing { get; init; } = ExtensionCasing.Lower;

    /// <inheritdoc />
    public override RuleKind Kind => RuleKind.ChangeExtension;

    /// <summary>
    /// Kennzeichnet, dass die Pipeline die Endung statt des Namens übergeben muss.
    /// </summary>
    public override bool TargetsExtension => true;

    /// <summary>
    /// Prüft beide Endungen auf verbotene Zeichen.
    /// </summary>
    /// <returns>Fehlermeldung oder <c>null</c>.</returns>
    public override string? ValidateSettings() =>
        FileNameValidator.DescribeInvalidCharacter(OnlyExtension) ?? FileNameValidator.DescribeInvalidCharacter(NewExtension);

    /// <summary>
    /// Berechnet die neue Endung.
    /// </summary>
    /// <param name="extension">Bisherige Endung mit Punkt oder leer.</param>
    /// <returns>Neue Endung mit Punkt oder leer.</returns>
    public string ConvertExtension(string extension)
    {
        var current = extension.TrimStart('.');
        var filter = OnlyExtension.Trim().TrimStart('.');
        if (filter.Length > 0 && !string.Equals(current, filter, StringComparison.OrdinalIgnoreCase))
        {
            return extension;
        }

        var replacement = NewExtension.Trim().TrimStart('.');
        var result = replacement.Length > 0 ? replacement : current;
        result = Casing switch
        {
            ExtensionCasing.Lower => result.ToLowerInvariant(),
            ExtensionCasing.Upper => result.ToUpperInvariant(),
            _ => result,
        };
        return result.Length == 0 ? string.Empty : "." + result;
    }

    /// <summary>
    /// Ändert die übergebene Endung.
    /// </summary>
    /// <param name="input">Die Datei; <see cref="RuleInput.Name"/> enthält die Endung.</param>
    /// <returns>Das Ergebnis mit neuer Endung.</returns>
    protected override RuleOutput ApplyToFile(RuleInput input) =>
        RuleOutput.Changed(input.Name.ReplaceText(ConvertExtension(input.Name.Text), Id));
}
