# DSMS – Technische Architektur

Diese Dokumentation beschreibt den **aktuellen** technischen Aufbau des Projekts auf Basis des vorhandenen Codes. Sie richtet sich an Entwickler, die am DSMS weiterarbeiten.

## Überblick

```
┌─────────────────────────────────────────────────────────────┐
│  Browser (Blazor Server – SignalR, InteractiveServer)      │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│  Dsms.Web                                                    │
│  ├─ Components/     Razor UI, Layout, Identity-Seiten       │
│  ├─ Services/       Dashboard, Dateispeicher, User-Kontext   │
│  ├─ Data/           DbContext, Identity-User, Seed           │
│  └─ Domain/         Entities, Enums, Rollen                  │
└───────────────────────────┬─────────────────────────────────┘
                            │ EF Core (Pomelo)
┌───────────────────────────▼─────────────────────────────────┐
│  MySQL 8                                                     │
└─────────────────────────────────────────────────────────────┘
         │ Dateisystem: Dsms.Web/Data/Uploads/{tenantId}/
         └──────────────────────────────────────────────────
```

Es gibt **keine** weiteren Projekte in der Solution (kein separates Domain-, API- oder Test-Projekt).

## Projektstruktur und wichtige Ordner

```
c:\code\DS\
├── Dsms.sln
├── docker-compose.yml
├── README.md
├── Project_Overview.md
├── Architecture.md
├── Changelog.md
├── website.md
└── Dsms.Web/
    ├── Program.cs                 # Start, DI, Pipeline
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── Dsms.Web.csproj
    ├── Domain/
    │   ├── Entities/              # Fach-Entities (inkl. ProcessingActivity, Tom, ServiceProvider)
    │   ├── Enums/                 # u. a. ProcessingActivityStatus, Tom*, ServiceProvider*, Dpia*
    │   ├── ProcessingActivityLabels.cs
    │   ├── DsfaLabels.cs
    │   ├── TomLabels.cs
    │   ├── ServiceProviderLabels.cs
    │   └── DsmsRoles.cs
    ├── Data/
    │   ├── ApplicationDbContext.cs
    │   ├── ApplicationUser.cs
    │   └── Seed/DatabaseSeeder.cs
    ├── Services/
    │   ├── ICurrentUserContext.cs / CurrentUserContext.cs
    │   ├── DashboardService.cs
    │   ├── ProcessingActivityRelationsService.cs
    │   └── DocumentStorageService.cs
    ├── Migrations/                # EF Core InitialCreate
    ├── Components/
    │   ├── App.razor, Routes.razor
    │   ├── Layout/                # MainLayout, NavMenu, LoginLayout
    │   ├── Pages/                 # Fachseiten (inkl. ProcessingActivities/, Toms/, ServiceProviders/)
    │   ├── Shared/                # PageHeader, PageHelpButton, StatusBadge, DocumentUploadComponent, …
    │   └── Account/               # Identity UI + Endpunkte
    ├── wwwroot/                   # CSS, Bootstrap, favicon
    ├── Properties/launchSettings.json
    └── Data/Uploads/              # Laufzeit-Uploads (gitignored)
```

## Technologien und Frameworks

| Komponente | Version / Paket |
|------------|-----------------|
| Target Framework | `net9.0` |
| Blazor | Server, `AddInteractiveServerComponents()` |
| EF Core | 9.0.8 (`Microsoft.EntityFrameworkCore.*`) |
| Identity | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 9.0.8 |
| MySQL Provider | `Pomelo.EntityFrameworkCore.MySql` 9.0.0 |
| MySQL Server-Version (konfiguriert) | `8.0.36` in `Program.cs` |
| Email (SMTP) | `MailKit` 4.16.0 |
| Diagnostik (Dev) | `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` |

## Aufbau der Blazor-Server-Anwendung

### Einstieg und Routing

- **`Components/App.razor`:** HTML-Gerüst, Bootstrap, DSMS-CSS, `<Routes />`, Reconnect-Modal.
- **`Components/Routes.razor`:** Zentraler `Router` mit `AuthorizeRouteView` und `MainLayout`; nicht autorisierte Nutzer → `RedirectToLogin`.
- **Rendermodus:** Fachseiten nutzen `@rendermode InteractiveServer` für interaktive Formulare und Event-Handler.

### Layouts

| Layout | Verwendung |
|--------|------------|
| `MainLayout` | Standard-App mit Sidebar (`NavMenu`) |
| `LoginLayout` | Login ohne Sidebar |
| `ManageLayout` | Identity-Kontoverwaltung |

### Shared-Komponenten

- `PageHeader` – Titel und Aktionen
- `StatusBadge` – farbige Status-Anzeige
- `ArchiveViewToggle` – Umschalter Aktiv / Archiv (steuert EF Global Query Filter)
- `ArchiveListActions` – Archivieren / Wiederherstellen in Listen
- `ArchiveConfirmModal` – Bestätigungsdialog mit Abhängigkeitswarnungen
- `ArchivedBadge` – Kennzeichnung archivierter Einträge

### Datenzugriff in der UI

Fachseiten injizieren häufig **`ApplicationDbContext`** direkt (`[Inject]`). Es gibt **keine** separaten Repository- oder Application-Service-Layer für CRUD – außer `DashboardService` und `DocumentStorageService`.

## Datenbankanbindung (MySQL)

### Konfiguration

Connection String-Schlüssel: **`DefaultConnection`**

| Datei | Datenbankname (Beispiel) |
|-------|--------------------------|
| `appsettings.json` | `dsms_dev` (lokaler Fallback, `changeme`) |
| `appsettings.Development.json` | `dsms_dev` |

Laden in `Program.cs`: `builder.Configuration.GetConnectionString("DefaultConnection")`.

**Production (Docker):** Connection String über Environment Variable `ConnectionStrings__DefaultConnection` (in `docker-compose.yml` aus `.env`-Variablen). Siehe [Production_Deployment.md](./Production_Deployment.md).

**Lokale Entwicklung:** `docker compose up -d db` startet nur MySQL; Passwörter in `.env` (aus `.env.example`).

### DbContext

`ApplicationDbContext` erbt von `IdentityDbContext<ApplicationUser>` und registriert:

