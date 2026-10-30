using FileRenamer.Core.Rules.Templates;

namespace FileRenamer.App.ViewModels;

/// <summary>
/// Programmweit gespeicherte eigene Vorlagen für „Name aus Vorlage“, getrennt nach Fotos und Audio (E106).
/// Lebt im Hauptfenster-ViewModel, damit die Listen den Wechsel des Editors überdauern; jede Änderung
/// wird über <see cref="Changed"/> gemeldet und dort gespeichert.
/// </summary>
/// <param name="photo">Eigene Foto-Vorlagen, neueste zuerst.</param>
/// <param name="audio">Eigene Audio-Vorlagen, neueste zuerst.</param>
public sealed class TemplateLibrary(IReadOnlyList<string> photo, IReadOnlyList<string> audio)
{
    /// <summary>
    /// Eigene Foto-Vorlagen, neueste zuerst.
    /// </summary>
    public IReadOnlyList<string> Photo { get; private set; } = photo;

    /// <summary>
    /// Eigene Audio-Vorlagen, neueste zuerst.
    /// </summary>
    public IReadOnlyList<string> Audio { get; private set; } = audio;

    /// <summary>
    /// Tritt nach jeder Änderung einer Liste ein.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Liefert die Liste eines Tabs.
    /// </summary>
    /// <param name="isAudio"><c>true</c> für Audio, sonst Fotos.</param>
    /// <returns>Die eigenen Vorlagen.</returns>
    public IReadOnlyList<string> Get(bool isAudio) => isAudio ? Audio : Photo;

    /// <summary>
    /// Nimmt eine Vorlage auf, sofern sie neu und nicht leer ist.
    /// </summary>
    /// <param name="isAudio"><c>true</c> für Audio, sonst Fotos.</param>
    /// <param name="builtIn">Die Beispielvorlagen des Tabs.</param>
    /// <param name="template">Die Vorlage.</param>
    public void Add(bool isAudio, IEnumerable<string> builtIn, string template) =>
        Set(isAudio, CustomTemplateList.Add(Get(isAudio), builtIn, template));

    /// <summary>
    /// Entfernt eine Vorlage.
    /// </summary>
    /// <param name="isAudio"><c>true</c> für Audio, sonst Fotos.</param>
    /// <param name="template">Die Vorlage.</param>
    public void Remove(bool isAudio, string template) => Set(isAudio, CustomTemplateList.Remove(Get(isAudio), template));

    /// <summary>
    /// Ersetzt eine Liste und meldet die Änderung, falls sich etwas geändert hat.
    /// </summary>
    /// <param name="isAudio"><c>true</c> für Audio, sonst Fotos.</param>
    /// <param name="templates">Die neue Liste.</param>
    private void Set(bool isAudio, IReadOnlyList<string> templates)
    {
        if (templates.SequenceEqual(Get(isAudio)))
        {
            return;
        }

        if (isAudio)
        {
            Audio = templates;
        }
        else
        {
            Photo = templates;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
