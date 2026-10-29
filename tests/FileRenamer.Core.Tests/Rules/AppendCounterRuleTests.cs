using FileRenamer.Core.Rules;
using FileRenamer.Core.Tests.Support;

namespace FileRenamer.Core.Tests.Rules;

/// <summary>
/// Tests für „Zähler anhängen“.
/// </summary>
public sealed class AppendCounterRuleTests
{
    /// <summary>
    /// Standardwerte: nach dem Namen, Start 1, Schritt 1, drei Stellen, Trennzeichen „-“, natürliche Namenssortierung.
    /// </summary>
    [Fact]
    public void Apply_Defaults_NumbersByNaturalNameOrder()
    {
        var names = new AppendCounterRule().ApplyToAll(
            RuleTestHelper.File("datei10"), RuleTestHelper.File("datei2"), RuleTestHelper.File("datei1"));

        Assert.Equal(["datei10-003", "datei2-002", "datei1-001"], names);
    }

    /// <summary>
    /// Startwert, Schrittweite, Stellen, Position und absteigende Sortierung werden berücksichtigt.
    /// </summary>
    [Fact]
    public void Apply_CustomSettings()
    {
        var rule = new AppendCounterRule
        {
            Start = 10, Step = 5, Digits = 2, Separator = "_", Position = AffixPosition.BeforeName, Descending = true,
        };

        var names = rule.ApplyToAll(RuleTestHelper.File("a"), RuleTestHelper.File("b"), RuleTestHelper.File("c"));

        Assert.Equal(["20_a", "15_b", "10_c"], names);
    }

    /// <summary>
    /// Sortierung nach Größe und nach Änderungsdatum.
    /// </summary>
    [Fact]
    public void Apply_SortsBySizeAndModificationDate()
    {
        var big = RuleTestHelper.File("a", modified: new DateTime(2026, 1, 1), size: 300);
        var small = RuleTestHelper.File("b", modified: new DateTime(2026, 3, 1), size: 100);

        Assert.Equal(["a-002", "b-001"], new AppendCounterRule { SortBy = CounterSortCriterion.Size }.ApplyToAll(big, small));
        Assert.Equal(["a-001", "b-002"], new AppendCounterRule { SortBy = CounterSortCriterion.ModificationDate }.ApplyToAll(big, small));
    }

    /// <summary>
    /// Negative Werte erhalten ein Minuszeichen vor den aufgefüllten Ziffern; mehr Stellen als gefordert bleiben erhalten.
    /// </summary>
    [Fact]
    public void FormatCounter_HandlesNegativeAndLongValues()
    {
        var rule = new AppendCounterRule { Digits = 3 };

        Assert.Equal("-005", rule.FormatCounter(-5));
        Assert.Equal("1234", rule.FormatCounter(1234));
    }

    /// <summary>
    /// Ungültige Stellenanzahl wird abgewiesen.
    /// </summary>
    [Fact]
    public void ValidateSettings_RejectsInvalidDigits()
    {
        Assert.NotNull(new AppendCounterRule { Digits = 0 }.ValidateSettings());
        Assert.NotNull(new AppendCounterRule { Separator = "?" }.ValidateSettings());
    }
}
