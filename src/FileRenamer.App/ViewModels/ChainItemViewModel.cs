using CommunityToolkit.Mvvm.ComponentModel;
using FileRenamer.Core.Rules;

namespace FileRenamer.App.ViewModels;

/// <summary>
/// Eintrag der Verfahrenskette (untere Hälfte, links): Verfahren mit Checkbox und Fehlerkennzeichnung.
/// </summary>
public sealed partial class ChainItemViewModel : ObservableObject
{
    /// <summary>
    /// Erzeugt einen Ketteneintrag.
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    public ChainItemViewModel(RenameRule rule) => Rule = rule;

    /// <summary>
    /// Das Verfahren mit seinen aktuellen Parametern.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEnabled), nameof(Kind), nameof(DisplayName))]
    public partial RenameRule Rule { get; set; }

    /// <summary>
    /// Fehlermeldung (E40) oder <c>null</c>.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? Error { get; set; }

    /// <summary>
    /// Ob der Eintrag fehlerhaft ist.
    /// </summary>
    public bool HasError => Error is not null;

    /// <summary>
    /// Art des Verfahrens (bestimmt die Farbe).
    /// </summary>
    public RuleKind Kind => Rule.Kind;

    /// <summary>
    /// Anzeigename.
    /// </summary>
    public string DisplayName => RuleCatalog.GetDisplayName(Kind);

    /// <summary>
    /// Ob das Verfahren angewandt wird (Checkbox).
    /// </summary>
    public bool IsEnabled
    {
        get => Rule.IsEnabled;
        set
        {
            if (value != Rule.IsEnabled)
            {
                Rule = Rule with { IsEnabled = value };
            }
        }
    }
}
