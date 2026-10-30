using FileRenamer.Core.Preview;
using FileRenamer.Core.Rules;

namespace FileRenamer.App.ViewModels;

/// <summary>
/// Ein farbig hervorzuhebender Abschnitt des neuen Namens.
/// </summary>
/// <param name="Text">Der Text.</param>
/// <param name="Kind">Art des maßgeblichen Verfahrens oder <c>null</c> bei Originaltext.</param>
public sealed record HighlightSpan(string Text, RuleKind? Kind);

/// <summary>
/// Zeile der zweispaltigen Vorschau (untere Hälfte, rechts).
/// </summary>
public sealed class PreviewRowViewModel
{
    private readonly Action<string, bool> _selectionChanged;
    private bool _isSelected;

    /// <summary>
    /// Erzeugt die Zeile aus der Vorschau einer Datei.
    /// </summary>
    /// <param name="preview">Die Vorschau der Datei.</param>
    /// <param name="kindsById">Zuordnung Verfahrens-Id → Art (für die Farben).</param>
    /// <param name="preferredRuleId">Id des ausgewählten Verfahrens (Vorrang bei Überschneidungen, E5).</param>
    /// <param name="selectionChanged">Wird mit Dateipfad und neuem Zustand aufgerufen, wenn die Checkbox wechselt.</param>
    public PreviewRowViewModel(
        FilePreview preview, IReadOnlyDictionary<Guid, RuleKind> kindsById, Guid? preferredRuleId, Action<string, bool> selectionChanged)
    {
        _selectionChanged = selectionChanged;
        _isSelected = preview.IsSelected;
        FullPath = preview.File.FullPath;
        OriginalName = preview.OriginalDisplayName;
        HasConflict = preview.HasConflict;
        HasHint = preview.HasHint && !HasConflict;
        IssueText = preview.Issues.Count == 0 ? null : string.Join(Environment.NewLine, preview.Issues.Select(issue => issue.Message));
        // Eine geänderte, sonst ausgeblendete Endung wird eingeblendet (E73)
        var segments = preview.IsExtensionChanged
            ? preview.NewDisplayName.Segments.Concat(preview.NewExtension.Segments)
            : preview.NewDisplayName.Segments;
        Spans = segments
            .Select(segment => new HighlightSpan(segment.Text, ResolveKind(segment.GetHighlightSource(preferredRuleId), kindsById)))
            .ToList();
    }

    /// <summary>
    /// Vollständiger Pfad der Datei.
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// Ob die Datei ausgewählt ist (Checkbox, E52). Eine Änderung wird an das Hauptfenster-ViewModel gemeldet.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (value != _isSelected)
            {
                _isSelected = value;
                _selectionChanged(FullPath, value);
            }
        }
    }

    /// <summary>
    /// Ursprünglicher Name (ohne ausgeblendete Erweiterung).
    /// </summary>
    public string OriginalName { get; }

    /// <summary>
    /// Abschnitte des neuen Namens.
    /// </summary>
    public IReadOnlyList<HighlightSpan> Spans { get; }

    /// <summary>
    /// Ob die Zeile einen Konflikt hat.
    /// </summary>
    public bool HasConflict { get; }

    /// <summary>
    /// Ob die Zeile (nur) Hinweise hat.
    /// </summary>
    public bool HasHint { get; }

    /// <summary>
    /// Text für den Tooltip des Statussymbols.
    /// </summary>
    public string? IssueText { get; }

    /// <summary>
    /// Ermittelt die Verfahrensart zu einer Id.
    /// </summary>
    /// <param name="ruleId">Die Id oder <c>null</c>.</param>
    /// <param name="kindsById">Die Zuordnung.</param>
    /// <returns>Die Art oder <c>null</c>.</returns>
    private static RuleKind? ResolveKind(Guid? ruleId, IReadOnlyDictionary<Guid, RuleKind> kindsById) =>
        ruleId is { } id && kindsById.TryGetValue(id, out var kind) ? kind : null;
}
