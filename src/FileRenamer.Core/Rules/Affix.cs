using FileRenamer.Core.Naming;

namespace FileRenamer.Core.Rules;

/// <summary>
/// Hilfsfunktionen zum Anfügen eines Zusatzes (Datum, Zähler) vor oder nach dem Namen.
/// </summary>
internal static class Affix
{
    /// <summary>
    /// Fügt den Zusatz samt Trennzeichen vor oder nach dem Namen ein.
    /// </summary>
    /// <param name="name">Der aktuelle Name.</param>
    /// <param name="affix">Der Zusatz.</param>
    /// <param name="separator">Trennzeichen zwischen Name und Zusatz.</param>
    /// <param name="position">Vor oder nach dem Namen.</param>
    /// <param name="ruleId">Id des verursachenden Verfahrens.</param>
    /// <returns>Der geänderte Name.</returns>
    public static SegmentedName Add(SegmentedName name, string affix, string separator, AffixPosition position, Guid ruleId) =>
        position == AffixPosition.BeforeName
            ? name.Insert(0, affix + separator, ruleId)
            : name.Insert(name.Length, separator + affix, ruleId);
}