| DbSet | Entity |
|-------|--------|
| `Licenses` | `License` (Guid-Id, kaufmännische/technische Kundeneinheit; Limit-Felder, `null` = unbegrenzt) |
| `SubscriptionPlans` | `SubscriptionPlan` (Guid-Id, Tarifvorlage; Limit-Felder, `null` = unbegrenzt; Änderungen wirken nicht auf bestehende Lizenzen) |
| `Tenants` | `Tenant` (inkl. `LicenseId`, Verantwortlichen-Stammdaten für VVT: `LegalName`, `Street`, `HouseNumber`, `PostalCode`, `City`, `Phone`, `Email`, `Website`, DSB-Felder `Dpo*`, Löschfelder `IsDeletionRequested`, `DeletionRequestedAt`, `DeletionRequestedByUserId`, `DeletionScheduledAt`) |
| `AuditTemplates` | `AuditTemplate` |
| `AuditQuestions` | `AuditQuestion` |
| `AuditRuns` | `AuditRun` |
| `AuditAnswers` | `AuditAnswer` |
| `Measures` | `Measure` |
| `EvidenceDocuments` | `EvidenceDocument` (Metadaten inkl. `DocumentType`, optional `DocumentCategoryId` → `DocumentCategory`; Dateien im Dateisystem) |
| `DocumentCategories` | `DocumentCategory` (mandantenbezogene, editierbare Kategorien) |
| `DocumentLinks` | `DocumentLink` (Many-to-Many Bezüge Dokument ↔ Fachobjekt) |
| `ProcessingActivities` | `ProcessingActivity` |
| `Toms` | `Tom` |
| `ProcessingActivityToms` | `ProcessingActivityTom` |
| `ServiceProviders` | `ServiceProvider` (Entity; DbSet-Alias wegen DI-Namenskollision) |
| `ProcessingActivityServiceProviders` | `ProcessingActivityServiceProvider` |
| `ServiceProviderToms` | `ServiceProviderTom` |
| `ProcessingActivityMeasures` | `ProcessingActivityMeasure` |
| `ProcessingActivityAuditAnswers` | `ProcessingActivityAuditAnswer` |
| `DataProtectionImpactAssessments` | `DataProtectionImpactAssessment` |
| `PrivacyIncidents` | `PrivacyIncident` |
| `PrivacyIncidentProcessingActivities` | `PrivacyIncidentProcessingActivity` |
| `PrivacyIncidentServiceProviders` | `PrivacyIncidentServiceProvider` |
| `PrivacyIncidentMeasures` | `PrivacyIncidentMeasure` |
| `DataSubjectRequests` | `DataSubjectRequest` |
| `DataSubjectRequestProcessingActivities` | `DataSubjectRequestProcessingActivity` |
| `DataSubjectRequestMeasures` | `DataSubjectRequestMeasure` |
| `DataSubjectRequestServiceProviders` | `DataSubjectRequestServiceProvider` |
| `DataProtectionRoles` | `DataProtectionRole` (organisatorische Datenschutzrollen je Mandant; optional `LinkedUserId`, `ReportsToRoleId`, `DeputyRoleId`) |
| `TrainingTemplates` | `TrainingTemplate` (Schulungsvorlage; mandanteneigen oder global; Karten, Assets, Quiz) |
| `TrainingTemplateSections` | `TrainingTemplateSection` (Markdown-Karte pro Vorlage) |
| `TrainingTemplateAssets` | `TrainingTemplateAsset` (Bilder/Medien, getrennt vom Dokumentenmodul) |
| `TrainingQuestions` | `TrainingQuestion` (Quizfrage) |
| `TrainingQuestionOptions` | `TrainingQuestionOption` (Antwortoption) |
| `Trainings` | `Training` (konkrete Schulungsdurchführung je Mandant) |
| `TrainingParticipants` | `TrainingParticipant` (Stammdaten Schulungsteilnehmer je Mandant; keine App-Benutzer) |
| `TrainingAssignments` | `TrainingAssignment` (Zuweisung Teilnehmer ↔ Schulung, Zugangscode-Hash, Einladungsstatus) |
| `TrainingAssignmentSectionProgress` | `TrainingAssignmentSectionProgress` (Kartenfortschritt pro Zuweisung) |
| `TrainingQuizAttempts` | `TrainingQuizAttempt` (Quiz-Versuch inkl. Score, Passed) |
| `TrainingQuizAnswers` | `TrainingQuizAnswer` (gespeicherte Antworten pro Versuch) |
| `EmailSettings` | `EmailSettings` (plattformweit, kein Mandantenfilter) |
| `EmailTemplates` | `EmailTemplate` (plattformweit, eindeutiger `TemplateKey`) |
| `PageHelpContents` | `PageHelpContent` (plattformweit, eindeutiger `Key`; Hilfetexte für Fachseiten) |

Zusätzlich alle **ASP.NET Identity**-Standardtabellen (`AspNetUsers`, `AspNetRoles`, …).

### Wichtige Modellregeln (`OnModelCreating`)

- **Restrict** beim Löschen von Mandanten, wenn abhängige Fachdaten existieren.
- **Cascade** von Vorlage → Fragen; von Durchlauf → Antworten.
- **Unique Index** auf `(AuditRunId, AuditQuestionId)` für Antworten.
- **SetNull** bei Löschen eines Audit-Durchlaufs für verknüpfte Maßnahmen und Dokumente; **SetNull** bei Löschen einer Audit-Antwort für `Measure.AuditAnswerId`.

### Datenmodell (Fach-Entities)

Archivierbare Module erben von **`ArchivableEntityBase`** (`EntityBase` + `IArchivable` + `ITenantEntity`):

- `IsArchived` (bool, Standard `false`)
- `ArchivedAt` (DateTime?, optional)
- `ArchivedByUserId` (string?, Identity-User-ID)

Betroffene Entities: `ProcessingActivity`, `DataProtectionImpactAssessment`, `Tom`, `ServiceProvider`, `AuditTemplate`, `TrainingTemplate`, `Training`, `AuditRun`, `Measure`, `PrivacyIncident`, `DataSubjectRequest`, `EvidenceDocument`.

**`AuditTemplate`** erbt nur von `ArchivableEntityBase` (nicht `ITenantEntity`): `TenantId` bei eigenen Vorlagen gesetzt, bei globalen Vorlagen (`Official`, `Community`) `null`. Zusätzlich `CommunityStatus` und Prüffelder für Einreichungen. Sichtbarkeit: eigene Mandantenvorlagen + globale `Official`/`Community` über Query Filter. Community-Freigabe erstellt separate globale Kopie; Ursprungsvorlage bleibt beim Mandanten (`CommunityStatus = Approved`).

**`TrainingTemplate`** (Schulungen & Awareness, Vorlagen): erbt von `ArchivableEntityBase` mit nullable `TenantId`. Mandantenvorlage: `TenantId` gesetzt, `IsGlobal = false`. Globale Vorlage: `TenantId = null`, `IsGlobal = true` (Superuser). Community-Workflow: Mandanten-Admins reichen eigene Vorlagen ein (`CommunityStatus = Submitted`); Superuser geben frei (neue globale Kopie mit `IsCommunityTemplate = true`, `SourceTemplateId`) oder lehnen ab. Kind-Entities mit Markdown, Assets und Quiz. Keine Teilnehmer-Tabellen.

**`Training`** (konkrete Schulung/Durchführung): erbt von `ArchivableEntityBase`, Pflicht-`TenantId`, optional `TrainingTemplateId` (V1: Referenz, kein Inhaltssnapshot – siehe TODO in Entity). Felder: Titel, Beschreibung, `TrainingType`, Zielgruppe, `TrainingStatus`, Verantwortlicher (User/Freitext), `AccessCodeValidityDays` (1–90, Standard 14), Notizen. Teilnehmerzahlen werden aus `TrainingAssignments` berechnet (Legacy-Feld `ParticipantCount` in DB, nicht mehr führend in UI). Nachweise über normale `EvidenceDocument` + `DocumentLinks`. Teilnehmerportal: `/schulung/teilnahme` (Zugang), `/schulung/teilnahme/inhalt` (Durchführung).

