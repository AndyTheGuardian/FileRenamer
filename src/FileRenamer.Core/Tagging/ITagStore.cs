namespace FileRenamer.Core.Tagging;

/// <summary>
/// Liest und schreibt die Tags von Audiodateien (entkoppelt von der Bibliothek, testbar).
/// </summary>
public interface ITagStore
{
    /// <summary>
    /// Prüft, ob eine Datei nach ihrer Endung eine unterstützte Audiodatei ist.
    /// </summary>
    /// <param name="path">Pfad oder Dateiname.</param>
    /// <returns><c>true</c>, wenn Tags gelesen und geschrieben werden können.</returns>
    bool IsSupported(string path);

    /// <summary>
    /// Liest die Tags einer Datei.
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <param name="customNames">Namen der benutzerdefinierten Felder, die mitgelesen werden.</param>
    /// <returns>Die Tags.</returns>
    /// <exception cref="TagStoreException">Die Datei kann nicht gelesen werden.</exception>
    AudioTags Read(string path, IReadOnlyCollection<string> customNames);

    /// <summary>
    /// Schreibt die Tags einer Datei. Geändert werden nur die Bestandteile, die sich vom aktuellen Stand der Datei
    /// unterscheiden; benutzerdefinierte Felder aus <paramref name="customNames"/>, die in <paramref name="tags"/>
    /// fehlen, werden entfernt.
    /// </summary>
    /// <param name="path">Pfad der Datei.</param>
    /// <param name="tags">Der gewünschte Stand.</param>
    /// <param name="customNames">Namen der verwalteten benutzerdefinierten Felder.</param>
    /// <exception cref="TagStoreException">Die Datei kann nicht geschrieben werden.</exception>
    void Write(string path, AudioTags tags, IReadOnlyCollection<string> customNames);
}

/// <summary>
/// Fehler beim Lesen oder Schreiben von Tags.
/// </summary>
/// <param name="message">Die Meldung.</param>
/// <param name="inner">Die ursprüngliche Ausnahme.</param>
public sealed class TagStoreException(string message, Exception? inner = null) : Exception(message, inner);
