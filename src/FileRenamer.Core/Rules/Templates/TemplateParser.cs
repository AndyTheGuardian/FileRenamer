using System.Text.RegularExpressions;

namespace FileRenamer.Core.Rules.Templates;

/// <summary>
/// Teil einer Vorlage: fester Text oder Platzhalter mit optionalem Format.
/// </summary>
/// <param name="Literal">Fester Text oder <c>null</c>.</param>
/// <param name="Placeholder">Name des Platzhalters (klein) oder <c>null</c>.</param>
/// <param name="Format">Format nach dem Doppelpunkt oder <c>null</c>.</param>
public sealed record TemplateToken(string? Literal, string? Placeholder, string? Format);

/// <summary>
/// Zerlegt Vorlagen wie <c>%artist% - %title%</c> oder <c>%date:yyyy-MM-dd%</c> in Teile.
/// </summary>
public static partial class TemplateParser
{
    /// <summary>
    /// Zerlegt eine Vorlage.
    /// </summary>
    /// <param name="template">Die Vorlage.</param>
    /// <returns>Die Teile in Reihenfolge.</returns>
    public static IReadOnlyList<TemplateToken> Parse(string template)
    {
        var tokens = new List<TemplateToken>();
        var position = 0;
        foreach (Match match in PlaceholderPattern().Matches(template))
        {
            if (match.Index > position)
            {
                tokens.Add(new TemplateToken(template[position..match.Index], null, null));
            }

            var format = match.Groups[2].Success ? match.Groups[2].Value : null;
            tokens.Add(new TemplateToken(null, match.Groups[1].Value.ToLowerInvariant(), format));
            position = match.Index + match.Length;
        }

        if (position < template.Length)
        {
            tokens.Add(new TemplateToken(template[position..], null, null));
        }

        return tokens;
    }

    /// <summary>
    /// Platzhalter: <c>%name%</c> oder <c>%name:format%</c>.
    /// </summary>
    /// <returns>Der reguläre Ausdruck.</returns>
    [GeneratedRegex(@"%(\w+)(?::([^%]+))?%")]
    private static partial Regex PlaceholderPattern();
}
