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
