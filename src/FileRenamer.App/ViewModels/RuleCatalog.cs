using FileRenamer.App.Localization;
using FileRenamer.App.ViewModels.Editors;
using FileRenamer.Core.Localization;
using FileRenamer.Core.Rules;

namespace FileRenamer.App.ViewModels;

/// <summary>
/// Eintrag der gruppierten Verfahrensliste: Gruppenüberschrift oder Verfahren.
/// </summary>
public abstract record RuleListEntry
{
    /// <summary>
    /// Ob der Eintrag ausgewählt werden kann (Überschriften nicht).
    /// </summary>
    public abstract bool IsSelectable { get; }
}

/// <summary>
/// Gruppenüberschrift der Verfahrensliste (E72).
/// </summary>
/// <param name="Title">Überschrift.</param>
public sealed record RuleGroupHeader(string Title) : RuleListEntry
{
    /// <inheritdoc />
    public override bool IsSelectable => false;
}

/// <summary>
/// Eintrag der Verfahrensliste (obere Hälfte, links). Gleichheit gilt nach der Verfahrensart, damit die
/// Auswahl nach einem Sprachwechsel auch in der neu aufgebauten Liste erhalten bleibt.
/// </summary>
/// <param name="Kind">Art des Verfahrens.</param>
/// <param name="DisplayName">Anzeigename.</param>
public sealed record RuleKindItem(RuleKind Kind, string DisplayName) : RuleListEntry
{
    /// <inheritdoc />
    public override bool IsSelectable => true;

    /// <summary>
    /// Vergleicht nur die Verfahrensart.
    /// </summary>
    /// <param name="other">Der andere Eintrag.</param>
    /// <returns><c>true</c> bei gleicher Art.</returns>
    public bool Equals(RuleKindItem? other) => other is not null && Kind == other.Kind;

    /// <summary>
    /// Hashwert passend zu <see cref="Equals(RuleKindItem?)"/>.
    /// </summary>
    /// <returns>Der Hashwert.</returns>
    public override int GetHashCode() => Kind.GetHashCode();
}

/// <summary>
/// Zentrale Zuordnung von Verfahrensarten zu Anzeigenamen, Gruppen und Editoren.
/// </summary>
public static class RuleCatalog
{
    private static UiLanguage? _builtFor;
    private static IReadOnlyList<RuleListEntry> _entries = [];

    /// <summary>
    /// Die gruppierte Verfahrensliste wie im freigegebenen Mockup v10 (E72), in der aktuellen Sprache.
    /// Nach einem Sprachwechsel wird sie neu aufgebaut.
    /// </summary>
    public static IReadOnlyList<RuleListEntry> Entries
    {
        get
        {
            if (_builtFor != Loc.Language)
            {
                _entries = BuildEntries();
                _builtFor = Loc.Language;
            }

            return _entries;
        }
    }

    /// <summary>
    /// Alle Verfahren ohne Überschriften.
    /// </summary>
    public static IReadOnlyList<RuleKindItem> All => Entries.OfType<RuleKindItem>().ToList();

    /// <summary>
    /// Liefert den Anzeigenamen einer Verfahrensart.
    /// </summary>
    /// <param name="kind">Die Verfahrensart.</param>
    /// <returns>Der Anzeigename.</returns>
    public static string GetDisplayName(RuleKind kind) => Loc.T("Rule." + kind);

    /// <summary>
    /// Baut die Liste in der aktuellen Sprache auf.
    /// </summary>
    /// <returns>Überschriften und Verfahren.</returns>
    private static List<RuleListEntry> BuildEntries() =>
    [
        new RuleGroupHeader(Loc.T("Group.Text")),
        Item(RuleKind.ReplaceText), Item(RuleKind.InsertText), Item(RuleKind.ChangeCase),
        Item(RuleKind.CleanCharacters), Item(RuleKind.RemoveRange),
        new RuleGroupHeader(Loc.T("Group.DateNumbers")),
        Item(RuleKind.AppendDate), Item(RuleKind.AppendCounter), Item(RuleKind.ShiftDigitRange),
        new RuleGroupHeader(Loc.T("Group.MetadataFolders")),
        Item(RuleKind.Template), Item(RuleKind.InsertFolder),
        new RuleGroupHeader(Loc.T("Group.Extension")),
        Item(RuleKind.ChangeExtension),
    ];

    /// <summary>
    /// Erzeugt einen Listeneintrag mit übersetztem Namen.
    /// </summary>
    /// <param name="kind">Die Verfahrensart.</param>
    /// <returns>Der Eintrag.</returns>
    private static RuleKindItem Item(RuleKind kind) => new(kind, GetDisplayName(kind));

    /// <summary>
    /// Erzeugt einen Editor mit Standardwerten. Das Laden der Standardwerte prüft sie und
    /// berechnet das Beispiel, sodass es sofort sichtbar ist (E97).
    /// </summary>
    /// <param name="kind">Die Verfahrensart.</param>
    /// <returns>Der Editor.</returns>
    public static RuleEditorViewModel CreateEditor(RuleKind kind)
    {
        var editor = NewEditor(kind);
        editor.Load(editor.CreateRule());
        return editor;
    }

    /// <summary>
    /// Erzeugt den passenden Editor ohne Prüfung.
    /// </summary>
    /// <param name="kind">Die Verfahrensart.</param>
    /// <returns>Der Editor.</returns>
    private static RuleEditorViewModel NewEditor(RuleKind kind) => kind switch
    {
        RuleKind.ReplaceText => new ReplaceTextEditorViewModel(),
        RuleKind.AppendDate => new AppendDateEditorViewModel(),
        RuleKind.AppendCounter => new AppendCounterEditorViewModel(),
        RuleKind.ShiftDigitRange => new ShiftDigitRangeEditorViewModel(),
        RuleKind.InsertText => new InsertTextEditorViewModel(),
        RuleKind.ChangeCase => new ChangeCaseEditorViewModel(),
        RuleKind.CleanCharacters => new CleanCharactersEditorViewModel(),
        RuleKind.RemoveRange => new RemoveRangeEditorViewModel(),
        RuleKind.Template => new TemplateEditorViewModel(),
        RuleKind.InsertFolder => new InsertFolderEditorViewModel(),
        RuleKind.ChangeExtension => new ChangeExtensionEditorViewModel(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>
    /// Erzeugt einen Editor, der mit den Parametern eines vorhandenen Verfahrens gefüllt ist.
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    /// <returns>Der Editor.</returns>
    public static RuleEditorViewModel CreateEditorFor(RenameRule rule)
    {
        var editor = NewEditor(rule.Kind);
        editor.Load(rule);
        return editor;
    }
}
