using FileRenamer.Core.Validation;

namespace FileRenamer.Core.Tests.Validation;

/// <summary>
/// Tests für die plattformübergreifende Dateinamensprüfung.
/// </summary>
public sealed class FileNameValidatorTests
{
    /// <summary>
    /// Ungültige Namen werden erkannt.
    /// </summary>
    /// <param name="fileName">Der Dateiname.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("..")]
    [InlineData("a:b.txt")]
    [InlineData("a?.txt")]
    [InlineData("a\tb.txt")]
    [InlineData("datei.")]
    [InlineData("datei ")]
    [InlineData("CON")]
    [InlineData("nul.txt")]
    [InlineData("com1 .log")]
    public void Validate_InvalidNames_ReturnError(string fileName)
    {
        Assert.NotNull(FileNameValidator.Validate(fileName));
    }

    /// <summary>
    /// Gültige Namen werden akzeptiert, auch reservierte Wörter als Teil eines längeren Namens.
    /// </summary>
    /// <param name="fileName">Der Dateiname.</param>
    [Theory]
    [InlineData("bericht.txt")]
    [InlineData("CONTROL.txt")]
    [InlineData(".bashrc")]
    [InlineData("Ä Ö Ü ß – 2026.pdf")]
    public void Validate_ValidNames_ReturnNull(string fileName)
    {
        Assert.Null(FileNameValidator.Validate(fileName));
    }

    /// <summary>
    /// Namen über 255 Zeichen sind ungültig.
    /// </summary>
    [Fact]
    public void Validate_TooLong_ReturnsError()
    {
        Assert.Null(FileNameValidator.Validate(new string('a', 255)));
        Assert.NotNull(FileNameValidator.Validate(new string('a', 256)));
    }
}
