namespace FileRenamer.Core.Naming;

/// <summary>
/// Ein zusammenhängender Abschnitt eines Dateinamens mit der Information,
/// welche Umbenennungsverfahren ihn erzeugt oder verändert haben.
/// </summary>
/// <param name="Text">Der Text des Abschnitts.</param>
/// <param name="Sources">
/// Ids der beteiligten Verfahren in Anwendungsreihenfolge; leer bei unverändertem Originaltext.
/// </param>
public sealed record NameSegment(string Text, IReadOnlyList<Guid> Sources)
{
    /// <summary>
    /// Gibt an, ob der Abschnitt unveränderter Originaltext ist.
    /// </summary>
    /// <returns><c>true</c>, wenn kein Verfahren beteiligt war.</returns>
    public bool IsOriginal => Sources.Count == 0;

    /// <summary>
    /// Ermittelt das Verfahren, dessen Farbe für die Hervorhebung verwendet wird.
    /// Ist das bevorzugte Verfahren beteiligt, hat es Vorrang; sonst gilt das zuletzt angewandte.
    /// </summary>
    /// <param name="preferredRuleId">Id des aktuell ausgewählten Verfahrens oder <c>null</c>.</param>
    /// <returns>Id des maßgeblichen Verfahrens oder <c>null</c> bei Originaltext.</returns>
    public Guid? GetHighlightSource(Guid? preferredRuleId)
    {
        if (IsOriginal)
        {
            return null;
        }

        if (preferredRuleId is { } preferred && Sources.Contains(preferred))
        {
            return preferred;
        }

        return Sources[^1];
    }

    /// <summary>
    /// Prüft, ob zwei Abschnitte dieselben Verfahren als Herkunft haben.
    /// </summary>
    /// <param name="other">Der zu vergleichende Abschnitt.</param>
    /// <returns><c>true</c> bei identischer Herkunft.</returns>
    public bool HasSameSources(NameSegment other) => Sources.SequenceEqual(other.Sources);
}
