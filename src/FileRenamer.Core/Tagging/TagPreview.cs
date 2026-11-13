using FileRenamer.Core.Tagging.Changes;

namespace FileRenamer.Core.Tagging;

/// <summary>
/// Tag-Vorschau einer Datei: Stand vorher und nachher, Herkunft jedes geänderten Bestandteils und Hinweise.
/// </summary>
/// <param name="Path">Pfad der Datei.</param>
/// <param name="Original">Stand vor den vorgemerkten Änderungen.</param>
/// <param name="Result">Stand nach den vorgemerkten Änderungen.</param>
/// <param name="Sources">Schlüssel (Feld, <see cref="TagFields.Covers"/>, <see cref="TagFields.Lyrics"/>) → Id der Änderung,
/// die ihn zuletzt geändert hat.</param>
/// <param name="Hints">Hinweise der Änderungen.</param>
public sealed record TagFilePreview(
    string Path, AudioTags Original, AudioTags Result, IReadOnlyDictionary<string, Guid> Sources, IReadOnlyList<string> Hints)
{
    /// <summary>
    /// Ob sich die Tags der Datei ändern.
    /// </summary>
    public bool IsChanged => !Original.Equals(Result);
}

/// <summary>
/// Berechnet die Tag-Vorschau: wendet die aktiven vorgemerkten Änderungen der Reihe nach auf ihre Dateien an
/// (spätere haben Vorrang) und merkt sich, welche Änderung welchen Bestandteil zuletzt geändert hat.
/// </summary>
public static class TagPreviewPipeline
{
    /// <summary>
    /// Erstellt die Vorschau.
    /// </summary>
    /// <param name="files">Die Audiodateien in Listenreihenfolge mit ihren gelesenen Tags.</param>
    /// <param name="changes">Die aktiven Änderungen in Anwendungsreihenfolge.</param>
    /// <returns>Je Datei die Vorschau in Listenreihenfolge.</returns>
    public static IReadOnlyList<TagFilePreview> Create(IReadOnlyList<(string Path, AudioTags Tags)> files, IReadOnlyList<TagChange> changes)
    {
        var states = files.ToDictionary(file => file.Path, file => new FileState(file.Tags), StringComparer.Ordinal);
        foreach (var change in changes)
        {
            ApplyChange(change, states);
        }

        return files.Select(file => states[file.Path].ToPreview(file.Path, file.Tags)).ToList();
    }

    /// <summary>
    /// Wendet eine Änderung auf ihre noch vorhandenen Dateien an.
    /// </summary>
    /// <param name="change">Die Änderung.</param>
    /// <param name="states">Zwischenstand je Datei.</param>
    private static void ApplyChange(TagChange change, Dictionary<string, FileState> states)
    {
        var inputs = change.Files.Where(states.ContainsKey).Select(path => new TagStepInput(path, states[path].Tags)).ToList();
        var results = change.Apply(inputs);
        for (var index = 0; index < inputs.Count; index++)
        {
            states[inputs[index].Path].Take(results[index], change.Id);
        }
    }

    /// <summary>
    /// Veränderlicher Zwischenstand einer Datei während der Berechnung.
    /// </summary>
    /// <param name="tags">Anfangsstand.</param>
    private sealed class FileState(AudioTags tags)
    {
        private readonly Dictionary<string, Guid> _sources = new(StringComparer.Ordinal);
        private readonly List<string> _hints = [];

        /// <summary>
        /// Aktueller Stand.
        /// </summary>
        public AudioTags Tags { get; private set; } = tags;

        /// <summary>
        /// Übernimmt das Ergebnis eines Schritts und vermerkt die Herkunft geänderter Bestandteile.
        /// </summary>
        /// <param name="result">Das Ergebnis.</param>
        /// <param name="changeId">Id der Änderung.</param>
        public void Take(TagStepResult result, Guid changeId)
        {
            foreach (var key in result.Tags.DifferingKeys(Tags))
            {
                _sources[key] = changeId;
            }

            Tags = result.Tags;
            if (result.Hint is { } hint && !_hints.Contains(hint))
            {
                _hints.Add(hint);
            }
        }

        /// <summary>
        /// Erzeugt die Vorschau; Herkunft nur für Bestandteile, die sich gegenüber dem Original unterscheiden.
        /// </summary>
        /// <param name="path">Pfad der Datei.</param>
        /// <param name="original">Stand vorher.</param>
        /// <returns>Die Vorschau.</returns>
        public TagFilePreview ToPreview(string path, AudioTags original)
        {
            var changed = Tags.DifferingKeys(original).ToHashSet(StringComparer.Ordinal);
            var sources = _sources.Where(pair => changed.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
            return new TagFilePreview(path, original, Tags, sources, _hints.ToList());
        }
    }
}
