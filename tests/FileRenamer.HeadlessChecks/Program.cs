using Avalonia;
using Avalonia.Headless;

namespace FileRenamer.HeadlessChecks;

/// <summary>
/// Einstieg der Oberflächenprüfungen: startet Avalonia ohne sichtbares Fenster und führt die Prüfungen des Bereichs
/// „Tags“ und der Einstellungen aus. Screenshots landen im angegebenen Ordner.
/// </summary>
public static class Program
{
    /// <summary>
    /// Führt alle Prüfungen aus.
    /// </summary>
    /// <param name="args">Optional der Ordner für Screenshots (Standard: aktueller Ordner).</param>
    /// <returns>Anzahl fehlgeschlagener Prüfungen (0 = alles bestanden).</returns>
    public static int Main(string[] args)
    {
        var outputDirectory = args.Length > 0 ? args[0] : ".";
        Directory.CreateDirectory(outputDirectory);
        AppBuilder.Configure<FileRenamer.App.App>().UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .SetupWithoutStarting();

        var runner = new CheckRunner(outputDirectory);
        new TagAreaChecks(runner).Run();
        new SettingsChecks(runner).Run();
        Console.WriteLine(runner.Failures == 0 ? $"ALLE {runner.Count} PRÜFUNGEN BESTANDEN" : $"{runner.Failures} VON {runner.Count} FEHLGESCHLAGEN");
        return runner.Failures;
    }
}
