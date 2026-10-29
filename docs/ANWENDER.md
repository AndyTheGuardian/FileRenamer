# FileRenamer – Anwenderdokumentation

FileRenamer benennt eine oder viele Dateien auf einmal um. Dazu stellen Sie mehrere Verfahren zu
einer **Kette** zusammen, die nacheinander angewandt werden. Eine Vorschau zeigt jederzeit, wie die
Dateien heißen werden; erst mit **Umbenennen** werden die Dateien tatsächlich geändert.

Unterstützte Systeme: Windows 10/11, Linux, macOS ab Version 14.

## Das Fenster

| Bereich | Funktion |
|---------|----------|
| Menü und Leiste oben | Dateien/Ordner hinzufügen, Profile, Theme (System, Hell, Dunkel) |
| Oben links: **Verfahren** | Auswahl des Verfahrens, das Sie einstellen möchten |
| Oben rechts: **Optionen** | Einstellungen des gewählten Verfahrens |
| Unten links: **Angewandte Verfahren** | Die Kette in Anwendungsreihenfolge |
| Unten rechts: **Vorschau** | Aktueller und neuer Name; Änderungen sind in der Farbe des Verfahrens hinterlegt |
| Fußzeile | Zusammenfassung, „Letzte Umbenennung widerrufen“, „Änderungen verwerfen“, „Umbenennen“ |

## Schritt für Schritt

1. **Dateien hinzufügen:** über *Datei → Dateien hinzufügen…* bzw. *Ordner hinzufügen…* oder per
   Drag &amp; Drop auf das Fenster. Bei Ordnern werden die Dateien direkt darin übernommen, Unterordner nicht.
2. **Verfahren wählen** (oben links) und **Optionen einstellen** (oben rechts).
3. **Zur Kette hinzufügen** klicken. Das Verfahren erscheint unten links und wirkt sofort auf die Vorschau.
4. Weitere Verfahren hinzufügen. Sie werden in der Reihenfolge der Kette angewandt.
5. Vorschau prüfen und **Umbenennen** klicken. Nach der Sicherheitsabfrage werden die Dateien umbenannt.

## Die Kette bearbeiten

- **Eintrag anklicken:** Die Optionen erscheinen oben rechts; jede Änderung wirkt sofort.
  „Zur Kette hinzufügen“ legt in diesem Fall eine Kopie an.
- **Checkbox:** schaltet ein Verfahren in der Vorschau aus und wieder ein (Rückgängig pro Verfahren).
- **✕:** entfernt das Verfahren aus der Kette.
- **⋮⋮ ziehen:** ändert die Reihenfolge.
- **Rot umrandeter Eintrag:** Das Verfahren ist so nicht anwendbar; der Tooltip nennt den Grund.

Dasselbe Verfahren darf mehrfach in der Kette vorkommen. Die farbige Markierung des gerade gewählten
Ketteneintrags hat in der Vorschau Vorrang, wenn sich Änderungen überschneiden.

## Die Verfahren

### Text ersetzen
Sucht einen Text und ersetzt den **ersten**, **letzten** oder **alle** Treffer. Groß-/Kleinschreibung
wird beachtet. Mit **Regulärer Ausdruck** sind Muster möglich, z. B. `(\d{4})-(\d{2})` → `$2.$1`.

### Datum / Zeit anfügen
Fügt das **Erstellungs-** oder **Änderungsdatum** vor oder nach dem Namen ein, optional mit Uhrzeit.
Das Format wählen Sie aus der Liste oder geben es selbst ein (`yyyy` Jahr, `MM` Monat, `dd` Tag,
`HH` Stunde, `mm` Minute, `ss` Sekunde). Ungültige Formate, z. B. mit Doppelpunkt, werden rot gemeldet.
Beide Trennzeichen (Name ↔ Datum, Datum ↔ Uhrzeit) sind frei wählbar.
Ist kein Erstellungsdatum verfügbar (häufig unter Linux), wird das Änderungsdatum verwendet und die
Zeile mit ⚠ markiert.

### Zähler anhängen
Hängt eine fortlaufende Nummer vor oder nach dem Namen an. Einstellbar sind Startwert, Schrittweite,
Stellen (führende Nullen), Trennzeichen sowie die Sortierung (Name, Erstellungsdatum, Änderungsdatum,
Größe; auf- oder absteigend). Namen werden „natürlich“ sortiert: `datei2` kommt vor `datei10`.

