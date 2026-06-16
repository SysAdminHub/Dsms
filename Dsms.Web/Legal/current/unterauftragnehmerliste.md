# Unterauftragnehmerliste

Stand: {{LegalVersionDate}}

Unterauftragnehmerliste  
für die SaaS-Anwendung „Datenschutz-Cloud“

Stand: 11.06.2026

**Anbieter der Datenschutz-Cloud:**

Stefan Keller – The SysAdminHub  
Stefan Keller  
Sonthofer Str. 2  
87527 Sonthofen  
Deutschland

E-Mail: support@datenschutz-cloud.eu

Diese Unterauftragnehmerliste informiert darüber, welche Dienstleister im Rahmen der Bereitstellung, des Betriebs, der Wartung, des E-Mail-Versands und der Datensicherung der Datenschutz-Cloud eingesetzt werden.

Die nachfolgend genannten Dienstleister können personenbezogene Daten im Rahmen einer Auftragsverarbeitung verarbeiten. Mit den Dienstleistern werden, soweit erforderlich, Verträge zur Auftragsverarbeitung gemäß Art. 28 DSGVO abgeschlossen.

## Übersicht

| Unterauftragnehmer | Anschrift | Leistung | Ort der Verarbeitung | Rolle/Zweck |
|--------------------|-----------|----------|----------------------|-------------|
| STRATO (STRATO GmbH) | Otto-Ostrowski-Straße 7, 10249 Berlin, Deutschland | Hosting und technische Bereitstellung der Server- und Web-Infrastruktur | Deutschland | Hosting-Anbieter |
| Hetzner Online GmbH | Industriestraße 25, 91710 Gunzenhausen, Deutschland | Infrastruktur für eigenen Mailserver | Deutschland / EU | E-Mail-Infrastruktur |
| Hetzner Online GmbH | Industriestraße 25, 91710 Gunzenhausen, Deutschland | Speicherung verschlüsselter Backups | Deutschland / EU | Backup-Speicher |

---

## 1. STRATO

**Unterauftragnehmer:** STRATO

**Adresse:**  
STRATO GmbH  
Otto-Ostrowski-Straße 7  
10249 Berlin  
Deutschland

**Land der Verarbeitung:** Deutschland

**Zweck des Einsatzes:**  
Hosting und technische Bereitstellung der Server- und Web-Infrastruktur der Datenschutz-Cloud.

**Betroffene Datenkategorien:**

- technische Zugriffsdaten
- Server-Logdaten
- Datenbankdaten
- Anwendungsdaten
- hochgeladene Dokumente
- technische Systemdaten

**Art der Verarbeitung:**

- Speicherung
- Bereitstellung
- technischer Betrieb
- Sicherung der Verfügbarkeit
- technische Wartung im Rahmen der Hosting-Leistung

**Drittlandübermittlung:**  
Nein, nach aktuellem Stand nicht vorgesehen.

**AV-Vertrag:**  
Ein Vertrag zur Auftragsverarbeitung mit STRATO wird abgeschlossen bzw. im STRATO-Kundenbereich dokumentiert. STRATO stellt Informationen und eine Vereinbarung zur Auftragsverarbeitung bereit.

---

## 2. Hetzner Online GmbH – Infrastruktur für E-Mail-Server

**Unterauftragnehmer:**  
Hetzner Online GmbH  
Industriestraße 25  
91710 Gunzenhausen  
Deutschland

**Land der Verarbeitung:** Deutschland / EU

**Zweck des Einsatzes:**  
Bereitstellung der Server-Infrastruktur für den vom Anbieter selbst betriebenen Mailserver.

**Betroffene Datenkategorien:**

- E-Mail-Adressen
- Namen, sofern in E-Mails enthalten
- Benutzer- und Mandantenbezug
- Inhalte systembezogener E-Mails
- technische Mail-Logdaten

**Art der Verarbeitung:**

- Übermittlung
- Speicherung
- Versand systembezogener E-Mails
- technischer Betrieb der Mailserver-Infrastruktur

**Typische E-Mails:**

- Passwort-Reset-E-Mails
- Einladungs-E-Mails
- Registrierungs- und Provisionierungsbenachrichtigungen
- Systembenachrichtigungen
- Support- und Feedbackkommunikation
- Erinnerungen, sofern aktiviert

