using System.Globalization;
using FileRenamer.Core.Validation;

namespace FileRenamer.Core.Tests.Validation;

/// <summary>
/// Tests für die Prüfung frei eingegebener Datums- und Uhrzeitformate.
/// </summary>
public sealed class DateFormatValidatorTests
{
    /// <summary>
    /// Alle vordefinierten Datumsformate sind gültig.
    /// </summary>
    /// <param name="format">Das Format.</param>
    [Theory]
    [InlineData("yyyy-MM-dd")]
    [InlineData("yyyyMMdd")]
    [InlineData("yyyy_MM_dd")]
    [InlineData("dd.MM.yyyy")]
    [InlineData("dd-MM-yyyy")]
    [InlineData("MM-dd-yyyy")]
    [InlineData("d")]
    public void ValidateDateFormat_PredefinedFormats_AreValid(string format)
    {
        Assert.Null(DateFormatValidator.ValidateDateFormat(format, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Alle vordefinierten Uhrzeitformate sind gültig, Formate mit Doppelpunkt nicht.
    /// </summary>
    [Fact]
    public void ValidateTimeFormat_ChecksForbiddenCharacters()
    {
        foreach (var format in new[] { "HH-mm-ss", "HHmmss", "HH.mm.ss", "HH-mm" })
        {
            Assert.Null(DateFormatValidator.ValidateTimeFormat(format, CultureInfo.InvariantCulture));
        }

        Assert.NotNull(DateFormatValidator.ValidateTimeFormat("HH:mm", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Ein einzelnes Zeichen wird als benutzerdefiniertes Format behandelt (nicht als Standardformat).
    /// </summary>
    [Fact]
    public void Format_SingleCharacter_IsCustomFormat()
    {
        Assert.Equal("5", DateFormatValidator.Format(new DateTime(2026, 3, 5), "d", CultureInfo.InvariantCulture));
    }
}
