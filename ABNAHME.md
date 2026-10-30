# FileRenamer – Abnahmeprotokoll

| | |
|---|---|
| **Projekt** | FileRenamer – kaskadierendes Umbenennen von Dateien (Single, Batch) |
| **Datum** | 2026-10-29 |
| **Grundlage** | `SPECS.md`, Entscheidungen E1–E55 in `PROTOKOLL.md`, Mockup v3 (`mockups/mockup-v3.html`) |
| **Technik** | .NET 10 (LTS), Avalonia 12.1.4, CommunityToolkit.Mvvm 8.4.2, xUnit v3 4.0.2 |
| **Prüfumgebung** | Windows Server 2022, .NET SDK 10.0.402 |

## 1. Abnahmekriterien laut SPECS

| Nr. | Kriterium | Nachweis | Ergebnis |
|-----|-----------|----------|----------|
| 1 | **Gestaltung** – Mockup abgenommen | Mockup v1 und v2 mit Änderungswünschen, **Mockup v3 abgenommen** am 2026-10-29 (E42); Oberfläche anschließend erneut mit dem Mockup abgeglichen (E51) | ✅ erfüllt |
| 2 | **Fachliche Korrektheit** – Code besteht alle Tests | `dotnet test`: **101 von 101 Tests bestanden**, Build ohne Warnungen und Fehler (Abschlusslauf 2026-10-29) | ✅ erfüllt |
| 3 | **Korrektheit in Vorschau und tatsächlicher Umbenennung** | Integrationstests mit den Dateien aus `testdata/`: Vorschau und Ergebnis auf der Platte werden je Verfahren verglichen | ✅ erfüllt |
| 4 | **Testdateien** für die Verfahren | Ordner `testdata/01…06` mit Beschreibung in `testdata/README.md` | ✅ erfüllt |
| 5 | **Abschließende Abnahme** | Manueller Test durch den Anwender am 2026-10-29, ohne Beanstandungen zurückgemeldet; Bestätigung siehe Abschnitt 7 | ⏳ Unterschrift offen |

## 2. Anforderungen aus SPECS.md

