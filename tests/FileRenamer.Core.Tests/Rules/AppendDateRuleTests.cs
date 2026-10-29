using System.Globalization;
using FileRenamer.Core.Rules;
using FileRenamer.Core.Tests.Support;

namespace FileRenamer.Core.Tests.Rules;

/// <summary>
/// Tests für „Datum / Zeit anfügen“.
/// </summary>
public sealed class AppendDateRuleTests
{
    private static readonly DateTime Created = new(2026, 10, 12, 14, 30, 5);
    private static readonly DateTime Modified = new(2026, 10, 29, 8, 5, 0);

    /// <summary>
    /// Das Datum wird mit Trennzeichen vor bzw. nach dem Namen eingefügt.
    /// </summary>
    /// <param name="position">Position.</param>
    /// <param name="expected">Erwarteter Name.</param>
    [Theory]
    [InlineData(AffixPosition.BeforeName, "2026-10-12_bericht")]
    [InlineData(AffixPosition.AfterName, "bericht_2026-10-12")]
    public void Apply_InsertsDateAtPosition(AffixPosition position, string expected)
    {
        var rule = CreateRule() with { Position = position };

        Assert.Equal(expected, rule.ApplyTo(RuleTestHelper.File("bericht", Created, Modified)).Name.Text);
    }

    /// <summary>
    /// Uhrzeit, eigenes Format und eigene Trennzeichen werden verwendet.
    /// </summary>
    [Fact]
    public void Apply_WithTimeAndCustomSeparators()
    {
        var rule = CreateRule() with
        {
            DateFormat = "yyyyMMdd", IncludeTime = true, TimeFormat = "HH.mm", NameSeparator = " - ", DateTimeSeparator = "T",
        };

        Assert.Equal("20261012T14.30 - bericht", rule.ApplyTo(RuleTestHelper.File("bericht", Created, Modified)).Name.Text);
    }

    /// <summary>
    /// Mit Quelle Änderungsdatum wird dieses verwendet.
    /// </summary>
    [Fact]
    public void Apply_ModificationDate()
    {
        var rule = CreateRule() with { Source = DateSource.ModificationDate };

        Assert.Equal("2026-10-29_bericht", rule.ApplyTo(RuleTestHelper.File("bericht", Created, Modified)).Name.Text);
    }

    /// <summary>
    /// Fehlt das Erstellungsdatum, wird das Änderungsdatum mit Hinweis verwendet.
    /// </summary>
    [Fact]
    public void Apply_MissingCreationDate_FallsBackWithHint()
    {
        var output = CreateRule().ApplyTo(RuleTestHelper.File("bericht", created: null, Modified));

        Assert.Equal("2026-10-29_bericht", output.Name.Text);
        Assert.Equal(IssueSeverity.Hint, Assert.Single(output.Issues).Severity);
    }

    /// <summary>
    /// Ungültige Formate werden abgewiesen (z. B. Doppelpunkt, fehlender Datumsbestandteil).
    /// </summary>
    /// <param name="dateFormat">Datumsformat.</param>
    [Theory]
    [InlineData("HH:mm")]
    [InlineData("abc-qrs")]
    [InlineData("dd/MM/yyyy")]
    [InlineData("")]
    public void ValidateSettings_RejectsInvalidDateFormats(string dateFormat)
    {
        Assert.NotNull((CreateRule() with { DateFormat = dateFormat }).ValidateSettings());
    }

    /// <summary>
    /// Ein ungültiges Uhrzeitformat wird nur geprüft, wenn die Uhrzeit angefügt wird.
    /// </summary>
    [Fact]
    public void ValidateSettings_TimeFormatOnlyCheckedWhenIncluded()
    {
        var rule = CreateRule() with { TimeFormat = "HH:mm" };

        Assert.Null(rule.ValidateSettings());
        Assert.NotNull((rule with { IncludeTime = true }).ValidateSettings());
    }

    /// <summary>
    /// Erzeugt ein Verfahren mit fester Kultur für reproduzierbare Ergebnisse.
    /// </summary>
    /// <returns>Das Verfahren.</returns>
    private static AppendDateRule CreateRule() => new() { Culture = CultureInfo.InvariantCulture };
}
