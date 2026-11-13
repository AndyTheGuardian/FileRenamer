namespace FileRenamer.Core.Preview;

/// <summary>
/// Eine Spalte einer Vorschauliste.
/// </summary>
/// <param name="Key">Eindeutiger Schlüssel, über den Kopf und Zellen zugeordnet werden.</param>
/// <param name="Width">Feste Breite in Pixeln; <c>null</c> teilt den restlichen Platz mit den anderen solchen Spalten.</param>
/// <param name="MinWidth">Mindestbreite in Pixeln.</param>
public sealed record ColumnState(string Key, double? Width, double MinWidth = ColumnLayout.DefaultMinWidth);

/// <summary>
/// Lage einer Spalte nach der Aufteilung der verfügbaren Breite.
/// </summary>
/// <param name="Key">Schlüssel der Spalte.</param>
/// <param name="X">Linker Rand.</param>
/// <param name="Width">Breite.</param>
public sealed record ColumnSlot(string Key, double X, double Width)
{
    /// <summary>
    /// Rechter Rand.
    /// </summary>
    public double Right => X + Width;
}

/// <summary>
/// Gespeicherte Spalte (programmweite Einstellungen, E148).
/// </summary>
/// <param name="Key">Schlüssel der Spalte.</param>
/// <param name="Width">Feste Breite oder <c>null</c> für eine Spalte, die sich den Platz teilt.</param>
public sealed record SavedColumn(string Key, double? Width);

/// <summary>
/// Reihenfolge und Breiten der Spalten einer Vorschauliste (E146). Spalten mit fester Breite behalten sie; Spalten
/// ohne feste Breite teilen sich den restlichen Platz zu gleichen Teilen. Gibt es keine solche Spalte, füllt die
/// letzte Spalte den Rest. Änderungen gelten bis zum Programmende; über <see cref="Save"/> wird die Anordnung
/// gespeichert (E148) und beim nächsten Start wiederhergestellt. <see cref="RestoreSaved"/> verwirft ungespeicherte
/// Änderungen, <see cref="Reset"/> setzt die Werkseinstellung, ohne die gespeicherte Anordnung zu löschen.
/// </summary>
public sealed class ColumnLayout
{
    /// <summary>
    /// Standard-Mindestbreite einer Spalte.
    /// </summary>
    public const double DefaultMinWidth = 32;

    private readonly IReadOnlyList<ColumnState> _defaults;
    private List<ColumnState> _columns;
    private IReadOnlyList<ColumnState>? _saved;
    private double _viewport;

    /// <summary>
    /// Legt die Standardspalten fest.
    /// </summary>
    /// <param name="defaults">Die Spalten in Standardreihenfolge.</param>
    public ColumnLayout(IEnumerable<ColumnState> defaults)
    {
        _defaults = defaults.ToList();
        _columns = [.. _defaults];
    }

    /// <summary>
    /// Wird nach jeder Änderung von Reihenfolge oder Breite ausgelöst.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Wird ausgelöst, wenn die Anordnung gespeichert wurde.
    /// </summary>
    public event EventHandler? SavedChanged;

    /// <summary>
    /// Ob eine gespeicherte Anordnung vorliegt.
    /// </summary>
    public bool HasSaved => _saved is not null;

    /// <summary>
    /// Die gespeicherte Anordnung zum Ablegen in den Einstellungen, sonst <c>null</c>.
    /// </summary>
    public IReadOnlyList<SavedColumn>? Saved => _saved?.Select(column => new SavedColumn(column.Key, column.Width)).ToList();

    /// <summary>
    /// Die Spalten in aktueller Reihenfolge.
    /// </summary>
    public IReadOnlyList<ColumnState> Columns => _columns;

    /// <summary>
    /// Ob Reihenfolge oder Breiten vom Standard abweichen.
    /// </summary>
    public bool IsDefault => _columns.SequenceEqual(_defaults);

