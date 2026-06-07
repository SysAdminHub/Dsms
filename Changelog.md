# Changelog

Alle nennenswerten Änderungen an diesem Projekt werden in dieser Datei dokumentiert.

## [Unreleased]

### Hinzugefügt

- **Zentraler Emailservice (Version 1 – Fundament):**
  - Globale SMTP-Einstellungen für Superuser unter `/platform/email/settings` (Plattform → Email)
  - Email-Vorlagen-Verwaltung unter `/platform/email/templates` und `/platform/email/templates/edit/{Id}`
  - Services: `IEmailService`, `IEmailSettingsService`, `IEmailTemplateService`, `IEmailTemplateRenderer`, `IEmailSecretProtector`
  - SMTP-Passwort-Schutz via ASP.NET Data Protection (`EmailSecretProtector`)
  - Platzhalterersetzung im Format `{{VariableName}}` mit Vorschau (Beispieldaten) und Testmail
  - Standardvorlagen (Seed): PasswordReset, WelcomeSetPassword, Reminder, TestEmail – ohne Überschreiben angepasster Vorlagen
  - Vorbereitete Methoden: `SendPasswordResetEmailAsync`, `SendWelcomeSetPasswordEmailAsync`, `SendReminderEmailAsync` (noch nicht in Workflows integriert)
  - EF-Migration `AddEmailSettingsAndTemplates` (Tabellen `EmailSettings`, `EmailTemplates`)
  - NuGet-Paket `MailKit` 4.16.0 für SMTP-Versand
  - **Bewusst nicht enthalten:** EmailLog / Versandprotokoll, mandantenspezifische SMTP-Einstellungen, vollständige Passwortreset-/Einladungs-/Reminder-Workflows

- **Maßnahmen direkt aus Auditfragen erstellen:**
  - Button „+ Maßnahme anlegen“ in `/audit-runs/answers/{Id}` neben „Speichern“
  - Sichtbar nur bei Bewertungen mit Handlungsbedarf (Offen, Teilweise, Nicht konform) über `ComplianceLabels.ShouldShowCreateMeasureButton`
  - Kein Button bei Konform oder Nicht anwendbar
  - Vorausgefüllte Maßnahme über `/measures/edit?auditRunId=…&auditAnswerId=…` (Titel, Beschreibung, Audit-Durchlauf)
  - Optionale Verknüpfung `Measure.AuditAnswerId` (EF-Migration `AddMeasureAuditAnswerLink`)
  - Hinweis „X Maßnahme(n) vorhanden“ pro Auditfrage, verlinkt auf gefilterte Maßnahmenliste (`/measures?auditAnswerId=…`)
  - Tenant-Prüfung beim Prefill und Speichern (Audit-Antwort muss zum Mandanten gehören)

### Hinzugefügt (früher)

- **QR-Code für Zwei-Faktor-Authentifizierung:**
  - EnableAuthenticator: QR-Code-Anzeige (QRCoder) unter dem Secret Key, otpauth-URI mit App-Name „DSMS“
  - Fallback: Secret Key bleibt sichtbar, wenn QR-Generierung fehlschlägt

### Behoben

- **Mandanten-Switcher:** Wechsel läuft über GET `/tenant/switch/{tenantId}` statt direkt aus dem Blazor-Circuit – Session-Persistenz funktioniert wieder (Fehler „session cannot be established after the response has started“)

- **Login mit aktivierter 2FA:** `RequiresTwoFactor` wird korrekt erkannt und leitet zu `/Account/LoginWith2fa` weiter (mit `ReturnUrl` und `RememberMe`); kein falscher Passwort-Fehler mehr
- **IdentityRedirectManager:** `NavigationException` wird nicht mehr abgefangen (Redirect nach Form-POST funktioniert); `forceLoad` für 2FA-Weiterleitung

