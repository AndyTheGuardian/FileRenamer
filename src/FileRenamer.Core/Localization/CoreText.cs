using System.Globalization;

namespace FileRenamer.Core.Localization;

/// <summary>
/// Sprache der Oberfläche und der Meldungen (E92).
/// </summary>
public enum UiLanguage
{
    /// <summary>Deutsch.</summary>
    German,

    /// <summary>Englisch.</summary>
    English,
}

/// <summary>
/// Alle Meldungen der Fachlogik (Hinweise, Konflikte, Prüfungen) in Deutsch und Englisch. Die Sprache gilt
/// programmweit; die Oberfläche setzt sie beim Start und bei jedem Wechsel und berechnet danach die Vorschau neu.
/// </summary>
public static partial class CoreText
{
    /// <summary>
    /// Aktuelle Sprache der Meldungen (Standard: Deutsch).
    /// </summary>
    public static UiLanguage Language { get; set; } = UiLanguage.German;

    /// <summary>
    /// Ermittelt die Sprache für „System“: Deutsch bei deutscher Systemsprache, sonst Englisch (E93).
    /// </summary>
    /// <param name="culture">Die Systemkultur.</param>
    /// <returns>Die Sprache.</returns>
    public static UiLanguage FromCulture(CultureInfo culture) =>
        culture.TwoLetterISOLanguageName == "de" ? UiLanguage.German : UiLanguage.English;

    /// <summary>
    /// Ermittelt die Sprache zu einer gespeicherten Auswahl.
    /// </summary>
    /// <param name="preference">Die Auswahl.</param>
    /// <param name="systemCulture">Die Systemkultur (für „System“).</param>
    /// <returns>Die Sprache.</returns>
    public static UiLanguage Resolve(Profiles.LanguagePreference preference, CultureInfo systemCulture) => preference switch
    {
        Profiles.LanguagePreference.German => UiLanguage.German,
        Profiles.LanguagePreference.English => UiLanguage.English,
        _ => FromCulture(systemCulture),
    };

    // ---------- Dateinamen ----------

    /// <summary>Leerer Dateiname.</summary>
    public static string FileNameEmpty => Pick("Der Dateiname ist leer.", "The file name is empty.");

    /// <summary>„.“ oder „..“.</summary>
    /// <param name="name">Der Name.</param>
    /// <returns>Die Meldung.</returns>
    public static string FileNameInvalid(string name) => Pick($"„{name}“ ist kein gültiger Dateiname.", $"“{name}” is not a valid file name.");

    /// <summary>Steuerzeichen.</summary>
    public static string ControlCharacter => Pick("Unzulässiges Steuerzeichen im Dateinamen.", "Control characters are not allowed in file names.");

    /// <summary>Verbotenes Zeichen.</summary>
    /// <param name="character">Das Zeichen.</param>
    /// <returns>Die Meldung.</returns>
    public static string ForbiddenCharacter(char character) => Pick(
        $"Unzulässiges Zeichen „{character}“ (verboten: < > : \" / \\ | ? *).",
        $"Character “{character}” is not allowed (forbidden: < > : \" / \\ | ? *).");

    /// <summary>Punkt oder Leerzeichen am Ende.</summary>
    public static string TrailingDotOrSpace => Pick(
        "Der Dateiname darf nicht auf Punkt oder Leerzeichen enden.", "The file name must not end with a dot or a space.");

    /// <summary>Reservierter Name.</summary>
    /// <param name="stem">Der Name.</param>
    /// <returns>Die Meldung.</returns>
    public static string ReservedName(string stem) => Pick($"„{stem}“ ist ein reservierter Name.", $"“{stem}” is a reserved name.");

    /// <summary>Zu langer Name.</summary>
    /// <param name="length">Länge.</param>
    /// <param name="max">Höchstlänge.</param>
    /// <returns>Die Meldung.</returns>
    public static string FileNameTooLong(int length, int max) => Pick(
        $"Der Dateiname ist zu lang ({length} von maximal {max} Zeichen).", $"The file name is too long ({length} of at most {max} characters).");

    /// <summary>Doppelter Zielname.</summary>
    public static string DuplicateTarget => Pick("Der Zielname kommt in der Auswahl mehrfach vor.", "The new name occurs more than once in the selection.");

    /// <summary>Zielname existiert.</summary>
    public static string TargetExists => Pick(
        "Eine Datei oder ein Ordner mit diesem Namen existiert bereits.", "A file or folder with this name already exists.");