**`TrainingParticipant`** / **`TrainingAssignment`**: Schulungsteilnehmer sind fachliche Datensätze, keine Identity-Benutzer. `NormalizedEmail` (trim, lowercase) mit eindeutigem Index pro Mandant. **Neuanlage nur zentral** unter `/trainings/participants` (einzeln/Bulk); Schulungsdetail weist nur vorhandene aktive Teilnehmer per Mehrfachauswahl zu. Zuweisung mit E-Mail-Snapshot, 6-stelliger Zugangscode (nur Hash via `PasswordHasher`), Einladungsstatus (`TrainingAssignmentStatus`), Sperrlogik (`FailedAccessAttempts`, `LockedUntilUtc`). Fortschritt in `TrainingAssignmentSectionProgress`; Quiz in `TrainingQuizAttempt`/`TrainingQuizAnswer`. E-Mail-Vorlage `TrainingInvitation`.

Nicht archivierbar (weiterhin `EntityBase`): `Tenant`, `AuditQuestion`, `AuditAnswer`, Join-Tabellen, **`TenantOnboardingTask`** (mandantenbezogene Dashboard-Checkliste „Erste Schritte“; eindeutiger Index `TenantId` + `Key`).

**`License`** ist eine eigenständige Entity mit **`Guid Id`** (nicht `EntityBase`). Enthält Kundendaten, Status, Gültigkeit und Limit-Felder (lizenzweit und pro Mandant). `LicenseNumber` wird automatisch vergeben (Format `LIC-{Jahr}-{Sequenz}`).

**`SubscriptionPlan`** ist eine Tarifvorlage mit **`Guid Id`** (nicht `EntityBase`). Enthält Anzeigenamen, reguläre Preise (`PriceMonthly`, `PriceYearly`), optionale Sonderpreise (`IsPromotionalPriceEnabled`, `PromotionalMonthlyPrice`, `PromotionalYearlyPrice`, `PromotionalBadgeText`), optionale externe Billing-IDs, `IsPublicSignupEnabled` (öffentliche Registrierungsseite), `HasTrainingModule` (Feature-Lock Schulungsmodul) und dieselben Limit-Felder wie `License`. `null` = unbegrenzt. Sonderpreise wirken nur auf die öffentliche Registrierungsanzeige und den effektiven `PendingSignup.Amount`; bestehende Lizenzen werden nicht geändert. Änderungen an Plänen **ändern bestehende Lizenzen nicht**; beim Signup werden Planwerte in eine neue `License` kopiert über `PlanToLicenseMapper` / `ProvisioningService`.

**Plan-to-License Mapping:** `PlanToLicenseService` lädt einen aktiven `SubscriptionPlan`, kopiert alle Limit-Felder 1:1 (`null` bleibt `null`) und setzt `License.PlanName` auf `SubscriptionPlan.DisplayName`. Beispiel: Plan „Pro“ mit `MaxUsersPerTenant = 25` → License „Muster GmbH“ mit `MaxUsersPerTenant = 25`. Wird der Plan später auf 50 geändert, bleibt die bestehende License bei 25.

**Provisioning:** `ProvisioningService.ProvisionCustomerAsync` erstellt in einer Transaktion License (über `PlanToLicenseMapper`), ersten Mandanten (`Tenant.LicenseId`) und Admin-Benutzer (`ApplicationUser.LicenseId`, Rolle `Admin`, `UserTenant`-Zuordnung). Nach dem Commit wird optional eine Passwortvergabe-Mail über `SendProvisioningWelcomeEmailAsync` versendet. Bei E-Mail-Fehler bleiben die angelegten Daten bestehen. Später für Free-Signup und Mollie-Webhook wiederverwendbar; Public Signup und Mollie noch nicht implementiert.

**Rabattcodes:** Plattformweite Entity `DiscountCode` (Guid-Id) mit Typ (`Percentage`, `FixedAmount`, `FreeMonths`), optionaler Plan- und Abrechnungsbindung, Gültigkeitszeitraum und Nutzungslimit. Verwaltung nur für Superuser unter `/platform/discount-codes`. Öffentlicher Signup: Validierung und Preisvorschau über `IDiscountCodeValidationService`; Snapshot in `PendingSignup`. Vor Provisionierung erneute Prüfung via `ValidateForProvisioningAsync`. Einlösung (`CurrentRedemptions++`, `PendingSignup.DiscountRedeemedAt`) erst nach erfolgreichem Provisioning in derselben EF-Transaktion wie License/Tenant/Admin (`ProvisioningService` mit `PendingSignupId`). FreeMonths: `License.ValidUntil` und `NextInvoiceDate` auf kostenlose Laufzeit; `BillingStatus = NotRequired`. Prozent-/Betragsrabatte: Finalbeträge aus Snapshot unverändert.

**Public-Signup:** Öffentliche Route `/signup` (ohne Anmeldung, `PublicSignupLayout`). Lädt aktive Pläne mit `IsActive == true` und `IsPublicSignupEnabled == true` über `GetPublicSignupPlansAsync()`. Tarifauswahl als Karten mit optionaler Sonderpreis-Anzeige (durchgestrichener Regulärpreis, Badge); Formular erst nach Planwahl. Der effektive Betrag (`PendingSignup.Amount`) wird in `PublicSignupService.ResolveBillingAmount` aus dem gewählten Abrechnungszeitraum, ggf. aktivem Sonderpreis und Rabattcode ermittelt. Free- und kostenpflichtige Pläne: `PendingSignupService.CreateForPublicSignupAsync` → Status Provisioning → finale Rabattprüfung → `ProvisioningService` mit `Source = "PublicSignup"` und `PendingSignupId` → Status Provisioned (oder Failed). Lizenzlaufzeit beim automatischen Public Signup: standardmäßig 1 Monat; bei FreeMonths-Rabatt: `heute + FreeMonths`. Erfolgsseite `/signup/success`. Kostenpflichtige Pläne: Rechnungsdaten in `PendingSignup`, `PaymentProvider = ManualInvoice`, Rechnung manuell später. Kein Mollie, kein Auto-Login.

**Paid-Signup (Legacy-Route):** `/signup/paid` leitet auf die vereinheitlichte `/signup`-Seite weiter. Der frühere separate Paid-Signup-Flow (`PaidSignupService`, `Source = "PaidSignup"`) bleibt im Code für Kompatibilität, wird aber nicht mehr über eine eigene Seite angesteuert.

**PendingSignup:** Historie und Zwischenspeicher für Registrierungen. Entity `PendingSignup` speichert Plan-Snapshots, Registrierungsdaten, Rechnungsdaten (Paid), Rechnungsverwaltung (`BillingStatus`, `NextInvoiceDate`, `InvoiceSentAt`, `InvoicePaidAt`, `BillingNote`) und Provisioning-Ergebnis. Beim Public Signup wird der Eintrag erstellt und nach erfolgreicher Provisionierung auf Status Provisioned gesetzt. Superuser-Verwaltung unter `/platform/signups` inkl. manueller Rechnungsaktionen (ohne automatische Lizenzverlängerung). Mollie/Webhook und PDF-Rechnungen noch nicht implementiert.