| Anforderung | Umsetzung | Nachweis | Status |
|-------------|-----------|----------|--------|
| Avalonia-Desktopanwendung | `src/FileRenamer.App` | Build, App-Start unter Windows | ✅ |
| Dateien per Menü oder Drag & Drop, ein oder mehrere | Datei → Dateien/Ordner hinzufügen; Ablage auf dem Fenster; Ordner ohne Unterordner (E16) | Code; nicht per Klick getestet (B5) | ✅ |
| **Text ersetzen** – erster / letzter / alle Treffer | `ReplaceTextRule`, inkl. regulärer Ausdrücke, Groß-/Kleinschreibung beachtet (E6) | `ReplaceTextRuleTests`, Integrationstest | ✅ |
| **Datum (optional Zeit) anfügen**, vor/nach, Format wählbar | `AppendDateRule`; Vorlagenliste + freies, geprüftes Format; Trennzeichen wählbar (E3, E22, E29) | `AppendDateRuleTests`, `DateFormatValidatorTests`, Integrationstest | ✅ |
| **Zähler anhängen**, führende Nullen, Sortierkriterien auf-/absteigend | `AppendCounterRule`; Start, Schritt, Stellen, Trennzeichen; Name/Erstellungs-/Änderungsdatum/Größe (E7) | `AppendCounterRuleTests`, Integrationstest | ✅ |
| **Ziffernketten weiterzählen** (Beispiele aus SPECS) | `ShiftDigitRangeRule` („Ziffernkette Wertebereich verschieben“, E35) | SPECS-Beispiele als eigene Tests: `name-001` + 100 → `name-101`; `name-301-001` Nr. 2 + 10 → `name-301-011` | ✅ |
| **Zeichenkette an Index einfügen** | `InsertTextRule`; ab 0, vom Anfang oder Ende, begrenzt (E10) | `InsertTextRuleTests`, Integrationstest | ✅ |
| Dateierweiterung ignorieren und ausblenden | Nur letzte Endung (E11) | `RenamePipelineTests` | ✅ |
| Manuelle Änderung in der Dateiliste | **entfällt** (E12) | – | ➖ abweichend |
| Zweigeteilter Bildschirm: oben Verfahren + Optionen, unten Kette + zweispaltige Vorschau | wie Mockup v3 | Headless-Rendering, Mockup-Abgleich | ✅ |
| Verfahren per Checkbox an-/abwählen und entfernen | Kette mit Checkbox und ✕, Reihenfolge per Drag & Drop (E13) | Code, Rendering | ✅ |
| Echtzeitvorschau mit farbiger Hervorhebung, Farbbalken je Verfahren | Herkunftsabschnitte (`SegmentedName`), Farbe je Verfahren, Vorrang des gewählten Verfahrens (E5) | `SegmentedNameTests`, `RenamePipelineTests`, Rendering | ✅ |
| Rückgängig pro Verfahren | in der Vorschau per Checkbox (E4); zusätzlich „Letzte Umbenennung widerrufen“ (E49) | `FileRenameExecutorTests`, Integrationstest `Undo_…` | ✅ |
| Bestätigen / Verwerfen, Sicherheitsabfrage | „Umbenennen“ mit Sicherheitsabfrage, „Änderungen verwerfen“ (E46) | Code | ✅ |
| Einstellungen als Profil | benannte Profile als JSON, editierbares Profilfeld, Speichern-Dialog (E15, E48) | `ProfileStoreTests` | ✅ |
| Light-/Dark-Mode, Auswahl System/Hell/Dunkel | Theme-Icons und Menü, helles Theme ohne reines Weiß (E30, E34) | Rendering hell/dunkel | ✅ |
| Wenig Abhängigkeiten, plattformübergreifend unterstützte Versionen | nur Avalonia, CommunityToolkit.Mvvm, xUnit; .NET 10 LTS (E27) | `*.csproj` | ✅ |
| Windows, Linux, macOS | macOS ab **14** statt 10.13 (E1) | nur unter Windows geprüft (B4) | ⚠️ eingeschränkt |

Zusätzlich auf Wunsch umgesetzt: Konflikt- und Namensprüfung (E23–E28), Umbenennen nach dem Alles-oder-nichts-Prinzip (E25, E26), Dateiauswahl per Checkbox (E52–E54), „Liste leeren“ (E50), App-Icon (E55).

## 3. Testergebnis

Abschlusslauf am 2026-10-29: **101 Tests, 101 bestanden, 0 fehlgeschlagen, 0 übersprungen.**

| Testklasse | Prüft |
|------------|-------|
| `ReplaceTextRuleTests`, `AppendDateRuleTests`, `AppendCounterRuleTests`, `ShiftDigitRangeRuleTests`, `InsertTextRuleTests` | die fünf Verfahren einzeln, inkl. SPECS-Beispiele, Überlauf, negative Werte, Fallback Erstellungsdatum |
| `RenamePipelineTests` | Kaskade, Erweiterung, Deaktivieren, Dateiauswahl, Schrittinformationen, Konflikte, Hervorhebung |
| `FileNameValidatorTests`, `DateFormatValidatorTests` | ungültige Namen und Formate auf allen Plattformen |
| `FileRenameExecutorTests` | Ringtausch, Groß-/Kleinschreibung, Rücknahme bei Fehlern, Widerrufen |
| `ProfileStoreTests` | Speichern/Laden/Löschen von Profilen und Einstellungen |
| `RenameIntegrationTests` | echte Dateien: Vorschau = Ergebnis auf der Platte je Verfahren, Widerrufen, Kollision |
| `SegmentedNameTests`, `NaturalStringComparerTests` | Herkunft der Abschnitte, natürliche Sortierung |

## 4. Abweichungen von SPECS.md (vereinbart)

