<img src="src/FileRenamer.App/Assets/app.png" alt="FileRenamer-Icon" width="96" align="right" />

# FileRenamer

Plattformübergreifende Desktopanwendung zum **kaskadierenden Umbenennen von Dateien** – einzeln oder
im Stapel. Mehrere Umbenennungsverfahren werden zu einer Kette verbunden; eine farbige Echtzeitvorschau
zeigt jederzeit, wie die Dateien heißen werden. Erst nach Bestätigung wird tatsächlich umbenannt.

![FileRenamer im hellen Theme](docs/images/screenshot-hell.png)

## Funktionen

- **Fünf Verfahren**, beliebig kombinierbar und mehrfach verwendbar:
  - **Text ersetzen** – erster, letzter oder alle Treffer; auch reguläre Ausdrücke
  - **Datum / Zeit anfügen** – Erstellungs- oder Änderungsdatum, vor oder nach dem Namen, Format wählbar
  - **Zähler anhängen** – Startwert, Schrittweite, führende Nullen, Sortierung nach Name, Datum oder Größe
  - **Ziffernkette Wertebereich verschieben** – vorhandene Zahlen im Namen um einen Wert verschieben (`name-001` + 100 → `name-101`)
  - **Text einfügen** – an einer Position vom Anfang oder vom Ende
- **Kette** per Drag & Drop sortieren, Verfahren per Checkbox ein- und ausschalten
- **Vorschau** mit Hervorhebung in der Farbe des jeweiligen Verfahrens, Hinweisen und Konflikten
- **Dateiauswahl** per Checkbox – nur ausgewählte Dateien werden umbenannt
- **Sicheres Umbenennen**: Konfliktprüfung (doppelte oder ungültige Namen, vorhandene Dateien),
  Alles-oder-nichts-Ausführung, Namenstausch a ↔ b, **Widerrufen** der letzten Umbenennung
- **Dateierweiterung ignorieren** (nur die letzte Endung)
- **Profile** zum Speichern und Laden ganzer Ketten
- **Theme**: System, Hell oder Dunkel

## Voraussetzungen

| | |
|---|---|
| Betriebssystem | Windows 10/11, Linux, macOS ab 14 |
| Zum Bauen | .NET SDK 10.0 |

## Schnellstart

```bash
dotnet run --project src/FileRenamer.App
```

Tests ausführen:

```bash
dotnet test --project tests/FileRenamer.Core.Tests
```

Eigenständig veröffentlichen (Beispiel Windows x64; für Linux `linux-x64`, für macOS `osx-arm64`):

```bash
dotnet publish src/FileRenamer.App -c Release -r win-x64 --self-contained
```

## Projektaufbau

| Pfad | Inhalt |
|------|--------|
| `src/FileRenamer.Core` | Fachlogik (Verfahren, Vorschau, Konfliktprüfung, Umbenennen, Profile) ohne Oberflächenabhängigkeit |
| `src/FileRenamer.App` | Avalonia-Oberfläche (MVVM) |
| `tests/FileRenamer.Core.Tests` | Unit- und Integrationstests (xUnit) |
| `testdata/` | Testdateien je Verfahren |
| `docs/` | Anwender- und Entwicklerdokumentation |
| `mockups/` | Gestaltungsentwürfe |

Technik: .NET 10 (LTS), Avalonia 12, CommunityToolkit.Mvvm, xUnit v3 – keine weiteren Abhängigkeiten.

## Dokumentation

- [Anwenderdokumentation](docs/ANWENDER.md) – Bedienung, Verfahren, Profile, Hinweise und Konflikte
- [Entwicklerdokumentation](docs/ENTWICKLER.md) – Architektur, Projektbaum, Programmablaufpläne, Erweiterung
- [Protokoll](PROTOKOLL.md) – Fragen, Entscheidungen, Fortschritt
- [Abnahmeprotokoll](ABNAHME.md)
- [Spezifikation](SPECS.md)

## Bekannte Einschränkungen

- Getestet unter Windows; Linux und macOS sind nicht geprüft.
- Unter Linux wird statt des Erstellungsdatums das Änderungsdatum verwendet (mit Hinweis).
- Kein Installer und kein macOS-App-Bundle.
- Widerrufen gilt für die letzte Umbenennung bis zum Programmende.
