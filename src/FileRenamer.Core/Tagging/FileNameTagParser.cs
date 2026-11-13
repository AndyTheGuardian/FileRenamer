using System.Text;
using System.Text.RegularExpressions;
using FileRenamer.Core.Localization;

namespace FileRenamer.Core.Tagging;

/// <summary>
/// Liest Tag-Felder mit einer Vorlage aus dem Dateinamen („Tags aus Dateinamen“), z. B. liefert
/// <c>%track% %title%</c> für <c>01 Like A Prayer.mp3</c> Track 1 und Titel „Like A Prayer“.
/// Enthält die Vorlage <c>/</c> oder <c>\</c>, werden die vorderen Teile mit den übergeordneten Ordnern verglichen
/// (z. B. <c>%artist%/%album%/%track% %title%</c>). <c>%skip%</c> überspringt einen beliebigen Teil.
/// </summary>
public static class FileNameTagParser
{
    /// <summary>
    /// Platzhalter → Feldschlüssel (<c>null</c> bei <c>%skip%</c>).
    /// </summary>
    public static IReadOnlyDictionary<string, string?> Placeholders { get; } = new Dictionary<string, string?>
    {
        ["artist"] = TagFields.Artist,
        ["title"] = TagFields.Title,
        ["album"] = TagFields.Album,
        ["albumartist"] = TagFields.AlbumArtist,
        ["track"] = TagFields.Track,
        ["disc"] = TagFields.Disc,
        ["year"] = TagFields.Year,
        ["genre"] = TagFields.Genre,
        ["composer"] = TagFields.Composer,
        ["skip"] = null,
    };

    private static readonly Regex PlaceholderPattern = new("%([a-z]+)%", RegexOptions.CultureInvariant);

    /// <summary>
    /// Prüft eine Vorlage.
    /// </summary>
    /// <param name="template">Die Vorlage.</param>
    /// <returns>Fehlermeldung oder <c>null</c>, wenn die Vorlage gültig ist.</returns>
    public static string? Validate(string? template)
    {
        if (string.IsNullOrWhiteSpace(template) || !PlaceholderPattern.IsMatch(template))
        {
            return CoreText.FileNameTemplateEmpty;
        }

        var unknown = PlaceholderPattern.Matches(template).Select(match => match.Groups[1].Value)
            .Where(name => !Placeholders.ContainsKey(name)).Distinct().ToList();
        return unknown.Count == 0 ? null : CoreText.UnknownPlaceholder(string.Join(", ", unknown.Select(name => $"%{name}%")));
    }

    /// <summary>
    /// Liest die Felder aus dem Pfad einer Datei.
    /// </summary>
    /// <param name="template">Die (gültige) Vorlage.</param>
    /// <param name="path">Pfad der Datei; die letzte Endung wird ignoriert.</param>
    /// <param name="underscoreAsSpace">Ob Unterstriche als Leerzeichen gelesen werden.</param>
    /// <returns>Feldschlüssel → Wert oder <c>null</c>, wenn der Name nicht zur Vorlage passt.</returns>
    public static IReadOnlyDictionary<string, string>? Parse(string template, string path, bool underscoreAsSpace)
    {
        if (Validate(template) is not null)
        {
            return null;
        }

        var subject = BuildSubject(template, path, underscoreAsSpace);
        var match = BuildRegex(template).Match(subject);
        return match.Success ? CollectValues(match) : null;
    }

    /// <summary>
    /// Bildet den zu vergleichenden Text: so viele Pfadteile wie die Vorlage, getrennt durch „/“.
    /// </summary>
    /// <param name="template">Die Vorlage.</param>
    /// <param name="path">Pfad der Datei.</param>
    /// <param name="underscoreAsSpace">Ob Unterstriche als Leerzeichen gelesen werden.</param>
    /// <returns>Der Text.</returns>
    private static string BuildSubject(string template, string path, bool underscoreAsSpace)
    {
        var depth = template.Count(c => c is '/' or '\\');
        var parts = new List<string> { Path.GetFileNameWithoutExtension(path) };
        var directory = Path.GetDirectoryName(path);
        for (var level = 0; level < depth && !string.IsNullOrEmpty(directory); level++)
        {
            parts.Insert(0, Path.GetFileName(directory));
            directory = Path.GetDirectoryName(directory);
        }

        var subject = string.Join('/', parts);
        return underscoreAsSpace ? subject.Replace('_', ' ') : subject;
    }

    /// <summary>
    /// Wandelt die Vorlage in einen regulären Ausdruck um: Texte wörtlich, Zahlenfelder als Ziffern, übrige Felder
    /// als kürzestmöglicher Text.
    /// </summary>
    /// <param name="template">Die Vorlage.</param>
    /// <returns>Der Ausdruck.</returns>
    private static Regex BuildRegex(string template)
    {
        var normalized = template.Replace('\\', '/');
        var pattern = new StringBuilder("^");
        var position = 0;
        var index = 0;
        foreach (Match placeholder in PlaceholderPattern.Matches(normalized))
        {
            pattern.Append(Regex.Escape(normalized[position..placeholder.Index]));
            pattern.Append(GroupFor(placeholder.Groups[1].Value, index++));
            position = placeholder.Index + placeholder.Length;
        }

        pattern.Append(Regex.Escape(normalized[position..])).Append('$');
        return new Regex(pattern.ToString(), RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// Liefert die Gruppe für einen Platzhalter; die Gruppennamen enthalten den Index, damit Platzhalter mehrfach
    /// vorkommen dürfen.
    /// </summary>
    /// <param name="name">Name des Platzhalters.</param>
    /// <param name="index">Laufende Nummer.</param>
    /// <returns>Das Teilmuster.</returns>
    private static string GroupFor(string name, int index)
    {
        var key = Placeholders[name];
        var body = key is TagFields.Track or TagFields.Disc or TagFields.Year ? @"\d+" : "[^/]+?";
        return key is null ? $"(?:{body})" : $"(?<{name}_{index}>{body})";
    }

    /// <summary>
    /// Übernimmt die Werte der benannten Gruppen; führende Nullen von Zahlen entfallen.
    /// </summary>
    /// <param name="match">Der Treffer.</param>
    /// <returns>Feldschlüssel → Wert.</returns>
    private static Dictionary<string, string> CollectValues(Match match)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var group in match.Groups.Cast<Group>().Where(group => group.Success && group.Name.Contains('_')))
        {
            var key = Placeholders[group.Name[..group.Name.LastIndexOf('_')]]!;
            var value = group.Value.Trim();
            values[key] = TagFields.Numeric.Contains(key) ? value.TrimStart('0') : value;
        }

        return values;
    }
}
