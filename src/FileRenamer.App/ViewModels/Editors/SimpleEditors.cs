using CommunityToolkit.Mvvm.ComponentModel;
using FileRenamer.App.Localization;
using FileRenamer.Core.Rules;

namespace FileRenamer.App.ViewModels.Editors;

/// <summary>
/// Auswahlmöglichkeit der Schreibweise mit Beispiel (E75: Beispiele direkt in der ComboBox).
/// </summary>
/// <param name="Mode">Die Schreibweise.</param>
/// <param name="Label">Anzeigename.</param>
/// <param name="Example">Beispielergebnis für „mein Urlaub-Foto_2026“ bzw. „my holiday-photo_2026“.</param>
public sealed record CaseOption(CaseMode Mode, string Label, string Example);

/// <summary>
/// Editor für „Groß-/Kleinschreibung“.
/// </summary>
public sealed partial class ChangeCaseEditorViewModel : RuleEditorViewModel
{
    /// <summary>
    /// Beispielname in der aktuellen Sprache (A11).
    /// </summary>
    private static string Sample => Loc.T("Sample.CaseName");

    /// <summary>
    /// Erzeugt den Editor und wählt die Standardschreibweise.
    /// </summary>
    public ChangeCaseEditorViewModel() : base(RuleKind.ChangeCase) => SelectedOption = Options.First(o => o.Mode == CaseMode.Pascal);

    /// <summary>
    /// Alle Schreibweisen mit Beispiel.
    /// </summary>
    public IReadOnlyList<CaseOption> Options { get; } =
    [
        Option(CaseMode.Lower, Loc.T("Case.Lower")), Option(CaseMode.Upper, Loc.T("Case.Upper")),
        Option(CaseMode.Sentence, Loc.T("Case.Sentence")), Option(CaseMode.Title, Loc.T("Case.Title")),
        Option(CaseMode.Pascal, "PascalCase"), Option(CaseMode.Camel, "camelCase"),
        Option(CaseMode.Snake, "snake_case"), Option(CaseMode.Kebab, "kebab-case"),
    ];

    /// <summary>
    /// Gewählte Schreibweise.
    /// </summary>
    [ObservableProperty]
    public partial CaseOption SelectedOption { get; set; }

    /// <inheritdoc />
    public override RenameRule CreateRule() => new ChangeCaseRule { Mode = SelectedOption.Mode };

    /// <inheritdoc />
    protected override void LoadSettings(RenameRule rule) =>
        SelectedOption = Options.First(option => option.Mode == ((ChangeCaseRule)rule).Mode);

    /// <inheritdoc />
    protected override string? CreateExample() => $"{Sample} → {ChangeCaseRule.Convert(Sample, SelectedOption.Mode)}";

    /// <summary>
    /// Erzeugt eine Auswahlmöglichkeit samt Beispiel.
    /// </summary>
    /// <param name="mode">Schreibweise.</param>
    /// <param name="label">Anzeigename.</param>
    /// <returns>Die Auswahlmöglichkeit.</returns>
    private static CaseOption Option(CaseMode mode, string label) => new(mode, label, ChangeCaseRule.Convert(Sample, mode));
}

/// <summary>
/// Editor für „Zeichen bereinigen“.
/// </summary>
public sealed partial class CleanCharactersEditorViewModel() : RuleEditorViewModel(RuleKind.CleanCharacters)
{
    private const string Sample = "  Café   Müller ";

    /// <summary>Leerzeichen am Rand entfernen.</summary>
    [ObservableProperty]
    public partial bool TrimSpaces { get; set; } = true;

    /// <summary>Mehrfache Leerzeichen zusammenfassen.</summary>
    [ObservableProperty]
    public partial bool CollapseSpaces { get; set; } = true;

    /// <summary>Umlaute umschreiben.</summary>
    [ObservableProperty]
    public partial bool TransliterateUmlauts { get; set; } = true;

    /// <summary>Akzente entfernen.</summary>
    [ObservableProperty]
    public partial bool RemoveAccents { get; set; } = true;

    /// <inheritdoc />
    public override RenameRule CreateRule() => new CleanCharactersRule
    {
        TrimSpaces = TrimSpaces, CollapseSpaces = CollapseSpaces, TransliterateUmlauts = TransliterateUmlauts, RemoveAccents = RemoveAccents,
    };

    /// <inheritdoc />
    protected override void LoadSettings(RenameRule rule)
    {
        var clean = (CleanCharactersRule)rule;
        TrimSpaces = clean.TrimSpaces;
        CollapseSpaces = clean.CollapseSpaces;
        TransliterateUmlauts = clean.TransliterateUmlauts;
        RemoveAccents = clean.RemoveAccents;
    }

    /// <inheritdoc />
    protected override string? CreateExample() =>
        $"„{Sample.Replace(' ', '␣')}“ → „{((CleanCharactersRule)CreateRule()).Clean(Sample).Replace(' ', '␣')}“";
}

/// <summary>
/// Editor für „Bereich entfernen / kürzen“.
/// </summary>
public sealed partial class RemoveRangeEditorViewModel() : RuleEditorViewModel(RuleKind.RemoveRange)
{
    /// <summary>Entfernen oder Kürzen.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCutMode))]
    public partial RemoveMode Mode { get; set; } = RemoveMode.Cut;