**LegalAcceptance:** Unveränderlicher Nachweisdatensatz für rechtliche Zustimmungen beim Public Signup. Tabelle `LegalAcceptances` (FK zu `Tenant`, `ApplicationUser`, optional `PendingSignupId`). Version und `EffectiveDate` aus `Legal/legal-documents.json` via `ILegalDocumentService`. Client-IP wird vor Speicherung über `IIpAnonymizationService` anonymisiert (`AnonymizedIpAddress`). Speicherung in derselben EF-Transaktion wie Provisioning (`ILegalAcceptanceService.AddWithinTransactionAsync`). Kein Mandanten-Query-Filter; Anzeige nur für Superuser auf der Registrierungsseite.

Alle anderen Fach-Entities erben von **`EntityBase`** (`Id`, `CreatedAt`, `UpdatedAt`).

```
License (Guid)
 ├── Tenant (LicenseId optional)
 └── ApplicationUser (LicenseId optional, Lizenz-Admins)

Tenant
 ├── AuditTemplate (Tenant | Official | Community) ── AuditQuestion
 │        └── AuditRun ── AuditAnswer (Snapshot + FK AuditQuestion)
 │              └── Measure (optional AuditAnswerId)
 ├── Measure (optional AuditRun, optional AuditAnswerId)
 ├── EvidenceDocument ←──→ Fachobjekte (DocumentLink: AuditRun, Measure, ServiceProvider, ProcessingActivity, Dsfa, PrivacyIncident, DataSubjectRequest, Tom)
 ├── ProcessingActivity (VVT) ←──→ Tom (ProcessingActivityTom)
 │        ←──→ ServiceProvider (ProcessingActivityServiceProvider, Rolle)
 │        ←──→ Measure (ProcessingActivityMeasure)
 │        ←──→ AuditAnswer (ProcessingActivityAuditAnswer)
 │        ── DataProtectionImpactAssessment (1:n DSFA)
 ├── Tom ←──→ ServiceProvider (ServiceProviderTom)
 ├── ServiceProvider
 ├── DataProtectionImpactAssessment
 ├── PrivacyIncident ←──→ ProcessingActivity (PrivacyIncidentProcessingActivity)
 │        ←──→ ServiceProvider (PrivacyIncidentServiceProvider)
 │        ←──→ Measure (PrivacyIncidentMeasure)
 │        ←──→ Tom (PrivacyIncidentTom)
 ├── DataSubjectRequest ←──→ ProcessingActivity (DataSubjectRequestProcessingActivity)
 │        ←──→ Measure (DataSubjectRequestMeasure)
 │        ←──→ ServiceProvider (DataSubjectRequestServiceProvider)
 │        ←──→ EvidenceDocument (DocumentLink)
 └── Tom

ApplicationUser.TenantId → logische Zuordnung (kein EF-FK auf Tenants)
```

**`ApplicationUser`** (Identity):

- `DisplayName`
- `TenantId` (nullable `int`) – ein Mandant pro Benutzer in V1; `null` für Superuser
- `LicenseId` (nullable `Guid`) – Zuordnung zu Kundenlizenz für Lizenz-Admins; Superuser typischerweise `null`
- `IsActive` – deaktivierte Konten können sich nicht anmelden
- `CreatedAt`, `CreatedByUserId` – Metadaten zur Kontoanlage

Felder **`AssignedUserId`** existieren auf `AuditRun` und `Measure`, werden in der UI **nicht** gesetzt.

## Services und Aufgaben

| Service | Registrierung | Aufgabe |
|---------|---------------|---------|
| `ICurrentUserContext` / `CurrentUserContext` | Scoped | User-ID, TenantId, Rollenprüfung via `AuthenticationStateProvider` + `UserManager` |
| `ISupportAccessService` / `SupportAccessService` | Scoped | Supportfreigaben (Mandanten-Admin), Supportmodus (Superuser), Validierung bei jedem Request |
| `ISupportContextService` / `SupportContextService` | Scoped | Session-Persistenz der aktiven Support-Grant-ID |
| `IUserAccessService` / `UserAccessService` | Scoped | Zentrale Berechtigungen: Plattform vs. Mandanten-Fachbereich (`CanAccessTenantBusinessModulesAsync`, `CanManageGlobalAuditTemplatesAsync`, `CanAccessTenantAuditsAsync`, `IsSupportModeAsync`), Mandantenzugriff, Benutzerverwaltung |
| `IUserManagementService` / `UserManagementService` | Scoped | Benutzerliste, Anlegen, Bearbeiten, Deaktivieren inkl. serverseitiger Validierung |
| `DashboardService` | Scoped | Kennzahlen, Statusgruppen (Kritisch/Hinweis/Gut/Neutral) und Listen für Dashboard-Donut-Kacheln (VVT, TOMs, DSFA, Dienstleister, Vorfälle, Maßnahmen, Audits) |
| `PrivacyIncidentRelationsService` | Scoped | Many-to-Many-Sync und Vorfallsnummern-Generierung (`INC-{Jahr}-{Sequenz}` pro Mandant) |
| `DataSubjectRequestRelationsService` | Scoped | Many-to-Many-Sync für Betroffenenanfragen |
| `DataSubjectRequestService` | Scoped | Anonymisierung personenbezogener Falldaten (serverseitig, ohne PII im Auditlog) |
| `ITenantOnboardingService` / `TenantOnboardingService` | Scoped | Mandanten-Checkliste „Erste Schritte“ auf dem Dashboard (Standardaufgaben anlegen, manuelles Abhaken) |
| `IPageHelpContentService` / `PageHelpContentService` | Scoped | Globale Hilfetexte für Fachseiten (Lesen für alle Angemeldeten; Bearbeiten nur Superuser) |
| `ProcessingActivityRelationsService` | Scoped | Laden/Speichern von VVT-Verknüpfungen, Warnhinweise, Mandantenvalidierung |
| `DocumentStorageService` | Scoped | Speichern von Upload-Dateien unter `Storage:UploadPath` (Default: `Data/Uploads/{tenantId}/`) |
| `ITenantExportService` / `TenantExportService` | Scoped | Vollständiger Mandanten-Export als ZIP (JSON-DTOs + Dokumentdateien) |
| `ITenantComplianceInfoService` / `TenantComplianceInfoService` | Scoped | DSGVO-Mandanten-Stammdaten lesen/speichern für aktuellen Mandanten (Admin/Superuser via `/tenant-daten`); TenantId serverseitig |
| `ITenantManagementService` / `TenantManagementService` | Scoped | Plattformweite Mandantenverwaltung (nur Superuser, `/tenants/edit`) |
| `ITenantDeletionService` / `TenantDeletionService` | Scoped | Löschanforderung markieren (`IsDeletionRequested`); Abbrechen nur Superuser |
| `TenantDataEndpoints` | Minimal API | `POST /tenant-daten/export` – ZIP-Download mit serverseitiger Berechtigungsprüfung |
| `DocumentUploadValidation` | Static | Dateityp-, MIME- und Größenprüfung für Uploads (PDF, DOCX, XLSX, JPG, PNG; max. 10 MB) |
| `DocumentLinksService` | Scoped | Many-to-Many-Verknüpfungen (`DocumentLink`); Laden, Setzen, Validierung mandantensicher |
| `DocumentCategoryService` | Scoped | Mandanten-Kategorien für Dokumente (CRUD, Aktiv/Inaktiv, Auditlog) |
| `DataProtectionRoleService` | Scoped | Organisatorische Datenschutzrollen (CRUD, Suche/Filter, Organigramm via `GetOrgChartAsync`, Berichtslinie/Vertretung, Exportdaten, Auditlog) |
| `DocumentFileEndpoints` | Minimal API | `GET /documents/{id}/download` und `/view` – mandantengebunden via EF-Filter |
| `ArchiveViewContextAccessor` | Scoped | Aktiv-/Archivansicht für EF Global Query Filter (`ShowArchivedOnly`) |
| `IArchivingService` / `ArchivingService` | Scoped | Soft Delete: Archivieren, Wiederherstellen, Abhängigkeitswarnungen |
| `IAuditTemplateService` / `AuditTemplateService` | Scoped | Mandantensichere Sichtbarkeit, Bearbeitungsrechte, Archivierung, Community-Workflow und Fragen-CRUD; Frage-Snapshots beim Auditstart |
| `TrainingTemplateAccessService` | Scoped | Berechtigungs-/Sichtbarkeitslogik für Schulungsvorlagen inkl. Community-Einreichung |
| `TrainingTemplateService` | Scoped | CRUD Vorlagen/Karten, Community-Workflow (Einreichen/Freigeben/Ablehnen), Validierung, `CopyTemplateAsync`, Markdown-Asset-Auflösung |
| `TrainingService` | Scoped | CRUD konkrete Schulungen, Erstellung aus Vorlage, Mandanten-/Berechtigungsprüfung |
| `TrainingParticipantService` | Scoped | Stammdaten Schulungsteilnehmer (CRUD, zentraler Bulk-Import, Auswahl-Liste) |
| `TrainingAssignmentService` | Scoped | Zuweisungen Teilnehmer ↔ Schulung (Mehrfachzuweisung, keine Neuanlage), Teilnehmerdetails für Admin |
| `TrainingAccessCodeService` | Scoped | 6-stelliger Code (RNG), Hash/Verify via `PasswordHasher`, Sperrlogik |
| `TrainingInvitationService` | Scoped | Einladungs-E-Mails mit Zugangscode (Status `Invited` nur bei erfolgreichem Versand) |
| `TrainingParticipantSessionService` | Scoped | Signierte HttpOnly-Cookie-Session für Teilnehmerportal (kein Identity-Login) |
| `TrainingParticipantPortalService` | Scoped | Zugang, Kartenfortschritt, Quiz-Auswertung, Abschluss (mit `IgnoreQueryFilters` für öffentlichen Zugang) |
| `TrainingTemplateAssetService` | Scoped | Schulungsasset-Upload, Validierung, Archivierung, Datei-Kopie |
| `TrainingQuestionService` | Scoped | Quiz-Fragen/-Optionen, `ValidateQuizAsync` |
| `TrainingAssetStorageService` | Scoped | Dateisystem unter `Storage:TrainingAssetPath` (Default: `Data/training-assets/`) |
| `TrainingAssetUploadValidation` | Static | Bildtypen PNG/JPG/WEBP/GIF, max. 5 MB, AssetKey-Format |
| `TrainingMarkdownAssetResolver` | Static | Platzhalter `{{asset:…}}` erkennen und in Bild-URLs umsetzen |
| `TrainingAssetEndpoints` | Minimal API | `GET /training-assets/{templateId}/{assetKey}` – autorisiert, mandantensicher |
| `TrainingParticipantAssetEndpoints` | Minimal API | `GET /training-portal-assets/{assetKey}` – nur mit gültiger Teilnehmer-Session |

