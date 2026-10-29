# FileRenamer – Protokoll

Stand: 2026-10-29

Diese Datei hält Ergebnisse, gestellte Fragen mit ihren Antworten und den Fortschritt des Projekts fest.
`SPECS.md` bleibt unverändert; Abweichungen und Präzisierungen der Spezifikation werden hier dokumentiert.

**Legende:** E = Entscheidung · F = offene Frage · V = Vorschlag · B = bekannte Einschränkung

---

## Fortschritt

| Datum      | Schritt                                                        |
|------------|----------------------------------------------------------------|
| 2026-10-29 | `CLAUDE.md` und `SPECS.md` gesichtet, erste Fragenrunde gestellt |
| 2026-10-29 | Antworten der ersten Fragenrunde eingearbeitet, Protokoll angelegt |
| 2026-10-29 | Zweite Fragenrunde beantwortet, alle Vorschläge V1–V9 angenommen |
| 2026-10-29 | F10–F12 beantwortet; Mockup v1 erstellt (`mockups/mockup-v1.html`) |
| 2026-10-29 | Mockup v1 nicht abgenommen (Änderungswünsche E30–E34); Mockup v2 erstellt (`mockups/mockup-v2.html`) |
| 2026-10-29 | F13, F14, V10, V11 geklärt; Mockup v3 erstellt (`mockups/mockup-v3.html`) |
| 2026-10-29 | F15 geklärt; Eingabebegrenzung für „Ziffernkette Nr.“ in Mockup v3 ergänzt |
| 2026-10-29 | F16, F17 geklärt; **Mockup v3 abgenommen** (Gestaltung freigegeben) |
| 2026-10-29 | Entwicklungsumgebung geprüft: .NET SDK 9.0 und 10.0 installiert (kein 8.0-SDK; .NET-8-Runtime vorhanden), keine Avalonia-Vorlagen |
| 2026-10-29 | F18 entschieden: .NET 10, macOS ab 14 |
| 2026-10-29 | V12/V13 freigegeben; Projektgerüst (.NET 10, Avalonia 12.1.4, CommunityToolkit.Mvvm 8.4.2, xunit.v3 4.0.2) angelegt |
| 2026-10-29 | Core implementiert: fünf Verfahren, Pipeline, Konfliktprüfung, zweiphasiges Umbenennen mit Rücknahme, Profile |
| 2026-10-29 | Testdateien (`testdata/`) und 95 Tests (Unit + Integration mit echten Dateien) – **alle bestanden** |
| 2026-10-29 | Avalonia-Oberfläche nach Mockup v3 umgesetzt; Build ohne Warnungen; Headless-Rendering (hell/dunkel, Konfliktfall) geprüft; App-Start unter Windows geprüft |
| 2026-10-29 | `docs/ENTWICKLER.md` und `docs/ANWENDER.md` erstellt |
| 2026-10-29 | F19, F20, A1–A7 geklärt; neue Anforderungen E48–E51 (Profile, Widerrufen, Liste leeren, Mockup-Abgleich) |

| 2026-10-29 | E48–E51 umgesetzt: editierbares Profilfeld + Speichern-Dialog, „Letzte Umbenennung widerrufen“, „Liste leeren“ unter der Vorschau, Mockup-Abgleich (Beispielzeile in allen Editoren außer „Text ersetzen“ wie im Mockup, Hinweis zur Ziffernketten-Nr. immer sichtbar, Unterstreichung in Verfahrensfarbe, Schrift mit fester Zeichenbreite für neue Namen, Statuszeile „· n Konflikt(e) – Umbenennen gesperrt“). 98 Tests bestanden; Headless-Rendering geprüft |

| 2026-10-29 | E52–E54 umgesetzt: Checkbox je Datei und im Spaltenkopf; nur ausgewählte Dateien durchlaufen die Kette. 101 Tests bestanden; Headless-Rendering geprüft |
| 2026-10-29 | Drei Icon-Entwürfe erstellt (`mockups/icon-vorschlaege.png`): A Dokument + Umbenennen, B Kaskade, C Hervorhebung |

