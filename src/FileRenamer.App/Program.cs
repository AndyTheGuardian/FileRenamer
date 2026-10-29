using Avalonia;

namespace FileRenamer.App;

/// <summary>
/// Einstiegspunkt der Desktop-Anwendung.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Startet die Avalonia-Anwendung mit klassischer Desktop-Lebensdauer.
    /// </summary>
    /// <param name="args">Befehlszeilenargumente.</param>
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Konfiguriert Avalonia (wird auch vom Designer verwendet).
    /// </summary>
    /// <returns>Der konfigurierte AppBuilder.</returns>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
