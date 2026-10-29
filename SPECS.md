## Projekt Titel: FileRenamer

Ein Programm zum kaskadierenden umbenennen von Dateien (Single, Batch)

## Was soll getan werden

Das Programm ist eine Avalonia Desktopanwendung, die per Menüpunkt oder Drag & Drop eine oder mehrere Dateien entgegennimmt. Über eine Auswahlliste stehen mehrere Umbenennungs-Verfahren zur Verfügung diese sind
- Text ersetzen -> findet den Textteil und ersetzt ihn mit einem einzugebenen Text. Zur Option stehen hierbei erster Treffer, letzter Treffer und alle Treffer
- Datum und optional Zeit anfügen -> Das filecreation date wird wählbar vor oder nach dem dateinamen angefügt. Über eine eingabe wie bsw. "YYYY-MM-DD" soll sich das Format dafür festlegen lassen
- Zähler anhängen -> vor oder nach den Dateinamen soll sich ein durchlaufender Zähler anfügen lassen. Dabei soll die Anzahl der führenen Nullen bestimmbar sein und welche Sortierkriterien dafür benutzt werden (z.B. nach erstellungsdatum, alphabetisch und auf- bzw. absteigend).
- Enthält der dateiname Bereits eine oder mehrere Ziffernkette soll es möglich sein, diese als zähler mit einer eingegeben Zahl weiter aufzuaddieren. Beispiel name-001.txt, name-002 wird mit der Eingabe 100 zu name-101.txt, name-102. Enthält der Dateiname mehrere dieser zifferketten soll sich bestimmen lassen, welche ziffernkette aufaddiert werden soll. Beispiel: name-301-001, name-432-002 wird mit der Eingabe Wert: 10, Position: 2 zu name-301-011, name-432-012
- es soll möglich sein an einem bestimmten Index des Dateinamens eine Zeichenkette einzufügen

Es soll möglich sein die Dateierweiterung für die Umbenennung zu ignorieren. Hierbei soll sie auch nicht angezeigt werden.

Eine weitere Möglichkeit soll eine Manuelle Änderung direkt in der Dateiliste sein.

Der Bildschirm soll zweigeteilt sein. Auf der oberen Hälfte sollen die Einstellungen erfolgen. Dazu steht auf der linken Seite ein Liste zur Verfügung, die die Verfahren auflistet. Rechts daneben werden bei gewähltem Verfahren die Optionen und potentiellen Eingabefelder dargestellt.

Auf der unteren Hälfte gibt es links einen Treeview, der die verwendeten Verfahren auflistet die sich mittels Checkbox an und abwählen oder entfernen lassen. Rechts daneben ist eine zweispaltige Liste mit den unveränderten Dateinamen und einer echtzeit Vorschau der Umbenannten Dateinamen. Dabei werden die geänderten Stellen farblich hervorgehoben. Jedes Verfahren hat dabei seine eigene Farbe, die in der Verfahrensliste durch einen kleinen senkrechten Balken, in der ensprechenden Farbe, vor dem Verfahrensnamen gekennzeichnet ist. Gibt es Überschneidungen bei den Umbenennungen, wird immer das aktuell angewandte Verfahren priorisiert.

Pro Verfahren sollen sich die Umbenennungen rückgängig machen lassen.

Unter der zweispaltigen Liste befindet sich eine Schaltfläche zur Bestätigung der Umbenennung und eine zum Verwerfen der Änderungen.

Wird die Schaltfläche zur Umbenennung gecklickt, soll eine zusätzliche Sicherheitsabfrage zur Bestätigung erscheinen.

Die Einstellungen sollen als Profil persistierbar sein.

Das Programm soll einen Light- und Dark-mode unterstützen. Als auswahl für das Theme sollen drei Möglichkeiten bestehen: System, Light, Dark.


## Was soll benutzt werden

Erstelle das Programm in C# und Avalonia. benutze dabei so wenig Abhängigkeiten wie möglich. Wenn möglich achte darauf, dass Versionen verwendet werden, die auf allen Platformen unterstützt werden.

## Wer nutzt das Projekt auf welcher Platform

Das Projekt kann von jedem benutzt werden. Es soll Platformübergreifend auf Windows, Linux und macOS (ab version 10.13) laufen.

## Wie wird der Erfolg geprüft

Das Projekt hat folgende Bewertungsrichlinien:
1. Gestaltung
2. Fachliche korrektheit

Für die Gestaltung erzeuge bitte zunächst ein Mockup, das abgenommen werden muss. Erzeuge bitte der Reihe nach Mockups, bis eins genehmight wird.

Die Korrektheit wird anhand von Tests gemessen. Erzeuge einen Ordner mit Testdateien, die die Verschiedenen Umbennenungsverfahren abbilden.

Prüfe die Korrektheit der Umbenennungen. Sowohl in der Vorschau, als auch bei der tatsächlichen Umbenennung.


## Wie ist das Verhalten bei offenen Fragen

Frage interaktiv nach. Mache keine Annahmen, Du darfst aber gern etwas Vorschlagen. Weise immer auf Probleme hin.

## Wann gilt das Projekt als abgeschlossen

Wenn die Gestaltung abgenommen ist, der Code alle Tests besteht und eine abschließende Abnahme (erstelle ein Protokoll) abgeschlossen ist.