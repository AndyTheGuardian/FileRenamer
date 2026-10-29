using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FileRenamer.Core.Rules;

namespace FileRenamer.App.ViewModels.Editors;

/// <summary>
/// Basis der Options-Editoren (obere Hälfte, rechts). Jede Änderung einer Einstellung wird geprüft
/// und über <see cref="SettingsChanged"/> gemeldet, damit die Vorschau sofort aktualisiert wird.
/// </summary>
public abstract partial class RuleEditorViewModel : ObservableObject
{
    private bool _isLoading;

    /// <summary>
    /// Erzeugt den Editor für eine Verfahrensart.
    /// </summary>
    /// <param name="kind">Die Verfahrensart.</param>
    protected RuleEditorViewModel(RuleKind kind) => Kind = kind;

    /// <summary>
    /// Wird ausgelöst, wenn sich eine Einstellung geändert hat.
    /// </summary>
    public event EventHandler? SettingsChanged;

    /// <summary>
    /// Art des Verfahrens.
    /// </summary>
    public RuleKind Kind { get; }

    /// <summary>
    /// Überschrift des Editors.
    /// </summary>
    public string Title => RuleCatalog.GetDisplayName(Kind);

    /// <summary>
    /// Meldung zu ungültigen Einstellungen oder <c>null</c>.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    public partial string? ValidationMessage { get; set; }

    /// <summary>
    /// Ob eine Meldung angezeigt wird.
    /// </summary>
    public bool HasValidationMessage => ValidationMessage is not null;

    /// <summary>
    /// Beispielausgabe mit den aktuellen Einstellungen (wie im Mockup) oder <c>null</c>.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasExample))]
    public partial string? Example { get; set; }

    /// <summary>
    /// Ob ein Beispiel angezeigt wird.
    /// </summary>
    public bool HasExample => Example is not null;

    /// <summary>
    /// Erzeugt ein neues Verfahren aus den aktuellen Einstellungen.
    /// </summary>
    /// <returns>Das Verfahren mit neuer Id.</returns>
    public abstract RenameRule CreateRule();

    /// <summary>
    /// Übernimmt die aktuellen Einstellungen in ein vorhandenes Verfahren (Id und Aktivierung bleiben).
    /// </summary>
    /// <param name="existing">Das vorhandene Verfahren.</param>
    /// <returns>Das aktualisierte Verfahren.</returns>
    public RenameRule ApplyTo(RenameRule existing) => CreateRule() with { Id = existing.Id, IsEnabled = existing.IsEnabled };

    /// <summary>
    /// Füllt den Editor mit den Parametern eines Verfahrens, ohne Änderungen zu melden.
    /// </summary>
    /// <param name="rule">Das Verfahren (passende Art).</param>
    public void Load(RenameRule rule)
    {
        _isLoading = true;
        try
        {
            LoadSettings(rule);
        }
        finally
        {
            _isLoading = false;
        }

        Validate();
    }

    /// <summary>
    /// Setzt alle Einstellungen auf Standardwerte (meldet eine Änderung).
    /// </summary>
    public void Reset()
    {
        Load(RuleCatalog.CreateEditor(Kind).CreateRule());
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Aktualisiert kontextabhängige Grenzen anhand der Namen an dieser Stelle der Kette.
    /// </summary>
    /// <param name="inputNames">Die Namen vor Anwendung dieses Verfahrens.</param>
    public virtual void UpdateContext(IReadOnlyList<string> inputNames)
    {
    }

    /// <summary>
    /// Überträgt die Parameter eines Verfahrens in die Editor-Eigenschaften.
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    protected abstract void LoadSettings(RenameRule rule);

    /// <summary>
    /// Gibt an, ob eine Eigenschaft eine Einstellung ist (und keine Anzeige-/Kontexteigenschaft).
    /// </summary>
    /// <param name="propertyName">Name der Eigenschaft.</param>
    /// <returns><c>true</c> bei einer Einstellung.</returns>
    protected virtual bool IsSettingProperty(string? propertyName) =>
        propertyName is not (nameof(ValidationMessage) or nameof(HasValidationMessage) or nameof(Example) or nameof(HasExample));

    /// <summary>
    /// Erzeugt den Beispieltext für gültige Einstellungen.
    /// </summary>
    /// <returns>Der Beispieltext oder <c>null</c>, wenn kein Beispiel gezeigt wird.</returns>
    protected virtual string? CreateExample() => null;

    /// <summary>
    /// Wendet das aktuelle Verfahren auf Beispielnamen an.
    /// </summary>
    /// <param name="names">Die Beispielnamen.</param>
    /// <returns>Die neuen Namen in derselben Reihenfolge.</returns>
    protected IReadOnlyList<string> ApplyToSamples(params string[] names)
    {
        var timestamp = new DateTime(2026, 10, 12, 14, 30, 5);
        var inputs = names.Select(name => new RuleInput(
            new Core.Files.FileEntry(name, timestamp, timestamp, 0), Core.Naming.SegmentedName.FromText(name))).ToList();
        return CreateRule().Apply(inputs).Select(output => output.Name.Text).ToList();
    }

    /// <summary>
    /// Prüft die Einstellungen und meldet Änderungen, sofern nicht gerade geladen wird.
    /// </summary>
    /// <param name="e">Die Änderungsinformation.</param>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (_isLoading || !IsSettingProperty(e.PropertyName))
        {
            return;
        }

        Validate();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Wandelt einen optionalen Zahlenwert eines NumericUpDown in eine Ganzzahl um.
    /// </summary>
    /// <param name="value">Der Wert.</param>
    /// <param name="fallback">Ersatzwert bei leerem Feld.</param>
    /// <returns>Die Ganzzahl.</returns>
    protected static long ToLong(decimal? value, long fallback) => value is { } number ? (long)decimal.Truncate(number) : fallback;

    /// <summary>
    /// Wird nach jeder Prüfung aufgerufen, z. B. um ein Beispiel neu zu berechnen.
    /// </summary>
    protected virtual void OnSettingsValidated()
    {
    }

    /// <summary>
    /// Prüft die aktuellen Einstellungen und setzt <see cref="ValidationMessage"/>.
    /// </summary>
    private void Validate()
    {
        ValidationMessage = CreateRule().ValidateSettings();
        Example = HasValidationMessage ? null : CreateExample();
        OnSettingsValidated();
    }
}
