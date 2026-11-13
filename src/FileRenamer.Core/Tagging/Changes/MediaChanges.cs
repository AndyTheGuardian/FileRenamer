using FileRenamer.Core.Localization;

namespace FileRenamer.Core.Tagging.Changes;

/// <summary>
/// „Cover einfügen“: bettet je Datei ein Bild ein (dasselbe Bild für alle oder je Datei ein Bild aus ihrem Ordner).
/// </summary>
/// <param name="ImagesByFile">Pfad der Audiodatei → einzufügendes Bild; fehlende Dateien bleiben mit Hinweis unverändert.</param>
/// <param name="ReplaceSameType">Ob ein vorhandenes Bild desselben Typs ersetzt wird.</param>
public sealed record AddCoverChange(IReadOnlyDictionary<string, CoverImage> ImagesByFile, bool ReplaceSameType) : TagChange
{
    /// <summary>
    /// Funktion „Cover“.
    /// </summary>
    public override TagFunction Function => TagFunction.Cover;

    /// <summary>
    /// Fügt das Bild ein; bei <see cref="ReplaceSameType"/> ersetzt es Bilder desselben Typs.
    /// </summary>
    /// <param name="input">Die Datei.</param>
    /// <returns>Das Ergebnis.</returns>
    protected override TagStepResult ApplyToFile(TagStepInput input)
    {
        if (!ImagesByFile.TryGetValue(input.Path, out var image))
        {
            return new TagStepResult(input.Tags, CoreText.NoCoverFound);
        }

        var kept = ReplaceSameType ? input.Tags.Covers.Where(cover => cover.Type != image.Type) : input.Tags.Covers;
        return new TagStepResult(input.Tags.WithCovers(kept.Append(image).ToList()));
    }
}

/// <summary>
/// „Cover entfernen“: entfernt die eingebetteten Bilder eines Typs oder alle.
/// </summary>
/// <param name="Type">Der Bildtyp oder <c>null</c> für alle Bilder.</param>
public sealed record RemoveCoverChange(CoverType? Type) : TagChange
{
    /// <summary>
    /// Funktion „Cover“.
    /// </summary>
    public override TagFunction Function => TagFunction.Cover;

    /// <summary>
    /// Entfernt die Bilder.
    /// </summary>
    /// <param name="input">Die Datei.</param>
    /// <returns>Das Ergebnis.</returns>
    protected override TagStepResult ApplyToFile(TagStepInput input) =>
        new(input.Tags.WithCovers(Type is { } type ? input.Tags.Covers.Where(cover => cover.Type != type).ToList() : []));
}

/// <summary>
/// „Lyrics“: setzt die Lyrics je Datei (aus dem Textfeld oder importiert). Die Zeitstempel von LRC-Dateien
/// bleiben als Text erhalten (E123).
/// </summary>
/// <param name="LyricsByFile">Pfad der Audiodatei → Lyrics (leer = entfernen); fehlende Dateien bleiben mit Hinweis unverändert.</param>
/// <param name="Language">Sprache als dreistelliger Code (ID3).</param>
public sealed record SetLyricsChange(IReadOnlyDictionary<string, string> LyricsByFile, string Language) : TagChange
{
    /// <summary>
    /// Funktion „Lyrics“.
    /// </summary>
    public override TagFunction Function => TagFunction.Lyrics;

    /// <summary>
    /// Setzt die Lyrics.
    /// </summary>
    /// <param name="input">Die Datei.</param>
    /// <returns>Das Ergebnis.</returns>
    protected override TagStepResult ApplyToFile(TagStepInput input) =>
        LyricsByFile.TryGetValue(input.Path, out var lyrics)
            ? new TagStepResult(input.Tags.WithLyrics(lyrics, Language))
            : new TagStepResult(input.Tags, CoreText.NoLyricsFileFound);
}
