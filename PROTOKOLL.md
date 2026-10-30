# FileRenamer – Protokoll

Stand: 2026-10-30

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
| 2026-10-30 | Neue Anforderungen: Verfahren Groß/Kleinschreibung, EXIF-Datum als Datumsquelle, EXIF-Vorlage, ID3-Vorlage; offene Fragen F22–F31 und Vorschläge V14–V20 gestellt |
| 2026-10-30 | F22–F31 beantwortet, V14–V20 angenommen (E56–E68); Restfragen F32–F35; Mockup v4 erstellt (`mockups/mockup-v4.html`) |

| 2026-10-30 | F32–F37 beantwortet (E69–E74); Mockup v4 nicht freigegeben (Änderungswünsche E75, E76); Mockup v5 erstellt (`mockups/mockup-v5.html`); neue Frage F38 |

| 2026-10-30 | F38 beantwortet (E77); Mockup v5 nicht freigegeben (Änderungswünsche E78, E79); Mockup v6 erstellt (`mockups/mockup-v6.html`); neue Fragen F39, F40 |

| 2026-10-30 | F39, F40 beantwortet (E80, E81); Mockup v6 nicht freigegeben (Änderungswunsch E82); Mockup v7 erstellt (`mockups/mockup-v7.html`); neue Frage F41 |

| 2026-10-30 | Mockup v7 nicht freigegeben (Änderungswünsche E83, E84); Mockup v8 erstellt (`mockups/mockup-v8.html`); neue Fragen F42, F43 |

| 2026-10-30 | F41–F43 beantwortet (E85–E87); Mockup v8 nicht freigegeben (Änderungen E86, E88); Mockup v9 erstellt (`mockups/mockup-v9.html`); neue Frage F44 |

| 2026-10-30 | Sättigung auf HSV-Prinzip korrigiert (E86); Mockup v10 erstellt (`mockups/mockup-v10.html`) |

| 2026-10-30 | F44 beantwortet (E89); **Mockup v10 freigegeben** (E90); Umsetzung begonnen |

| 2026-10-30 | Erweiterung umgesetzt: eigene Metadaten-Leser (JPEG, TIFF, HEIC, PNG, 9 RAW-Formate, MP3/ID3, FLAC, M4A), 6 neue Verfahren, EXIF-Datumsquelle, gruppierte Verfahrensliste, Akzentfarbe mit HSV-Sättigung und benutzerdefinierten Farben; Build ohne Warnungen, **181/181 Tests bestanden** (u. a. 28 Metadaten-Tests mit erzeugten Mediendateien, Integrationstest mit echten Dateien); Headless-Rendering hell/dunkel/Akzentfarbe geprüft; App-Start geprüft; Dokumentation, README und Abnahmeprotokoll aktualisiert |

