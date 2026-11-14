using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;
using FileRenamer.App.ViewModels.Tags;

namespace FileRenamer.HeadlessChecks;

/// <summary>
/// Teil der Prüfungen: einheitliche Höhe aller Bedienelemente und normale Schrift gewählter Umschalter (E160) in
/// beiden Bereichen, allen Tag-Editoren und den Einstellungen.
/// </summary>
public sealed partial class SettingsChecks
{
    /// <summary>Soll-Höhe der Bedienelemente.</summary>
    private const double ControlHeight = 33;

    /// <summary>
    /// Prüft den Bereich „Umbenennen“ mit jedem Verfahren und jede Tag-Funktion.
    /// </summary>
    private void ControlHeightsInMainWindow()
    {
        foreach (var kind in _vm.RuleKinds.ToList())
        {
            _vm.SelectedRuleKind = kind;
            CheckRunner.Pump();
            CheckControls("Umbenennen/" + kind.Kind, _window);
        }

        _vm.SelectedRuleKind = _vm.RuleKinds[0];
        _vm.IsTagArea = true;
        foreach (var item in _vm.Tags.FunctionEntries.OfType<TagFunctionItem>().ToList())
        {
            _vm.Tags.SelectedFunction = item;
            CheckRunner.Pump();
            CheckControls("Tags/" + item.Function, _window);
        }

        _vm.IsTagArea = false;
        CheckRunner.Pump();
    }

    /// <summary>
    /// Prüft Höhe und Schrift der sichtbaren Bedienelemente unterhalb eines Elements.
    /// </summary>
    /// <param name="area">Name des Bereichs für die Meldung.</param>
    /// <param name="root">Fenster oder Bereich.</param>
    private void CheckControls(string area, Visual root)
    {
        var controls = root.GetVisualDescendants().OfType<Control>().Where(IsBoxControl).ToList();
        var wrong = controls.Where(c => Math.Abs(c.Bounds.Height - ControlHeight) > 0.5)
            .Select(c => $"{c.GetType().Name}({Label(c)})={c.Bounds.Height:0.#}").Distinct().ToList();
        Check($"{area}: alle Bedienelemente {ControlHeight} px hoch ({controls.Count})", controls.Count > 0 && wrong.Count == 0, string.Join(" | ", wrong));
        var bold = root.GetVisualDescendants().OfType<Control>().Where(IsActiveToggle)
            .Where(c => c.GetVisualDescendants().OfType<TextBlock>().Any(t => t.IsEffectivelyVisible && t.FontWeight != FontWeight.Normal))
            .Select(Label).ToList();
        Check($"{area}: gewählte Umschalter mit normaler Schrift", bold.Count == 0, string.Join(" | ", bold));
    }

    /// <summary>
    /// Ob ein Element ein kastenförmiges Bedienelement ist: Schaltfläche, Text-, Zahlen- oder Auswahlfeld, Umschalter.
    /// Ausgenommen sind kleine Elemente im Text (Platzhalter-Chips, Farbflächen, ✕/↺ ohne Rahmen), Checkboxen,
    /// mehrzeilige Textfelder und Teile anderer Steuerelemente.
    /// </summary>
    /// <param name="control">Das Element.</param>
    /// <returns><c>true</c>, wenn es die einheitliche Höhe haben soll.</returns>
    private static bool IsBoxControl(Control control)
    {
        if (!control.IsEffectivelyVisible || control.FindAncestorOfType<NumericUpDown>() is not null || control.FindAncestorOfType<ComboBox>() is not null
            || control.FindAncestorOfType<ScrollBar>() is not null)
        {
            return false;
        }

        return control switch
        {
            CheckBox => false,
            RadioButton radio => radio.Classes.Contains("segment"),
            Button button => !button.Classes.Contains("chip") && !button.Classes.Contains("swatch") && button.BorderThickness != default
                && button.Background is not ISolidColorBrush { Color.A: 0 },
            TextBox box => !box.AcceptsReturn,
            ListBox list => list.Classes.Contains("segments"),
            ComboBox or NumericUpDown => true,
            _ => false,
        };
    }

    /// <summary>
    /// Ob ein Element ein gewählter Umschalter ist (Segment oder gewähltes Feld eines Segment-ListBox).
    /// </summary>
    /// <param name="control">Das Element.</param>
    /// <returns><c>true</c> bei einem gewählten Umschalter.</returns>
    private static bool IsActiveToggle(Control control) => control.IsEffectivelyVisible && control switch
    {
        RadioButton { IsChecked: true } radio => radio.Classes.Contains("segment"),
        ListBoxItem { IsSelected: true } item => item.FindAncestorOfType<ListBox>()?.Classes.Contains("segments") == true,
        _ => false,
    };

    /// <summary>
    /// Kurzbezeichnung eines Elements für Meldungen.
    /// </summary>
    /// <param name="control">Das Element.</param>
    /// <returns>Name, Beschriftung oder erster Text.</returns>
    private static string Label(Control control) => control switch
    {
        ContentControl { Content: string text } => text,
        _ => control.Name ?? control.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault()?.Text ?? string.Empty,
    };
}