| 2026-10-29 | F21 entschieden: Icon C eingebunden (E55); Build ohne Fehler, 101 Tests bestanden, App-Start mit Icon unter Windows geprüft |

| 2026-10-29 | Manueller Test durch den Anwender ohne Beanstandungen; Abschlusslauf: Build ohne Warnungen, 101/101 Tests bestanden; `ABNAHME.md` erstellt |

| 2026-10-29 | Entwicklerdokumentation ergänzt: Projektbaum und Programmablaufpläne (Gesamtablauf, Vorschau-Pipeline, Umbenennen/Widerrufen) als Mermaid-Diagramme |

| 2026-10-29 | `README.md` erstellt (Überblick, Funktionen, Schnellstart, Projektaufbau, Verweise); Screenshots unter `docs/images/` |

Nächster Schritt: Abnahmeerklärung in `ABNAHME.md` (Abschnitt 7) durch den Anwender ausfüllen.

---

## Entscheidungen

| ID  | Thema | Entscheidung | Datum |
|-----|-------|--------------|-------|
| E1  | Plattform | macOS-Mindestversion wird auf **14** angehoben (statt 10.13 laut SPECS; zunächst 12, mit F18 auf 14 geändert). Grund: .NET 10 setzt macOS 14 voraus. | 2026-10-29 |
| E2  | Erstellungsdatum | Ist das Erstellungsdatum nicht verfügbar (z. B. Linux), wird auf das Änderungsdatum ausgewichen und der Anwender darauf hingewiesen. | 2026-10-29 |
| E3  | Datumsformat | Auswahl aus einer Liste vordefinierter Schreibweisen, inkl. Varianten mit Uhrzeit. Das Trennzeichen (Name ↔ Datum sowie Datum ↔ Uhrzeit) ist frei wählbar. | 2026-10-29 |
| E4  | Rückgängig | „Rückgängig pro Verfahren“ bezieht sich nur auf die Vorschau. | 2026-10-29 |
| E5  | Farbpriorität | Bei Überschneidungen wird die Hervorhebung des **gerade ausgewählten** Verfahrens priorisiert. | 2026-10-29 |
| E6  | Text ersetzen | Groß-/Kleinschreibung wird berücksichtigt; reguläre Ausdrücke sind erlaubt. | 2026-10-29 |
| E7  | Zähler | Trennzeichen (Default `-`), Startwert (Default 1) und Schrittweite (Default 1) sind eingebbar. Sortierkriterium wählbar: Name, Erstellungsdatum, Änderungsdatum, Größe (auf-/absteigend laut SPECS). | 2026-10-29 |
| E8  | Ziffernketten | Position zählt ab 1 von links. Überlauf erweitert die Zahl (995 + 10 = 1005). Negative Summanden sind erlaubt. | 2026-10-29 |
| E9  | Ziffernketten | „Anzahl der Stellen“ ist eine Mindestbreite, aufgefüllt mit führenden Nullen. Standard: Breite der ursprünglichen Ziffernkette. | 2026-10-29 |
| E10 | Text einfügen | Position zählt ab 0, Eingabe per Zahlenfeld mit Auf-/Ab-Pfeilen, begrenzt auf ersten/letzten gültigen Index. „Vom Ende zählen“ per Checkbox. | 2026-10-29 |
| E11 | Dateierweiterung | Als Erweiterung gilt nur die letzte Endung (`archiv.tar.gz` → `.gz`). „Erweiterung ignorieren“ gilt global für alle Verfahren. | 2026-10-29 |
| E12 | Manuelle Änderung | **Entfällt** – die manuelle Änderung direkt in der Dateiliste wird nicht umgesetzt (Abweichung von SPECS, zu komplex). | 2026-10-29 |
| E13 | Kette | Reihenfolge der Verfahren ist per Drag & Drop im Treeview änderbar. Dasselbe Verfahren darf mehrfach vorkommen und hat dieselbe Farbe. | 2026-10-29 |
| E14 | Treeview | Die Baumstruktur zeigt nur den Verfahrensnamen (keine Unterknoten). | 2026-10-29 |
| E15 | Profile | Gespeichert werden die Verfahren mit Parametern und ihrer Reihenfolge. Profile sind benennbar. Ablage als JSON (System.Text.Json) im Benutzer-Anwendungsdatenordner (`%APPDATA%/FileRenamer`, `~/.config/FileRenamer`, `~/Library/Application Support/FileRenamer`). | 2026-10-29 |
| E16 | Drag & Drop | Ordner werden angenommen, deren Unterordner nicht. | 2026-10-29 |
| E17 | Mockup | Eine HTML-Skizze reicht. | 2026-10-29 |
| E18 | Tests | xUnit ist als Testabhängigkeit erlaubt. | 2026-10-29 |
| E19 | Dokumentation | Summary-Kommentare vor jeder Klasse **und** jeder Methode. | 2026-10-29 |
| E20 | Datumsquelle | Datumsquelle im Verfahren wählbar (Erstellungs- oder Änderungsdatum); Fallback laut E2 nur bei gewähltem, aber fehlendem Erstellungsdatum. | 2026-10-29 |
| E21 | Hinweis Fallback | Betroffene Dateien werden in der Vorschau mit Warnsymbol und Tooltip markiert (kein Dialog). | 2026-10-29 |
| E22 | Datumsformate | Vordefinierte Liste: `yyyy-MM-dd`, `yyyyMMdd`, `yyyy_MM_dd`, `dd.MM.yyyy`, `dd-MM-yyyy`, `MM-dd-yyyy`; Uhrzeit optional: `HH-mm-ss`, `HHmmss`, `HH.mm.ss`, `HH-mm`. Kein `:` (unter Windows ungültig). | 2026-10-29 |
| E23 | Konflikte | Doppelte Zielnamen und Kollisionen mit existierenden Dateien werden in der Vorschau rot markiert; Umbenennen ist gesperrt, solange Konflikte bestehen. | 2026-10-29 |
| E24 | Ungültige Namen | Plattformübergreifende Validierung: verbotene Zeichen `< > : " / \ | ? *` und Steuerzeichen, reservierte Windows-Namen (`CON`, `NUL`, `COM1` …), Punkt/Leerzeichen am Ende, leerer Name, Länge > 255. Betroffene Zeilen markieren, Umbenennen sperren. | 2026-10-29 |
| E25 | Umbenennungsablauf | Zweiphasige Umbenennung über temporäre Namen (löst Ringtausch und reine Groß-/Kleinschreibungsänderungen). | 2026-10-29 |
| E26 | Fehler beim Umbenennen | Alles-oder-nichts: Bei einem Fehler werden bereits umbenannte Dateien zurückbenannt, danach Fehlerbericht. | 2026-10-29 |
| E27 | Technik | **.NET 10 (LTS, Support bis Nov. 2028)** + Avalonia, MVVM mit CommunityToolkit.Mvvm, Tests mit xUnit. (Ursprünglich .NET 8, wegen Supportende am 2026-11-10 geändert, siehe F18.) | 2026-10-29 |
| E28 | Ziffernketten | Wird ein Ergebnis negativ (z. B. `005` − 10), wird die Zeile als Konflikt markiert und Umbenennen gesperrt (wie E23). | 2026-10-29 |
| E29 | Datumsformat | Zusätzlich zur Liste gibt es ein freies Textfeld für das Format. Die Eingabe wird auf Umsetzbarkeit validiert (gültiges .NET-Format, keine in Dateinamen verbotenen Zeichen). | 2026-10-29 |
| E30 | Gestaltung | Das helle Theme verwendet kein reines Weiß (gedämpfte Grautöne für Fenster, Flächen und Eingabefelder). | 2026-10-29 |
| E31 | Gestaltung | Bei Feldern mit zugehöriger Checkbox steht erst das Eingabefeld, dahinter die Checkbox (Mindeststellen, Ziffernkette Nr., Position bei Text einfügen). | 2026-10-29 |
| E32 | Ziffernketten | ~~Zusätzliches Eingabefeld „Schrittweite“~~ – **zurückgenommen**: keine Schrittweite; eine Schrittweite gibt es nur bei „Zähler anhängen“ (E7). | 2026-10-29 |
| E33 | Ziffernketten | Standard für die Ziffernkette ist die letzte gefundene (Checkbox „Letzte gefundene“; abgewählt wird die Nummer von links verwendet). | 2026-10-29 |
| E34 | Gestaltung | Theme-Auswahl über eigene Icons (Monitor = System, Sonne = Hell, Mond = Dunkel) mit Tooltip, ohne Label „Theme“. | 2026-10-29 |
| E35 | Ziffernketten | Das Verfahren heißt **„Ziffernkette Wertebereich verschieben“** (statt „Ziffernkette hochzählen“). | 2026-10-29 |
| E36 | Ziffernketten | Dateinamen ohne Ziffernkette bleiben unverändert; die Zeile erhält in der Vorschau einen Hinweis. | 2026-10-29 |
| E37 | Bedienablauf | Verfahren links wählen → Optionen rechts einstellen → „Zur Kette hinzufügen“. Klick auf einen Ketteneintrag lädt dessen Optionen zum Bearbeiten; Änderungen wirken sofort auf die Vorschau. Entfernen per ✕. | 2026-10-29 |
| E38 | Menü/Toolbar | Menü „Datei“ (Dateien/Ordner hinzufügen, Liste leeren), „Profile“ (Laden, Speichern, Speichern unter, Verwalten), „Ansicht“ (Theme). Profil- und Theme-Auswahl zusätzlich in der Toolbar. | 2026-10-29 |
| E39 | Ziffernketten | Eine Ziffernkette-Nr., die nicht in allen Dateien mit Ziffernketten vorkommt, wird schon bei der Eingabe abgefangen: Das Feld ist auf 1 … kleinste Anzahl Ziffernketten (unter den Dateien mit Ziffernketten) begrenzt, ungültige Eingaben werden mit Meldung markiert. | 2026-10-29 |
| E40 | Ziffernketten | Wird die Nr. nachträglich ungültig (Dateien hinzugefügt, Kette umsortiert, vorheriges Verfahren fügt Ziffern hinzu), wird der Ketteneintrag rot mit Tooltip markiert, betroffene Zeilen erhalten einen Konflikt und Umbenennen ist gesperrt, bis die Nr. angepasst ist. Der Wert wird nicht automatisch geändert. | 2026-10-29 |
| E41 | Ziffernketten | Die Begrenzung der Nr. wird auf die Namen an dieser Stelle der Kette berechnet (nach den vorherigen Verfahren), nicht auf die Originalnamen. | 2026-10-29 |
| E42 | Gestaltung | **Mockup v3** (`mockups/mockup-v3.html`) ist abgenommen und gilt als verbindliche Vorlage für die Oberfläche. | 2026-10-29 |
| E43 | Projektstruktur | `src/FileRenamer.Core` (Logik ohne UI-Abhängigkeit), `src/FileRenamer.App` (Avalonia-Oberfläche), `tests/FileRenamer.Core.Tests` (xUnit), `testdata/` (Testdateien je Verfahren), `docs/ENTWICKLER.md`, `docs/ANWENDER.md`, `ABNAHME.md`. Pakete: Avalonia 12.1.4, CommunityToolkit.Mvvm 8.4.2, xunit.v3 4.0.2. | 2026-10-29 |
| E52 | Dateiauswahl | Jede Zeile der Dateiliste erhält eine Checkbox; Änderungen gelten nur für ausgewählte Dateien. Eine Checkbox im Spaltenkopf wählt alle bzw. keine aus (teilweise Auswahl = unbestimmt). Neue Dateien sind ausgewählt. | 2026-10-29 |
| E53 | Dateiauswahl | Abgewählte Dateien durchlaufen die Kette nicht: kein Zählerwert (Ausgewählte werden lückenlos nummeriert), keine Berücksichtigung bei der erlaubten Ziffernketten-Nr. | 2026-10-29 |
| E54 | Dateiauswahl | Abgewählte Dateien bleiben ausgegraut mit unverändertem Namen sichtbar; ihr Name zählt für die Konfliktprüfung weiter als belegt. | 2026-10-29 |
| E55 | App-Icon | Entwurf C („Aa“ mit Unterstreichung in den fünf Verfahrensfarben, dunkler Grund). Eingebunden als `Assets/app.ico` (16–256 px, Programmsymbol der .exe) und `Assets/app.png` (512 px, Fenstersymbol). | 2026-10-29 |
| E44 | Architektur | Gemeinsame Basis für alle Verfahren, Pipeline wendet die Kette an. Namen werden als Segmente mit Herkunft geführt (Grundlage der Hervorhebung). Dateisystemzugriffe über eigene Schnittstelle (testbar ohne echte Dateien). | 2026-10-29 |
| E45 | Nach dem Umbenennen | Die Kette bleibt erhalten, die Dateiliste zeigt die neuen Namen (F19). | 2026-10-29 |
| E46 | Änderungen verwerfen | „Änderungen verwerfen“ leert die Kette, die Dateiliste bleibt (F20). | 2026-10-29 |
| E47 | Annahmen | A1 (Zähler sortiert nach ursprünglichem Namen, natürlich), A2 (Kette als Liste unter „▾ Kette“), A4 (Linux: Änderungsdatum), A5 (Standardwerte), A6 (Theme/Profil merken), A7 (Ketteneintrag rot bei Konflikt) bestätigt. | 2026-10-29 |
| E48 | Profile | Statt A3: Die Profilauswahl ist editierbar (Name eintippen). „Speichern“ öffnet immer den Dialog „Profil speichern“ (vorbelegt mit dem eingegebenen bzw. aktuellen Namen, Rückfrage beim Überschreiben) – so lassen sich beliebig viele Profile anlegen. Geladen wird ein Profil nur durch Auswahl aus der Liste. | 2026-10-29 |
| E49 | Widerrufen | Neue Schaltfläche „Letzte Umbenennung widerrufen“: macht die zuletzt ausgeführte Umbenennung rückgängig (alles oder nichts, mit Sicherheitsabfrage). | 2026-10-29 |
| E50 | Liste leeren | Zusätzliche Schaltfläche „Liste leeren“ unter der Vorschauliste (wie Menü „Datei → Liste leeren“). | 2026-10-29 |
| E51 | Mockup-Abgleich | Die Oberfläche wird erneut mit Mockup v3 abgeglichen und angeglichen. | 2026-10-29 |

