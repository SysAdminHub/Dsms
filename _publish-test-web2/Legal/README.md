# Rechtliche Dokumente

Dieser Ordner enthält die rechtlichen Dokumente der Anwendung (Impressum, Datenschutzerklärung, AGB, AVV, TOM, Unterauftragnehmerliste) als Markdown-Dateien.

## Versionierung

Bei Änderungen an den Rechtstexten muss die Version in `legal-documents.json` angepasst werden (`version` und `effectiveDate`).

## Archivierung

Wenn Kunden bereits einer bestimmten Version zugestimmt haben, sollten alte Versionen nicht überschrieben werden. Lege geänderte Texte stattdessen in einem neuen Versionsordner ab (z. B. `2026-07-01/`) und aktualisiere die Pfade in `legal-documents.json` entsprechend.

## Geplante Verwendung

Die Markdown-Dateien sollen später für Legal-Seiten in der Anwendung, PDF-Downloads und E-Mail-Anhänge verwendet werden.

## Struktur

- `current/` – aktuell gültige Rechtstexte
- `legal-documents.json` – zentrale Metadaten mit Version und Dateipfaden
