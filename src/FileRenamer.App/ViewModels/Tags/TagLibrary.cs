using FileRenamer.Core.Profiles;
using FileRenamer.Core.Rules.Templates;
using FileRenamer.Core.Tagging;

namespace FileRenamer.App.ViewModels.Tags;

/// <summary>
/// Programmweite Einstellungen des Bereichs „Tags“: eigene Genres (E134), hinzugefügte Felder (E131) und das
/// Verkleinern von Covern (E127, E132). Änderungen werden über <see cref="Changed"/> zum Speichern gemeldet.
/// </summary>
/// <param name="settings">Die geladenen Einstellungen.</param>
public sealed class TagLibrary(AppSettings settings)
{
    /// <summary>
    /// Eigene Genres, neueste zuerst.
    /// </summary>
    public IReadOnlyList<string> CustomGenres { get; private set; } = settings.CustomGenres;

    /// <summary>
    /// Hinzugefügte Felder (Schlüssel aus <see cref="TagFields"/>).
    /// </summary>
    public IReadOnlyList<string> ExtraFields { get; private set; } = settings.ExtraTagFields;

    /// <summary>
    /// Ob Cover beim Einfügen verkleinert werden.
    /// </summary>
    public bool ShrinkCovers { get; private set; } = settings.ShrinkCovers;

    /// <summary>
    /// Höchstmaß der längsten Seite in Pixeln.
    /// </summary>
    public int CoverMaxPixels { get; private set; } = settings.CoverMaxPixels;

    /// <summary>
    /// Wird nach jeder Änderung ausgelöst (Speichern der Einstellungen).
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Namen der benutzerdefinierten Felder (ohne Präfix), die mitgelesen und geschrieben werden.
    /// </summary>
    public IReadOnlyList<string> CustomFieldNames => ExtraFields.Where(TagFields.IsCustom).Select(TagFields.CustomName).ToList();

    /// <summary>
    /// Nimmt ein Genre auf, wenn es weder in der Standardliste noch schon unter den eigenen steht.
    /// </summary>
    /// <param name="genre">Das Genre.</param>
    public void AddGenre(string genre)
    {
        var updated = CustomTemplateList.Add(CustomGenres, Genres.Standard, genre, Genres.MaxCustomCount);
        if (!ReferenceEquals(updated, CustomGenres))
        {
            CustomGenres = updated;
            OnChanged();
        }
    }

    /// <summary>
    /// Entfernt ein eigenes Genre.
    /// </summary>
    /// <param name="genre">Das Genre.</param>
    public void RemoveGenre(string genre)
    {
        CustomGenres = CustomTemplateList.Remove(CustomGenres, genre);
        OnChanged();
    }

    /// <summary>
    /// Nimmt ein Feld dauerhaft ins Formular auf.
    /// </summary>
    /// <param name="key">Der Schlüssel.</param>
    public void AddField(string key)
    {
        if (!ExtraFields.Contains(key))
        {
            ExtraFields = [.. ExtraFields, key];
            OnChanged();
        }
    }

    /// <summary>
    /// Blendet ein Feld aus dem Formular aus (der Tag bleibt in den Dateien).
    /// </summary>
    /// <param name="key">Der Schlüssel.</param>
    public void RemoveField(string key)
    {
        ExtraFields = ExtraFields.Where(existing => existing != key).ToList();
        OnChanged();
    }

    /// <summary>
    /// Speichert die Einstellungen zum Verkleinern.
    /// </summary>
    /// <param name="shrink">Ob verkleinert wird.</param>
    /// <param name="maxPixels">Höchstmaß.</param>
    public void SetCoverShrinking(bool shrink, int maxPixels)
    {
        if (shrink != ShrinkCovers || maxPixels != CoverMaxPixels)
        {
            ShrinkCovers = shrink;
            CoverMaxPixels = maxPixels;
            OnChanged();
        }
    }

    /// <summary>
    /// Überträgt die Werte in Einstellungen.
    /// </summary>
    /// <param name="target">Die übrigen Einstellungen.</param>
    /// <returns>Die ergänzten Einstellungen.</returns>
    public AppSettings ApplyTo(AppSettings target) => target with
    {
        CustomGenres = CustomGenres,
        ExtraTagFields = ExtraFields,
        ShrinkCovers = ShrinkCovers,
        CoverMaxPixels = CoverMaxPixels,
    };

    /// <summary>
    /// Meldet eine Änderung.
    /// </summary>
    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