### Ziffernkette Wertebereich verschieben
Addiert einen Wert (auch negativ) auf eine Zahl im Namen, z. B. `name-001` + 100 → `name-101`.
Standardmäßig wird die **letzte** Ziffernkette verwendet. Ohne Haken bei „Letzte gefundene“ wählen
Sie die Nummer von links (z. B. Nr. 2 bei `name-301-001`). Erlaubt sind nur Nummern, die alle Dateien
mit Ziffernketten besitzen; das Feld zeigt den gültigen Bereich.
Die ursprüngliche Breite bleibt standardmäßig erhalten (`001` → `011`), alternativ legen Sie die
Mindeststellen fest. Wird eine Zahl größer, wächst sie mit (`995` + 10 → `1005`).
Namen ohne Ziffernkette bleiben unverändert (⚠); ein negatives Ergebnis ist ein Konflikt (⛔).

### Text einfügen
Fügt Text an einer Position ein, gezählt ab 0 vom Anfang oder (Haken) vom Ende. Liegt die Position
hinter dem Namensende, wird am Ende eingefügt.

## Dateien auswählen

Jede Zeile der Vorschau hat eine Checkbox. Umbenannt werden nur **ausgewählte** Dateien.

- Abgewählte Dateien werden ausgegraut und behalten ihren Namen. Sie durchlaufen die Kette nicht:
  Der Zähler nummeriert nur die ausgewählten Dateien lückenlos, und die erlaubte Ziffernketten-Nr.
  richtet sich nur nach ihnen.
- Der Name einer abgewählten Datei bleibt belegt. Würde eine ausgewählte Datei genauso heißen, ist das ein Konflikt.
- Die Checkbox im Spaltenkopf wählt alle bzw. keine Datei aus. Bei teilweiser Auswahl zeigt sie einen Zwischenzustand.
- Neu hinzugefügte Dateien sind ausgewählt.

## Dateierweiterung ignorieren

Ist der Haken über der Vorschau gesetzt, wirken die Verfahren nur auf den Namen ohne die **letzte**
Endung (bei `archiv.tar.gz` also ohne `.gz`). Die Endung wird dann auch nicht angezeigt, bleibt beim
Umbenennen aber erhalten.

## Hinweise und Konflikte

| Symbol | Bedeutung |
|--------|-----------|
| ⚠ | Hinweis, z. B. Ausweichen auf Änderungsdatum oder keine Ziffernkette gefunden. Umbenennen ist möglich. |
| ⛔ (rote Zeile) | Konflikt: doppelter Zielname, Name existiert bereits, ungültiger Name (verbotene Zeichen `< > : " / \ | ? *`, reservierte Namen wie `CON`, Punkt/Leerzeichen am Ende, mehr als 255 Zeichen) oder negatives Ergebnis. **Umbenennen ist gesperrt.** |

Fahren Sie mit der Maus über das Symbol, um den Grund zu sehen.

## Umbenennen und Sicherheit

- Vor dem Umbenennen erscheint eine Sicherheitsabfrage.
- Es wird immer **alles oder nichts** umbenannt: Tritt ein Fehler auf (z. B. Datei gesperrt), werden
  alle bereits umbenannten Dateien wieder zurückbenannt.
- Tauschen von Namen (a ↔ b) und reine Änderungen der Groß-/Kleinschreibung funktionieren.
- Nach dem Umbenennen bleibt die Kette erhalten; die Liste zeigt die neuen Namen.
- **Letzte Umbenennung widerrufen** gibt den zuletzt umbenannten Dateien nach einer Sicherheitsabfrage ihre
  vorherigen Namen zurück (ebenfalls alles oder nichts). Ist ein früherer Name inzwischen belegt, wird nichts geändert.
- **Änderungen verwerfen** leert die Kette; die Dateien bleiben in der Liste.
- **Liste leeren** (unter der Vorschau oder *Datei → Liste leeren*) entfernt alle Dateien aus der Liste – die Dateien selbst bleiben unverändert.

## Profile

Ein Profil speichert die Kette mit allen Einstellungen und der Reihenfolge sowie „Dateierweiterung ignorieren“.

- **Speichern** (Leiste) bzw. *Profile → Profil speichern…* öffnet immer den Dialog „Profil speichern“.
  Er ist mit dem Namen aus dem Profilfeld vorbelegt. Ein neuer Name legt ein weiteres Profil an,
  ein vorhandener Name wird nach Rückfrage überschrieben.
- Im Profilfeld können Sie einen Namen direkt eintippen; Eintippen allein lädt nichts.
- **Laden:** ein Profil in der aufgeklappten Liste wählen (oder *Profile → Profil laden…*).
- *Profile → Profil löschen…* entfernt das aktuelle Profil.

Das zuletzt verwendete Profil und das Theme werden beim nächsten Start wiederhergestellt.

## Theme

Die drei Symbole oben rechts (bzw. *Ansicht*) wählen **System** (Monitor), **Hell** (Sonne) oder **Dunkel** (Mond).
