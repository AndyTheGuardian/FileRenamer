namespace FileRenamer.Core.Rules;

/// <summary>
/// Art eines Umbenennungsverfahrens. Bestimmt u. a. die Farbe der Hervorhebung.
/// </summary>
public enum RuleKind
{
    /// <summary>Text ersetzen.</summary>
    ReplaceText,

    /// <summary>Datum / Zeit anfügen.</summary>
    AppendDate,

    /// <summary>Zähler anhängen.</summary>
    AppendCounter,

    /// <summary>Ziffernkette Wertebereich verschieben.</summary>
    ShiftDigitRange,

    /// <summary>Text einfügen.</summary>
    InsertText,
}

/// <summary>
/// Welche Treffer beim Text ersetzen berücksichtigt werden.
/// </summary>
public enum MatchOccurrence
{
    /// <summary>Nur der erste Treffer.</summary>
    First,

    /// <summary>Nur der letzte Treffer.</summary>
    Last,

    /// <summary>Alle Treffer.</summary>
    All,
}

/// <summary>
/// Ob ein Zusatz vor oder nach dem Dateinamen angefügt wird.
/// </summary>
public enum AffixPosition
{
    /// <summary>Vor dem Namen.</summary>
    BeforeName,

    /// <summary>Nach dem Namen.</summary>
    AfterName,
}

/// <summary>
/// Quelle des Datums für „Datum / Zeit anfügen“.
/// </summary>
public enum DateSource
{
    /// <summary>Erstellungsdatum (Fallback: Änderungsdatum mit Hinweis).</summary>
    CreationDate,

    /// <summary>Änderungsdatum.</summary>
    ModificationDate,
}

/// <summary>
/// Sortierkriterium für die Vergabe der Zählerwerte.
/// </summary>
public enum CounterSortCriterion
{
    /// <summary>Ursprünglicher Dateiname (natürliche Sortierung).</summary>
    Name,

    /// <summary>Erstellungsdatum.</summary>
    CreationDate,

    /// <summary>Änderungsdatum.</summary>
    ModificationDate,

    /// <summary>Dateigröße.</summary>
    Size,
}
