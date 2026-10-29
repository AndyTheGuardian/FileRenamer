using CommunityToolkit.Mvvm.ComponentModel;
using FileRenamer.Core.Rules;

namespace FileRenamer.App.ViewModels.Editors;

/// <summary>
/// Editor für „Ziffernkette Wertebereich verschieben“. Die Ziffernketten-Nr. wird gegen die Namen an
/// dieser Stelle der Kette geprüft (E39, E41); ein ungültiger Wert wird markiert, aber nicht geändert (E40).
/// </summary>
public sealed partial class ShiftDigitRangeEditorViewModel() : RuleEditorViewModel(RuleKind.ShiftDigitRange)
{
    /// <summary>
    /// Der zu addierende Wert.
    /// </summary>
    [ObservableProperty]
    public partial decimal? Offset { get; set; } = 1;

    /// <summary>
    /// Ob die letzte gefundene Ziffernkette verwendet wird.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunNumberEnabled))]
    public partial bool UseLastRun { get; set; } = true;

    /// <summary>
    /// Nummer der Ziffernkette von links.
    /// </summary>
    [ObservableProperty]
    public partial decimal? RunNumber { get; set; } = 1;

    /// <summary>
    /// Ob die Originalbreite beibehalten wird.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMinDigitsEnabled))]
    public partial bool KeepOriginalWidth { get; set; } = true;

    /// <summary>
    /// Mindeststellen.
    /// </summary>
    [ObservableProperty]
    public partial decimal? MinDigits { get; set; } = 1;

    /// <summary>
    /// Größte gültige Nr. an dieser Stelle der Kette oder <c>null</c>, wenn kein Name Ziffern enthält.
    /// </summary>
    [ObservableProperty]
    public partial int? MaxRunNumber { get; set; }

    /// <summary>
    /// Hinweis oder Fehlermeldung zur Nr.
    /// </summary>
    [ObservableProperty]
    public partial string RunNumberMessage { get; set; } = string.Empty;

    /// <summary>
    /// Ob die Nr. außerhalb des gültigen Bereichs liegt.
    /// </summary>
    [ObservableProperty]
    public partial bool IsRunNumberInvalid { get; set; }

    /// <summary>
    /// Ob das Nummernfeld bearbeitbar ist.
    /// </summary>
    public bool IsRunNumberEnabled => !UseLastRun;

    /// <summary>
    /// Ob das Mindeststellenfeld bearbeitbar ist.
    /// </summary>
    public bool IsMinDigitsEnabled => !KeepOriginalWidth;

    /// <summary>
    /// Erzeugt das Verfahren aus den Eingaben.
    /// </summary>
    /// <returns>Das Verfahren.</returns>
    public override RenameRule CreateRule() => new ShiftDigitRangeRule
    {
        Offset = ToLong(Offset, 0),
        UseLastRun = UseLastRun,
        RunNumber = (int)ToLong(RunNumber, 1),
        KeepOriginalWidth = KeepOriginalWidth,
        MinDigits = (int)ToLong(MinDigits, 1),
    };

    /// <summary>
    /// Ermittelt die größte gültige Nr. aus den Namen an dieser Stelle der Kette.
    /// </summary>
    /// <param name="inputNames">Die Namen vor Anwendung dieses Verfahrens.</param>
    public override void UpdateContext(IReadOnlyList<string> inputNames)
    {
        MaxRunNumber = ShiftDigitRangeRule.GetMaxRunNumber(inputNames);
        UpdateRunNumberMessage();
    }

    /// <summary>
    /// Beispiel wie im Mockup für „name-301-001“.
    /// </summary>
    /// <returns>Der Beispieltext.</returns>
    protected override string? CreateExample() => $"name-301-001 → {ApplyToSamples("name-301-001")[0]}";

    /// <summary>
    /// Übernimmt die Parameter eines Verfahrens.
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    protected override void LoadSettings(RenameRule rule)
    {
        var shift = (ShiftDigitRangeRule)rule;
        Offset = shift.Offset;
        UseLastRun = shift.UseLastRun;
        RunNumber = shift.RunNumber;
        KeepOriginalWidth = shift.KeepOriginalWidth;
        MinDigits = shift.MinDigits;
    }

    /// <summary>
    /// Kontext- und Anzeigeeigenschaften sind keine Einstellungen.
    /// </summary>
    /// <param name="propertyName">Name der Eigenschaft.</param>
    /// <returns><c>true</c> bei einer Einstellung.</returns>
    protected override bool IsSettingProperty(string? propertyName) =>
        base.IsSettingProperty(propertyName)
        && propertyName is not (nameof(MaxRunNumber) or nameof(RunNumberMessage) or nameof(IsRunNumberInvalid)
            or nameof(IsRunNumberEnabled) or nameof(IsMinDigitsEnabled));

    /// <summary>
    /// Aktualisiert die Meldung zur Nr. nach jeder Prüfung.
    /// </summary>
    protected override void OnSettingsValidated() => UpdateRunNumberMessage();

    /// <summary>
    /// Setzt Hinweis bzw. Fehlermeldung zur Ziffernketten-Nr.
    /// </summary>
    private void UpdateRunNumberMessage()
    {
        var range = MaxRunNumber switch
        {
            null => null,
            1 => "1",
            { } max => $"1–{max}",
        };
        var number = (int)ToLong(RunNumber, 1);

        IsRunNumberInvalid = !UseLastRun && MaxRunNumber is { } limit && number > limit;
        RunNumberMessage = range is null
            ? "Keine Datei enthält an dieser Stelle eine Ziffernkette."
            : IsRunNumberInvalid
                ? $"Ungültig: Nicht alle Dateien haben eine Ziffernkette Nr. {number}. Erlaubt: {range}."
                : $"Erlaubt: {range} (kleinste Anzahl Ziffernketten unter den Dateien mit Ziffernketten).";
    }
}