- **Dokumenten-Anzeige in allen Modulen vereinheitlicht:**
  - Wiederverwendbare Komponente `LinkedDocumentsSection` für konsistentes Laden und Anzeige
  - Dokumente sichtbar in DSFA (Detail + Bearbeiten), Audit-Durchläufe (Bearbeiten + Fragen), Maßnahmen (Bearbeiten)
  - TOM-Detail: Nachweise über zugeordnete Verarbeitungstätigkeiten (kein direkter FK im Datenmodell)
  - EF `Include(x => x.Documents)` beim Laden der übergeordneten Entitäten

- **Dokumenten-Verknüpfungen nachträglich bearbeiten:**
  - Service `DocumentLinksService` – aktualisiert nur FK-Felder, Datei bleibt unverändert
  - Modal `DocumentLinksEditModal` – Verknüpfungen hinzufügen, ändern oder entfernen (optional leer)
  - Button „Bearbeiten“ in der Dokumentenliste (nur aktive Ansicht)
  - Mandantenvalidierung für alle Ziel-Entitäten

- **Dokumenten-Upload mit Viewer und Download:**
  - Verzögerter Upload: Datei auswählen, Metadaten/Verknüpfungen setzen, erst beim Klick auf „Hochladen“ speichern
  - Validierung client- und serverseitig: Dateiendung (PDF, DOCX, XLSX, JPG, PNG), MIME-Type, max. 10 MB
  - Komponenten `DocumentUploadComponent`, `DocumentActions` (Download + PDF-Viewer im Modal)
  - HTTP-Endpunkte `GET /documents/{id}/download` und `/view` (PDF inline, mandantengebunden)
  - Integration in Dokumentenliste sowie VVT-, Dienstleister- und DSFA-Detailseiten
  - Bestehende `DocumentStorageService`- und Archivierungslogik unverändert

- **Archivierung (Soft Delete) für alle Compliance-Module:**
  - Datenmodell: `ArchivableEntityBase` mit `IsArchived`, `ArchivedAt`, `ArchivedByUserId` auf allen 8 Hauptmodulen (VVT, DSFA, TOM, Dienstleister, Audit-Vorlagen, Audit-Durchläufe, Maßnahmen, Dokumente)
  - Interfaces `IArchivable`, `ITenantEntity` für wiederverwendbare Logik
  - EF Global Query Filter: Standardansicht nur aktive Einträge; Archivansicht über `ArchiveViewContextAccessor`
  - Service `IArchivingService` / `ArchivingService`: Archivieren, Wiederherstellen, Abhängigkeitswarnungen (ohne harte Blockade)
  - UI: `ArchiveViewToggle`, `ArchiveListActions`, `ArchiveConfirmModal`, `ArchivedBadge` in allen Modul-Listen
  - Buttons „Archivieren“ / „Wiederherstellen“ statt physischem Löschen
  - EF-Migration `AddArchivingSoftDelete`

- **SaaS-Basis: Rollen und Benutzer-/Mandantenverwaltung (Version 1):**
  - Neue Rolle **Superuser** (plattformweit, `TenantId` optional null)
  - **Admin** nur noch mandantenbezogene Benutzerverwaltung; **Mandanten** (`/tenants`) nur Superuser
  - Services `IUserAccessService`, `IUserManagementService` – zentrale serverseitige Prüfungen (Rollen, Mandant, bearbeitbare Benutzer)
  - Benutzer anlegen (`/users/create`), bearbeiten inkl. **IsActive** (Deaktivierung statt Löschen)
  - `ApplicationUser`: `IsActive`, `CreatedAt`, `CreatedByUserId`
  - EF-Migration `AddUserProfileFieldsForSaaS`
  - Demo-Benutzer `superuser@demo.local` (Seed bei leerer Datenbank)
  - Login blockiert inaktive Konten
  - Navigation: Sektion „Plattform“ (Superuser) und „Verwaltung“ (Benutzer für Superuser/Admin)
  - **Ein Benutzer = ein Mandant** (außer Superuser); Architektur kommentiert für spätere Multi-Tenant-Zuordnung