**Teilnehmerportal (öffentlich):** `/schulung/teilnahme` (Login mit E-Mail + Code), `/schulung/teilnahme/inhalt` (Karten, Quiz, Abschluss). Layout `TrainingParticipantLayout` ohne App-Sidebar. Konfiguration `TrainingAccess` in `appsettings.json` (`SessionLifetimeHours`, Cookie-Name).

**Admin-UI (Schulungsvorlagen):** `/training-templates` (Liste; Mandant oder Superuser ohne TenantContext für globale Vorlagen), `/training-templates/edit` (Neu), `/training-templates/edit/{id}` (Bearbeiten). Community-Einreichung im Mandantenkontext. Superuser-Prüfung: `/platform/training-templates/community` und `/platform/training-templates/community/{id}`. Button „Schulung erstellen“ → `/trainings/edit?templateId={id}` (nur Mandantenkontext).

**Admin-UI (Schulungen):** `/trainings` (Liste), `/trainings/edit` (Neu), `/trainings/edit/{id}` (Bearbeiten), `/trainings/{id}` (Detail mit Tab „Teilnehmer“), `/trainings/participants` (Teilnehmerübersicht). Nachweise: `/documents?prefillTrainingId={id}`.

| `IdentityRedirectManager` | Scoped | Weiterleitungen nach Login/Logout |
| `IdentityRevalidatingAuthenticationStateProvider` | Scoped | Auth-State-Revalidierung für Blazor |
| `IdentityNoOpEmailSender` | Singleton | Identity-Stub (Passwort-Reset etc. noch ohne Workflow-Anbindung) |
| `IEmailService` / `EmailService` | Scoped | Zentraler SMTP-Versand via MailKit |
| `IEmailSettingsService` / `EmailSettingsService` | Scoped | SMTP-Einstellungen CRUD + Testmail + Systembenachrichtigungen (nur Superuser) |
| `IEmailTemplateService` / `EmailTemplateService` | Scoped | Vorlagen CRUD, Vorschau, Testmail aus Vorlage (nur Superuser) |
| `IEmailTemplateRenderer` / `EmailTemplateRenderer` | Scoped | Platzhalterersetzung `{{VariableName}}` |
| `IEmailSecretProtector` / `EmailSecretProtector` | Scoped | SMTP-Passwort-Schutz via ASP.NET Data Protection |
| `IPasswordResetService` / `PasswordResetService` | Scoped | Passwortreset und Willkommens-Einladungen via Identity-Token + `IEmailService`; Rate Limit über `IDistributedCache` |
| `IReminderService` / `ReminderService` | Scoped | Manuelle Erinnerungsvorschau und Sammelversand an Mandanten-Admins (kein Background-Job, keine History) |
| `ILicenseService` / `LicenseService` | Scoped | Lizenz-CRUD, Usage-Counts, Limit-Prüfung und -Durchsetzung |
| `ILicenseFeatureService` / `LicenseFeatureService` | Scoped | Lizenzbasierte Feature-Locks (z. B. `HasTrainingModuleAsync` für Mandant) |
| `ISubscriptionPlanService` / `SubscriptionPlanService` | Scoped | Tarifvorlagen-CRUD (Superuser); `GetPublicSignupPlansAsync()`, `GetPublicSignupPlanByIdAsync()` für Signup |
| `IPlanToLicenseService` / `PlanToLicenseService` | Scoped | Erstellt neue `License` aus `SubscriptionPlan` (Werte werden kopiert, nicht verknüpft) |
| `IProvisioningService` / `ProvisioningService` | Scoped | Provisioniert Kunde: License + Tenant + Admin + Passwortvergabe-Mail |
| `IPublicSignupService` / `PublicSignupService` | Scoped | Öffentlicher Signup mit Tarifauswahl → PendingSignup + direkte Provisionierung; interne Benachrichtigung via `ISignupNotificationService` |
| `ISignupNotificationService` / `SignupNotificationService` | Scoped | Interne E-Mail nach erfolgreichem Public Signup; Empfänger aus `EmailSettings.SystemNotificationRecipientEmail` |
| `IFeedbackService` / `FeedbackService` | Scoped | Benutzer-Feedback per E-Mail an `AppBranding.SupportEmail`; Vorlage `FeedbackMessageToSupport`; keine DB-Persistenz |
| `IPaidSignupService` / `PaidSignupService` | Scoped | Legacy Paid-Signup-Service (nicht mehr über eigene Seite) |
| `IPendingSignupService` / `PendingSignupService` | Scoped | Zwischenspeicher für ausstehende Registrierungen; Public Signup; Rechnungsverwaltung (NextInvoiceDate, BillingStatus-Aktionen) |
| `ILegalDocumentService` / `LegalDocumentService` | Scoped | Liest `Legal/legal-documents.json` und Markdown-Dateien für öffentliche Legal-Seiten |
| `ILegalAcceptanceService` / `LegalAcceptanceService` | Scoped | Speichert und liest Nachweisdatensätze `LegalAcceptance` (Signup-Zustimmung) |
| `ILegalPdfService` / `LegalPdfService` | Scoped | PDF-Generierung aus Legal-Markdown (QuestPDF); AVV-Paket kombiniert AVV+TOM+Unterauftragnehmer |
| `ISignupLegalEmailService` / `SignupLegalEmailService` | Scoped | Bestätigungsmail nach Public Signup mit Legal-PDF-Anhängen |
| `IIpAnonymizationService` / `IpAnonymizationService` | Scoped | Anonymisiert IP-Adressen vor Speicherung in Nachweisdatensätzen (IPv4 /24, IPv6 /64) |
| `ILogService` / `LogService` | Scoped | Zentrales Audit- und Systemprotokoll (`LogEntry`-Tabelle); siehe `Logging.md` |
| `ILogQueryService` / `LogQueryService` | Scoped | Superuser-Protokoll (`GetPlatformLogsAsync`: alle Mandanten, Metadaten-only Details); Admin-Auditlog (`GetAdminAuditLogsAsync`: Lizenz/Mandantenfilter, inkl. Feldänderungen) |
| `ILicenseCreateGuard` / `LicenseCreateGuard` | Scoped | Lizenzlimit-Prüfung mit automatischer Audit-Protokollierung bei Blockierung |