    /// <summary>Rücknahme fehlgeschlagen.</summary>
    /// <param name="from">Aktueller Pfad.</param>
    /// <param name="to">Ursprünglicher Pfad.</param>
    /// <param name="reason">Grund.</param>
    /// <returns>Die Meldung.</returns>
    public static string RollbackFailed(string from, string to, string reason) => Pick(
        $"„{from}“ konnte nicht zurück nach „{to}“ benannt werden: {reason}", $"“{from}” could not be renamed back to “{to}”: {reason}");

    // ---------- Formate ----------

    /// <summary>Format fehlt.</summary>
    public static string FormatMissing => Pick("Bitte ein Format angeben.", "Please enter a format.");

    /// <summary>Datumsformat ohne Datumsbestandteil.</summary>
    public static string DateFormatWithoutDate => Pick(
        "Das Datumsformat enthält keinen Datumsbestandteil (y, M, d).", "The date format contains no date part (y, M, d).");

    /// <summary>Uhrzeitformat ohne Zeitbestandteil.</summary>
    public static string TimeFormatWithoutTime => Pick(
        "Das Uhrzeitformat enthält keinen Zeitbestandteil (H, h, m, s).", "The time format contains no time part (H, h, m, s).");

    /// <summary>Ungültiges Format.</summary>
    public static string FormatInvalid => Pick("Das Format ist ungültig.", "The format is invalid.");

    /// <summary>Format ergibt verbotene Zeichen.</summary>
    /// <param name="sample">Beispielergebnis.</param>
    /// <param name="reason">Grund.</param>
    /// <returns>Die Meldung.</returns>
    public static string FormatProduces(string sample, string reason) => Pick($"Das Format ergibt „{sample}“: {reason}", $"The format produces “{sample}”: {reason}");

    // ---------- Verfahren ----------

    /// <summary>Ungültiger regulärer Ausdruck.</summary>
    /// <param name="reason">Grund.</param>
    /// <returns>Die Meldung.</returns>
    public static string InvalidRegex(string reason) => Pick($"Ungültiger regulärer Ausdruck: {reason}", $"Invalid regular expression: {reason}");

    /// <summary>Zeitüberschreitung beim regulären Ausdruck.</summary>
    public static string RegexTimeout => Pick(
        "Der reguläre Ausdruck braucht zu lange (Zeitüberschreitung).", "The regular expression takes too long (timeout).");

    /// <summary>Erstellungsdatum fehlt.</summary>
    public static string CreationDateFallback => Pick(
        "Erstellungsdatum nicht verfügbar – Änderungsdatum wurde verwendet.", "Creation date not available – modification date was used.");

    /// <summary>Erstellungsdatum fehlt bei der Sortierung.</summary>
    public static string CreationDateSortFallback => Pick(
        "Erstellungsdatum nicht verfügbar – für die Sortierung wurde das Änderungsdatum verwendet.",
        "Creation date not available – the modification date was used for sorting.");

    /// <summary>EXIF-Datum fehlt.</summary>
    public static string ExifDateFallback => Pick(
        "Kein EXIF-Aufnahmedatum – Änderungsdatum wurde verwendet.", "No EXIF capture date – modification date was used.");

    /// <summary>Ungültige Stellenanzahl.</summary>
    /// <param name="max">Höchstwert.</param>
    /// <returns>Die Meldung.</returns>
    public static string DigitsRange(int max) => Pick($"Die Stellenanzahl muss zwischen 1 und {max} liegen.", $"The number of digits must be between 1 and {max}.");

    /// <summary>Ungültige Mindeststellen.</summary>
    /// <param name="max">Höchstwert.</param>
    /// <returns>Die Meldung.</returns>
    public static string MinDigitsRange(int max) => Pick($"Die Mindeststellen müssen zwischen 1 und {max} liegen.", $"The minimum digits must be between 1 and {max}.");

    /// <summary>Ziffernketten-Nr. kleiner 1.</summary>
    public static string RunNumberTooSmall => Pick("Die Nummer der Ziffernkette muss mindestens 1 sein.", "The digit run number must be at least 1.");

    /// <summary>Keine Ziffernkette.</summary>
    public static string NoDigitRun => Pick("Keine Ziffernkette gefunden – Name unverändert.", "No digit run found – name unchanged.");

