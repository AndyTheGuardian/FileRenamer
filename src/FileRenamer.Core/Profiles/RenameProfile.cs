using FileRenamer.Core.Rules;

namespace FileRenamer.Core.Profiles;

/// <summary>
/// Ein benanntes Profil: die Kette der Verfahren mit Parametern und Reihenfolge sowie die
/// Einstellung „Dateierweiterung ignorieren“.
/// </summary>
/// <param name="Name">Anzeigename des Profils.</param>
/// <param name="IgnoreExtension">Ob die Dateierweiterung ignoriert wird.</param>
/// <param name="Rules">Die Verfahren in Kettenreihenfolge.</param>
public sealed record RenameProfile(string Name, bool IgnoreExtension, IReadOnlyList<RenameRule> Rules);
