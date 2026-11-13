using FileRenamer.App.Localization;
using FileRenamer.Core.Tagging;
using FileRenamer.Core.Tagging.Changes;

namespace FileRenamer.App.ViewModels.Tags;

/// <summary>
/// Eintrag der Funktionsliste (oben links) oder Gruppenüberschrift.
/// </summary>
public abstract record TagFunctionEntry
{
    /// <summary>
    /// Ob der Eintrag auswählbar ist (Überschriften nicht).
    /// </summary>
    public abstract bool IsSelectable { get; }
}

/// <summary>
/// Überschrift einer Gruppe der Funktionsliste.
/// </summary>
/// <param name="Title">Die Überschrift.</param>
public sealed record TagFunctionGroup(string Title) : TagFunctionEntry
{
    /// <summary>
    /// Überschriften sind nicht auswählbar.
    /// </summary>
    public override bool IsSelectable => false;
}

/// <summary>
/// Eine Funktion der Liste.
/// </summary>
/// <param name="Function">Die Funktion.</param>
public sealed record TagFunctionItem(TagFunction Function) : TagFunctionEntry
{
    /// <summary>
    /// Funktionen sind auswählbar.
    /// </summary>
    public override bool IsSelectable => true;

    /// <summary>
    /// Angezeigter Name.
    /// </summary>
    public string DisplayName => GetTitle(Function);

    /// <summary>
    /// Die Liste in Gruppen wie im Mockup v13.
    /// </summary>
    /// <returns>Die Einträge.</returns>
    public static IReadOnlyList<TagFunctionEntry> CreateEntries() =>
    [
        new TagFunctionGroup(Loc.T("TagGroup.Tags")),
        new TagFunctionItem(TagFunction.Edit),
        new TagFunctionItem(TagFunction.FromFileName),
        new TagFunctionItem(TagFunction.TrackNumbers),
        new TagFunctionGroup(Loc.T("TagGroup.Media")),
        new TagFunctionItem(TagFunction.Cover),
        new TagFunctionItem(TagFunction.Lyrics),
    ];

    /// <summary>
    /// Liefert den Namen einer Funktion.
    /// </summary>
    /// <param name="function">Die Funktion.</param>
    /// <returns>Der Name.</returns>
    public static string GetTitle(TagFunction function) => Loc.T("TagFunction." + function);
}

/// <summary>
/// Bezeichnungen der Tag-Felder in der aktuellen Sprache.
/// </summary>
public static class TagFieldNames
{
    /// <summary>
    /// Liefert die Bezeichnung eines Felds; benutzerdefinierte Felder heißen wie eingegeben.
    /// </summary>
    /// <param name="key">Der Schlüssel.</param>
    /// <returns>Die Bezeichnung.</returns>
    public static string Get(string key) => TagFields.IsCustom(key) ? TagFields.CustomName(key) : Loc.T("TagField." + key);

    /// <summary>
    /// Ermittelt den Schlüssel zu einer Eingabe in „Feld hinzufügen“: bekannte Bezeichnungen (in beiden Sprachen)
    /// ergeben das passende weitere Feld, alles andere ein benutzerdefiniertes Feld.
    /// </summary>
    /// <param name="input">Die Eingabe.</param>
    /// <returns>Der Schlüssel oder <c>null</c> bei leerer Eingabe.</returns>
    public static string? ToKey(string? input)
    {
        var text = input?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var known = TagFields.Standard.Concat(TagFields.Extra).FirstOrDefault(key => Matches(key, text));
        return known ?? TagFields.Custom(text);
    }

    /// <summary>
    /// Prüft, ob eine Eingabe die Bezeichnung oder den Schlüssel eines Felds trifft.
    /// </summary>
    /// <param name="key">Der Schlüssel.</param>
    /// <param name="text">Die Eingabe.</param>
    /// <returns><c>true</c> bei Übereinstimmung (ohne Groß-/Kleinschreibung).</returns>
    private static bool Matches(string key, string text) =>
        string.Equals(key, text, StringComparison.OrdinalIgnoreCase)
        || (Strings.All.TryGetValue("TagField." + key, out var names)
            && (string.Equals(names.German, text, StringComparison.OrdinalIgnoreCase)
                || string.Equals(names.English, text, StringComparison.OrdinalIgnoreCase)));
}
