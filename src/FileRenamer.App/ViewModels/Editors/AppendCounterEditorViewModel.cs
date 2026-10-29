using CommunityToolkit.Mvvm.ComponentModel;
using FileRenamer.Core.Rules;

namespace FileRenamer.App.ViewModels.Editors;

/// <summary>
/// Auswahlmöglichkeit für das Sortierkriterium.
/// </summary>
/// <param name="Criterion">Das Kriterium.</param>
/// <param name="DisplayName">Anzeigename.</param>
public sealed record SortOption(CounterSortCriterion Criterion, string DisplayName);

/// <summary>
/// Editor für „Zähler anhängen“.
/// </summary>
public sealed partial class AppendCounterEditorViewModel : RuleEditorViewModel
{
    /// <summary>
    /// Erzeugt den Editor und wählt die Standardsortierung.
    /// </summary>
    public AppendCounterEditorViewModel() : base(RuleKind.AppendCounter) => SelectedSort = SortOptions[0];

    /// <summary>
    /// Wählbare Sortierkriterien.
    /// </summary>
    public IReadOnlyList<SortOption> SortOptions { get; } =
    [
        new(CounterSortCriterion.Name, "Name"),
        new(CounterSortCriterion.CreationDate, "Erstellungsdatum"),
        new(CounterSortCriterion.ModificationDate, "Änderungsdatum"),
        new(CounterSortCriterion.Size, "Größe"),
    ];

    /// <summary>
    /// Vor oder nach dem Namen.
    /// </summary>
    [ObservableProperty]
    public partial AffixPosition Position { get; set; } = AffixPosition.AfterName;

    /// <summary>
    /// Startwert.
    /// </summary>
    [ObservableProperty]
    public partial decimal? Start { get; set; } = 1;

    /// <summary>
    /// Schrittweite.
    /// </summary>
    [ObservableProperty]
    public partial decimal? Step { get; set; } = 1;

    /// <summary>
    /// Stellenanzahl (führende Nullen).
    /// </summary>
    [ObservableProperty]
    public partial decimal? Digits { get; set; } = 3;

    /// <summary>
    /// Trennzeichen.
    /// </summary>
    [ObservableProperty]
    public partial string Separator { get; set; } = "-";

    /// <summary>
    /// Gewähltes Sortierkriterium.
    /// </summary>
    [ObservableProperty]
    public partial SortOption SelectedSort { get; set; }

    /// <summary>
    /// Ob absteigend sortiert wird.
    /// </summary>
    [ObservableProperty]
    public partial bool Descending { get; set; }

    /// <summary>
    /// Erzeugt das Verfahren aus den Eingaben.
    /// </summary>
    /// <returns>Das Verfahren.</returns>
    public override RenameRule CreateRule() => new AppendCounterRule
    {
        Position = Position,
        Start = ToLong(Start, 1),
        Step = ToLong(Step, 1),
        Digits = (int)ToLong(Digits, 1),
        Separator = Separator,
        SortBy = SelectedSort.Criterion,
        Descending = Descending,
    };

    /// <summary>
    /// Beispiel wie im Mockup: die ersten beiden Werte für „bericht“.
    /// </summary>
    /// <returns>Der Beispieltext.</returns>
    protected override string? CreateExample() =>
        string.Join(", ", ApplyToSamples("bericht", "bericht").Order(Core.Naming.NaturalStringComparer.Instance)) + ", …";

    /// <summary>
    /// Übernimmt die Parameter eines Verfahrens.
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    protected override void LoadSettings(RenameRule rule)
    {
        var counter = (AppendCounterRule)rule;
        Position = counter.Position;
        Start = counter.Start;
        Step = counter.Step;
        Digits = counter.Digits;
        Separator = counter.Separator;
        SelectedSort = SortOptions.First(option => option.Criterion == counter.SortBy);
        Descending = counter.Descending;
    }
}
