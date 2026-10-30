using FileRenamer.Core.Rules;
using FileRenamer.Core.Tests.Support;

namespace FileRenamer.Core.Tests.Rules;

/// <summary>
/// Tests für „Groß-/Kleinschreibung“, „Zeichen bereinigen“, „Bereich entfernen / kürzen“ und „Ordnername einfügen“.
/// </summary>
public sealed class TextRulesTests
{
    /// <summary>
    /// Alle Schreibweisen am Beispiel aus dem Mockup (E58, E59).
    /// </summary>
    /// <param name="mode">Schreibweise.</param>
    /// <param name="expected">Erwartetes Ergebnis.</param>
    [Theory]
    [InlineData(CaseMode.Lower, "mein urlaub-foto_2026")]
    [InlineData(CaseMode.Upper, "MEIN URLAUB-FOTO_2026")]
    [InlineData(CaseMode.Sentence, "Mein urlaub-foto_2026")]
    [InlineData(CaseMode.Title, "Mein Urlaub-Foto_2026")]
    [InlineData(CaseMode.Pascal, "MeinUrlaubFoto2026")]
    [InlineData(CaseMode.Camel, "meinUrlaubFoto2026")]
    [InlineData(CaseMode.Snake, "mein_urlaub_foto_2026")]
    [InlineData(CaseMode.Kebab, "mein-urlaub-foto-2026")]
    public void ChangeCase_AllModes(CaseMode mode, string expected)
    {
        Assert.Equal(expected, new ChangeCaseRule { Mode = mode }.ApplyTo("mein Urlaub-Foto_2026").Name.Text);
    }

    /// <summary>
    /// Wechsel klein→GROSS trennt Wörter; ß bleibt beim Großschreiben ß.
    /// </summary>
    [Fact]
    public void ChangeCase_CamelBoundaryAndSharpS()
    {
        Assert.Equal("mein_bild_v2", new ChangeCaseRule { Mode = CaseMode.Snake }.ApplyTo("meinBild v2").Name.Text);
        Assert.Equal("STRASSE GROß", ChangeCaseRule.Convert("Strasse groß", CaseMode.Upper));
    }

    /// <summary>
    /// Bei gleicher Länge werden nur die geänderten Zeichen hervorgehoben.
    /// </summary>
    [Fact]
    public void ChangeCase_HighlightsOnlyChangedCharacters()
    {
        var name = new ChangeCaseRule { Mode = CaseMode.Upper }.ApplyTo("ABc").Name;

        Assert.Equal("AB", name.Segments[0].Text);
        Assert.True(name.Segments[0].IsOriginal);
        Assert.Equal("C", name.Segments[1].Text);
    }

    /// <summary>
    /// Zeichen bereinigen mit allen Optionen; Umlaute vor Akzenten.
    /// </summary>
    [Fact]
    public void CleanCharacters_AllOptions()
    {
        Assert.Equal("Cafe Mueller Strasse", new CleanCharactersRule().ApplyTo("  Café   Müller Straße ").Name.Text);
        Assert.Equal("  Café   Mueller", new CleanCharactersRule { TrimSpaces = false, CollapseSpaces = false, RemoveAccents = false }.ApplyTo("  Café   Müller").Name.Text);
    }

    /// <summary>
    /// Bereich entfernen vom Anfang, vom Ende und begrenzt; Kürzen auf Länge.
    /// </summary>
    /// <param name="mode">Modus.</param>
    /// <param name="position">Position.</param>
    /// <param name="fromEnd">Vom Ende.</param>
    /// <param name="count">Anzahl.</param>
    /// <param name="maxLength">Höchstlänge.</param>
    /// <param name="expected">Erwartetes Ergebnis.</param>
    [Theory]
    [InlineData(RemoveMode.Cut, 0, false, 4, 20, "0001")]
    [InlineData(RemoveMode.Cut, 3, false, 1, 20, "IMG0001")]
    [InlineData(RemoveMode.Cut, 0, true, 4, 20, "IMG_")]
    [InlineData(RemoveMode.Cut, 6, false, 10, 20, "IMG_00")]
    [InlineData(RemoveMode.Cut, 50, false, 2, 20, "IMG_0001")]
    [InlineData(RemoveMode.Truncate, 0, false, 1, 5, "IMG_0")]
    [InlineData(RemoveMode.Truncate, 0, false, 1, 50, "IMG_0001")]
    public void RemoveRange_CutsAndTruncates(RemoveMode mode, int position, bool fromEnd, int count, int maxLength, string expected)
    {
        var rule = new RemoveRangeRule { Mode = mode, Position = position, FromEnd = fromEnd, Count = count, MaxLength = maxLength };

        Assert.Equal(expected, rule.ApplyTo("IMG_0001").Name.Text);
    }

    /// <summary>
    /// Ungültige Zahlenwerte werden abgewiesen.
    /// </summary>
    [Fact]
    public void RemoveRange_ValidatesSettings()
    {
        Assert.NotNull(new RemoveRangeRule { Count = 0 }.ValidateSettings());
        Assert.NotNull(new RemoveRangeRule { Mode = RemoveMode.Truncate, MaxLength = 0 }.ValidateSettings());
    }

    /// <summary>
    /// Ordnername der Ebene 1 und 2, vor und nach dem Namen; fehlende Ebene → Hinweis.
    /// </summary>
    [Fact]
    public void InsertFolder_Levels()
    {
        var file = RuleTestHelper.File("IMG_0001") with { FullPath = Path.Combine(Path.GetTempPath(), "Reisen", "Urlaub 2026", "IMG_0001") };

        Assert.Equal("Urlaub 2026_IMG_0001", new InsertFolderRule().ApplyTo(file).Name.Text);
        Assert.Equal("IMG_0001 - Reisen", new InsertFolderRule { Level = 2, Position = AffixPosition.AfterName, Separator = " - " }.ApplyTo(file).Name.Text);

        var output = new InsertFolderRule { Level = 50 }.ApplyTo(file);
        Assert.Equal("IMG_0001", output.Name.Text);
        Assert.Equal(IssueSeverity.Hint, Assert.Single(output.Issues).Severity);
    }
}
