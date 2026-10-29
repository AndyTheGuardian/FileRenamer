using CommunityToolkit.Mvvm.ComponentModel;
using FileRenamer.Core.Rules;

namespace FileRenamer.App.ViewModels.Editors;

/// <summary>
/// Editor für „Text ersetzen“.
/// </summary>
public sealed partial class ReplaceTextEditorViewModel() : RuleEditorViewModel(RuleKind.ReplaceText)
{
    /// <summary>
    /// Gesuchter Text bzw. regulärer Ausdruck.
    /// </summary>
    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    /// <summary>
    /// Ersatztext.
    /// </summary>
    [ObservableProperty]
    public partial string Replacement { get; set; } = string.Empty;

    /// <summary>
    /// Trefferauswahl.
    /// </summary>
    [ObservableProperty]
    public partial MatchOccurrence Occurrence { get; set; } = MatchOccurrence.All;

    /// <summary>
    /// Ob ein regulärer Ausdruck verwendet wird.
    /// </summary>
    [ObservableProperty]
    public partial bool UseRegex { get; set; }

    /// <summary>
    /// Erzeugt das Verfahren aus den Eingaben.
    /// </summary>
    /// <returns>Das Verfahren.</returns>
    public override RenameRule CreateRule() => new ReplaceTextRule
    {
        SearchText = SearchText,
        Replacement = Replacement,
        Occurrence = Occurrence,
        UseRegex = UseRegex,
    };

    /// <summary>
    /// Übernimmt die Parameter eines Verfahrens.
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    protected override void LoadSettings(RenameRule rule)
    {
        var replace = (ReplaceTextRule)rule;
        SearchText = replace.SearchText;
        Replacement = replace.Replacement;
        Occurrence = replace.Occurrence;
        UseRegex = replace.UseRegex;
    }
}
