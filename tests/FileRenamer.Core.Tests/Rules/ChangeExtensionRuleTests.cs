using FileRenamer.Core.Files;
using FileRenamer.Core.Preview;
using FileRenamer.Core.Rules;
using FileRenamer.Core.Tests.Support;

namespace FileRenamer.Core.Tests.Rules;

/// <summary>
/// Tests für „Erweiterung ändern“ – auch bei aktivem „Dateierweiterung ignorieren“ (E73).
/// </summary>
public sealed class ChangeExtensionRuleTests
{
    private readonly InMemoryFileSystem _fileSystem = new();

    /// <summary>
    /// Filter, neue Endung und Schreibweise.
    /// </summary>
    /// <param name="only">Nur bei Endung.</param>
    /// <param name="replacement">Neue Endung.</param>
    /// <param name="casing">Schreibweise.</param>
    /// <param name="extension">Ausgangsendung.</param>
    /// <param name="expected">Erwartete Endung.</param>
    [Theory]
    [InlineData("jpeg", "jpg", ExtensionCasing.Lower, ".JPEG", ".jpg")]
    [InlineData("jpeg", "jpg", ExtensionCasing.Lower, ".png", ".png")]
    [InlineData("", "", ExtensionCasing.Upper, ".jpg", ".JPG")]
    [InlineData("", "", ExtensionCasing.Unchanged, ".Jpg", ".Jpg")]
    [InlineData(".JPEG", ".jpg", ExtensionCasing.Unchanged, ".jpeg", ".jpg")]
    [InlineData("", "txt", ExtensionCasing.Lower, "", ".txt")]
    public void ConvertExtension(string only, string replacement, ExtensionCasing casing, string extension, string expected)
    {
        var rule = new ChangeExtensionRule { OnlyExtension = only, NewExtension = replacement, Casing = casing };

        Assert.Equal(expected, rule.ConvertExtension(extension));
    }

    /// <summary>
    /// Wirkt bei ausgeblendeter und eingeblendeter Endung; die geänderte ausgeblendete Endung wird markiert.
    /// </summary>
    /// <param name="ignoreExtension">Dateierweiterung ignorieren.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Pipeline_ChangesExtensionInBothModes(bool ignoreExtension)
    {
        var files = new List<FileEntry> { _fileSystem.Add(Path.Combine(RuleTestHelper.Directory, "Foto.JPEG")), _fileSystem.Add(Path.Combine(RuleTestHelper.Directory, "Text.txt")) };
        RenameRule[] chain = [new InsertTextRule { Text = "x_" }, new ChangeExtensionRule { OnlyExtension = "jpeg", NewExtension = "jpg" }];

        var result = new RenamePipeline(_fileSystem).CreatePreview(files, chain, ignoreExtension);

        Assert.Equal(["x_Foto.jpg", "x_Text.txt"], result.Files.Select(file => file.NewFileName));
        Assert.Equal(ignoreExtension, result.Files[0].IsExtensionChanged);
        Assert.False(result.Files[1].IsExtensionChanged);
    }
}
