using FileRenamer.Core.Rules;
using FileRenamer.Core.Tests.Support;

namespace FileRenamer.Core.Tests.Rules;

/// <summary>
/// Tests für „Text einfügen“.
/// </summary>
public sealed class InsertTextRuleTests
{
    /// <summary>
    /// Position ab 0 vom Anfang bzw. vom Ende; Werte außerhalb werden begrenzt.
    /// </summary>
    /// <param name="position">Position.</param>
    /// <param name="fromEnd">Vom Ende zählen.</param>
    /// <param name="expected">Erwarteter Name.</param>
    [Theory]
    [InlineData(0, false, "_xbericht")]
    [InlineData(3, false, "ber_xicht")]
    [InlineData(0, true, "bericht_x")]
    [InlineData(2, true, "beric_xht")]
    [InlineData(99, false, "bericht_x")]
    [InlineData(99, true, "_xbericht")]
    public void Apply_InsertsAtPosition(int position, bool fromEnd, string expected)
    {
        var rule = new InsertTextRule { Text = "_x", Position = position, FromEnd = fromEnd };

        Assert.Equal(expected, rule.ApplyTo("bericht").Name.Text);
    }

    /// <summary>
    /// Negative Position und verbotene Zeichen werden abgewiesen.
    /// </summary>
    [Fact]
    public void ValidateSettings_RejectsInvalidInput()
    {
        Assert.NotNull(new InsertTextRule { Text = "x", Position = -1 }.ValidateSettings());
        Assert.NotNull(new InsertTextRule { Text = "a/b" }.ValidateSettings());
    }
}