### Offene Punkte (SaaS V1)

- Mehrere Mandanten pro Benutzer und Rollen pro Mandant (geplant, nicht umgesetzt)
- Mandantenwechsel / Arbeitskontext für Superuser in Compliance-Modulen
- Kein Impersonation, keine Abrechnung
- Bestehende Installationen: Superuser-Rolle wird angelegt; Konto `superuser@demo.local` nur bei Erst-Seed

- **Modul DSFA (Datenschutz-Folgenabschätzung):**
  - Entity `DataProtectionImpactAssessment` mit Status, Restrisiko, Ergebnis und Prüffeldern
  - Enums `DpiaStatus`, `DpiaResidualRisk`, `DpiaOutcome` mit deutschen Labels (`DsfaLabels`)
  - 1:n-Beziehung zu `ProcessingActivity` (mehrere DSFA pro Verarbeitungstätigkeit möglich)
  - EF-Migration `AddDataProtectionImpactAssessments` – Tabelle `DataProtectionImpactAssessments`; `EvidenceDocuments.DataProtectionImpactAssessmentId`
  - Blazor-Seiten: Liste (`/dsfa`), Detail (`/dsfa/{Id}`), Anlegen/Bearbeiten (`/dsfa/edit`, nur Admin/Auditor)
  - Menüpunkt „DSFA“ in der Sidebar (nach Verarbeitungstätigkeiten)
  - VVT-Detailseite: DSFA-Bereich mit Kennzahlen, neuester DSFA, Warnhinweise, Link „DSFA anlegen“
  - Dashboard: DSFA gesamt, in Prüfung, hohes/kritisches Restrisiko, überfällige Prüfungen, VVT mit DSFA-Pflicht ohne DSFA
  - Dokumentenmodul: optionale Zuordnung zu DSFA beim Upload
  - Mandantenschutz beim Laden und Speichern (TenantId-Filter, Validierung der Verarbeitungstätigkeit)

### Offene Punkte (DSFA)

- `DpiaRequired` an Verarbeitungstätigkeiten ist ein bool (nur Ja/Nein); Wert „Zu prüfen“ ist im Datenmodell nicht abbildbar
- Kein separater Workflow / Versionierung für DSFA-Freigaben
- Kein Löschen von DSFA-Einträgen über die UI
- Demo-Seed enthält keine Beispiel-DSFA

- **Feature Verknüpfungen (Verarbeitungstätigkeit als zentrale Übersicht):**
  - Erweiterte VVT-Detailseite (`/processing-activities/{Id}`) mit Abschnitten: Grunddaten, Datenschutzbewertung, TOMs, Dienstleister, Dokumente, Maßnahmen, Audit-Antworten, DSFA, Warnhinweise
  - Bearbeitungsseite Verknüpfungen (`/processing-activities/links/{Id}`, nur Admin/Auditor)
  - Service `ProcessingActivityRelationsService` inkl. Mandantenvalidierung beim Speichern
  - Wiederverwendung bestehender Join-Tabellen `ProcessingActivityToms`, `ProcessingActivityServiceProviders`
  - Neu: `ProcessingActivityMeasures`, `ProcessingActivityAuditAnswers`; `EvidenceDocument.ProcessingActivityId`
  - EF-Migration `AddProcessingActivityRelations`
  - Dokumenten-Upload: Zuordnung zu Verarbeitungstätigkeit
  - Maßnahmen-Bearbeitung: Zuordnung zu Verarbeitungstätigkeiten (Admin/Auditor)
  - Dashboard-Kennzahlen zu VVT-Verknüpfungen (ohne TOMs/Dokumente, offene Maßnahmen, DSFA erforderlich, Risiko-Dienstleister)
  - Deutsche Labels: `ComplianceLabels`, `MeasureLabels`
  - Audit-Antworten: Many-to-Many zu VVT (nicht 1:n auf `AuditAnswer`)