    /// <summary>
    /// Sichtbare Breite der Spalten, von der Ansicht gesetzt (Spaltenkopf). Zeilen einer horizontal scrollenden Liste
    /// ordnen ihre Spalten nach dieser Breite an (E151). Eine Änderung löst <see cref="Changed"/> aus.
    /// </summary>
    public double Viewport
    {
        get => _viewport;
        set
        {
            if (Math.Abs(value - _viewport) < 0.5)
            {
                return;
            }

            _viewport = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gesamtbreite der Spalten bei einer verfügbaren Breite: mindestens diese Breite, mehr, wenn die festen Breiten
    /// und Mindestbreiten nicht hineinpassen (dann wird horizontal gescrollt, E151).
    /// </summary>
    /// <param name="available">Verfügbare Breite.</param>
    /// <returns>Die Gesamtbreite.</returns>
    public double Extent(double available) => Math.Max(available, Arrange(available).Sum(slot => slot.Width));

    /// <summary>
    /// Verschiebt eine Spalte.
    /// </summary>
    /// <param name="key">Schlüssel der Spalte.</param>
    /// <param name="index">Zielposition in der Liste ohne die verschobene Spalte.</param>
    public void Move(string key, int index)
    {
        var column = Find(key);
        var columns = _columns.Where(item => item != column).ToList();
        columns.Insert(Math.Clamp(index, 0, columns.Count), column);
        Apply(columns);
    }

    /// <summary>
    /// Setzt die feste Breite einer Spalte; die Mindestbreite wird eingehalten.
    /// </summary>
    /// <param name="key">Schlüssel der Spalte.</param>
    /// <param name="width">Die neue Breite.</param>
    public void SetWidth(string key, double width)
    {
        var column = Find(key);
        var updated = column with { Width = Math.Max(column.MinWidth, Math.Round(width)) };
        Apply(_columns.Select(item => item == column ? updated : item).ToList());
    }

    /// <summary>
    /// Stellt die Werkseinstellung wieder her; die gespeicherte Anordnung bleibt erhalten.
    /// </summary>
    public void Reset() => Apply([.. _defaults]);

    /// <summary>
    /// Speichert die aktuelle Anordnung.
    /// </summary>
    public void Save()
    {
        _saved = [.. _columns];
        SavedChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Stellt die gespeicherte Anordnung wieder her; ohne gespeicherte Anordnung die Werkseinstellung.
    /// </summary>
    public void RestoreSaved() => Apply([.. _saved ?? _defaults]);

    /// <summary>
    /// Übernimmt eine beim Start gelesene Anordnung als gespeicherte und aktuelle. Passt sie nicht zu den Spalten
    /// (fehlende, doppelte oder unbekannte Schlüssel), bleibt die Werkseinstellung.
    /// </summary>
    /// <param name="saved">Die gelesene Anordnung oder <c>null</c>.</param>
    public void LoadSaved(IReadOnlyList<SavedColumn>? saved)
    {
        if (saved is null || !saved.Select(column => column.Key).Order().SequenceEqual(_defaults.Select(column => column.Key).Order()))
        {
            return;
        }

        _saved = saved.Select(column => _defaults.First(item => item.Key == column.Key) with
        {
            Width = column.Width is { } width ? Math.Max(width, _defaults.First(item => item.Key == column.Key).MinWidth) : null,
        }).ToList();
        Apply([.. _saved]);
    }

    /// <summary>
    /// Teilt die verfügbare Breite auf die Spalten auf.
    /// </summary>
    /// <param name="available">Verfügbare Breite.</param>
    /// <returns>Lage jeder Spalte in aktueller Reihenfolge.</returns>
    public IReadOnlyList<ColumnSlot> Arrange(double available)
    {
        var flexible = _columns.Count(column => column.Width is null);
        var remaining = Math.Max(0, available - _columns.Sum(column => column.Width ?? 0));
        var slots = new List<ColumnSlot>(_columns.Count);
        var x = 0d;
        for (var index = 0; index < _columns.Count; index++)
        {
            var width = WidthOf(_columns[index], flexible, remaining, isLast: index == _columns.Count - 1);
            slots.Add(new ColumnSlot(_columns[index].Key, x, width));
            x += width;
        }

        return slots;
    }

    /// <summary>
    /// Ermittelt die Breite einer Spalte.
    /// </summary>
    /// <param name="column">Die Spalte.</param>
    /// <param name="flexible">Anzahl der Spalten ohne feste Breite.</param>
    /// <param name="remaining">Platz neben den festen Spalten.</param>
    /// <param name="isLast">Ob es die letzte Spalte ist.</param>
    /// <returns>Die Breite.</returns>
    private static double WidthOf(ColumnState column, int flexible, double remaining, bool isLast) => column.Width switch
    {
        null => Math.Max(column.MinWidth, remaining / flexible),
        { } fixedWidth when flexible == 0 && isLast => fixedWidth + remaining,
        { } fixedWidth => fixedWidth,
    };

    /// <summary>
    /// Sucht eine Spalte.
    /// </summary>
    /// <param name="key">Schlüssel der Spalte.</param>
    /// <returns>Die Spalte.</returns>
    private ColumnState Find(string key) =>
        _columns.FirstOrDefault(column => column.Key == key) ?? throw new ArgumentException($"Unbekannte Spalte: {key}", nameof(key));

    /// <summary>
    /// Übernimmt einen neuen Zustand und meldet die Änderung.
    /// </summary>
    /// <param name="columns">Die Spalten.</param>
    private void Apply(List<ColumnState> columns)
    {
        if (columns.SequenceEqual(_columns))
        {
            return;
        }

        _columns = columns;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
