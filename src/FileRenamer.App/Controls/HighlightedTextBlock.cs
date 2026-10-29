using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using FileRenamer.App.ViewModels;

namespace FileRenamer.App.Controls;

/// <summary>
/// TextBlock, der einen Namen abschnittsweise mit der Farbe des jeweiligen Verfahrens hinterlegt.
/// Bei einem Theme-Wechsel werden die Farben neu aus den Ressourcen gelesen.
/// </summary>
public sealed class HighlightedTextBlock : TextBlock
{
    /// <summary>
    /// Die anzuzeigenden Abschnitte.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyList<HighlightSpan>?> SpansProperty =
        AvaloniaProperty.Register<HighlightedTextBlock, IReadOnlyList<HighlightSpan>?>(nameof(Spans));

    /// <summary>
    /// Baut die Inlines bei einem Theme-Wechsel neu auf.
    /// </summary>
    public HighlightedTextBlock() => ActualThemeVariantChanged += (_, _) => RebuildInlines();

    /// <summary>
    /// Die anzuzeigenden Abschnitte.
    /// </summary>
    public IReadOnlyList<HighlightSpan>? Spans
    {
        get => GetValue(SpansProperty);
        set => SetValue(SpansProperty, value);
    }

    /// <summary>
    /// Verwendet die Stile eines normalen TextBlocks.
    /// </summary>
    protected override Type StyleKeyOverride => typeof(TextBlock);

    /// <summary>
    /// Baut die Inlines neu auf, wenn sich die Abschnitte ändern.
    /// </summary>
    /// <param name="change">Die Änderungsinformation.</param>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SpansProperty)
        {
            RebuildInlines();
        }
    }

    /// <summary>
    /// Erzeugt je Abschnitt einen Run; hervorgehobene Abschnitte erhalten wie im Mockup einen farbigen
    /// Hintergrund und eine Unterstreichung in der Farbe des Verfahrens.
    /// </summary>
    private void RebuildInlines()
    {
        var inlines = new InlineCollection();
        foreach (var span in Spans ?? [])
        {
            var run = new Run(span.Text);
            if (span.Kind is { } kind)
            {
                run.Background = FindBrush("RuleHighlight." + kind);
                run.TextDecorations = CreateUnderline(FindBrush("RuleColor." + kind));
            }

            inlines.Add(run);
        }

        Inlines = inlines;
    }

    /// <summary>
    /// Sucht einen Pinsel in den Theme-Ressourcen.
    /// </summary>
    /// <param name="key">Ressourcenschlüssel.</param>
    /// <returns>Der Pinsel oder <c>null</c>.</returns>
    private IBrush? FindBrush(string key) =>
        this.TryFindResource(key, ActualThemeVariant, out var resource) ? resource as IBrush : null;

    /// <summary>
    /// Erzeugt eine farbige Unterstreichung.
    /// </summary>
    /// <param name="brush">Farbe der Linie.</param>
    /// <returns>Die Textdekoration oder <c>null</c> ohne Farbe.</returns>
    private static TextDecorationCollection? CreateUnderline(IBrush? brush) =>
        brush is null
            ? null
            : [new TextDecoration { Location = TextDecorationLocation.Underline, Stroke = brush, StrokeThickness = 2 }];
}
