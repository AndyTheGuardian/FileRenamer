namespace FileRenamer.Core.Tagging.Changes;

/// <summary>
/// Die Funktionen des Bereichs „Tags“ (Farbe und Editor je Funktion).
/// </summary>
public enum TagFunction
{
    /// <summary>Tags bearbeiten.</summary>
    Edit,

    /// <summary>Tags aus Dateinamen.</summary>
    FromFileName,

    /// <summary>Tracknummern vergeben.</summary>
    TrackNumbers,

    /// <summary>Cover.</summary>
    Cover,

    /// <summary>Lyrics.</summary>
    Lyrics,
}

/// <summary>
/// Eingabe eines Schritts für eine Datei.
/// </summary>
/// <param name="Path">Pfad der Datei.</param>
/// <param name="Tags">Stand der Tags vor dem Schritt.</param>
public sealed record TagStepInput(string Path, AudioTags Tags);

/// <summary>
/// Ergebnis eines Schritts für eine Datei.
/// </summary>
/// <param name="Tags">Stand der Tags nach dem Schritt.</param>
/// <param name="Hint">Hinweis (z. B. Dateiname passt nicht zur Vorlage) oder <c>null</c>.</param>
public sealed record TagStepResult(AudioTags Tags, string? Hint = null);

/// <summary>
/// Eine vorgemerkte Änderung (E116). Sie gilt für die Dateien, für die sie vorgemerkt wurde (E136), und wird
/// erst mit „Tags speichern“ geschrieben. Änderungen sind unveränderlich; spätere Änderungen haben Vorrang.
/// </summary>
public abstract record TagChange
{
    /// <summary>
    /// Eindeutige Kennung (Herkunft in der Vorschau).
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Die Dateien, für die die Änderung vorgemerkt wurde, in Listenreihenfolge.
    /// </summary>
    public IReadOnlyList<string> Files { get; init; } = [];

    /// <summary>
    /// Die Funktion, aus der die Änderung stammt.
    /// </summary>
    public abstract TagFunction Function { get; }

    /// <summary>
    /// Wendet die Änderung auf ihre Dateien an. Standard: jede Datei für sich.
    /// </summary>
    /// <param name="inputs">Die Dateien in der Reihenfolge von <see cref="Files"/>.</param>
    /// <returns>Je Eingabe ein Ergebnis in derselben Reihenfolge.</returns>
    public virtual IReadOnlyList<TagStepResult> Apply(IReadOnlyList<TagStepInput> inputs) => inputs.Select(ApplyToFile).ToList();

    /// <summary>
    /// Wendet die Änderung auf eine Datei an.
    /// </summary>
    /// <param name="input">Die Datei.</param>
    /// <returns>Das Ergebnis.</returns>
    protected abstract TagStepResult ApplyToFile(TagStepInput input);
}
