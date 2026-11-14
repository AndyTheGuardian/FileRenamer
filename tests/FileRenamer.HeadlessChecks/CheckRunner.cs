using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace FileRenamer.HeadlessChecks;

/// <summary>
/// Gemeinsame Hilfen der Prüfungen: Ergebnis melden und zählen, Oberfläche weiterlaufen lassen, Screenshots speichern.
/// </summary>
/// <param name="outputDirectory">Ordner für Screenshots.</param>
public sealed class CheckRunner(string outputDirectory)
{
    /// <summary>Anzahl ausgeführter Prüfungen.</summary>
    public int Count { get; private set; }

    /// <summary>Anzahl fehlgeschlagener Prüfungen.</summary>
    public int Failures { get; private set; }

    /// <summary>
    /// Meldet das Ergebnis einer Prüfung auf der Konsole („OK“ bzw. „FAIL“).
    /// </summary>
    /// <param name="name">Was geprüft wird.</param>
    /// <param name="ok">Ob die Prüfung bestanden ist.</param>
    /// <param name="detail">Zusatzinformation zur Fehlersuche.</param>
    public void Check(string name, bool ok, string detail = "")
    {
        Count++;
        Console.WriteLine($"{(ok ? "OK  " : "FAIL")} {name} {detail}");
        if (!ok)
        {
            Failures++;
        }
    }

    /// <summary>
    /// Lässt Dispatcher und Darstellung einige Durchläufe weiterlaufen (Layout, Bindungen, Rendern).
    /// </summary>
    public static void Pump()
    {
        for (var i = 0; i < 6; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }

    /// <summary>
    /// Wartet auf eine Aufgabe und lässt währenddessen den Dispatcher laufen.
    /// </summary>
    /// <param name="task">Die Aufgabe.</param>
    public static void Wait(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);
        while (!task.IsCompleted)
        {
            Dispatcher.UIThread.RunJobs();
        }

        if (task.IsFaulted)
        {
            throw task.Exception!;
        }
    }

    /// <summary>
    /// Speichert das aktuelle Bild eines Fensters als PNG.
    /// </summary>
    /// <param name="window">Das Fenster.</param>
    /// <param name="fileName">Dateiname im Ausgabeordner.</param>
    public void Shot(TopLevel window, string fileName)
    {
        ArgumentNullException.ThrowIfNull(window);
        Pump();
        window.CaptureRenderedFrame()?.Save(Path.Combine(outputDirectory, fileName), new PngBitmapEncoderOptions());
    }

    /// <summary>
    /// Sucht eine Farbressource der Anwendung für das aktuelle Design.
    /// </summary>
    /// <param name="key">Schlüssel der Ressource.</param>
    /// <returns>Die Farbe als Text (z. B. „#fff3f4f6“) oder „?“.</returns>
    public static string Resource(string key) =>
        Application.Current!.TryFindResource(key, Application.Current!.ActualThemeVariant, out var value) && value is Avalonia.Media.ISolidColorBrush brush
            ? brush.Color.ToString()
            : "?";
}
