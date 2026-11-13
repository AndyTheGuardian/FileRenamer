using Avalonia;
using Avalonia.Controls;
using FileRenamer.Core.Tagging.Changes;

namespace FileRenamer.App.Controls;

/// <summary>
/// Angehängte Eigenschaft, die einem Steuerelement die Klasse <c>fn-&lt;Funktion&gt;</c> gibt. Darüber setzen die Stile
/// in <c>App.axaml</c> Balken- und Hervorhebungsfarbe der Funktionen des Bereichs „Tags“ (wie im Mockup v13).
/// </summary>
public sealed class TagColor : AvaloniaObject
{
    private const string ClassPrefix = "fn-";

    /// <summary>
    /// Die Funktion, deren Farbe das Steuerelement erhält, oder <c>null</c> für keine Farbe.
    /// </summary>
    public static readonly AttachedProperty<TagFunction?> FunctionProperty =
        AvaloniaProperty.RegisterAttached<TagColor, Control, TagFunction?>("Function");

    /// <summary>
    /// Meldet die Klassenpflege bei Änderungen an.
    /// </summary>
    static TagColor() => FunctionProperty.Changed.AddClassHandler<Control>(OnFunctionChanged);

    /// <summary>
    /// Liest die Funktion.
    /// </summary>
    /// <param name="control">Das Steuerelement.</param>
    /// <returns>Die Funktion oder <c>null</c>.</returns>
    public static TagFunction? GetFunction(Control control) => control.GetValue(FunctionProperty);

    /// <summary>
    /// Setzt die Funktion.
    /// </summary>
    /// <param name="control">Das Steuerelement.</param>
    /// <param name="value">Die Funktion oder <c>null</c>.</param>
    public static void SetFunction(Control control, TagFunction? value) => control.SetValue(FunctionProperty, value);

    /// <summary>
    /// Ersetzt die Farbklasse.
    /// </summary>
    /// <param name="control">Das Steuerelement.</param>
    /// <param name="args">Die Änderung.</param>
    private static void OnFunctionChanged(Control control, AvaloniaPropertyChangedEventArgs args)
    {
        foreach (var existing in control.Classes.Where(name => name.StartsWith(ClassPrefix, StringComparison.Ordinal)).ToList())
        {
            control.Classes.Remove(existing);
        }

        if (args.NewValue is TagFunction function)
        {
            control.Classes.Add(ClassPrefix + function);
        }
    }
}
