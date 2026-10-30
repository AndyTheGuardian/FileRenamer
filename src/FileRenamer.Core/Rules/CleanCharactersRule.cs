using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FileRenamer.Core.Rules;

/// <summary>
/// Verfahren „Zeichen bereinigen“ (V16/E67): Leerzeichen am Rand entfernen, mehrfache Leerzeichen
/// zusammenfassen, Umlaute umschreiben und Akzente entfernen. Umlaute werden vor den Akzenten behandelt,
/// damit „ä“ zu „ae“ und nicht zu „a“ wird.
/// </summary>
public sealed partial record CleanCharactersRule : PerFileRenameRule
{
    private static readonly Dictionary<char, string> Umlauts = new()
    {
        ['ä'] = "ae", ['ö'] = "oe", ['ü'] = "ue", ['Ä'] = "Ae", ['Ö'] = "Oe", ['Ü'] = "Ue", ['ß'] = "ss", ['ẞ'] = "SS",
    };

    /// <summary>
    /// Leerzeichen am Anfang und Ende entfernen.
    /// </summary>
    public bool TrimSpaces { get; init; } = true;

    /// <summary>
    /// Mehrfache Leerzeichen zu einem zusammenfassen.
    /// </summary>
    public bool CollapseSpaces { get; init; } = true;

    /// <summary>
    /// Umlaute und ß umschreiben.
    /// </summary>
    public bool TransliterateUmlauts { get; init; } = true;

    /// <summary>
    /// Akzente entfernen (é → e).
    /// </summary>
    public bool RemoveAccents { get; init; } = true;

    /// <inheritdoc />
    public override RuleKind Kind => RuleKind.CleanCharacters;

    /// <summary>
    /// Bereinigt einen Text mit den gewählten Optionen.
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <returns>Der bereinigte Text.</returns>
    public string Clean(string text)
    {
        if (TransliterateUmlauts)
        {
            text = string.Concat(text.Select(c => Umlauts.TryGetValue(c, out var replacement) ? replacement : c.ToString()));
        }

        if (RemoveAccents)
        {
            text = StripAccents(text);
        }

        if (CollapseSpaces)
        {
            text = MultipleSpaces().Replace(text, " ");
        }

        return TrimSpaces ? text.Trim() : text;
    }

    /// <summary>
    /// Wendet die Bereinigung an.
    /// </summary>
    /// <param name="input">Die Datei mit ihrem aktuellen Namen.</param>
    /// <returns>Das Ergebnis für diese Datei.</returns>
    protected override RuleOutput ApplyToFile(RuleInput input) =>
        RuleOutput.Changed(input.Name.ReplaceText(Clean(input.Name.Text), Id));

    /// <summary>
    /// Entfernt diakritische Zeichen über die Unicode-Zerlegung.
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <returns>Der Text ohne Akzente.</returns>
    private static string StripAccents(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
        {
            builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Zwei oder mehr Leerzeichen.
    /// </summary>
    /// <returns>Der reguläre Ausdruck.</returns>
    [GeneratedRegex(" {2,}")]
    private static partial Regex MultipleSpaces();
}