Details und Code-Beispiele: **`Logging.md`** im Projektroot.

## Authentifizierung und Berechtigungen

### Identity-Konfiguration (`Program.cs`)

- `AddIdentityCore<ApplicationUser>` mit Rollen (`IdentityRole`)
- Passwort: min. 8 Zeichen, Ziffer + Kleinbuchstabe erforderlich
- `RequireConfirmedAccount = false` (Demo/Intern)
- Cookies: `AddIdentityCookies()`
- Zentraler Emailversand: `EmailService` (MailKit); Passwortreset nutzt `PasswordResetService` + Vorlage `PasswordReset`
- Identity-Stub `IdentityNoOpEmailSender` bleibt für übrige Identity-UI (Registrierung etc.)
- Passwortreset- und Einladungs-Token: `UserManager.GeneratePasswordResetTokenAsync` / `ResetPasswordAsync`; Lebensdauer 60 Min. (`DataProtectionTokenProviderOptions`); **Dsms.Web** und **Dsms.Provisioning** teilen `DataProtection:ApplicationName` (`DatenschutzCloud`) und denselben Key-Ring (`DataProtection:KeysPath`, lokal typisch `../DataProtection-Keys`)
- Keine eigene `PasswordResetTokens`- oder `UserInvitationTokens`-Tabelle
- Benutzeranlage: `CreateAsync(user)` ohne Passwort, danach `SendWelcomeInvitationAsync` mit Template `WelcomeSetPassword`

### Rollen (`DsmsRoles`)

| Rolle | Typische Rechte (aus `[Authorize]`, NavMenu, `IUserAccessService`) |
|-------|---------------------------------------------------------------------|
| **Superuser** | Plattform-Administration **ohne** Mandantenkontext: Mandanten, Benutzer, Lizenzen, globale Audit-Vorlagen, Supportzugriffsliste (`/platform/support-access`). **Kein** Fachzugriff ohne gültigen, vom Mandanten freigegebenen Supportmodus (`SupportAccessGrant` + Session). Im Supportmodus: Fachmodule **des freigegebenen Mandanten** lesen **und bearbeiten** wie ein Mandanten-Admin bis `ValidUntil` / Widerruf |
| **Admin** | Benutzer im eigenen Mandant; **keine** Mandantenverwaltung; Compliance wie bisher für `TenantId` |
| **Auditor** | Compliance-Inhalte **nur lesen** (Listen, Details, Audit-Antworten im Lesemodus, Dokument-Download); **kein** Anlegen/Bearbeiten/Archivieren; **keine** Benutzerverwaltung |
| **User** | Listen lesen, Detailansichten, Fragen beantworten, Maßnahmen, Dokumente; **kein** Bearbeiten von Stammdaten/Vorlagen (VVT, DSFA, TOMs, Dienstleister, Audit-Durchläufe) |

**Rollen-Konstanten für Autorisierung:** `DsmsRoles.ComplianceEditor` (nur Admin) für Razor-`[Authorize]` auf Plattformseiten; Fach-Bearbeitungsseiten prüfen zur Laufzeit `CanEditComplianceContentAsync()`. `DsmsRoles.ComplianceViewer` (Admin, Auditor, User) für lesenden Zugriff. Zentrale Prüfungen über `IUserAccessService` (u. a. `HasEffectiveTenantAdminPermissionsAsync()` = echter Mandanten-Admin **oder** Superuser im gültigen Supportmodus; `CanAccessTenantBusinessModulesAsync()`, `CanManageGlobalAuditTemplatesAsync()`, `CanAccessTenantAuditsAsync()`, `CanEditComplianceContentAsync()`, `CanEditTenantOperationalContentAsync()`). Schreibaktionen im Supportmodus werden im Auditlog mit `[Supportmodus]` und `SupportAccessGrantId` markiert (`LogService`). Routen-Klassifizierung: `RouteAccessClassifier` (Plattform vs. globale Audit-Vorlagen vs. Fachmodule). UI-Gates: `TenantContextGate` + `BusinessModuleAccessGate` in `MainLayout`. **Datenschutzrollen (Organisation):** Lesen für alle Mandantenrollen; Bearbeiten über `CanManageDataProtectionRolesAsync()` (= effektiver Mandanten-Admin). **Datenschutzvorfälle / Betroffenenanfragen:** Mandanten-Admin/User; Superuser nur im gültigen Supportmodus (Admin-Niveau); nicht Auditor.

**Unterschied Superuser vs. Admin:** Superuser ist mandantenunabhängig (`TenantId` null außer im Supportmodus) und verwaltet die Plattform; Admin ist strikt an einen `TenantId` gebunden und kann unter `/admin/support-access` zeitlich begrenzten Supportzugriff freigeben. Superuser darf Supportfreigaben **nicht** selbst erstellen.

