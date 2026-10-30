using FileRenamer.Core.Localization;
using System.Text.RegularExpressions;

namespace FileRenamer.Core.Rules;

/// <summary>
/// Verfahren „Text ersetzen“: findet einen Text (oder regulären Ausdruck) im Namen und ersetzt
/// den ersten, letzten oder alle Treffer. Groß-/Kleinschreibung wird immer berücksichtigt.
/// </summary>
public sealed record ReplaceTextRule : PerFileRenameRule
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gesuchter Text bzw. regulärer Ausdruck. Bei leerem Suchtext bleibt der Name unverändert.
    /// </summary>
    public string SearchText { get; init; } = string.Empty;

    /// <summary>
    /// Ersatztext. Bei regulären Ausdrücken sind Gruppenverweise wie <c>$1</c> möglich.
    /// </summary>
    public string Replacement { get; init; } = string.Empty;

    /// <summary>
    /// Welche Treffer ersetzt werden.
    /// </summary>
    public MatchOccurrence Occurrence { get; init; } = MatchOccurrence.All;

    /// <summary>
    /// Ob <see cref="SearchText"/> als regulärer Ausdruck interpretiert wird.
    /// </summary>
    public bool UseRegex { get; init; }

    /// <inheritdoc />
    public override RuleKind Kind => RuleKind.ReplaceText;

    /// <summary>
    /// Prüft, ob der reguläre Ausdruck gültig ist.
    /// </summary>
    /// <returns>Fehlermeldung oder <c>null</c>.</returns>
    public override string? ValidateSettings()
    {
        if (!UseRegex || SearchText.Length == 0)
        {
            return null;
        }

        try
        {
            _ = CreateRegex();
            return null;
        }
        catch (ArgumentException exception)
        {
            return CoreText.InvalidRegex(exception.Message);
        }
    }

    /// <summary>
    /// Ersetzt die ausgewählten Treffer im Namen; die Ersetzung erfolgt von hinten nach vorn,
    /// damit die Positionen der übrigen Treffer gültig bleiben.
    /// </summary>
    /// <param name="input">Die Datei mit ihrem aktuellen Namen.</param>
    /// <returns>Das Ergebnis für diese Datei.</returns>
    protected override RuleOutput ApplyToFile(RuleInput input)
    {
        if (SearchText.Length == 0)
        {
            return RuleOutput.Unchanged(input.Name);
        }

        IReadOnlyList<TextMatch> matches;
        try
        {
            matches = SelectOccurrences(FindMatches(input.Name.Text));
        }
        catch (RegexMatchTimeoutException)
        {
            return RuleOutput.Unchanged(input.Name,
                RenameIssue.Conflict(CoreText.RegexTimeout, Id));
        }

        var name = input.Name;
        foreach (var match in matches.OrderByDescending(match => match.Index))
        {
            name = name.Replace(match.Index, match.Length, match.Replacement, Id);
        }

        return RuleOutput.Changed(name);
    }

    /// <summary>
    /// Findet alle Treffer im Text.
    /// </summary>
    /// <param name="text">Der zu durchsuchende Name.</param>
    /// <returns>Alle Treffer in aufsteigender Reihenfolge.</returns>
    private List<TextMatch> FindMatches(string text) =>
        UseRegex ? FindRegexMatches(text) : FindLiteralMatches(text);

    /// <summary>
    /// Findet nicht überlappende Treffer des wörtlichen Suchtexts.
    /// </summary>
    /// <param name="text">Der zu durchsuchende Name.</param>
    /// <returns>Die Treffer.</returns>
    private List<TextMatch> FindLiteralMatches(string text)
    {
        var matches = new List<TextMatch>();
        var index = text.IndexOf(SearchText, StringComparison.Ordinal);

        while (index >= 0)
        {
            matches.Add(new TextMatch(index, SearchText.Length, Replacement));
            index = text.IndexOf(SearchText, index + SearchText.Length, StringComparison.Ordinal);
        }

        return matches;
    }

    /// <summary>
    /// Findet die Treffer des regulären Ausdrucks und berechnet den jeweiligen Ersatztext.
    /// </summary>
    /// <param name="text">Der zu durchsuchende Name.</param>
    /// <returns>Die Treffer.</returns>
    private List<TextMatch> FindRegexMatches(string text) =>
        CreateRegex().Matches(text)
            .Select(match => new TextMatch(match.Index, match.Length, match.Result(Replacement)))
            .ToList();

    /// <summary>
    /// Wählt entsprechend <see cref="Occurrence"/> die zu ersetzenden Treffer aus.
    /// </summary>
    /// <param name="matches">Alle Treffer.</param>
    /// <returns>Die ausgewählten Treffer.</returns>
    private IReadOnlyList<TextMatch> SelectOccurrences(List<TextMatch> matches) =>
        matches.Count == 0
            ? matches
            : Occurrence switch
            {
                MatchOccurrence.First => [matches[0]],
                MatchOccurrence.Last => [matches[^1]],
                _ => matches,
            };

    /// <summary>
    /// Erzeugt den regulären Ausdruck mit Zeitlimit (Schutz vor sehr aufwendigen Mustern).
    /// </summary>
    /// <returns>Der reguläre Ausdruck.</returns>
    private Regex CreateRegex() => new(SearchText, RegexOptions.CultureInvariant, RegexTimeout);

    /// <summary>
    /// Ein Treffer mit Position, Länge und berechnetem Ersatztext.
    /// </summary>
    /// <param name="Index">Startposition.</param>
    /// <param name="Length">Länge.</param>
    /// <param name="Replacement">Ersatztext.</param>
    private sealed record TextMatch(int Index, int Length, string Replacement);
}