| 2026-10-30 | Neue Anforderungen „Profil zurücksetzen“ und „Englisch“ geklärt (E91–E93), Annahmen A8–A10 festgehalten |
| 2026-10-30 | E91–E93 umgesetzt: Meldungen der Fachlogik zweisprachig (`CoreText`), Oberflächentexte zweisprachig (`Loc`, `Strings`, XAML-Erweiterung `{loc:T …}`), Menü „Ansicht → Sprache“ mit Fensterneuaufbau ohne Neustart, Sprachwahl gespeichert; Schaltfläche „Zurücksetzen“ und Menü „Profile → Profil zurücksetzen“. Build ohne Warnungen, **188/188 Tests bestanden**; Headless-Ablauf Deutsch → Englisch → Zurücksetzen → Deutsch geprüft (dabei gefunden und behoben: nach dem Wechsel blieb das Editor-Beispiel deutsch, weil die alte Verfahrensliste die Auswahl zurücksetzte); App-Start geprüft |
| 2026-10-30 | Rückmeldung umgesetzt (E94–E98): Drag &amp; Drop mit Live-Verschiebung und Hervorhebung, gerundete Segment-Außenkanten, mittig ausgerichtete Schreibweise-Auswahl, übersetzte Beispielnamen; dabei gefunden und behoben: neu gewählte Editoren zeigten ihr Beispiel erst nach einer Änderung (E97), und die rote Fehlerumrandung von Ketteneinträgen griff wegen eines lokal gesetzten Rahmens nicht. Build ohne Warnungen, **188/188 Tests bestanden**; Headless-Prüfung mit simuliertem Ziehen per Maus (Reihenfolge und Vorschau ändern sich während des Ziehens, Auswahl bleibt erhalten) |
| 2026-10-30 | A12, A13 bestätigt; zweite Rückmeldung umgesetzt (E99–E102): Karteireiter bei „Name aus Vorlage“, gemeinsame Grundlinie in der Schreibweise-Auswahl (gemessen: identisch), vollständiger Segment-Rahmen bei heller Akzentfarbe, Drag &amp; Drop mit Abbild am Mauszeiger und Einfügelinie (neue Klasse `ChainDragController`). Build ohne Warnungen, **188/188 Tests bestanden**; Headless-Prüfung mit simuliertem Ziehen nach unten und oben |
| 2026-10-30 | Manueller Test der Rückmeldungen (E99–E102) durch den Anwender: „Funktioniert sehr gut“ |
| 2026-10-30 | F45–F48 geklärt, E103 umgesetzt: Reiter 28 px hoch wie die Segment-Schaltflächen (Fluent-Mindesthöhe 48 px aufgehoben), aktiver Reiter mit Auswahlfarbe bzw. bei Fokus im Editor in voller Akzentfarbe. Build ohne Warnungen, **188/188 Tests bestanden**; headless beide Zustände geprüft, Höhen gemessen |
| 2026-10-30 | E104, E105 umgesetzt: Rahmen des Sättigungsfelds vollständig, Platzhalter über die volle Breite. Build ohne Warnungen, **188/188 Tests bestanden**; headless geprüft (Rahmenstärke und Höhe gemessen, Screenshots) |
| 2026-10-30 | F49–F52 geklärt, E106 umgesetzt: eigene Vorlagen je Tab speichern, auswählen, per ✕ entfernen; programmweit gespeichert (`AppSettings.CustomPhotoTemplates`/`CustomAudioTemplates`), Listenregeln in `CustomTemplateList` (Core). Dabei E107 behoben. Build ohne Warnungen, **198/198 Tests bestanden** (10 neue); headless geprüft: Speichern-Schaltfläche nur bei neuer Vorlage aktiv, Liste mit Trennlinie, Liste bleibt über Editorwechsel erhalten, Auswahl übernimmt und setzt zurück, Mausklick auf ✕ entfernt ohne auszuwählen, Einstellungsdatei aktualisiert |
| 2026-10-30 | A14 bestätigt; E108 umgesetzt (Speichern-Schaltflächen in Akzentfarbe), Build und Headless-Lauf ohne Fehler |
| 2026-10-30 | **Erweiterung vom Anwender abgenommen**; Abnahmeerklärung in `ABNAHME.md` (N4) eingetragen |
| 2026-10-30 | Sprachwechsel vom Anwender in der laufenden App geprüft – funktioniert; Einschränkung B12 entfällt |