---

## Offene Fragen

| ID  | Thema | Frage | Status |
|-----|-------|-------|--------|
_(keine)_

---

---

## Bekannte Einschränkungen

| ID | Einschränkung |
|----|---------------|
| B1 | macOS 10.13–13 wird nicht unterstützt (siehe E1). |
| B2 | Auf vielen Linux-Dateisystemen ist das Erstellungsdatum nicht verfügbar (siehe E2). |
| B3 | Keine manuelle Änderung in der Dateiliste (siehe E12). |
| B4 | Getestet nur unter Windows (Tests, Build, App-Start). Linux und macOS sind nicht geprüft. |
| B5 | Interaktive Bedienung (Drag &amp; Drop, Dialoge, Dateiauswahl) wurde nicht per Klick getestet, nur gebaut und per Headless-Rendering angesehen. |
| B6 | Kein macOS-App-Bundle (`.app`, `LSMinimumSystemVersion` 14.0) und kein Installer im Projekt. |
| B7 | Abweichungen vom Mockup v3: gestrichelte Rahmen (Ablagehinweis, Beispiel) sind in Avalonia nicht verfügbar und daher durchgezogen; „Profile verwalten…“ entfällt (E48); zusätzliche Schaltflächen „Liste leeren“ und „Letzte Umbenennung widerrufen“ (E49, E50). |
| B8 | Widerrufen gilt nur für die letzte Umbenennung und nur bis zum Programmende. |

