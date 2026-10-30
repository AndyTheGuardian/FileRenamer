using FileRenamer.Core.Rules.Templates;

namespace FileRenamer.Core.Tests.Rules;

/// <summary>
/// Tests für die Liste eigener Vorlagen (E106).
/// </summary>
public sealed class CustomTemplateListTests
{
    private static readonly string[] BuiltIn = ["%artist% - %title%"];

    /// <summary>
    /// Neue Vorlagen kommen nach vorn, Leerraum am Rand wird entfernt.
    /// </summary>
    [Fact]
    public void Add_PutsNewestFirstAndTrims()
    {
        var list = CustomTemplateList.Add(["%a%"], BuiltIn, "  %b%  ");

        Assert.Equal(["%b%", "%a%"], list);
    }

    /// <summary>
    /// Leere, bereits gespeicherte und Beispielvorlagen werden nicht aufgenommen.
    /// </summary>
    /// <param name="template">Die eingegebene Vorlage.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("%a%")]
    [InlineData(" %a% ")]
    [InlineData("%artist% - %title%")]
    public void Add_IgnoresEmptyAndDuplicates(string? template)
    {
        IReadOnlyList<string> custom = ["%a%"];

        Assert.False(CustomTemplateList.CanAdd(custom, BuiltIn, template));
        Assert.Same(custom, CustomTemplateList.Add(custom, BuiltIn, template));
    }

    /// <summary>
    /// Über der Höchstzahl entfällt die älteste Vorlage.
    /// </summary>
    [Fact]
    public void Add_DropsOldestBeyondMaximum()
    {
        var full = Enumerable.Range(1, CustomTemplateList.MaxCount).Select(i => $"%t{i}%").Reverse().ToList();

        var list = CustomTemplateList.Add(full, BuiltIn, "%new%");

        Assert.Equal(CustomTemplateList.MaxCount, list.Count);
        Assert.Equal("%new%", list[0]);
        Assert.DoesNotContain("%t1%", list);
    }

    /// <summary>
    /// Entfernen lässt die übrigen Vorlagen in ihrer Reihenfolge.
    /// </summary>
    [Fact]
    public void Remove_KeepsOrderOfOthers()
    {
        Assert.Equal(["%c%", "%a%"], CustomTemplateList.Remove(["%c%", "%b%", "%a%"], "%b%"));
    }
}