**Drittlandübermittlung:**  
Nein, nach aktuellem Stand nicht vorgesehen.

**AV-Vertrag:**  
Ein Vertrag zur Auftragsverarbeitung mit Hetzner wird abgeschlossen bzw. im Hetzner-Kundenbereich dokumentiert. Hetzner stellt einen AV-Vertrag nach Art. 28 DSGVO bereit.

---

## 3. Hetzner Online GmbH – Backup-Speicher

**Unterauftragnehmer:**  
Hetzner Online GmbH  
Industriestraße 25  
91710 Gunzenhausen  
Deutschland

**Land der Verarbeitung:** Deutschland / EU

**Zweck des Einsatzes:**  
Speicherung verschlüsselter Backups der Datenschutz-Cloud.

**Betroffene Datenkategorien:**

- Datenbankdaten
- Anwendungsdaten
- hochgeladene Dokumente
- technische Konfigurationsdaten
- Systemdaten, soweit für eine Wiederherstellung erforderlich

**Art der Verarbeitung:**

- Speicherung
- Sicherung
- Wiederherstellung im Bedarfsfall
- technische Bereitstellung von Speicherinfrastruktur

**Technische Umsetzung:**  
Die Backups werden über Backrest verwaltet und basieren technisch auf restic. Die Übertragung zum Backup-Ziel erfolgt über eine verschlüsselte Verbindung, insbesondere per SFTP. Die Speicherung erfolgt verschlüsselt im restic-Repository. Restic verschlüsselt Backup-Daten im Repository; Backrest dient als Weboberfläche und Orchestrator für restic-Backups.

**Aufbewahrung:**  
Die Aufbewahrung der Backups beträgt derzeit bis zu 3 Monate.

**Drittlandübermittlung:**  
Nein, nach aktuellem Stand nicht vorgesehen.

**AV-Vertrag:**  
Ein Vertrag zur Auftragsverarbeitung mit Hetzner wird abgeschlossen bzw. im Hetzner-Kundenbereich dokumentiert. Hetzner stellt einen AV-Vertrag nach Art. 28 DSGVO bereit.

---

## 4. Weitere Unterauftragnehmer

Derzeit werden keine weiteren Unterauftragnehmer für den produktiven Betrieb der Datenschutz-Cloud eingesetzt.

Sollten künftig weitere Unterauftragnehmer eingesetzt werden, wird diese Liste entsprechend aktualisiert.

Mögliche künftige Unterauftragnehmer können insbesondere betreffen:

- Zahlungsdienstleister
- Monitoring-Dienstleister
- Support- oder Ticketsysteme
- E-Mail-Dienstleister
- DNS-, CDN- oder Sicherheitsdienste
- externe Wartungs- oder Entwicklungsdienstleister

## 5. Änderung der Unterauftragnehmer

Der Anbieter ist berechtigt, Unterauftragnehmer zu ändern, zu ersetzen oder weitere Unterauftragnehmer einzusetzen, soweit dies für den Betrieb, die Sicherheit, die Wartung oder die Weiterentwicklung der Datenschutz-Cloud erforderlich ist.

Kunden werden über wesentliche Änderungen der Unterauftragnehmerliste in geeigneter Weise informiert.

Kunden können aus wichtigem datenschutzrechtlichem Grund gegen den Einsatz eines neuen Unterauftragnehmers widersprechen.

## 6. Drittlandübermittlung

Eine Verarbeitung personenbezogener Daten außerhalb der Europäischen Union oder des Europäischen Wirtschaftsraums ist derzeit nicht vorgesehen.

Sollte künftig eine Drittlandübermittlung erforderlich werden, erfolgt diese nur auf Grundlage einer geeigneten Rechtsgrundlage nach der DSGVO, insbesondere eines Angemessenheitsbeschlusses der Europäischen Kommission oder geeigneter Garantien.

## 7. Kontakt

Bei Fragen zur Unterauftragnehmerliste können Kunden den Anbieter kontaktieren:

Stefan Keller – The SysAdminHub  
E-Mail: support@datenschutz-cloud.eu