    /// <summary>Ziffernketten-Nr. fehlt.</summary>
    /// <param name="number">Gewählte Nummer.</param>
    /// <param name="count">Vorhandene Anzahl.</param>
    /// <returns>Die Meldung.</returns>
    public static string RunNumberMissing(int number, int count) => Pick(
        $"Ziffernkette Nr. {number} ist nicht vorhanden (nur {count}).", $"Digit run no. {number} does not exist (only {count}).");

    /// <summary>Negatives Ergebnis.</summary>
    /// <param name="calculation">Rechnung.</param>
    /// <returns>Die Meldung.</returns>
    public static string NegativeResult(string calculation) => Pick($"Das Ergebnis wäre negativ ({calculation}).", $"The result would be negative ({calculation}).");

    /// <summary>Negative Position.</summary>
    public static string PositionNegative => Pick("Die Position darf nicht negativ sein.", "The position must not be negative.");

    /// <summary>Anzahl kleiner 1.</summary>
    public static string CountTooSmall => Pick("Es muss mindestens ein Zeichen entfernt werden.", "At least one character must be removed.");

    /// <summary>Höchstlänge kleiner 1.</summary>
    public static string MaxLengthTooSmall => Pick("Die maximale Länge muss mindestens 1 sein.", "The maximum length must be at least 1.");

    /// <summary>Ordnerebene kleiner 1.</summary>
    public static string LevelTooSmall => Pick("Die Ordnerebene muss mindestens 1 sein.", "The folder level must be at least 1.");

    /// <summary>Ordnerebene fehlt.</summary>
    /// <param name="level">Die Ebene.</param>
    /// <returns>Die Meldung.</returns>
    public static string FolderLevelMissing(int level) => Pick(
        $"Ordnerebene {level} ist nicht vorhanden – Name unverändert.", $"Folder level {level} does not exist – name unchanged.");

    /// <summary>Weder Foto noch Audio.</summary>
    public static string NotMediaFile => Pick("Weder Foto noch Audiodatei – Name unverändert.", "Neither photo nor audio file – name unchanged.");

    /// <summary>Fehlende Werte.</summary>
    /// <param name="placeholders">Platzhalter.</param>
    /// <param name="replacement">Ersatztext.</param>
    /// <returns>Die Meldung.</returns>
    public static string MissingValues(string placeholders, string replacement) => Pick(
        $"Nicht vorhanden: {placeholders} → „{replacement}“.", $"Not available: {placeholders} → “{replacement}”.");

    /// <summary>Ungültige Tracknummer-Stellen.</summary>
    public static string TrackDigitsRange => Pick(
        "Die Stellen der Tracknummer müssen zwischen 1 und 9 liegen.", "The track number digits must be between 1 and 9.");

    /// <summary>Bezeichnung der Foto-Vorlage.</summary>
    public static string PhotoTemplateLabel => Pick("Foto-Vorlage", "Photo template");

    /// <summary>Bezeichnung der Audio-Vorlage.</summary>
    public static string AudioTemplateLabel => Pick("Audio-Vorlage", "Audio template");

    /// <summary>Unbekannte Platzhalter.</summary>
    /// <param name="placeholders">Platzhalter.</param>
    /// <returns>Die Meldung.</returns>
    public static string UnknownPlaceholder(string placeholders) => Pick($"unbekannter Platzhalter {placeholders}.", $"unknown placeholder {placeholders}.");

    /// <summary>Format nur als Nullen.</summary>
    /// <param name="name">Platzhalter.</param>
    /// <returns>Die Meldung.</returns>
    public static string ZeroFormatOnly(string name) => Pick(
        $"Format für %{name}% nur als Nullen für die Stellenzahl, z. B. %{name}:000%.",
        $"Format for %{name}% only as zeros for the number of digits, e.g. %{name}:000%.");

    /// <summary>Kein Format möglich.</summary>
    /// <param name="name">Platzhalter.</param>
    /// <returns>Die Meldung.</returns>
    public static string NoFormatAllowed(string name) => Pick($"Für %{name}% ist kein Format möglich.", $"%{name}% does not accept a format.");

    /// <summary>Standard-Ersatztext für fehlende Werte (A9).</summary>
    public static string Unknown => Pick("Unbekannt", "Unknown");

    /// <summary>
    /// Wählt den Text der aktuellen Sprache.
    /// </summary>
    /// <param name="german">Deutscher Text.</param>
    /// <param name="english">Englischer Text.</param>
    /// <returns>Der Text.</returns>
    private static string Pick(string german, string english) => Language == UiLanguage.German ? german : english;
}