**Version 1 – Mandant pro Benutzer:** `ApplicationUser.TenantId` (nullable). Keine `UserTenants`-Tabelle; Architektur über `IUserAccessService`/`UserManagementService` erweiterbar für Multi-Tenant-Zuordnung und Rollen pro Mandant.

### Mandanten- und Archivfilter

- **Global Query Filters** in `ApplicationDbContext.ApplyTenantQueryFilters()`:
  - Mandant: `TenantId == TenantContextAccessor.CurrentTenantId` (ohne gesetzten Kontext: keine Zeilen)
  - Archiv: `IsArchived == ArchiveViewContextAccessor.ShowArchivedOnly` (Standard: nur aktive Einträge)
- `TenantContextAccessor` ist ein **Scoped-Cache**; die ASP.NET-Session (Key `Dsms.CurrentTenantId`) ist die persistente Quelle
- Pro Request/Circuit: `TenantInitializationMiddleware` und `TenantService.EnsureTenantContextAsync()` laden bei leerem Cache aus der Session (Recovery nach Circuit-Verlust, inkl. Zugriffsprüfung)
- **Mandantenwechsel:** Persistenz in ASP.NET-Session; aus interaktiven Blazor-Komponenten nur per HTTP-Redirect auf `GET /tenant/switch/{tenantId}` (Session ist nach Circuit-Start nicht mehr beschreibbar)
- `ArchiveViewContextAccessor.ShowArchivedOnly` wird über `ArchiveViewToggle` in Listenansichten umgeschaltet
- Archivieren/Wiederherstellen: `IArchivingService` mit `IgnoreQueryFilters()` und expliziter `TenantId`-Prüfung
- Admin-Abfragen (Benutzer-/Mandantenverwaltung): `IgnoreQueryFilters()` wo nötig
- `/tenants`, `/platform/licenses`, `/platform/plans` und `/platform/email/*` nur Superuser; `/users` gefiltert über `UserManagementService`
- `/passwort-vergessen` und `/passwort-zuruecksetzen` öffentlich (ohne Mandantenauswahl)
- `/admin/erinnerungen` nur Superuser ohne Mandantenauswahl (alle aktiven Mandanten)
- Email-Routen sind von der Mandantenauswahl ausgenommen (`TenantService.IsTenantRequiredForRoute`)

### Identity-Endpunkte

`MapAdditionalIdentityEndpoints()` in `IdentityComponentsEndpointRouteBuilderExtensions.cs` – u. a. Logout, externe Logins, Download persönlicher Daten.

## Konfigurationsdateien

| Datei | Inhalt |
|-------|--------|
| `appsettings.json` | Lokaler Connection-String-Fallback (`dsms_dev`/`changeme`), `Application:Version`, `AppBranding` (sichtbarer Produktname, `LogoUrl`, `ShortName`-Fallback, URLs, Tagline), `Storage:UploadPath`, Logging |
| `appsettings.Development.json` | `dsms_dev`, detaillierter EF-Logging |
| `.env.example` / `.env` | Docker-Production-Secrets (nur `.env.example` im Repo) |
| `docker-compose.yml` | MySQL 8 + `dsms-web` + `dsms-provisioning`, Volumes, Environment Variables |
| `Dsms.Web/Dockerfile` | Multi-Stage Production-Image für die Fachanwendung (Port 8080) |
| `Dsms.Provisioning/Dockerfile` | Multi-Stage Production-Image für die Provisioning-App (Port 8080) |
| `Properties/launchSettings.json` | `https://localhost:7245`, `http://localhost:5295` |
| `Dsms.Web.csproj` | `UserSecretsId` für lokale Secrets |

## Startlogik der Anwendung

Reihenfolge in `Program.cs`:

1. Services registrieren (Blazor, Identity, DbContext, Anwendungsservices)
2. `WebApplication` bauen
3. Pipeline: Exception Handler, Statuscode `/not-found`, HTTPS, Static Files, Antiforgery
4. `MapRazorComponents<App>()` mit Interactive Server
5. `MapAdditionalIdentityEndpoints()`
6. **`await DatabaseSeeder.SeedAsync(app.Services)`** vor `app.Run()`

### Entwicklung vs. Produktion

- **Development:** `UseMigrationsEndPoint()` (EF-Migrations-Seite bei Fehlern)
- **Production:** `UseExceptionHandler("/Error")`, HSTS

## Datenbankinitialisierung und Migrationen

### Migrationen

- Migrationen: **`InitialCreate`**, **`AddProcessingActivities`**, **`AddToms`**, **`AddServiceProviders`**, **`AddProcessingActivityRelations`**, **`AddDataProtectionImpactAssessments`**, **`AddArchivingSoftDelete`**, **`AddMeasureAuditAnswerLink`** (optionale Spalte `Measures.AuditAnswerId`), **`AddAuditTemplateTypeAndSnapshots`** (`AuditTemplates.TemplateType`, nullable `TenantId`, Snapshot-Felder auf `AuditAnswers` und `AuditRuns`), **`AddAuditTemplateCommunityFields`** (`CommunityStatus`, Einreichungs- und Prüffelder)
- Snapshot: `Migrations/ApplicationDbContextModelSnapshot.cs`

### Seed (`DatabaseSeeder.SeedAsync`)

1. **`await db.Database.MigrateAsync()`** – wendet ausstehende Migrationen an (Startzeit)
2. Rollen anlegen, falls fehlend (`Superuser`, `Admin`, `Auditor`, `User`)
3. Wenn **kein** Mandant existiert: Demo-Mandant, Fachdaten, vier Demo-Benutzer inkl. `superuser@demo.local` (ohne `TenantId`)

**Hinweis:** Es gibt **keine** separate Prüfung einzelner Tabellen/Spalten außerhalb von EF-Migrationen. Schema-Änderungen erfolgen über neue EF-Migrationen.

### Manuelle Migration (README)

```bash
cd Dsms.Web
dotnet ef migrations add <Name>
dotnet ef database update
```

## Verknüpfungen (Verarbeitungstätigkeit)

| Verknüpfung | Modell | Entscheidung |
|-------------|--------|--------------|
| TOMs | `ProcessingActivityTom` (bestehend) | Many-to-Many, `TenantId` auf Join |
| Dienstleister | `ProcessingActivityServiceProvider` (bestehend) | Many-to-Many; Rolle nur bei Bearbeitung am Dienstleister, VVT-Verknüpfungsseite setzt Standard `DataProcessor` |
| Dokumente | `DocumentLink` | Many-to-Many – ein Dokument kann mehrere Bezüge haben (Audit, Maßnahme, Dienstleister, VVT, DSFA, Vorfall, TOM) |
| Maßnahmen | `ProcessingActivityMeasure` | Many-to-Many – Maßnahme kann mehreren VVT-Einträgen zugeordnet sein |
| Audit-Antworten | `ProcessingActivityAuditAnswer` | Many-to-Many – keine `ProcessingActivityId` auf `AuditAnswer`, da Antworten über Durchlauf mandantenbezogen bleiben |
| DSFA | `DataProtectionImpactAssessment` (1:n zu `ProcessingActivity`) | Pflicht-FK; `TenantId` + Indexe; Cascade beim Löschen der VVT |

