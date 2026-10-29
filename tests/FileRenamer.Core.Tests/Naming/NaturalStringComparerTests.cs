using FileRenamer.Core.Naming;

namespace FileRenamer.Core.Tests.Naming;

/// <summary>
/// Tests für die natürliche Sortierung.
/// </summary>
public sealed class NaturalStringComparerTests
{
    /// <summary>
    /// Zahlen werden numerisch, Buchstaben ohne Groß-/Kleinschreibung sortiert.
    /// </summary>
    [Fact]
    public void Sort_OrdersNumbersNumerically()
    {
        string[] names = ["datei10.txt", "Datei2.txt", "datei1.txt", "anhang.txt", "datei003.txt"];

        var sorted = names.Order(NaturalStringComparer.Instance).ToList();

        Assert.Equal(["anhang.txt", "datei1.txt", "Datei2.txt", "datei003.txt", "datei10.txt"], sorted);
    }
}
