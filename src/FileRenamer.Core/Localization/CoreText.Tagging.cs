using FileRenamer.Core.Tagging;

namespace FileRenamer.Core.Localization;

/// <summary>
/// Teil von <see cref="CoreText"/>: Meldungen des Bereichs „Tags“.
/// </summary>
public static partial class CoreText
{
    /// <summary>Datei kann nicht gelesen werden.</summary>
    /// <param name="file">Dateiname.</param>
    /// <param name="reason">Ursache.</param>
    /// <returns>Die Meldung.</returns>
    public static string TagReadFailed(string file, string reason) =>
        Pick($"Die Tags von „{file}“ können nicht gelesen werden: {reason}", $"Cannot read the tags of “{file}”: {reason}");

    /// <summary>Datei kann nicht geschrieben werden.</summary>
    /// <param name="file">Dateiname.</param>
    /// <param name="reason">Ursache.</param>
    /// <returns>Die Meldung.</returns>
    public static string TagWriteFailed(string file, string reason) =>
        Pick($"Die Tags von „{file}“ können nicht geschrieben werden: {reason}", $"Cannot write the tags of “{file}”: {reason}");

    /// <summary>Dateiname passt nicht zur Vorlage.</summary>
    /// <param name="template">Die Vorlage.</param>
    /// <returns>Die Meldung.</returns>
    public static string FileNameDoesNotMatch(string template) =>
        Pick($"Der Dateiname passt nicht zur Vorlage {template}.", $"The file name does not match the template {template}.");

    /// <summary>Vorlage ohne Platzhalter.</summary>
    public static string FileNameTemplateEmpty =>
        Pick("Die Vorlage braucht mindestens einen Platzhalter.", "The template needs at least one placeholder.");

    /// <summary>Kein Bild für die Datei gefunden.</summary>
    public static string NoCoverFound =>
        Pick("Kein Bild gefunden – Cover unverändert.", "No image found – cover unchanged.");

    /// <summary>Keine Lyrics-Datei gefunden.</summary>
    public static string NoLyricsFileFound =>
        Pick("Keine Lyrics-Datei gefunden – Lyrics unverändert.", "No lyrics file found – lyrics unchanged.");

    /// <summary>Kein freier Dateiname.</summary>
    /// <param name="file">Gewünschter Name.</param>
    /// <returns>Die Meldung.</returns>
    public static string NoFreeFileName(string file) =>
        Pick($"Kein freier Dateiname für „{file}“.", $"No free file name for “{file}”.");

    /// <summary>Export fehlgeschlagen.</summary>
    /// <param name="file">Dateiname.</param>
    /// <param name="reason">Ursache.</param>
    /// <returns>Die Meldung.</returns>
    public static string ExportFailed(string file, string reason) =>
        Pick($"„{file}“ kann nicht geschrieben werden: {reason}", $"Cannot write “{file}”: {reason}");

    /// <summary>Ungültige Zahl in einem Zahlenfeld.</summary>
    /// <param name="field">Bezeichnung des Felds.</param>
    /// <returns>Die Meldung.</returns>
    public static string TagNumberInvalid(string field) =>
        Pick($"„{field}“ muss eine ganze Zahl sein.", $"“{field}” must be a whole number.");

    /// <summary>
    /// Kurzname eines Bildtyps für Dateinamen (Platzhalter <c>%type%</c>).
    /// </summary>
    /// <param name="type">Der Bildtyp.</param>
    /// <returns>Der Name.</returns>
    public static string CoverTypeFileName(CoverType type) => type switch
    {
        CoverType.FrontCover => Pick("Vorderseite", "front"),
        CoverType.BackCover => Pick("Rückseite", "back"),
        CoverType.LeafletPage => Pick("Booklet", "booklet"),
        CoverType.Media => Pick("Medium", "media"),
        CoverType.Artist => Pick("Interpret", "artist"),
        CoverType.BandLogo => Pick("Logo", "logo"),
        _ => Pick("Sonstiges", "other"),
    };
}