| ID | Abweichung | Grund |
|----|-----------|-------|
| E1 | macOS ab 14 statt 10.13 | .NET 10 setzt macOS 14 voraus; .NET 8 endet am 2026-11-10 |
| E12 | Keine manuelle Änderung in der Dateiliste | zu komplex, vom Anwender gestrichen |
| E47 (A2) | Kette als Liste unter „▾ Kette“ statt Treeview mit Unterknoten | wie im abgenommenen Mockup v3 |
| E48 | Profilverwaltung über editierbares Feld und Speichern-Dialog statt „Profile verwalten…“ | Wunsch des Anwenders |
| B7 | Durchgezogene statt gestrichelter Rahmen | in Avalonia nicht verfügbar |

## 5. Bekannte Einschränkungen

| ID | Einschränkung |
|----|---------------|
| B2/E47 | Unter Linux wird statt des Erstellungsdatums das Änderungsdatum verwendet (mit Hinweis) |
| B4 | Geprüft nur unter Windows; Linux und macOS sind nicht getestet |
| B5 | Drag & Drop, Dialoge und Dateiauswahl nur im manuellen Anwendertest geprüft, nicht automatisiert |
| B6 | Kein macOS-App-Bundle und kein Installer |
| B8 | Widerrufen nur für die letzte Umbenennung und bis zum Programmende |

## 6. Gelieferte Bestandteile

| Bestandteil | Ort |
|-------------|-----|
| Quellcode | `src/FileRenamer.Core`, `src/FileRenamer.App` |
| Tests und Testdateien | `tests/FileRenamer.Core.Tests`, `testdata/` |
| Mockups und Icon-Entwürfe | `mockups/` |
| Entwicklerdokumentation | `docs/ENTWICKLER.md` |
| Anwenderdokumentation | `docs/ANWENDER.md` |
| Fragen, Entscheidungen, Fortschritt | `PROTOKOLL.md` |
| Abnahmeprotokoll | `ABNAHME.md` |

## 7. Abnahmeerklärung

Die Kriterien 1–4 sind erfüllt. Der Anwender hat die Anwendung am 2026-10-29 manuell getestet und keine
Beanstandungen gemeldet. Die Einschränkungen aus Abschnitt 5 sind bekannt.

| | |
|---|---|
| Abnahme erteilt | ☐ ja  ☐ nein  ☐ unter Vorbehalt |
| Vorbehalte / Restpunkte | |
| Datum | |
| Name, Unterschrift | |

---

# Nachtrag: Erweiterung vom 2026-10-30

Grundlage: Entscheidungen E56–E108 in `PROTOKOLL.md`, freigegebenes **Mockup v10** (`mockups/mockup-v10.html`, E90).

## N1. Abnahmekriterien

| Nr. | Kriterium | Nachweis | Ergebnis |
|-----|-----------|----------|----------|
| 1 | Gestaltung – Mockup freigegeben | Mockups v4–v9 mit Änderungswünschen, **Mockup v10 freigegeben** (E90); Umsetzung per Headless-Rendering (hell, dunkel, Akzentfarbe) mit dem Mockup abgeglichen | ✅ erfüllt |
| 2 | Fachliche Korrektheit – alle Tests bestanden | **198 von 198 Tests** bestanden (vorher 101), Build ohne Warnungen und Fehler | ✅ erfüllt |
| 3 | Vorschau = tatsächliche Umbenennung | Integrationstest `MetadataTemplate_PreviewMatchesDisk` mit echten JPEG-, NEF-, MP3- und PDF-Dateien | ✅ erfüllt |
| 4 | Abschließende Abnahme | manueller Test durch den Anwender; Abnahme am 2026-10-30 erklärt | ✅ erfüllt |

## N2. Umgesetzte Anforderungen

