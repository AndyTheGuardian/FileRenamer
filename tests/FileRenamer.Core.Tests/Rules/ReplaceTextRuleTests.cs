using FileRenamer.Core.Rules;
using FileRenamer.Core.Tests.Support;

namespace FileRenamer.Core.Tests.Rules;

/// <summary>
/// Tests für „Text ersetzen“.
/// </summary>
public sealed class ReplaceTextRuleTests
{
    /// <summary>
    /// Erster, letzter und alle Treffer werden korrekt ersetzt.
    /// </summary>
    /// <param name="occurrence">Die Trefferauswahl.</param>
    /// <param name="expected">Erwarteter Name.</param>
    [Theory]
    [InlineData(MatchOccurrence.First, "X_IMG_IMG")]
    [InlineData(MatchOccurrence.Last, "IMG_IMG_X")]
    [InlineData(MatchOccurrence.All, "X_X_X")]
    public void Apply_SelectsOccurrences(MatchOccurrence occurrence, string expected)
    {
        var rule = new ReplaceTextRule { SearchText = "IMG", Replacement = "X", Occurrence = occurrence };

        Assert.Equal(expected, rule.ApplyTo("IMG_IMG_IMG").Name.Text);
    }

    /// <summary>
    /// Groß-/Kleinschreibung wird berücksichtigt.
    /// </summary>
    [Fact]
    public void Apply_IsCaseSensitive()
    {
        var rule = new ReplaceTextRule { SearchText = "img", Replacement = "x" };

        Assert.Equal("IMG_x", rule.ApplyTo("IMG_img").Name.Text);
    }

    /// <summary>
    /// Reguläre Ausdrücke inklusive Gruppenverweisen werden unterstützt.
    /// </summary>
    [Fact]
    public void Apply_Regex_SupportsGroups()
    {
        var rule = new ReplaceTextRule { SearchText = @"(\d{4})-(\d{2})", Replacement = "$2.$1", UseRegex = true };

        Assert.Equal("bild_10.2026", rule.ApplyTo("bild_2026-10").Name.Text);
    }

    /// <summary>
    /// Ein ungültiger regulärer Ausdruck wird als Parameterfehler gemeldet.
    /// </summary>
    [Fact]
    public void ValidateSettings_InvalidRegex_ReturnsError()
    {
        var rule = new ReplaceTextRule { SearchText = "([a-", UseRegex = true };

        Assert.StartsWith("Ungültiger regulärer Ausdruck", rule.ValidateSettings());
    }

    /// <summary>
    /// Leerer Suchtext und fehlende Treffer lassen den Namen unverändert.
    /// </summary>
    [Fact]
    public void Apply_NoMatch_LeavesNameUnchanged()
    {
        Assert.Equal("abc", new ReplaceTextRule().ApplyTo("abc").Name.Text);
        Assert.Equal("abc", new ReplaceTextRule { SearchText = "x", Replacement = "y" }.ApplyTo("abc").Name.Text);
    }
}
