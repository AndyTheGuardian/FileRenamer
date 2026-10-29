using CommunityToolkit.Mvvm.ComponentModel;
using FileRenamer.Core.Rules;

namespace FileRenamer.App.ViewModels.Editors;

/// <summary>
/// Editor für „Text einfügen“. Das Positionsfeld ist auf 0 … Länge des längsten Namens begrenzt (E10).
/// </summary>
public sealed partial class InsertTextEditorViewModel() : RuleEditorViewModel(RuleKind.InsertText)
{
    /// <summary>
    /// Obergrenze, solange keine Dateien geladen sind.
    /// </summary>
    private const int UnboundedPosition = 255;

    /// <summary>
    /// Der einzufügende Text.
    /// </summary>
    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    /// <summary>
    /// Position ab 0.
    /// </summary>
    [ObservableProperty]
    public partial decimal? Position { get; set; } = 0;

    /// <summary>
    /// Ob vom Ende gezählt wird.
    /// </summary>
    [ObservableProperty]
    public partial bool FromEnd { get; set; }

    /// <summary>
    /// Größte sinnvolle Position (Länge des längsten Namens an dieser Stelle der Kette).
    /// </summary>
    [ObservableProperty]
    public partial decimal MaxPosition { get; set; } = UnboundedPosition;

    /// <summary>
    /// Erzeugt das Verfahren aus den Eingaben.
    /// </summary>
    /// <returns>Das Verfahren.</returns>
    public override RenameRule CreateRule() => new InsertTextRule
    {
        Text = Text,
        Position = (int)ToLong(Position, 0),
        FromEnd = FromEnd,
    };

    /// <summary>
    /// Setzt die Obergrenze der Position.
    /// </summary>
    /// <param name="inputNames">Die Namen vor Anwendung dieses Verfahrens.</param>
    public override void UpdateContext(IReadOnlyList<string> inputNames) =>
        MaxPosition = inputNames.Count == 0 ? UnboundedPosition : Math.Max(inputNames.Max(name => name.Length), (int)ToLong(Position, 0));

    /// <summary>
    /// Beispiel wie im Mockup für „bericht“.
    /// </summary>
    /// <returns>Der Beispieltext.</returns>
    protected override string? CreateExample() => $"bericht → {ApplyToSamples("bericht")[0]}";

    /// <summary>
    /// Übernimmt die Parameter eines Verfahrens.
    /// </summary>
    /// <param name="rule">Das Verfahren.</param>
    protected override void LoadSettings(RenameRule rule)
    {
        var insert = (InsertTextRule)rule;
        Text = insert.Text;
        Position = insert.Position;
        FromEnd = insert.FromEnd;
    }

    /// <summary>
    /// Die Obergrenze ist keine Einstellung.
    /// </summary>
    /// <param name="propertyName">Name der Eigenschaft.</param>
    /// <returns><c>true</c> bei einer Einstellung.</returns>
    protected override bool IsSettingProperty(string? propertyName) =>
        base.IsSettingProperty(propertyName) && propertyName != nameof(MaxPosition);
}
