# Testdateien

Die Ordner enthalten kleine Textdateien, an denen die Umbenennungsverfahren geprüft werden.
Die Integrationstests (`tests/FileRenamer.Core.Tests/Integration`) kopieren einen Ordner in ein
temporäres Verzeichnis, erzeugen die Vorschau, benennen tatsächlich um und vergleichen beides.
Die Dateien können auch für manuelle Tests in der Anwendung verwendet werden (vorher kopieren!).

| Ordner | Verfahren | Geprüfte Fälle |
|--------|-----------|----------------|
| `01-text-ersetzen` | Text ersetzen | erster / letzter / alle Treffer, regulärer Ausdruck, Groß-/Kleinschreibung |
| `02-datum` | Datum / Zeit anfügen | vor / nach dem Namen, mit Uhrzeit, eigene Trennzeichen (Dateidaten werden im Test gesetzt) |
| `03-zaehler` | Zähler anhängen | natürliche Sortierung (`datei2` vor `datei10`), Startwert, Schrittweite, Stellen, absteigend |
| `04-ziffernkette` | Ziffernkette Wertebereich verschieben | Beispiele aus SPECS (`name-001` + 100, `name-301-001` Nr. 2 + 10), Überlauf `995` + 10, Name ohne Ziffernkette |
| `05-text-einfuegen` | Text einfügen | Position vom Anfang / vom Ende, Begrenzung auf Namensende |
| (erzeugt) | Name aus Vorlage, Metadaten | Die Foto- und Audiodateien mit EXIF/ID3/FLAC/M4A-Metadaten erzeugen die Tests selbst (`tests/…/Support/MediaFiles.cs`), damit keine fremden Binärdateien im Projekt liegen |
| `06-konflikte` | Konflikte / Ablauf | Ringtausch `a`↔`b`, reine Groß-/Kleinschreibung `Foto.JPG` → `foto.jpg`, Kollision mit vorhandener Datei |
