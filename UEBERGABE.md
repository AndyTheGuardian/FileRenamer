# FileRenamer – Übergabe

Stand: 2026-11-16 · Letzter Commit: `406385f` „Einstellungen wie PDF Utopia, einheitliche Höhen, Headless-Prüfungen“
(auf `master`, nach `origin` gepusht)

Diese Datei fasst alles zusammen, was zum Weiterarbeiten nötig ist – auch mit einem anderen Konto oder in einer
neuen Claude-Code-Sitzung ohne den bisherigen Gesprächsverlauf. Die Einzelheiten stehen in den unten genannten
Dateien; diese Übersicht ersetzt sie nicht.

---

## 1. Worum es geht

Plattformübergreifende Desktopanwendung (Windows, Linux, macOS 14+) mit zwei Bereichen:

- **Umbenennen** (Strg+1): elf Verfahren, zu einer Kette verbunden, mit farbiger Echtzeitvorschau, Profilen,
  Konflikterkennung, Widerrufen.
- **Tags** (Strg+2): Audio-Tags bearbeiten (TagLib#), Tags aus Dateinamen, Tracknummern, Cover, Lyrics; Änderungen
  werden vorgemerkt, in einer Vorschau gezeigt und alles oder nichts gespeichert.
- **Einstellungen** (Zahnrad, *Datei → Einstellungen…*): Design, Sprache (Deutsch/Englisch), Farben je Design,
  Farbprofile (Austauschformat wie PDF Utopia und AI Asset Manager), Cover verkleinern.

Ausführlich: `README.md`, `docs/ANWENDER.md`.

## 2. Wo was steht

| Datei / Ordner | Inhalt |
|---|---|
| `CLAUDE.md` (Projekt) | Arbeitsregeln für Claude – **zuerst lesen** (siehe Abschnitt 6) |
| `/home/andy/Claude/CLAUDE.md` | übergeordnete Regeln für alle Projekte im Arbeitsordner |
| `SPECS.md` | ursprüngliche Spezifikation – **nicht verändern** |
| `PROTOKOLL.md` | Fortschritt, alle Entscheidungen (E1–E160), Fragen mit Antworten (F…), Annahmen (A…), Vorschläge (V…), bekannte Einschränkungen (B…), Erledigtes – das Gedächtnis des Projekts |
| `ABNAHME.md` | Abnahmeprotokoll vom 2026-10-29 (Unterschrift offen) |
| `docs/ANWENDER.md` | Anwenderdokumentation mit Screenshots (`docs/images/`) |
| `docs/ENTWICKLER.md` | Entwicklerdokumentation: Befehle, Projektstruktur, Architektur, Ablaufpläne, Verfahren hinzufügen, Tests, Konventionen mit technischen Hinweisen |
| `mockups/` | Mockups v1–v13 (HTML); v3 = abgenommene Gestaltung, v13 = Bereich „Tags“ |
| `testdata/` | Testdateien je Verfahren (Beschreibung in `testdata/README.md`); `testdata - Kopie/` ist eine ältere Kopie |
| `Chatverlauf-FileRenamer.pdf` | Export eines früheren Gesprächsverlaufs |
| `publish/` | Veröffentlichte Builds (nicht im Git, `.gitignore`) |

## 3. Technik und Umgebung

- .NET SDK 10 (hier 10.0.114 unter Linux), C#, `Nullable` und `ImplicitUsings` aktiv (`Directory.Build.props`)
- Avalonia 12.1.4 (Fluent), CommunityToolkit.Mvvm 8.4.2, TagLibSharp 2.3.0, xUnit v3 4.0.2,
  Microsoft Testing Platform (`global.json`)
- Git: `https://github.com/AndyTheGuardian/FileRenamer.git`, Zweig `master`. Gepusht wird aus dem eigenen Terminal –
  die Claude-Shell hat keine GitHub-Anmeldung, `gh` ist nicht installiert.

## 4. Befehle

Alle Befehle aus dem Projektordner `/home/andy/Claude/FileRenamer`:

```bash
dotnet build
```

```bash
dotnet test
```

```bash
dotnet run --project src/FileRenamer.App
```

```bash
dotnet run --project tests/FileRenamer.HeadlessChecks -- /tmp/fr-headless
```

Erwarteter Stand: Build **ohne Warnungen**, **355 Tests** bestanden (320 Core, 35 App), **151 Headless-Prüfungen**
bestanden („ALLE 151 PRÜFUNGEN BESTANDEN“, Exit-Code 0, 28+ Screenshots im angegebenen Ordner). Einzelheiten zu den
Headless-Prüfungen: `docs/ENTWICKLER.md`, Abschnitt „Oberflächenprüfungen (Headless)“.

## 5. Aufbau in Kürze

| Projekt | Aufgabe |
|---|---|
| `src/FileRenamer.Core` | gesamte Fachlogik ohne Oberfläche: Verfahren (`Rules`), Vorschau (`Preview`), Metadaten, Profile und Einstellungen (`Profiles`), Tags (`Tagging`), Farben (`Theming`) |
| `src/FileRenamer.App` | Avalonia-Oberfläche: Views, ViewModels (MVVM), Dienste (Dialoge, Theme, Zwischenablage, Bildverarbeitung) |
| `tests/FileRenamer.Core.Tests` | Unit- und Integrationstests der Fachlogik |
| `tests/FileRenamer.App.Tests` | Tests der Einstellungen (`SettingsViewModel`, Farbprofile) |
| `tests/FileRenamer.HeadlessChecks` | Oberflächenprüfungen im echten Hauptfenster ohne Bildschirm (Konsolenprogramm, nicht Teil von `dotnet test`) |

Wichtige Stellen für typische Änderungen:

- Neues Verfahren: Anleitung in `docs/ENTWICKLER.md`, Abschnitt „Verfahren hinzufügen“.
- Texte (Deutsch/Englisch): `src/FileRenamer.App/Localization/Strings.cs`, Fachtexte `src/FileRenamer.Core/Localization`.
- Farben und Design: `Core/Theming` (Paletten, Profile), `App/Services/ThemeService.cs` (schreibt die Ressourcen),
  `App.axaml` (Stile; `ControlHeight` = 33 px für alle Bedienelemente).
- Einstellungen: `App/ViewModels/Settings/`, `App/Views/SettingsWindow.axaml`, gespeichert in `AppSettings`
  (`settings.json` unter `~/.config/FileRenamer` bzw. `%APPDATA%\FileRenamer`).

## 6. Arbeitsweise (vereinbart)

Aus `CLAUDE.md` und den bisherigen Absprachen:

1. **Keine eigenen Annahmen.** Bei Unklarheiten nachfragen, bis die Aufgabe eindeutig ist; Vorschläge sind erwünscht,
   müssen aber bestätigt werden. Fragen im Chat stellen **und** in `PROTOKOLL.md` festhalten.
2. **Jede Klasse und Methode** bekommt eine deutsche `<summary>`; kurze, nicht verschachtelte Methoden; Clean Code, SOLID.
3. **`SPECS.md` nie ändern**; Abweichungen und Präzisierungen kommen als Entscheidung (E…) in `PROTOKOLL.md`.
4. **`PROTOKOLL.md` im selben Arbeitsschritt pflegen:** Stand-Datum, Fortschrittszeile, neue IDs (E/F/A/V/B),
   Erledigtes mit Datum in „Erledigt“, „Nächster Schritt“ am Ende des Fortschritts.
5. **Entwickler- und Anwenderdoku** bei jeder sichtbaren Änderung mitziehen (`docs/`).
6. **Prüfen vor dem Melden:** Build ohne Warnungen, `dotnet test`, Headless-Prüfungen; Oberflächenänderungen per
   Screenshot (hell und dunkel) ansehen. Fehler zuerst headless nachstellen, dann beheben; neue Prüfung ergänzen,
   die mit dem alten Stand fehlschlägt.
7. **Committen nur auf Wunsch** des Anwenders; auf einem eigenen Zweig, Zusammenführen nach `master` nach Rückfrage.
   Commit-Nachrichten auf Deutsch.
8. **Antworten:** Der Anwender schreibt Deutsch oder Englisch; Dokumentation und `PROTOKOLL.md` sind deutsch.
9. Testinstanzen der App dürfen den Anwender nicht stören: GUI-Tests in einem eigenen X-Server (Xephyr) unter
   `dbus-run-session -- …` starten, sonst öffnen sich Dateidialoge auf dem echten Bildschirm.

## 7. Aktueller Stand

Zuletzt umgesetzt (alles committet und gepusht, Einzelheiten in `PROTOKOLL.md`):

- **E150–E153:** Beide Dateilisten gleich – Checkboxen auch im Tagger, kleine Zeilen, horizontales Scrollen mit
  mitlaufendem Spaltenkopf, Hinweis in der leeren Liste, „Liste leeren“ in der Titelzeile.
- **E154:** Im Tagger sind alle Audiodateien ausgewählt.
- **E155–E158:** Einstellungsdialog aus PDF Utopia übernommen (Farben je Design, Farbprofile, Zahnrad).
- **E159:** Headless-Prüfungen im Projekt (`tests/FileRenamer.HeadlessChecks`).
- **E160:** Einheitliche Höhe 33 px, gewählte Umschalter ohne fette Schrift.

**Nächster Schritt:** Anwendertest der Einstellungen und des Bereichs „Tags“ in der laufenden App; Rückmeldungen
wie bisher als Entscheidungen bzw. Fehlerkorrekturen in `PROTOKOLL.md` aufnehmen.

## 8. Offen und bekannt

- **Keine offenen Fragen oder Annahmen** in `PROTOKOLL.md` (Stand dieser Übergabe).
- **B16:** Sprachwechsel *im* Einstellungsdialog (Dialog schließt sich und öffnet sich über dem neu aufgebauten
  Fenster wieder) und Export/Import über die echte Dateiauswahl sind nicht in der laufenden App geprüft.
- **Abnahme:** Unterschrift in `ABNAHME.md` offen; die Bereiche „Tags“ und „Einstellungen“ kamen nach der Abnahme
  hinzu und sind dort noch nicht nachgetragen.
- **B4** („nur unter Windows getestet“) ist veraltet: Build, Tests und Headless-Prüfungen laufen inzwischen unter
  Linux; macOS ist weiterhin ungeprüft. Bei Gelegenheit in `PROTOKOLL.md` anpassen.
- Die übergeordnete `/home/andy/Claude/CLAUDE.md` nennt `OFFENE-PUNKTE.md` und nach der Abnahme zusätzlich ein
  Benutzerhandbuch. FileRenamer führt die offenen Punkte bisher in `PROTOKOLL.md`; ein Benutzerhandbuch gibt es noch
  nicht (nur `docs/ANWENDER.md`). Vor der nächsten Abnahme mit dem Anwender klären.
- Übrige bekannte Einschränkungen: `PROTOKOLL.md`, Abschnitt „Bekannte Einschränkungen“ (B1–B16).

## 9. Einstieg in einer neuen Sitzung

1. `CLAUDE.md`, diese Datei und `PROTOKOLL.md` (mindestens „Fortschritt“ ab 2026-11-09 und die Entscheidungen ab
   E130) lesen.
2. `git pull`, dann die Befehle aus Abschnitt 4 ausführen und den erwarteten Stand bestätigen.
3. Für die Oberfläche `docs/ENTWICKLER.md`, Abschnitt „Konventionen“, lesen – dort stehen die Avalonia-Besonderheiten,
   die schon Fehler verursacht haben (z. B. lokale Werte überstimmen Stile, Hintergrund nötig für Treffer,
   geerbte Eigenschaften erreichen Listenzeilen nicht).
4. Diese Übergabe bei größeren Änderungen mit aktualisieren (Stand, letzter Commit, Abschnitte 7 und 8).