---

## Erledigt

| ID | Frage / Vorschlag | Ergebnis | Datum |
|----|-------------------|----------|-------|
| F1 | Negative Werte bei Ziffernketten erlaubt? | Ja → E8; Folgefrage F10 | 2026-10-29 |
| F2 | Bedeutung „Anzahl der Stellen“ | → E9 | 2026-10-29 |
| F3 | Erweiterung ignorieren global oder pro Verfahren? | Global → E11 | 2026-10-29 |
| F4 | Reihenfolge der Verfahren änderbar? | Per Drag & Drop → E13 | 2026-10-29 |
| F5 | Positionseingabe beim Text einfügen | → E10 | 2026-10-29 |
| F6 | Verhalten der manuellen Änderung | Funktion entfällt → E12 | 2026-10-29 |
| F7 | Freie Formateingabe / Trennzeichen Datum ↔ Uhrzeit | Trennzeichen wählbar → E3; Formateingabe → F11 | 2026-10-29 |
| F8 | Darstellung des Fallback-Hinweises | → E21 | 2026-10-29 |
| F9 | Speicherort/Format der Profile | → E15 | 2026-10-29 |
| V1–V9 | Alle Vorschläge | Angenommen → E15, E20–E27; CommunityToolkit.Mvvm offen als F12 | 2026-10-29 |
| F10 | Negatives Ergebnis bei Ziffernketten | Konflikt markieren, Umbenennen sperren → E28 | 2026-10-29 |
| F11 | Freie Formateingabe | Zusätzliches Textfeld mit Validierung → E29 | 2026-10-29 |
| F12 | MVVM-Toolkit | CommunityToolkit.Mvvm verwenden → E27 | 2026-10-29 |
| F13 | Namen ohne Ziffernkette | Unverändert mit Hinweis → E36; Folgefrage F15 | 2026-10-29 |
| F14 | Bedeutung „Schrittweite“ | Schrittweite entfällt beim Verfahren, Umbenennung → E32, E35 | 2026-10-29 |
| V10 | Bedienablauf Kette | Angenommen → E37 | 2026-10-29 |
| V11 | Menü und Toolbar | Beides angenommen → E38 | 2026-10-29 |
| F15 | Ziffernkette-Nr. nicht vorhanden | Schon bei der Eingabe abfangen → E39; Folgefragen F16, F17 | 2026-10-29 |
| F16 | Nachträglich ungültige Nr. | Vorschlag angenommen → E40 | 2026-10-29 |
| F17 | Bezugsbasis der Begrenzung | Namen an der Kettenposition → E41 | 2026-10-29 |
| F18 | .NET 8 Supportende 2026-11-10 | .NET 10, macOS 14 → E1, E27, B1 | 2026-10-29 |
| F21 | App-Icon | Entwurf C → E55 | 2026-10-29 |
| F19 | Verhalten nach dem Umbenennen | Kette bleibt → E45 | 2026-10-29 |
| F20 | Änderungen verwerfen | Leert die Kette; zusätzlich Button „Liste leeren“ → E46, E50 | 2026-10-29 |
| A1–A7 | Annahmen | A3 ersetzt durch E48, übrige bestätigt → E47 | 2026-10-29 |
| V12 | Projektstruktur | Freigegeben → E43 | 2026-10-29 |
| V13 | Architektur | Freigegeben → E44 | 2026-10-29 |
