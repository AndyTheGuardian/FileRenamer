using Avalonia;
using Avalonia.Controls;
using FileRenamer.Core.Rules;

namespace FileRenamer.App.Controls;

/// <summary>
/// Angefügte Eigenschaft, die einem Steuerelement die Stilklasse <c>kind-&lt;Verfahren&gt;</c> gibt.
/// Die Farbe selbst kommt aus Theme-abhängigen Stilen (passt sich Hell/Dunkel an).
/// </summary>
public sealed class RuleColor : AvaloniaObject
{
    private const string ClassPrefix = "kind-";

    /// <summary>
    /// Die Art des Verfahrens, nach der eingefärbt wird.
    /// </summary>
    public static readonly AttachedProperty<RuleKind?> KindProperty =
        AvaloniaProperty.RegisterAttached<RuleColor, Control, RuleKind?>("Kind");

    /// <summary>
    /// Registriert die Reaktion auf Wertänderungen.
    /// </summary>
    static RuleColor() => KindProperty.Changed.AddClassHandler<Control>(OnKindChanged);

    /// <summary>
    /// Liest die Verfahrensart.
    /// </summary>
    /// <param name="control">Das Steuerelement.</param>
    /// <returns>Die Verfahrensart.</returns>
    public static RuleKind? GetKind(Control control) => control.GetValue(KindProperty);

    /// <summary>
    /// Setzt die Verfahrensart.
    /// </summary>
    /// <param name="control">Das Steuerelement.</param>
    /// <param name="value">Die Verfahrensart.</param>
    public static void SetKind(Control control, RuleKind? value) => control.SetValue(KindProperty, value);

    /// <summary>
    /// Ersetzt die bisherige Farbklasse durch die neue.
    /// </summary>
    /// <param name="control">Das Steuerelement.</param>
    /// <param name="args">Die Änderungsinformation.</param>
    private static void OnKindChanged(Control control, AvaloniaPropertyChangedEventArgs args)
    {
        foreach (var existing in control.Classes.Where(name => name.StartsWith(ClassPrefix, StringComparison.Ordinal)).ToList())
        {
            control.Classes.Remove(existing);
        }

        if (args.NewValue is RuleKind kind)
        {
            control.Classes.Add(ClassPrefix + kind);
        }
    }
}