Nächster Schritt: keiner – Grundversion (2026-10-29) und Erweiterung (2026-10-30) sind abgenommen, siehe `ABNAHME.md`.

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
| E56 | Metadaten-Leser | Eigene Leser ohne neue Abhängigkeit für alle Formate aus E57 (F22). | 2026-10-30 |
| E57 | Formate | Bilder: JPEG, TIFF, HEIC, PNG, RAW (Umfang F32). Audio: MP3 (ID3v1, ID3v2.2–2.4), FLAC (Vorbis Comments), M4A (iTunes-Atome) – gemeinsame Platzhalter (F22, F23). | 2026-10-30 |
| E58 | Groß/Klein | Neues Verfahren „Groß-/Kleinschreibung“: kleinschreibung, GROSSSCHREIBUNG, Erster Buchstabe groß, Jedes Wort groß, PascalCase, camelCase, snake_case, kebab-case (F24). | 2026-10-30 |
| E59 | Groß/Klein | Wortgrenzen: Leerzeichen, `_`, `-`, `.` und Wechsel klein→GROSS; Ziffern bleiben am Wort; ß bleibt ß (F25). | 2026-10-30 |
| E60 | EXIF-Datum | Datumsquelle „EXIF-Aufnahmedatum“: DateTimeOriginal → DateTimeDigitized → DateTime; ohne EXIF-Datum Änderungsdatum mit Hinweis wie E2 (F26). | 2026-10-30 |
| E61 | Vorlage | Die Vorlage ersetzt den Namen; `%name%` steht für den bisherigen Namen (F27). | 2026-10-30 |
| E62 | Platzhalter | EXIF zusätzlich: Orientierung (0/90/180/270°), Brennweite, GPS-Breite/-Länge, Höhe, Bildbeschreibung, Ersteller, Copyright. ID3-Liste wie vorgeschlagen. Format je Platzhalter `%name:format%`, z. B. `%date:yyyy-MM-dd%`; `%track%` standardmäßig zweistellig, anpassbar (`%track:000%`) (F28). | 2026-10-30 |
| E63 | Platzhalter | Fehlende Werte werden durch einen Ersatztext ersetzt (Standard „Unbekannt“, änderbar) (F29). | 2026-10-30 |
| E64 | Platzhalter | Verbotene Zeichen in Werten werden durch ein frei wählbares Zeichen ersetzt, Standard `_` (F30). | 2026-10-30 |
| E65 | Platzhalter | Unbekannte Platzhalter sind Fehler: Verfahren rot, Umbenennen gesperrt (F31). | 2026-10-30 |
| E66 | Bedienung/Technik | Platzhalter als Schaltflächen, die an der Cursorposition einfügen (V14); Metadaten werden je Datei einmal gelesen und zwischengespeichert (V15). | 2026-10-30 |
| E67 | Neue Verfahren | „Zeichen bereinigen“ (V16), „Bereich entfernen / kürzen“ (V17), „Erweiterung ändern“ (V18), „Ordnername einfügen“ (V19). | 2026-10-30 |
| E68 | Vorlage | EXIF und ID3 werden in **einem** Verfahren „Name aus Vorlage“ mit allen Platzhaltern (Datei, Ordner, EXIF, Audio) zusammengefasst (V20). | 2026-10-30 |
| E69 | RAW-Formate | DNG, CR2, CR3, NEF, ARW, ORF, RW2, RAF, PEF (F32). | 2026-10-30 |
| E70 | Beschreibungsfelder | Bildbeschreibung, Ersteller, Copyright: zuerst EXIF, dann IPTC, dann XMP; der erste vorhandene Wert zählt (F33). | 2026-10-30 |
| E71 | GPS | Dezimalgrad mit 5 Nachkommastellen und Himmelsrichtung (`48.13743N`, `11.57549E`), Höhe in ganzen Metern (`519m`) (F34). | 2026-10-30 |
| E72 | Verfahrensliste | Gruppierung in „Text“, „Datum &amp; Zahlen“, „Metadaten &amp; Ordner“, „Erweiterung“ (F35). | 2026-10-30 |
| E73 | Erweiterung ändern | Wirkt auch bei aktivem „Dateierweiterung ignorieren“; die geänderte Endung wird dann in der Vorschau eingeblendet (F36). | 2026-10-30 |
| E74 | Vorlage | `%date%`/`%time%` ohne EXIF weichen auf das Änderungsdatum aus (mit Hinweis, wie E60) (F37). | 2026-10-30 |
| E75 | Groß/Klein | Die Beispiele stehen direkt in der ComboBox (breit genug); die Tabelle aller Schreibweisen entfällt. | 2026-10-30 |
| E76 | Vorlage | „Name aus Vorlage“ wird in die Tabs „Fotos“ und „Audio“ aufgeteilt; im Audio-Tab ist die Stellenanzahl der Tracknummer einstellbar (Standard 2). | 2026-10-30 |
| E77 | Vorlage | Je Tab eine eigene Vorlage: Foto-Vorlage für Bilddateien, Audio-Vorlage für Audiodateien; leere Vorlage = unverändert; andere Dateitypen unverändert mit Hinweis (F38). | 2026-10-30 |
| E78 | Vorlage | Platzhalter umbrechen eingerückt hinter der Gruppenbezeichnung (nicht am Zeilenanfang). | 2026-10-30 |
| E79 | Akzentfarbe | Wählbare Akzentfarbe für Steuerelemente (Checkboxen, Schaltflächen, Auswahl): farbiger Kreis neben der Theme-Auswahl öffnet eine Auswahl mit 5 Pastell- und 5 kräftigen Farben sowie ein Feld für einen Hex-Code (`#25A0C3`); zusätzlich im Menü „Ansicht“; wird wie das Theme gespeichert. | 2026-10-30 |
| E80 | Akzentfarbe | Schrift auf akzentfarbigen Flächen automatisch hell oder dunkel je nach Helligkeit der Akzentfarbe (F39). | 2026-10-30 |
| E81 | Akzentfarbe | „Standard“ setzt auf das bisherige Blau `#2F6FDF` zurück (F40). | 2026-10-30 |
| E82 | Akzentfarbe | Pastellfarben 20 % kräftiger (Sättigung ×1,2, Helligkeit um 20 % des Abstands zu Weiß gesenkt): `#8EBCEA`, `#A0ECD1`, `#FFD3B5`, `#E1A2C6`, `#B8C2EA`. Zusätzliche Reihe „Neutral“: 95 % Schwarz `#0D0D0D`, 95 % Weiß `#F2F2F2`. | 2026-10-30 |
| E83 | Akzentfarbe | Die Sättigung der Pastellfarben ist in Prozent einstellbar und wird gespeichert (Bedeutung siehe F42). | 2026-10-30 |
| E84 | Akzentfarbe | Selbst erstellte Farben erscheinen in der Liste unter „Benutzerdefiniert“ und werden gespeichert (Details siehe F43). | 2026-10-30 |
| E85 | Akzentfarbe | Bei zu geringem Kontrast zum Hintergrund (Verhältnis &lt; 1,6) erhalten akzentfarbige Flächen automatisch einen Rahmen in Schriftfarbe (F41). | 2026-10-30 |
| E86 | Pastell-Sättigung | Der Prozentwert ist die **absolute Sättigung nach dem HSV-Prinzip** (0 % = 0, 100 % = 255) aller Pastellfarben; Farbton (H) und Hellwert (V) bleiben. Einstellbar von 10 % bis 90 % (≙ 26–230 von 255) (F42; am 2026-10-30 von HSL auf HSV korrigiert). | 2026-10-30 |
| E87 | Benutzerdefiniert | Per Hex-Code übernommene Farben werden automatisch aufgenommen, ohne Duplikate, höchstens 10 (älteste fällt weg), Entfernen über ✕ (F43). | 2026-10-30 |
| E88 | Bedienung | Der Fokusring um den Sättigungsregler erscheint nur bei Bedienung per Tastatur (Tab), nicht bei Mausbedienung. | 2026-10-30 |
| E89 | Pastell-Sättigung | Standardwert 30 % (≙ 77/255) (F44). | 2026-10-30 |
| E90 | Gestaltung | **Mockup v10** (`mockups/mockup-v10.html`) ist freigegeben und gilt als Vorlage für die Erweiterung. | 2026-10-30 |
| E91 | Profil zurücksetzen | Schaltfläche „Zurücksetzen“ neben „Speichern“ und Menü „Profile → Profil zurücksetzen“: entfernt alle Verfahren aus der Kette, das Profilfeld bleibt leer, gespeicherte Profile bleiben auswählbar. Enthält die Kette Verfahren, erscheint vorher eine Sicherheitsabfrage. | 2026-10-30 |
| E92 | Sprache | Oberfläche auf Deutsch und Englisch. Auswahl unter „Ansicht → Sprache“: System, Deutsch, English; wirkt sofort ohne Neustart und wird gespeichert. | 2026-10-30 |
| E93 | Sprache | „System“: Deutsch bei deutschem Betriebssystem, sonst Englisch. | 2026-10-30 |
| E94 | Drag &amp; Drop | Beim Ziehen eines Ketteneintrags wandert dieser sofort an die Position unter dem Mauszeiger und ist hervorgehoben (Auswahlfarbe, Rahmen in Akzentfarbe); die Vorschau zeigt die neue Reihenfolge schon während des Ziehens. Die Auswahl des Eintrags bleibt beim Verschieben erhalten. | 2026-10-30 |
| E95 | Segment-Schaltflächen | Die äußeren Segmente einer Auswahlgruppe erhalten außen dieselbe Rundung wie Buttons und Textfelder (4 px). | 2026-10-30 |
| E96 | Schreibweise-Auswahl | Bezeichnung und Beispiel jeder Zeile der ComboBox „Schreibweise“ werden vertikal mittig ausgerichtet. | 2026-10-30 |
| E97 | Editor-Beispiel | Ein neu gewählter Editor prüft seine Standardwerte sofort und zeigt das Beispiel ohne vorherige Änderung (Fehler gefunden bei der Prüfung von A11: u. a. „Ordnername einfügen“, „Erweiterung ändern“ und „Name aus Vorlage“ zeigten zunächst kein Beispiel). | 2026-10-30 |
| E99 | Vorlage | Die Tabs „Fotos“/„Audio“ werden wie im Mockup v10 als Karteireiter gestaltet: Leiste mit Grundlinie, gewählter Reiter mit Rahmen oben und seitlich, Fensterhintergrund und fetter Schrift; nicht gewählte Reiter gedämpft. | 2026-10-30 |
| E100 | Schreibweise-Auswahl | Statt mittiger Ausrichtung (E96) sitzen Bezeichnung und Beispiel auf derselben Grundlinie (Ursache: unterschiedliche Schriften mit verschiedenen Ober-/Unterlängen). | 2026-10-30 |
| E101 | Segment-Schaltflächen | Das gewählte Segment wird über seinen Nachbarn gezeichnet, damit der Rahmen bei heller Akzentfarbe (E85) auf allen vier Seiten sichtbar ist. | 2026-10-30 |
| E102 | Drag &amp; Drop | Ersetzt E94, wie im Mockup v10: Ein Abbild des Eintrags klebt am Mauszeiger, der Eintrag bleibt blass an seinem Platz, eine Linie in Akzentfarbe zeigt die Einfügestelle (über dem Ziel beim Ziehen nach oben, darunter beim Ziehen nach unten). Verschoben wird erst beim Loslassen; die Liste springt beim Ziehen nicht mehr. | 2026-10-30 |
| E103 | Vorlage | Ergänzt E99: Reiter so hoch wie die Segment-Schaltflächen. Der aktive Reiter ist farbig hinterlegt: Liegt der Fokus irgendwo im Editor „Name aus Vorlage“, in voller Akzentfarbe mit Akzent-Schriftfarbe (Rahmen wie beim gewählten Segment); sonst in der Auswahlfarbe der Verfahrensliste. Verfahrensliste und Kette bleiben unverändert (F45–F48). | 2026-10-30 |
| E104 | Akzentfarbe | Das Prozentfeld der Pastell-Sättigung erhält denselben Rahmen und dieselbe Höhe (32 px) wie die übrigen Textfelder. Ursache des Fehlers: Das Feld war kleiner als die Fluent-Mindestgröße seines inneren Textfelds, Rahmen rechts, oben und unten wurden abgeschnitten. | 2026-10-30 |
| E105 | Vorlage | Der Editor „Name aus Vorlage“ ist nicht mehr auf 760 px begrenzt; die Platzhalter-Schaltflächen nutzen die volle Breite und brechen erst am rechten Rand um. | 2026-10-30 |
| E106 | Eigene Vorlagen | Bei „Name aus Vorlage“ steht in jedem Tab neben der Vorlagenauswahl eine Schaltfläche „Speichern“: Sie nimmt die eingegebene Vorlage ohne Rückfrage in die Liste dieses Tabs auf; leere oder bereits vorhandene Vorlagen werden nicht gespeichert. Eigene Vorlagen gelten programmweit (Einstellungen, getrennt nach Fotos und Audio), erscheinen oben in der Liste (neueste zuerst), durch eine Linie von den Beispielvorlagen getrennt, und lassen sich per ✕ entfernen. Höchstens 10 je Tab; beim Speichern einer elften entfällt die älteste (F49–F52). | 2026-10-30 |
| E107 | Vorlagenauswahl | Nach der Wahl einer Vorlage zeigt die Auswahl wieder „Vorlage wählen…“, damit dieselbe Vorlage erneut gewählt werden kann (Fehler gefunden bei E106: die Auswahl blieb stehen, mit eigenen Vorlagen sogar samt ✕ im zugeklappten Feld). | 2026-10-30 |
| E108 | Eigene Vorlagen | Die Schaltflächen „Speichern“ bei „Name aus Vorlage“ verwenden die Akzentfarbe wie „Zur Kette hinzufügen“. | 2026-10-30 |
| E98 | Sprache | A8–A10 bestätigt. Statt A11: Beispielnamen werden ebenfalls übersetzt, auch in der ComboBox „Schreibweise“ („mein Urlaub-Foto_2026“ → „my holiday-photo_2026“, „Urlaub 2026“ → „Holiday 2026“, „Reisen“ → „Travel“, „Musik“ → „Music“, „Foto“ → „Photo“, „Max Muster“ → „John Doe“). | 2026-10-30 |
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

