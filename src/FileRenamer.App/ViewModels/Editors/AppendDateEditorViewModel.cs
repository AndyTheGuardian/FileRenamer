using CommunityToolkit.Mvvm.ComponentModel;
using FileRenamer.Core.Rules;

namespace FileRenamer.App.ViewModels.Editors;

/// <summary>
/// Editor für „Datum / Zeit anfügen“. Eine Liste bietet Vorlagen, das Textfeld erlaubt eigene Formate (E29).
/// </summary>
public sealed partial class AppendDateEditorViewModel() : RuleEditorViewModel(RuleKind.AppendDate)
{
    /// <summary>
    /// Vordefinierte Datumsformate (E22).
    /// </summary>
    public IReadOnlyList<string> DatePresets { get; } =
        ["yyyy-MM-dd", "yyyyMMdd", "yyyy_MM_dd", "dd.MM.yyyy", "dd-MM-yyyy", "MM-dd-yyyy"];

    /// <summary>
    /// Vordefinierte Uhrzeitformate (E22, ohne Doppelpunkt).
    /// </summary>
    public IReadOnlyList<string> TimePresets { get; } = ["HH-mm-ss", "HHmmss", "HH.mm.ss", "HH-mm"];

    /// <summary>
    /// Datumsquelle.
    /// </summary>
    [ObservableProperty]
    public partial DateSource Source { get; set; } = DateSource.CreationDate;

    /// <summary>
    /// Vor oder nach dem Namen.
    /// </summary>
    [ObservableProperty]
    public partial AffixPosition Position { get; set; } = AffixPosition.BeforeName;

    /// <summary>
    /// In der Liste gewählte Datumsvorlage (oder <c>null</c> bei eigenem Format).
    /// </summary>
    [ObservableProperty]
    public partial string? SelectedDatePreset { get; set; } = "yyyy-MM-dd";

    /// <summary>
    /// Datumsformat (frei editierbar).
    /// </summary>
    [ObservableProperty]
    public partial string DateFormat { get; set; } = "yyyy-MM-dd";

    /// <summary>
    /// Ob die Uhrzeit angefügt wird.
    /// </summary>
    [ObservableProperty]
    public partial bool IncludeTime { get; set; }

    /// <summary>
    /// In der Liste gewählte Uhrzeitvorlage (oder <c>null</c> bei eigenem Format).
    /// </summary>
    [ObservableProperty]
    public partial string? SelectedTimePreset { get; set; } = "HH-mm-ss";

    /// <summary>
    /// Uhrzeitformat (frei editierbar).
    /// </summary>
    [ObservableProperty]
    public partial string TimeFormat { get; set; } = "HH-mm-ss";

    /// <summary>
    /// Trennzeichen zwischen Name und Datum.
    /// </summary>
    [ObservableProperty]
    public partial string NameSeparator { get; set; } = "_";

    /// <summary>
    /// Trennzeichen zwischen Datum und Uhrzeit.
    /// </summary>
    [ObservableProperty]
    public partial string DateTimeSeparator { get; set; } = "_";

    /// <summary>
    /// Erzeugt das Verfahren aus den Eingaben.
    /// </summary>
    /// <returns>Das Verfahren.</returns>
    public override RenameRule CreateRule() => new AppendDateRule
    {
        Source = Source,
        Position = Position,
        DateFormat = DateFormat,
        IncludeTime = IncludeTime,
        TimeFormat = TimeFormat,
        NameSeparator = NameSeparator,
        DateTimeSeparator = DateTimeSeparator,
    };

    /// <summary>
    /// Übernimmt die Parameter eines Verfahrens.
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    protected override void LoadSettings(RenameRule rule)
    {
        var date = (AppendDateRule)rule;
        Source = date.Source;
        Position = date.Position;
        DateFormat = date.DateFormat;
        IncludeTime = date.IncludeTime;
        TimeFormat = date.TimeFormat;
        NameSeparator = date.NameSeparator;
        DateTimeSeparator = date.DateTimeSeparator;
    }

    /// <summary>
    /// Die Vorlagenauswahl ist keine Einstellung.
    /// </summary>
    /// <param name="propertyName">Name der Eigenschaft.</param>
    /// <returns><c>true</c> bei einer Einstellung.</returns>
    protected override bool IsSettingProperty(string? propertyName) =>
        base.IsSettingProperty(propertyName)
        && propertyName is not (nameof(SelectedDatePreset) or nameof(SelectedTimePreset));

    /// <summary>
    /// Beispiel für den Namen „bericht“ (Datum 12.10.2026, 14:30:05).
    /// </summary>
    /// <returns>Der Beispieltext.</returns>
    protected override string? CreateExample() => ApplyToSamples(Localization.Loc.T("Sample.Report"))[0];

    /// <summary>
    /// Übernimmt eine gewählte Datumsvorlage ins Textfeld.
    /// </summary>
    /// <param name="value">Die Vorlage.</param>
    partial void OnSelectedDatePresetChanged(string? value)
    {
        if (value is not null)
        {
            DateFormat = value;
        }
    }

    /// <summary>
    /// Wählt die passende Vorlage aus oder hebt die Auswahl bei eigenem Format auf.
    /// </summary>
    /// <param name="value">Das Format.</param>
    partial void OnDateFormatChanged(string value) =>
        SelectedDatePreset = DatePresets.Contains(value) ? value : null;

    /// <summary>
    /// Übernimmt eine gewählte Uhrzeitvorlage ins Textfeld.
    /// </summary>
    /// <param name="value">Die Vorlage.</param>
    partial void OnSelectedTimePresetChanged(string? value)
    {
        if (value is not null)
        {
            TimeFormat = value;
        }
    }

    /// <summary>
    /// Wählt die passende Vorlage aus oder hebt die Auswahl bei eigenem Format auf.
    /// </summary>
    /// <param name="value">Das Format.</param>
    partial void OnTimeFormatChanged(string value) =>
        SelectedTimePreset = TimePresets.Contains(value) ? value : null;
}
