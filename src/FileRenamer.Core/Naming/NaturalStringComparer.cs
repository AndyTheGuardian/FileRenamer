namespace FileRenamer.Core.Naming;

/// <summary>
/// Vergleicht Zeichenketten „natürlich“: Ziffernfolgen werden als Zahlen verglichen
/// (<c>datei2</c> vor <c>datei10</c>), Buchstaben ohne Beachtung der Groß-/Kleinschreibung.
/// </summary>
public sealed class NaturalStringComparer : IComparer<string?>
{
    /// <summary>
    /// Gemeinsam nutzbare Instanz.
    /// </summary>
    public static NaturalStringComparer Instance { get; } = new();

    /// <summary>
    /// Vergleicht zwei Zeichenketten in natürlicher Sortierreihenfolge.
    /// </summary>
    /// <param name="x">Erste Zeichenkette.</param>
    /// <param name="y">Zweite Zeichenkette.</param>
    /// <returns>Negativ, null oder positiv wie bei <see cref="IComparer{T}.Compare"/>.</returns>
    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null || y is null)
        {
            return x is null ? -1 : 1;
        }

        var i = 0;
        var j = 0;

        while (i < x.Length && j < y.Length)
        {
            var result = char.IsAsciiDigit(x[i]) && char.IsAsciiDigit(y[j])
                ? CompareNumbers(x, ref i, y, ref j)
                : char.ToUpperInvariant(x[i++]).CompareTo(char.ToUpperInvariant(y[j++]));

            if (result != 0)
            {
                return result;
            }
        }

        var lengthResult = (x.Length - i).CompareTo(y.Length - j);
        return lengthResult != 0 ? lengthResult : string.CompareOrdinal(x, y);
    }

    /// <summary>
    /// Vergleicht die an den aktuellen Positionen beginnenden Ziffernfolgen numerisch
    /// und rückt beide Positionen hinter die Ziffernfolge.
    /// </summary>
    /// <param name="x">Erste Zeichenkette.</param>
    /// <param name="i">Position in <paramref name="x"/>.</param>
    /// <param name="y">Zweite Zeichenkette.</param>
    /// <param name="j">Position in <paramref name="y"/>.</param>
    /// <returns>Vergleichsergebnis der beiden Zahlen.</returns>
    private static int CompareNumbers(string x, ref int i, string y, ref int j)
    {
        var numberX = ReadDigits(x, ref i).TrimStart('0');
        var numberY = ReadDigits(y, ref j).TrimStart('0');

        var lengthResult = numberX.Length.CompareTo(numberY.Length);
        return lengthResult != 0 ? lengthResult : string.CompareOrdinal(numberX, numberY);
    }

    /// <summary>
    /// Liest eine zusammenhängende Ziffernfolge ab der angegebenen Position.
    /// </summary>
    /// <param name="text">Die Zeichenkette.</param>
    /// <param name="index">Startposition; steht danach hinter der Ziffernfolge.</param>
    /// <returns>Die gelesene Ziffernfolge.</returns>
    private static string ReadDigits(string text, ref int index)
    {
        var start = index;
        while (index < text.Length && char.IsAsciiDigit(text[index]))
        {
            index++;
        }

        return text[start..index];
    }
}