    /// <summary>Startposition ab 0.</summary>
    [ObservableProperty]
    public partial decimal? Position { get; set; } = 0;

    /// <summary>Vom Ende zählen.</summary>
    [ObservableProperty]
    public partial bool FromEnd { get; set; }

    /// <summary>Anzahl Zeichen.</summary>
    [ObservableProperty]
    public partial decimal? Count { get; set; } = 4;

    /// <summary>Maximale Länge.</summary>
    [ObservableProperty]
    public partial decimal? MaxLength { get; set; } = 20;

    /// <summary>
    /// Ob die Felder für „Zeichen entfernen“ angezeigt werden.
    /// </summary>
    public bool IsCutMode => Mode == RemoveMode.Cut;

    /// <inheritdoc />
    public override RenameRule CreateRule() => new RemoveRangeRule
    {
        Mode = Mode, Position = (int)ToLong(Position, 0), FromEnd = FromEnd, Count = (int)ToLong(Count, 1), MaxLength = (int)ToLong(MaxLength, 1),
    };

    /// <inheritdoc />
    protected override void LoadSettings(RenameRule rule)
    {
        var remove = (RemoveRangeRule)rule;
        Mode = remove.Mode;
        Position = remove.Position;
        FromEnd = remove.FromEnd;
        Count = remove.Count;
        MaxLength = remove.MaxLength;
    }

    /// <inheritdoc />
    protected override string? CreateExample()
    {
        var sample = IsCutMode ? "IMG_0001" : Loc.T("Remove.LongSample");
        return $"{sample} → {ApplyToSamples(sample)[0]}";
    }

    /// <inheritdoc />
    protected override bool IsSettingProperty(string? propertyName) =>
        base.IsSettingProperty(propertyName) && propertyName != nameof(IsCutMode);
}

/// <summary>
/// Editor für „Ordnername einfügen“.
/// </summary>
public sealed partial class InsertFolderEditorViewModel() : RuleEditorViewModel(RuleKind.InsertFolder)
{
    /// <summary>Vor oder nach dem Namen.</summary>
    [ObservableProperty]
    public partial AffixPosition Position { get; set; } = AffixPosition.BeforeName;

    /// <summary>Ordnerebene ab 1.</summary>
    [ObservableProperty]
    public partial decimal? Level { get; set; } = 1;

    /// <summary>Trennzeichen.</summary>
    [ObservableProperty]
    public partial string Separator { get; set; } = "_";

    /// <inheritdoc />
    public override RenameRule CreateRule() => new InsertFolderRule { Position = Position, Level = (int)ToLong(Level, 1), Separator = Separator };

    /// <inheritdoc />
    protected override void LoadSettings(RenameRule rule)
    {
        var folder = (InsertFolderRule)rule;
        Position = folder.Position;
        Level = folder.Level;
        Separator = folder.Separator;
    }

    /// <inheritdoc />
    protected override string? CreateExample()
    {
        var parent = Loc.T("Sample.ParentFolder");
        var child = Loc.T("Sample.Folder");
        var folder = ToLong(Level, 1) == 1 ? child : parent;
        var name = Position == AffixPosition.BeforeName ? folder + Separator + "IMG_0001" : "IMG_0001" + Separator + folder;
        return $"{parent}{Path.DirectorySeparatorChar}{child}{Path.DirectorySeparatorChar}IMG_0001 → {name}";
    }
}

/// <summary>
/// Editor für „Erweiterung ändern“.
/// </summary>
public sealed partial class ChangeExtensionEditorViewModel() : RuleEditorViewModel(RuleKind.ChangeExtension)
{
    /// <summary>Nur bei dieser Endung (leer = alle).</summary>
    [ObservableProperty]
    public partial string OnlyExtension { get; set; } = string.Empty;

    /// <summary>Neue Endung (leer = beibehalten).</summary>
    [ObservableProperty]
    public partial string NewExtension { get; set; } = string.Empty;

    /// <summary>Schreibweise.</summary>
    [ObservableProperty]
    public partial ExtensionCasing Casing { get; set; } = ExtensionCasing.Lower;

    /// <inheritdoc />
    public override RenameRule CreateRule() => new ChangeExtensionRule { OnlyExtension = OnlyExtension, NewExtension = NewExtension, Casing = Casing };

    /// <inheritdoc />
    protected override void LoadSettings(RenameRule rule)
    {
        var extension = (ChangeExtensionRule)rule;
        OnlyExtension = extension.OnlyExtension;
        NewExtension = extension.NewExtension;
        Casing = extension.Casing;
    }

    /// <inheritdoc />
    protected override string? CreateExample()
    {
        var sample = string.IsNullOrWhiteSpace(OnlyExtension) ? ".JPEG" : "." + OnlyExtension.Trim().TrimStart('.').ToUpperInvariant();
        var photo = Loc.T("Sample.Photo");
        return $"{photo}{sample} → {photo}{((ChangeExtensionRule)CreateRule()).ConvertExtension(sample)}";
    }
}
