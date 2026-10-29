using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileRenamer.Core.Profiles;

/// <summary>
/// Gemeinsame JSON-Einstellungen und Hilfsfunktionen zum Lesen und Schreiben von Dateien.
/// </summary>
internal static class JsonFileStore
{
    /// <summary>
    /// Serialisierungsoptionen: eingerückt, Aufzählungen als Text, Typkennung an beliebiger Position.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        AllowOutOfOrderMetadataProperties = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Schreibt ein Objekt als JSON; legt das Verzeichnis bei Bedarf an.
    /// </summary>
    /// <typeparam name="T">Typ des Objekts.</typeparam>
    /// <param name="path">Zieldatei.</param>
    /// <param name="value">Das Objekt.</param>
    public static void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(value, Options));
    }

    /// <summary>
    /// Liest ein Objekt aus einer JSON-Datei.
    /// </summary>
    /// <typeparam name="T">Typ des Objekts.</typeparam>
    /// <param name="path">Quelldatei.</param>
    /// <returns>Das Objekt oder <c>null</c>, wenn die Datei fehlt oder ungültig ist.</returns>
    public static T? Read<T>(string path) where T : class
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
