namespace FileRenamer.Core.Tagging;

/// <summary>
/// Auftrag, die Tags einer Datei von einem Stand in einen anderen zu bringen.
/// </summary>
/// <param name="Path">Pfad der Datei.</param>
/// <param name="Original">Stand vorher (für Rücknahme und Widerrufen).</param>
/// <param name="Target">Gewünschter Stand.</param>
public sealed record TagWriteJob(string Path, AudioTags Original, AudioTags Target);

/// <summary>
/// Ergebnis von „Tags speichern“ bzw. „Letzte Speicherung widerrufen“.
/// </summary>
/// <param name="Written">Die geschriebenen Aufträge (bei Erfolg).</param>
/// <param name="Error">Fehlermeldung oder <c>null</c> bei Erfolg.</param>
/// <param name="RollbackErrors">Fehler beim Zurückschreiben nach einem Fehler.</param>
public sealed record TagSaveResult(IReadOnlyList<TagWriteJob> Written, string? Error, IReadOnlyList<string> RollbackErrors)
{
    /// <summary>
    /// Ob alle Dateien geschrieben wurden.
    /// </summary>
    public bool Success => Error is null;

    /// <summary>
    /// Erzeugt die Gegenaufträge zum Widerrufen (E124).
    /// </summary>
    /// <returns>Die Aufträge in umgekehrter Reihenfolge.</returns>
    public IReadOnlyList<TagWriteJob> CreateUndoJobs() =>
        Written.Reverse().Select(job => job with { Original = job.Target, Target = job.Original }).ToList();
}

/// <summary>
/// Schreibt die Tags mehrerer Dateien nach dem Alles-oder-nichts-Prinzip: Schlägt eine Datei fehl, werden die bereits
/// geschriebenen Dateien auf ihren vorherigen Stand zurückgeschrieben.
/// </summary>
/// <param name="store">Zugriff auf die Tags.</param>
public sealed class TagWriter(ITagStore store)
{
    /// <summary>
    /// Schreibt die Aufträge; Aufträge ohne Änderung werden übersprungen.
    /// </summary>
    /// <param name="jobs">Die Aufträge.</param>
    /// <param name="customNames">Verwaltete benutzerdefinierte Felder.</param>
    /// <returns>Das Ergebnis.</returns>
    public TagSaveResult Save(IReadOnlyList<TagWriteJob> jobs, IReadOnlyCollection<string> customNames)
    {
        var written = new List<TagWriteJob>();
        foreach (var job in jobs.Where(job => !job.Original.Equals(job.Target)))
        {
            try
            {
                store.Write(job.Path, job.Target, customNames);
                written.Add(job);
            }
            catch (TagStoreException exception)
            {
                return new TagSaveResult([], exception.Message, RollBack(written, customNames));
            }
        }

        return new TagSaveResult(written, null, []);
    }

    /// <summary>
    /// Schreibt bereits geänderte Dateien in umgekehrter Reihenfolge auf ihren vorherigen Stand zurück.
    /// </summary>
    /// <param name="written">Die geschriebenen Aufträge.</param>
    /// <param name="customNames">Verwaltete benutzerdefinierte Felder.</param>
    /// <returns>Fehler beim Zurückschreiben.</returns>
    private List<string> RollBack(List<TagWriteJob> written, IReadOnlyCollection<string> customNames)
    {
        var errors = new List<string>();
        foreach (var job in Enumerable.Reverse(written))
        {
            try
            {
                store.Write(job.Path, job.Original, customNames);
            }
            catch (TagStoreException exception)
            {
                errors.Add(exception.Message);
            }
        }

        return errors;
    }
}
