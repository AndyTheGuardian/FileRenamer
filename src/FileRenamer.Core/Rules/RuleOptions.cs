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

    /// <summary>Groß-/Kleinschreibung.</summary>
    ChangeCase,

    /// <summary>Zeichen bereinigen.</summary>
    CleanCharacters,

    /// <summary>Bereich entfernen / kürzen.</summary>
    RemoveRange,

    /// <summary>Name aus Vorlage (EXIF / Audio-Tags).</summary>
    Template,

    /// <summary>Ordnername einfügen.</summary>
    InsertFolder,

    /// <summary>Erweiterung ändern.</summary>
    ChangeExtension,
}

/// <summary>
/// Schreibweise für „Groß-/Kleinschreibung“.
/// </summary>
public enum CaseMode
{
    /// <summary>kleinschreibung.</summary>
    Lower,

    /// <summary>GROSSSCHREIBUNG (ß bleibt ß).</summary>
    Upper,

    /// <summary>Erster buchstabe groß.</summary>
    Sentence,

    /// <summary>Jedes Wort Groß.</summary>
    Title,

    /// <summary>PascalCase.</summary>
    Pascal,

    /// <summary>camelCase.</summary>
    Camel,

    /// <summary>snake_case.</summary>
    Snake,

    /// <summary>kebab-case.</summary>
    Kebab,
}

/// <summary>
/// Modus für „Bereich entfernen / kürzen“.
/// </summary>
public enum RemoveMode
{
    /// <summary>Eine Anzahl Zeichen ab einer Position entfernen.</summary>
    Cut,

    /// <summary>Den Namen auf eine maximale Länge kürzen.</summary>
    Truncate,
}

/// <summary>
/// Schreibweise der Endung für „Erweiterung ändern“.
/// </summary>
public enum ExtensionCasing
{
    /// <summary>Unverändert.</summary>
    Unchanged,

    /// <summary>klein.</summary>
    Lower,

    /// <summary>GROSS.</summary>
    Upper,
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

    /// <summary>EXIF-Aufnahmedatum (Fallback: Änderungsdatum mit Hinweis, E60).</summary>
    ExifDate,
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
