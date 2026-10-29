using FileRenamer.Core.Rules;
using FileRenamer.Core.Tests.Support;

namespace FileRenamer.Core.Tests.Rules;

/// <summary>
/// Tests für „Ziffernkette Wertebereich verschieben“, inklusive der Beispiele aus SPECS.md.
/// </summary>
public sealed class ShiftDigitRangeRuleTests
{
    /// <summary>
    /// SPECS: name-001, name-002 mit Eingabe 100 → name-101, name-102.
    /// </summary>
    [Fact]
    public void Apply_SpecExample_AddsValue()
    {
        var rule = new ShiftDigitRangeRule { Offset = 100 };

        Assert.Equal(["name-101", "name-102"],
            rule.ApplyToAll(RuleTestHelper.File("name-001"), RuleTestHelper.File("name-002")));
    }

    /// <summary>
    /// SPECS: name-301-001, name-432-002 mit Wert 10, Position 2 → name-301-011, name-432-012.
    /// </summary>
    [Fact]
    public void Apply_SpecExample_SecondRun()
    {
        var rule = new ShiftDigitRangeRule { Offset = 10, UseLastRun = false, RunNumber = 2 };

        Assert.Equal(["name-301-011", "name-432-012"],
            rule.ApplyToAll(RuleTestHelper.File("name-301-001"), RuleTestHelper.File("name-432-002")));
    }

    /// <summary>
    /// Standard ist die letzte Ziffernkette; Nr. 1 zählt von links.
    /// </summary>
    [Fact]
    public void Apply_DefaultUsesLastRun_RunNumberCountsFromLeft()
    {
        Assert.Equal("name-301-011", new ShiftDigitRangeRule { Offset = 10 }.ApplyTo("name-301-001").Name.Text);
        Assert.Equal("name-311-001",
            new ShiftDigitRangeRule { Offset = 10, UseLastRun = false, RunNumber = 1 }.ApplyTo("name-301-001").Name.Text);
    }

    /// <summary>
    /// Überlauf erweitert die Zahl (995 + 10 = 1005).
    /// </summary>
    [Fact]
    public void Apply_Overflow_ExtendsNumber()
    {
        Assert.Equal("zahl-1005", new ShiftDigitRangeRule { Offset = 10 }.ApplyTo("zahl-995").Name.Text);
    }

    /// <summary>
    /// Negative Werte verringern; die Originalbreite bleibt als Mindestbreite erhalten.
    /// </summary>
    [Fact]
    public void Apply_NegativeOffset_KeepsWidth()
    {
        Assert.Equal("name-005", new ShiftDigitRangeRule { Offset = -10 }.ApplyTo("name-015").Name.Text);
    }

    /// <summary>
    /// Eine eigene Mindestbreite füllt mit Nullen auf.
    /// </summary>
    [Fact]
    public void Apply_MinDigits_PadsWithZeros()
    {
        var rule = new ShiftDigitRangeRule { Offset = 1, KeepOriginalWidth = false, MinDigits = 5 };

        Assert.Equal("name-00008", rule.ApplyTo("name-7").Name.Text);
    }

    /// <summary>
    /// Ein negatives Ergebnis ist ein Konflikt; der Name bleibt unverändert.
    /// </summary>
    [Fact]
    public void Apply_NegativeResult_IsConflict()
    {
        var output = new ShiftDigitRangeRule { Offset = -10 }.ApplyTo("name-005");

        Assert.Equal("name-005", output.Name.Text);
        Assert.Equal(IssueSeverity.Conflict, Assert.Single(output.Issues).Severity);
    }

    /// <summary>
    /// Ohne Ziffernkette bleibt der Name unverändert und es gibt einen Hinweis.
    /// </summary>
    [Fact]
    public void Apply_NoDigits_HintAndUnchanged()
    {
        var output = new ShiftDigitRangeRule { Offset = 10 }.ApplyTo("bericht");

        Assert.Equal("bericht", output.Name.Text);
        Assert.Equal(IssueSeverity.Hint, Assert.Single(output.Issues).Severity);
    }

    /// <summary>
    /// Eine nicht vorhandene Nr. ist ein Konflikt.
    /// </summary>
    [Fact]
    public void Apply_MissingRunNumber_IsConflict()
    {
        var output = new ShiftDigitRangeRule { UseLastRun = false, RunNumber = 2 }.ApplyTo("name-001");

        Assert.Equal(IssueSeverity.Conflict, Assert.Single(output.Issues).Severity);
    }

    /// <summary>
    /// Die größte gültige Nr. ist die kleinste Anzahl Ziffernketten unter den Namen mit Ziffernketten.
    /// </summary>
    [Fact]
    public void GetMaxRunNumber_UsesMinimumOfNamesWithDigits()
    {
        Assert.Equal(1, ShiftDigitRangeRule.GetMaxRunNumber(["a-1-2-3", "b-1", "ohne"]));
        Assert.Equal(2, ShiftDigitRangeRule.GetMaxRunNumber(["a-1-2", "b-3-4-5"]));
        Assert.Null(ShiftDigitRangeRule.GetMaxRunNumber(["ohne", "ziffern"]));
    }

    /// <summary>
    /// Sehr lange Ziffernketten verursachen keinen Überlauf.
    /// </summary>
    [Fact]
    public void Apply_VeryLongDigitRun_NoOverflow()
    {
        Assert.Equal("id-99999999999999999999999",
            new ShiftDigitRangeRule { Offset = 1 }.ApplyTo("id-99999999999999999999998").Name.Text);
    }
}
