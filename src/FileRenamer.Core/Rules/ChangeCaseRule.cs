using System.Globalization;
using System.Text.RegularExpressions;

namespace FileRenamer.Core.Rules;

/// <summary>
/// Verfahren „Groß-/Kleinschreibung“ (E58, E59). Wortgrenzen: Leerzeichen, <c>_</c>, <c>-</c>, <c>.</c> und der
/// Wechsel von klein zu GROSS; Ziffern bleiben am Wort; ß bleibt beim Großschreiben ß.
/// </summary>
public sealed partial record ChangeCaseRule : PerFileRenameRule
{
    private static readonly TextInfo Invariant = CultureInfo.InvariantCulture.TextInfo;

    /// <summary>
    /// Gewünschte Schreibweise.
    /// </summary>
    public CaseMode Mode { get; init; } = CaseMode.Pascal;

    /// <inheritdoc />
    public override RuleKind Kind => RuleKind.ChangeCase;

    /// <summary>
    /// Wandelt einen Text in die gewünschte Schreibweise um.
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <param name="mode">Die Schreibweise.</param>
    /// <returns>Der umgewandelte Text.</returns>
    public static string Convert(string text, CaseMode mode) => mode switch
    {
        CaseMode.Lower => Invariant.ToLower(text),
        CaseMode.Upper => Invariant.ToUpper(text),
        CaseMode.Sentence => text.Length == 0 ? text : Invariant.ToUpper(text[..1]) + Invariant.ToLower(text[1..]),
        CaseMode.Title => WordCharacters().Replace(text, match => Capitalize(match.Value)),
        CaseMode.Pascal => string.Concat(SplitWords(text).Select(Capitalize)),
        CaseMode.Camel => string.Concat(SplitWords(text).Select((word, index) => index == 0 ? Invariant.ToLower(word) : Capitalize(word))),
        CaseMode.Snake => string.Join('_', SplitWords(text).Select(word => Invariant.ToLower(word))),
        _ => string.Join('-', SplitWords(text).Select(word => Invariant.ToLower(word))),
    };

    /// <summary>
    /// Wendet die Schreibweise an; bei gleicher Länge werden nur die geänderten Zeichen markiert.
    /// </summary>
    /// <param name="input">Die Datei mit ihrem aktuellen Namen.</param>
    /// <returns>Das Ergebnis für diese Datei.</returns>
    protected override RuleOutput ApplyToFile(RuleInput input) =>
        RuleOutput.Changed(input.Name.ReplaceText(Convert(input.Name.Text, Mode), Id));

    /// <summary>
    /// Zerlegt einen Text in Wörter.
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <returns>Die Wörter ohne Trennzeichen.</returns>
    private static IEnumerable<string> SplitWords(string text) =>
        WordBoundary().Split(text).Where(word => word.Length > 0);

    /// <summary>
    /// Erster Buchstabe groß, Rest klein.
    /// </summary>
    /// <param name="word">Das Wort.</param>
    /// <returns>Das umgewandelte Wort.</returns>
    private static string Capitalize(string word) =>
        word.Length == 0 ? word : Invariant.ToUpper(word[..1]) + Invariant.ToLower(word[1..]);

    /// <summary>
    /// Trennstellen zwischen Wörtern: Trennzeichen oder Wechsel klein → GROSS.
    /// </summary>
    /// <returns>Der reguläre Ausdruck.</returns>
    [GeneratedRegex(@"[\s_.\-]+|(?<=\p{Ll})(?=\p{Lu})")]
    private static partial Regex WordBoundary();

    /// <summary>
    /// Zusammenhängende Wortzeichen (für „Jedes Wort Groß“, Trennzeichen bleiben erhalten).
    /// </summary>
    /// <returns>Der reguläre Ausdruck.</returns>
    [GeneratedRegex(@"[^\s_.\-]+")]
    private static partial Regex WordCharacters();
}
