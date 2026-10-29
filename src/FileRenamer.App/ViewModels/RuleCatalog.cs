using FileRenamer.App.ViewModels.Editors;
using FileRenamer.Core.Rules;

namespace FileRenamer.App.ViewModels;

/// <summary>
/// Eintrag der Verfahrensliste (obere Hälfte, links).
/// </summary>
/// <param name="Kind">Art des Verfahrens.</param>
/// <param name="DisplayName">Anzeigename.</param>
public sealed record RuleKindItem(RuleKind Kind, string DisplayName);

/// <summary>
/// Zentrale Zuordnung von Verfahrensarten zu Anzeigenamen und Editoren.
/// </summary>
public static class RuleCatalog
{
    /// <summary>
    /// Alle Verfahren in der Reihenfolge der Verfahrensliste.
    /// </summary>
    public static IReadOnlyList<RuleKindItem> All { get; } =
    [
        new(RuleKind.ReplaceText, "Text ersetzen"),
        new(RuleKind.AppendDate, "Datum / Zeit anfügen"),
        new(RuleKind.AppendCounter, "Zähler anhängen"),
        new(RuleKind.ShiftDigitRange, "Ziffernkette Wertebereich verschieben"),
        new(RuleKind.InsertText, "Text einfügen"),
    ];

    /// <summary>
    /// Liefert den Anzeigenamen einer Verfahrensart.
    /// </summary>
    /// <param name="kind">Die Verfahrensart.</param>
    /// <returns>Der Anzeigename.</returns>
    public static string GetDisplayName(RuleKind kind) => All.First(item => item.Kind == kind).DisplayName;

    /// <summary>
    /// Erzeugt einen Editor mit Standardwerten.
    /// </summary>
    /// <param name="kind">Die Verfahrensart.</param>
    /// <returns>Der Editor.</returns>
    public static RuleEditorViewModel CreateEditor(RuleKind kind) => kind switch
    {
        RuleKind.ReplaceText => new ReplaceTextEditorViewModel(),
        RuleKind.AppendDate => new AppendDateEditorViewModel(),
        RuleKind.AppendCounter => new AppendCounterEditorViewModel(),
        RuleKind.ShiftDigitRange => new ShiftDigitRangeEditorViewModel(),
        RuleKind.InsertText => new InsertTextEditorViewModel(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>
    /// Erzeugt einen Editor, der mit den Parametern eines vorhandenen Verfahrens gefüllt ist.
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    /// <returns>Der Editor.</returns>
    public static RuleEditorViewModel CreateEditorFor(RenameRule rule)
    {
        var editor = CreateEditor(rule.Kind);
        editor.Load(rule);
        return editor;
    }
}