## Annahmen (bitte bestätigen)

| ID | Thema | Annahme |
|----|-------|---------|
_(keine)_

---

## Bekannte Einschränkungen

| ID | Einschränkung |
|----|---------------|
| B1 | macOS 10.13–13 wird nicht unterstützt (siehe E1). |
| B2 | Auf vielen Linux-Dateisystemen ist das Erstellungsdatum nicht verfügbar (siehe E2). |
| B3 | Keine manuelle Änderung in der Dateiliste (siehe E12). |
| B4 | Getestet nur unter Windows (Tests, Build, App-Start). Linux und macOS sind nicht geprüft. |
| B5 | Interaktive Bedienung (Dialoge, Dateiauswahl) wurde nicht per Klick getestet, nur gebaut und per Headless-Rendering angesehen. Das Umsortieren der Kette ist headless und vom Anwender in der laufenden App geprüft (E102). |
| B6 | Kein macOS-App-Bundle (`.app`, `LSMinimumSystemVersion` 14.0) und kein Installer im Projekt. |
| B7 | Abweichungen vom Mockup v3: gestrichelte Rahmen (Ablagehinweis, Beispiel) sind in Avalonia nicht verfügbar und daher durchgezogen; „Profile verwalten…“ entfällt (E48); zusätzliche Schaltflächen „Liste leeren“ und „Letzte Umbenennung widerrufen“ (E49, E50). |
| B8 | Widerrufen gilt nur für die letzte Umbenennung und nur bis zum Programmende. |
| B9 | Die Metadaten-Leser sind mit von den Tests erzeugten, strukturell korrekten Dateien geprüft, nicht mit Originaldateien realer Kameras/Player. Herstellerspezifische Abweichungen (z. B. Makernotes, ungewöhnliche HEIC-Strukturen mit mehreren Extents, komprimierte ID3-Frames) werden nicht ausgewertet. |
| B10 | Bei sehr hellen Akzentfarben (z. B. 95 % Weiß) im hellen Theme bleiben Checkboxen und der Reglerknopf trotz Rahmenlösung (E85) blass. |
| B11 | Beim Sprachwechsel wird das Hauptfenster neu aufgebaut: Größe, Lage, Kette, Dateien, Auswahl und Editor-Einstellungen bleiben erhalten; die Position des Trennbalkens und Bildlaufpositionen werden zurückgesetzt. |