### Offene Punkte (Verknüpfungen)

- Download von Nachweisdokumenten in der UI fehlt
- Audit-Antworten können noch nicht direkt in der Antwortmaske (`/audit-runs/answers/{Id}`) VVT zugeordnet werden (nur über Verknüpfungsseite)
- Rolle `RoleInProcessing` bei Dienstleister-Verknüpfung von der VVT-Seite aus nicht editierbar (Standard: Auftragsverarbeiter)
- Maßnahmen-Zuordnung zu VVT erst nach dem ersten Speichern der Maßnahme (nicht beim Anlegen)

- **Modul Dienstleister / Auftragsverarbeiter:**
  - Entity `ServiceProvider` mit AVV-, TOM-Prüfung-, Drittland- und Risikofeldern
  - Enums `ServiceProviderType`, `ServiceProviderStatus`, `ServiceProviderRiskAssessment`, `ThirdCountryTransferLegalBasis`, `ProcessingRole` mit deutschen Labels (`ServiceProviderLabels`)
  - Many-to-Many zu Verarbeitungstätigkeiten über `ProcessingActivityServiceProvider` (Tabelle `ProcessingActivityServiceProviders`, `RoleInProcessing`, `TenantId`)
  - Many-to-Many zu TOMs über `ServiceProviderTom` (Tabelle `ServiceProviderToms`)
  - EF-Migration `AddServiceProviders`
  - Blazor-Seiten: Liste (`/service-providers`), Detail (`/service-providers/{Id}`), Anlegen/Bearbeiten (`/service-providers/edit`, nur Admin/Auditor)
  - Menüpunkt „Dienstleister“ in der Sidebar (nach TOM-Verzeichnis)
  - Dashboard: Kennzahlen Dienstleister gesamt, aktive Auftragsverarbeiter, ohne AVV, Drittlandbezug, hohes/kritisches Risiko, überfällige AVV-Prüfungen
  - Dokumentenmodul: optionale Zuordnung `EvidenceDocument.ServiceProviderId`
  - Demo-Seed: Beispiel-Lohnbuchhalter mit Verknüpfung zu „Personalverwaltung“
  - Mandantenschutz beim Speichern von Verknüpfungen und Dokument-Upload

### Offene Punkte (Dienstleister)

- VVT-Detailansicht zeigt verknüpfte Dienstleister noch nicht an
- Kein Löschen von Dienstleistern über die UI
- Kein Download von Nachweisdokumenten

- **Modul TOM-Verzeichnis:**
  - Entity `Tom` mit Kategorie, Schutzziel, Umsetzungsstatus, Owner, Gültig ab, nächster Prüfung, Nachweis/Referenz, Bemerkungen
  - Enums `TomCategory`, `TomProtectionGoal`, `TomImplementationStatus` mit deutschen UI-Labels (`TomLabels`)
  - Many-to-Many-Verknüpfung zu Verarbeitungstätigkeiten über `ProcessingActivityTom` (Tabelle `ProcessingActivityToms`, `TenantId`, Unique-Index auf TOM + Verarbeitungstätigkeit)
  - EF-Migration `AddToms` – Tabellen `Toms` und `ProcessingActivityToms`
  - Blazor-Seiten: Liste (`/toms`), Detail (`/toms/{Id}`), Anlegen/Bearbeiten (`/toms/edit`, nur Admin/Auditor)
  - Menüpunkt „TOM-Verzeichnis“ in der Sidebar (nach Verarbeitungstätigkeiten)
  - Dashboard: Kennzahlen TOMs gesamt, geplant, nicht umgesetzt, überfällige Prüfungen
  - Demo-Seed: Beispiel-TOM mit Verknüpfung zu „Personalverwaltung“
  - Mandantenschutz beim Speichern von Verknüpfungen (nur Verarbeitungstätigkeiten des eigenen Mandanten)

