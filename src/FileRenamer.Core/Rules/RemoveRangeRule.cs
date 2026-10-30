using FileRenamer.Core.Localization;
namespace FileRenamer.Core.Rules;

/// <summary>
/// Verfahren „Bereich entfernen / kürzen“ (V17/E67): entfernt eine Anzahl Zeichen ab einer Position
/// (ab 0, vom Anfang oder Ende) oder kürzt den Namen auf eine Höchstlänge.
/// </summary>
public sealed record RemoveRangeRule : PerFileRenameRule
{
    /// <summary>
    /// Entfernen oder Kürzen.
    /// </summary>
    public RemoveMode Mode { get; init; } = RemoveMode.Cut;

    /// <summary>
    /// Startposition ab 0.
    /// </summary>
    public int Position { get; init; }

    /// <summary>
    /// Ob die Position vom Ende gezählt wird.
    /// </summary>
    public bool FromEnd { get; init; }

    /// <summary>
    /// Anzahl zu entfernender Zeichen.
    /// </summary>
    public int Count { get; init; } = 4;

    /// <summary>
    /// Höchstlänge beim Kürzen.
    /// </summary>
    public int MaxLength { get; init; } = 20;

    /// <inheritdoc />
    public override RuleKind Kind => RuleKind.RemoveRange;

    /// <summary>
    /// Prüft die Zahlenwerte.
    /// </summary>
    /// <returns>Fehlermeldung oder <c>null</c>.</returns>
    public override string? ValidateSettings() => Mode switch
    {
        RemoveMode.Cut when Position < 0 => CoreText.PositionNegative,
        RemoveMode.Cut when Count < 1 => CoreText.CountTooSmall,
        RemoveMode.Truncate when MaxLength < 1 => CoreText.MaxLengthTooSmall,
        _ => null,
    };

    /// <summary>
    /// Entfernt den Bereich bzw. kürzt den Namen. Bereiche außerhalb des Namens werden begrenzt.
    /// </summary>
    /// <param name="input">Die Datei mit ihrem aktuellen Namen.</param>
    /// <returns>Das Ergebnis für diese Datei.</returns>
    protected override RuleOutput ApplyToFile(RuleInput input)
    {
        var length = input.Name.Length;
        if (Mode == RemoveMode.Truncate)
        {
            return length <= MaxLength
                ? RuleOutput.Unchanged(input.Name)
                : RuleOutput.Changed(input.Name.Replace(MaxLength, length - MaxLength, string.Empty, Id));
        }

        var start = FromEnd ? length - Position - Count : Position;
        var end = Math.Clamp(start + Count, 0, length);
        start = Math.Clamp(start, 0, length);
        return end <= start
            ? RuleOutput.Unchanged(input.Name)
            : RuleOutput.Changed(input.Name.Replace(start, end - start, string.Empty, Id));
    }
}