---

## Erledigt

| ID | Frage / Vorschlag | Ergebnis | Datum |
|----|-------------------|----------|-------|
| B12 | Sprachwechsel in der laufenden App per Menüklick ungeprüft | vom Anwender geprüft, funktioniert | 2026-10-30 |
| A14 | „Speichern“ nur bei gültiger Vorlage; Liste heißt „Vorlage wählen…“ | bestätigt | 2026-10-30 |
| F49–F52 | Eigene Vorlagen: Speichern, Ort, Entfernen, Anzeige | Schaltfläche „Speichern“, programmweit, ✕ mit max. 10, oben getrennt → E106 | 2026-10-30 |
| F45 | Farbe des aktiven Reiters | mit Fokus volle Akzentfarbe, ohne Fokus Auswahlfarbe → E103 | 2026-10-30 |
| F46 | Bedeutung „Fokus“ | Fokus irgendwo im Editor „Name aus Vorlage“ → E103 | 2026-10-30 |
| F47 | Fokusverhalten auch für Listen? | nein, nur Reiter → E103 | 2026-10-30 |
| F48 | Höhe der Reiter | wie Segment-Schaltflächen → E103 | 2026-10-30 |
| A12 | Eigennamen und „Café Müller“ bleiben unübersetzt | bestätigt | 2026-10-30 |
| A13 | ComboBox „Style“ = Auswahl „Schreibweise“ | bestätigt | 2026-10-30 |
| A8–A10 | Umfang der Übersetzung, Ersatztext, Zurücksetzen | bestätigt → E98 | 2026-10-30 |
| A11 | Beispielnamen bleiben unübersetzt | abgelehnt, Beispielnamen werden übersetzt → E98 | 2026-10-30 |
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
| F22–F31 | Neue Verfahren, Metadaten, Vorlagen | beantwortet → E56–E65 | 2026-10-30 |
| V14–V20 | Vorschläge | angenommen → E66–E68 | 2026-10-30 |
| F32–F37 | RAW, Beschreibungsfelder, GPS, Gruppierung, Erweiterung, Datum in Vorlage | beantwortet → E69–E74 | 2026-10-30 |
| F38 | Vorlage je Tab | angenommen → E77 | 2026-10-30 |
| F39, F40 | Schriftfarbe auf Akzent, „Standard“ | angenommen → E80, E81 | 2026-10-30 |
| F41–F43 | Rahmen bei geringem Kontrast, Sättigung, Benutzerdefiniert | beantwortet → E85–E87 | 2026-10-30 |
| F44 | Standard-Sättigung | 30 % → E89 | 2026-10-30 |
| F19 | Verhalten nach dem Umbenennen | Kette bleibt → E45 | 2026-10-29 |
| F20 | Änderungen verwerfen | Leert die Kette; zusätzlich Button „Liste leeren“ → E46, E50 | 2026-10-29 |
| A1–A7 | Annahmen | A3 ersetzt durch E48, übrige bestätigt → E47 | 2026-10-29 |
| V12 | Projektstruktur | Freigegeben → E43 | 2026-10-29 |
| V13 | Architektur | Freigegeben → E44 | 2026-10-29 |
