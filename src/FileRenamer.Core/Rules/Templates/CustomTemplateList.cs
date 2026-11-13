namespace FileRenamer.Core.Rules.Templates;

/// <summary>
/// Regeln für die Liste eigener Vorlagen eines Tabs von „Name aus Vorlage“ (E106):
/// neueste zuerst, keine leeren oder doppelten Einträge, höchstens <see cref="MaxCount"/>.
/// </summary>
public static class CustomTemplateList
{
    /// <summary>
    /// Höchstzahl eigener Vorlagen je Tab.
    /// </summary>
    public const int MaxCount = 10;

    /// <summary>
    /// Prüft, ob eine Vorlage gespeichert werden kann.
    /// </summary>
    /// <param name="custom">Die eigenen Vorlagen.</param>
    /// <param name="builtIn">Die Beispielvorlagen des Tabs.</param>
    /// <param name="template">Die eingegebene Vorlage.</param>
    /// <returns><c>true</c>, wenn sie nicht leer und weder eigene noch Beispielvorlage ist.</returns>
    public static bool CanAdd(IReadOnlyList<string> custom, IEnumerable<string> builtIn, string? template)
    {
        var trimmed = template?.Trim();
        return !string.IsNullOrEmpty(trimmed) && !custom.Contains(trimmed, StringComparer.Ordinal)
            && !builtIn.Contains(trimmed, StringComparer.Ordinal);
    }

    /// <summary>
    /// Nimmt eine Vorlage vorne in die Liste auf; bei mehr als <see cref="MaxCount"/> Einträgen entfällt die älteste.
    /// </summary>
    /// <param name="custom">Die eigenen Vorlagen (neueste zuerst).</param>
    /// <param name="builtIn">Die Beispielvorlagen des Tabs.</param>
    /// <param name="template">Die eingegebene Vorlage.</param>
    /// <param name="maxCount">Höchstzahl der Einträge (Standard <see cref="MaxCount"/>).</param>
    /// <returns>Die neue Liste; unverändert, wenn <see cref="CanAdd"/> nicht zutrifft.</returns>
    public static IReadOnlyList<string> Add(
        IReadOnlyList<string> custom, IEnumerable<string> builtIn, string? template, int maxCount = MaxCount) =>
        CanAdd(custom, builtIn, template) ? custom.Prepend(template!.Trim()).Take(maxCount).ToList() : custom;

    /// <summary>
    /// Entfernt eine Vorlage aus der Liste.
    /// </summary>
    /// <param name="custom">Die eigenen Vorlagen.</param>
    /// <param name="template">Die zu entfernende Vorlage.</param>
    /// <returns>Die neue Liste.</returns>
    public static IReadOnlyList<string> Remove(IReadOnlyList<string> custom, string template) =>
        custom.Where(entry => !string.Equals(entry, template, StringComparison.Ordinal)).ToList();
}