| Anforderung | Umsetzung | Tests |
|-------------|-----------|-------|
| Groß-/Kleinschreibung (8 Schreibweisen, Beispiele in der Auswahl) | `ChangeCaseRule` (E58, E59, E75) | `TextRulesTests` |
| Datum/Uhrzeit aus EXIF | Datumsquelle „EXIF-Aufnahmedatum“ mit Fallback (E60) | `TemplateRuleTests.AppendDate_ExifSource` |
| EXIF an den Namen hängen / ID3 als Name, gesteuert über Platzhalter | „Name aus Vorlage“ mit Tabs Fotos/Audio, `%name%`, Formate, Ersatztext, Ersatzzeichen, Tracknummer-Stellen (E61–E65, E68, E74, E76, E77) | `TemplateRuleTests` |
| Formate | eigene Leser: JPEG, TIFF, HEIC, PNG, DNG, CR2, CR3, NEF, ARW, ORF, RW2, RAF, PEF; MP3 (ID3v1/v2.2–2.4), FLAC, M4A (E56, E57, E69–E71) | `MetadataReaderTests` (28) |
| Zeichen bereinigen, Bereich entfernen/kürzen, Erweiterung ändern, Ordnername einfügen | vier weitere Verfahren (E67, E73) | `TextRulesTests`, `ChangeExtensionRuleTests` |
| Gruppierte Verfahrensliste | vier Gruppen (E72) | Rendering |
| Akzentfarbe | Kreis neben der Theme-Auswahl + Menü; Pastell mit HSV-Sättigung 10–90 % (Standard 30 %), kräftig, neutral, benutzerdefiniert (max. 10), Hex-Eingabe, „Standard“, automatische Schriftfarbe, Rahmen bei geringem Kontrast; gespeichert (E79–E89) | `AccentPaletteTests`, `ProfileStoreTests` |
| Profil zurücksetzen | Schaltfläche „Zurücksetzen“ + Menü „Profile → Profil zurücksetzen“; Rückfrage bei nicht leerer Kette; Kette und Profilfeld leer, Profile bleiben (E91) | Headless-Ablauf |
| Sprache Englisch | Oberfläche und Meldungen Deutsch/Englisch; „Ansicht → Sprache“ (System/Deutsch/English), Wechsel ohne Neustart, gespeichert; System: Deutsch nur bei deutschem System (E92, E93); Beispielnamen ebenfalls übersetzt (E98) | `CoreTextTests`, `ProfileStoreTests`, Headless-Ablauf |
| Eigene Vorlagen | „Speichern“ je Tab bei „Name aus Vorlage“, programmweit gespeichert, max. 10, ✕ zum Entfernen, oben in der Liste, Schaltfläche in Akzentfarbe (E106–E108) | `CustomTemplateListTests`, `ProfileStoreTests`, Headless-Ablauf |
| Rückmeldung zur Bedienung | Drag &amp; Drop mit Abbild am Mauszeiger und Einfügelinie (E102), gerundete Segment-Außenkanten mit vollständigem Rahmen bei heller Akzentfarbe (E95, E101), Schreibweise-Auswahl auf gemeinsamer Grundlinie (E100), Karteireiter bei „Name aus Vorlage“ mit fokusabhängiger Akzentfarbe und Platzhaltern über die volle Breite (E99, E103, E105), Rahmen des Sättigungsfelds (E104), Editor-Beispiel sofort sichtbar (E97) | Headless-Ablauf mit simuliertem Ziehen |

## N3. Bekannte Einschränkungen (zusätzlich zu Abschnitt 5)

| ID | Einschränkung |
|----|---------------|
| B9 | Metadaten-Leser mit erzeugten Testdateien geprüft, nicht mit Originaldateien realer Kameras/Player |
| B10 | Bei sehr hellen Akzentfarben im hellen Theme bleiben Checkboxen und Reglerknopf blass |
| B11 | Beim Sprachwechsel werden Trennbalken- und Bildlaufpositionen zurückgesetzt (Fenster wird neu aufgebaut) |

## N4. Abnahmeerklärung zur Erweiterung

| | |
|---|---|
| Abnahme erteilt | ☒ ja  ☐ nein  ☐ unter Vorbehalt |
| Vorbehalte / Restpunkte | keine (bekannte Einschränkungen siehe N3) |
| Datum | 2026-10-30 |
| Name, Unterschrift | |
