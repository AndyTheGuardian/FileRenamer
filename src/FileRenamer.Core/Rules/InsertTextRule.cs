using FileRenamer.Core.Validation;

namespace FileRenamer.Core.Rules;

/// <summary>
/// Verfahren „Text einfügen“: fügt Text an einer Position ein, gezählt ab 0 vom Anfang oder vom Ende.
/// Positionen außerhalb des Namens werden auf Anfang bzw. Ende begrenzt.
/// </summary>
public sealed record InsertTextRule : PerFileRenameRule
{
    /// <summary>
    /// Der einzufügende Text.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Einfügeposition ab 0.
    /// </summary>
    public int Position { get; init; }

    /// <summary>
    /// Ob die Position vom Ende des Namens gezählt wird.
    /// </summary>
    public bool FromEnd { get; init; }

    /// <inheritdoc />
    public override RuleKind Kind => RuleKind.InsertText;

    /// <summary>
    /// Prüft Position und Text.
    /// </summary>
    /// <returns>Fehlermeldung oder <c>null</c>.</returns>
    public override string? ValidateSettings() =>
        Position < 0
            ? "Die Position darf nicht negativ sein."
            : FileNameValidator.DescribeInvalidCharacter(Text);

    /// <summary>
    /// Fügt den Text an der (begrenzten) Position ein.
    /// </summary>
    /// <param name="input">Die Datei mit ihrem aktuellen Namen.</param>
    /// <returns>Das Ergebnis für diese Datei.</returns>
    protected override RuleOutput ApplyToFile(RuleInput input)
    {
        if (Text.Length == 0)
        {
            return RuleOutput.Unchanged(input.Name);
        }

        var length = input.Name.Length;
        var index = Math.Clamp(FromEnd ? length - Position : Position, 0, length);
        return RuleOutput.Changed(input.Name.Insert(index, Text, Id));
    }
}