### Offene Punkte (TOM)

- Nachweisdokumente: keine direkte Zuordnung zu TOMs im Dokumentenmodul (nur Freitext „Nachweis / Referenz“)
- VVT-Detailansicht zeigt verknüpfte TOMs noch nicht an

- **Modul Verzeichnis von Verarbeitungstätigkeiten (VVT):**
  - Entity `ProcessingActivity` mit Feldern gemäß Art. 30 DSGVO (Zweck, Rechtsgrundlage, Kategorien, Empfänger, Drittland, DSFA, Status, Owner, …)
  - Enum `ProcessingActivityStatus` (Entwurf, Aktiv, In Prüfung, Archiviert)
  - EF-Migration `AddProcessingActivities` – Tabelle `ProcessingActivities`, Index auf `TenantId`, Restrict beim Mandanten-Löschen
  - Fix: lange VVT-Textfelder als MySQL `TEXT` statt großer `VARCHAR` (MySQL-Zeilenlimit 65535 Bytes bei utf8mb4)
  - Blazor-Seiten: Liste (`/processing-activities`), Detail (`/processing-activities/{Id}`), Anlegen/Bearbeiten (`/processing-activities/edit`, nur Admin/Auditor)
  - Menüpunkt „Verarbeitungstätigkeiten“ in der Sidebar (angemeldete Benutzer)
  - Demo-Seed: Beispiel-Eintrag „Personalverwaltung“ bei leerer Datenbank
  - Deutsche Status-Labels via `ProcessingActivityLabels`
  - CSS-Klassen für Detailansicht (`dsms-detail-label`, `dsms-detail-value`)

### Erstellt

- Initiale Projektdokumentation erstellt.
- Aktueller Projektstand analysiert.
- Project_Overview.md erstellt.
- Architecture.md erstellt.
- Changelog.md erstellt.

### Vorhandener Projektstand

#### Lösung und Infrastruktur

- Visual-Studio-Lösung `Dsms.sln` mit einem Web-Projekt `Dsms.Web` (.NET 9)
- `docker-compose.yml` für MySQL 8.0 (`dsms_dev`, Port 3306)
- `README.md` mit Schnellstart, Demo-Zugängen und EF-Migrationshinweisen

#### Anwendungsstart

- `Program.cs`: Blazor Server, Identity, MySQL (Pomelo), Pipeline-Konfiguration
- `DatabaseSeeder`: `MigrateAsync`, Rollen-Seed, Demo-Fachdaten bei leerer Datenbank
- EF-Migration `InitialCreate` (Identity + Fachtabellen)

#### Domain und Datenbank

- Entities: `Tenant`, `AuditTemplate`, `AuditQuestion`, `AuditRun`, `AuditAnswer`, `Measure`, `EvidenceDocument`, `EntityBase`
- Enums: `AuditRunStatus`, `MeasureStatus`, `ComplianceLevel`
- Rollenkonstanten: `DsmsRoles` (Admin, Auditor, User)
- `ApplicationDbContext` mit Beziehungen, Längenbegrenzungen und Löschverhalten
- `ApplicationUser` mit `DisplayName` und `TenantId`

#### Services

- `ICurrentUserContext` / `CurrentUserContext`
- `DashboardService` (+ `DashboardSummary`)
- `DocumentStorageService` (Uploads unter `Data/Uploads/{tenantId}/`)

#### Blazor-UI – Fachseiten

- Dashboard (`/`)
- Audit-Vorlagen: Liste, Anlegen/Bearbeiten, Fragen hinzufügen
- Audit-Durchläufe: Liste, Anlegen/Bearbeiten, Antworten erfassen
- Maßnahmen: Liste, Anlegen/Bearbeiten
- Dokumente: Upload (max. 10 MB), Liste mit Metadaten
- Mandanten: Liste, Anlegen/Bearbeiten (nur Admin)
- Benutzer: Liste, Bearbeiten Anzeigename/Mandant/Rolle (nur Admin)