**Seiten:** `ProcessingActivities/Detail.razor`, `ProcessingActivities/Links.razor` (`[Authorize(Roles = ComplianceEditor)]`); DSFA: `Dsfa/Index.razor`, `Dsfa/Detail.razor`, `Dsfa/Edit.razor`.

**Routen DSFA:** `/dsfa`, `/dsfa/{Id}`, `/dsfa/edit`, `/dsfa/edit/{Id}` (Bearbeitung nur Admin).

**Mandantenschutz:** Alle Lade- und Speicheroperationen in `ProcessingActivityRelationsService` prüfen `TenantId` der Hauptentität und jeder referenzierten ID.

## Wichtige technische Entscheidungen

| Entscheidung | Begründung (aus Code/Kommentaren) |
|--------------|-----------------------------------|
| Blazor Server | Interaktive UI ohne separates SPA-Frontend |
| Monolith `Dsms.Web` | Einfaches Grundgerüst, eine deploybare Einheit |
| `TenantId` am User | Ein Mandant pro Benutzer in V1; Superuser ohne Mandant; kein Claim |
| `UserAccessService` / `UserManagementService` | Serverseitige SaaS-Berechtigungen statt verteilter Razor-Logik |
| Dateien im Dateisystem | DB nur Metadaten (`StoragePath`) |
| Seed beim Start | Demo/Entwicklung ohne separates Setup-Skript |
| Direkter DbContext in Razor | Schnelle Umsetzung, weniger Schichten |
| Eine Rolle pro User (Admin-UI) | Kommentar „Version 1“ in `Users/Edit.razor` |
| Identity-Standardvorlagen | Viele Account-Seiten unverändert aus Template |

## Abhängigkeiten (NuGet)

Aus `Dsms.Web.csproj`:

- `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` 9.0.8
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 9.0.8
- `Microsoft.EntityFrameworkCore.Design` 9.0.8 (Design-Time)
- `Microsoft.EntityFrameworkCore.Tools` 9.0.8
- `Pomelo.EntityFrameworkCore.MySql` 9.0.0

Weitere Abhängigkeiten kommen transitiv über das ASP.NET Core Web SDK.

## Identity- und Account-Komponenten

Unter `Components/Account/`:

- **Pages:** Login, Register, ForgotPassword, Manage/*, 2FA, …
- **Shared:** RedirectToLogin, ManageNavMenu, StatusMessage, …
- **C#-Hilfen:** `IdentityRedirectManager`, `IdentityRevalidatingAuthenticationStateProvider`, `IdentityNoOpEmailSender`, `IdentityComponentsEndpointRouteBuilderExtensions`

Die Login-Seite ist an das DSMS-Design angepasst; viele Manage-/Register-Seiten nutzen noch **englische Standardtexte**.

## Styling

- Bootstrap (unter `wwwroot/lib/bootstrap/`)
- `wwwroot/css/dsms-tokens.css`, `dsms-layout.css`, `dsms-components.css`
- `wwwroot/app.css`
- Scoped CSS für Layout-Komponenten

## Bekannte technische Schulden und Verbesserungsmöglichkeiten

| Thema | Beschreibung |
|-------|----------------|
| Keine Service-Schicht für CRUD | Logik in Razor-Komponenten; schwerer testbar |
| Kein globaler Mandanten-Query-Filter | Risiko bei neuen Queries ohne `TenantId`-Filter (VVT-Seiten filtern explizit nach `TenantId`) |
| VVT: Owner als Freitext | Keine Verknüpfung zu `ApplicationUser`; keine Benutzerauswahl in der UI |
| VVT: Kein Löschen in der UI | Analog zu anderen Fachmodulen |
| DSFA: `DpiaRequired` nur bool | Kein Wert „Zu prüfen“; kein Freigabe-Workflow |
| DSFA: kein Löschen in der UI | Analog zu anderen Fachmodulen |
| Audit-Antwort ↔ VVT nur über Links-Seite | Direkte Zuordnung in `Answers.razor` bewusst zurückgestellt |
| Kein FK `ApplicationUser` → `Tenant` | Referenzielle Integrität nur über Anwendungslogik |
| UI/API für `AssignedUserId` fehlt | Datenmodell vorbereitet, nicht genutzt |
| TOMs mit direkter Dokumenten-Verknüpfung | Über `DocumentLink` (EntityType `Tom`); zusätzlich Freitext `EvidenceReference` |
| Keine Lösch-UI | Nur DB-Löschregeln definiert |
| Register nicht verlinkt | Identity-Seite existiert, Produktfluss unklar |
| Englische Enum-Labels | UX-Verbesserung durch Display-Namen |
| `DatabaseSeeder` in Produktion | **Noch zu klären:** Seed nur für Dev oder abschaltbar machen |
| Identity-Seiten nicht lokalisiert | Inkonsistente Sprache |
| Keine automatisierten Tests im Repo | **Noch zu klären:** Teststrategie |

## Deployment-Hinweise (aus Code)

- MySQL 8 erforderlich
- Connection String über `ConnectionStrings__DefaultConnection` (Production) oder `appsettings.json` (lokal)
- Upload-Ordner `Data/Uploads` (konfigurierbar via `Storage:UploadPath` / `Storage__UploadPath`) – Docker-Volume `/app/Data/Uploads`
- Data Protection Keys persistent unter gemeinsamem Pfad `../DataProtection-Keys` (Solution-Root lokal; Docker-Volume `/app/DataProtection-Keys` für Dsms.Web und Dsms.Provisioning)
- HTTPS empfohlen (`UseHttpsRedirection`, HSTS in Production) – typisch per Reverse Proxy
- **Annahme:** Einzelinstanz-Deployment; Blazor Server und SignalR erfordern Sticky Sessions bei Skalierung – im Code nicht dokumentiert

## Geplante Marketing-Architektur (separates Projekt)

Briefing und Seitenstruktur: [`website.md`](./website.md).

| Host | Dienst | Status |
|------|--------|--------|
| `datenschutz-cloud.eu` | Öffentliche Marketingseite (`Dsms.Marketing`, geplant) | Nicht im Repo |
| `app.datenschutz-cloud.eu` | SaaS-App `Dsms.Web` | Konfiguriert in `AppBranding:AppUrl` |
| `demo.datenschutz-cloud.eu` | Demo-Instanz `Dsms.Web` | **TODO:** Deployment-Konzept |

**V1 Marketing:** kein Datenbankzugriff; statische Inhalte; Login/Register nur als Links zur App (`/Account/Login`, `/signup`).

**Später optional:** eigene MySQL-Datenbank `dsms_marketing` im gleichen Container – **kein** Zugriff auf Mandantendatenbank, **kein** gemeinsamer `ApplicationDbContext`.

**Domain-Hinweis:** `AppBranding:WebsiteUrl` ist `https://www.datenschutz-cloud.eu` (mit `www`); Marketing-Briefing nutzt `datenschutz-cloud.eu` ohne `www` – Canonical-Domain **TODO**.

## Verwandte Dokumentation

- [Project_Overview.md](./Project_Overview.md) – fachliche Gesamtübersicht
- [README.md](./README.md) – Schnellstart für Entwickler
- [Production_Deployment.md](./Production_Deployment.md) – Docker-Production-Deployment
- [website.md](./website.md) – Marketing-Webseite (Briefing)
- [Changelog.md](./Changelog.md) – Änderungshistorie