#### Blazor-UI – Layout und Shared

- `MainLayout`, `LoginLayout`, `NavMenu` (rollenbasiert)
- `PageHeader`, `StatusBadge`
- `Routes.razor` mit globaler Autorisierung
- DSMS-eigenes CSS (`dsms-tokens`, `dsms-layout`, `dsms-components`)

#### Identity / Account

- Login (deutsch, DSMS-Layout)
- Logout-Endpunkt, Redirect bei fehlender Autorisierung
- Identity-Standardseiten (Manage, Register, 2FA, Passwort-Reset, …) – größtenteils Template-Stand
- `IdentityNoOpEmailSender` (kein Mailversand)

#### Demo-Daten (Seed)

- Mandant „Demo GmbH“
- Vorlage „DSGVO-Basisaudit“ mit 4 Fragen
- Audit-Durchlauf „Audit Q1 2026“ (InProgress) mit Antworten und 2 Maßnahmen
- Benutzer: admin@demo.local, auditor@demo.local, user@demo.local (Passwort Demo123!)

### Offene Punkte (VVT)

- **Owner-Zuordnung:** Feld `Owner` ist Freitext; keine Auswahl aus Benutzern des Mandanten.
- **DSFA:** `DpiaRequired` ist bool (kein „Zu prüfen“); kein automatisierter Workflow bei Freigabe.
- **Versionierung / Historie:** Keine Änderungshistorie oder Freigabe-Workflow für VVT-Einträge.
- **Export:** Kein PDF/Excel-Export des Verzeichnisses.
- **Löschen:** Keine Löschfunktion für Verarbeitungstätigkeiten in der UI.
- **Bestehende Demo-DBs:** Seed für Beispiel-VVT nur bei komplett leerer Datenbank; bestehende Installationen erhalten nur die neue Tabelle per Migration.

### Offene Punkte

- **Zielgruppe und Produktnutzung:** Keine explizite Definition im Code (intern vs. Kundenprodukt).
- **Roadmap Version 2+:** Nur README-Hinweis „Version 1“, keine Feature-Liste im Repository.
- **Produktions-Seed:** Ob `DatabaseSeeder` mit Demo-Daten in Produktion laufen soll – nicht konfigurierbar.
- **Mandantensicherheit:** Kein zentraler EF-Filter; Vollständigkeit der Mandantenprüfung bei allen IDs/URLs nicht einzeln verifiziert.
- **Admin-Mandantenmodell:** Admin hat eigenen `TenantId` und sieht Compliance-Daten nur dafür; gewünschtes Verhalten für mandantenübergreifende Admins unklar.
- **Benutzer anlegen:** Keine UI zum Erstellen neuer Benutzer (nur Bearbeiten).
- **Registrierung:** `/Account/Register` existiert, ist nicht im Login verlinkt – gewollter Self-Service unklar.
- **Zuweisungen:** `AssignedUserId` auf `AuditRun` und `Measure` ohne UI.
- **Fragenverwaltung:** Fragen können hinzugefügt, aber nicht bearbeitet oder gelöscht werden.
- **Dokumente:** Kein Download in der Fach-UI; Löschen von Dokumenten nicht möglich.
- **Löschen:** Keine Löschfunktion für Fachdatensätze in der UI.
- **Lokalisierung:** Mischung Deutsch/Englisch; Enum-Werte im UI auf Englisch.
- **Tests:** Keine Testprojekte im Repository gefunden.
- **Lizenz:** Im README als „intern“ erwähnt, keine Lizenzdatei.
- **Skalierung Blazor Server:** Session-/SignalR-Anforderungen bei Mehrinstanz-Betrieb nicht dokumentiert im Code.
