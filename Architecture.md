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
    │   ├── Shared/                # PageHeader, StatusBadge, DocumentUploadComponent, DocumentActions, DocumentLinksEditModal
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
| `appsettings.json` | `dsms` |
| `appsettings.Development.json` | `dsms_dev` |

Docker Compose (`docker-compose.yml`) legt `dsms_dev` mit Root-Passwort `changeme` an – muss mit dem Connection String übereinstimmen.

### DbContext

`ApplicationDbContext` erbt von `IdentityDbContext<ApplicationUser>` und registriert:

| DbSet | Entity |
|-------|--------|
| `Tenants` | `Tenant` (inkl. `IsDeletionRequested`, `DeletionRequestedAt`, `DeletionRequestedByUserId`, `DeletionScheduledAt`) |
| `AuditTemplates` | `AuditTemplate` |
| `AuditQuestions` | `AuditQuestion` |
| `AuditRuns` | `AuditRun` |
| `AuditAnswers` | `AuditAnswer` |
| `Measures` | `Measure` |
| `EvidenceDocuments` | `EvidenceDocument` |
| `ProcessingActivities` | `ProcessingActivity` |
| `Toms` | `Tom` |
| `ProcessingActivityToms` | `ProcessingActivityTom` |
| `ServiceProviders` | `ServiceProvider` (Entity; DbSet-Alias wegen DI-Namenskollision) |
| `ProcessingActivityServiceProviders` | `ProcessingActivityServiceProvider` |
| `ServiceProviderToms` | `ServiceProviderTom` |
| `ProcessingActivityMeasures` | `ProcessingActivityMeasure` |
| `ProcessingActivityAuditAnswers` | `ProcessingActivityAuditAnswer` |
| `DataProtectionImpactAssessments` | `DataProtectionImpactAssessment` |

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

Betroffene Entities: `ProcessingActivity`, `DataProtectionImpactAssessment`, `Tom`, `ServiceProvider`, `AuditTemplate`, `AuditRun`, `Measure`, `EvidenceDocument`.

Nicht archivierbar (weiterhin `EntityBase`): `Tenant`, `AuditQuestion`, `AuditAnswer`, Join-Tabellen.

Alle anderen Fach-Entities erben von **`EntityBase`** (`Id`, `CreatedAt`, `UpdatedAt`).

```
Tenant
 ├── AuditTemplate ── AuditQuestion
 │        └── AuditRun ── AuditAnswer (→ AuditQuestion)
 │              ├── Measure (optional AuditAnswerId)
 │              └── EvidenceDocument
 ├── Measure (optional AuditRun, optional AuditAnswerId)
 ├── EvidenceDocument
 ├── ProcessingActivity (VVT) ←──→ Tom (ProcessingActivityTom)
 │        ←──→ ServiceProvider (ProcessingActivityServiceProvider, Rolle)
 │        ←──→ Measure (ProcessingActivityMeasure)
 │        ←──→ AuditAnswer (ProcessingActivityAuditAnswer)
 │        ── DataProtectionImpactAssessment (1:n DSFA)
 │        ── EvidenceDocument (ProcessingActivityId, optional)
 ├── Tom ←──→ ServiceProvider (ServiceProviderTom)
 ├── ServiceProvider ── EvidenceDocument (optional)
 ├── DataProtectionImpactAssessment ── EvidenceDocument (optional)
 └── Tom

ApplicationUser.TenantId → logische Zuordnung (kein EF-FK auf Tenants)
```

**`ApplicationUser`** (Identity):

- `DisplayName`
- `TenantId` (nullable `int`) – ein Mandant pro Benutzer in V1; `null` für Superuser
- `IsActive` – deaktivierte Konten können sich nicht anmelden
- `CreatedAt`, `CreatedByUserId` – Metadaten zur Kontoanlage

Felder **`AssignedUserId`** existieren auf `AuditRun` und `Measure`, werden in der UI **nicht** gesetzt.

## Services und Aufgaben

| Service | Registrierung | Aufgabe |
|---------|---------------|---------|
| `ICurrentUserContext` / `CurrentUserContext` | Scoped | User-ID, TenantId, Rollenprüfung via `AuthenticationStateProvider` + `UserManager` |
| `IUserAccessService` / `UserAccessService` | Scoped | Zentrale Berechtigungen (Superuser vs. Admin, Mandantenzugriff, bearbeitbare Benutzer) |
| `IUserManagementService` / `UserManagementService` | Scoped | Benutzerliste, Anlegen, Bearbeiten, Deaktivieren inkl. serverseitiger Validierung |
| `DashboardService` | Scoped | Kennzahlen und Listen für Dashboard (TOMs, Dienstleister, DSFA, VVT-Verknüpfungen) |
| `ProcessingActivityRelationsService` | Scoped | Laden/Speichern von VVT-Verknüpfungen, Warnhinweise, Mandantenvalidierung |
| `DocumentStorageService` | Scoped | Speichern von Upload-Dateien unter `Data/Uploads/{tenantId}/` (unverändert) |
| `ITenantExportService` / `TenantExportService` | Scoped | Vollständiger Mandanten-Export als ZIP (JSON-DTOs + Dokumentdateien) |
| `ITenantDeletionService` / `TenantDeletionService` | Scoped | Löschanforderung markieren (`IsDeletionRequested`); Abbrechen nur Superuser |
| `TenantDataEndpoints` | Minimal API | `POST /tenant-daten/export` – ZIP-Download mit serverseitiger Berechtigungsprüfung |
| `DocumentUploadValidation` | Static | Dateityp-, MIME- und Größenprüfung für Uploads (PDF, DOCX, XLSX, JPG, PNG; max. 10 MB) |
| `DocumentLinksService` | Scoped | Nachträgliches Aktualisieren der Verknüpfungen (`EvidenceDocument`-FKs) |
| `DocumentFileEndpoints` | Minimal API | `GET /documents/{id}/download` und `/view` – mandantengebunden via EF-Filter |
| `ArchiveViewContextAccessor` | Scoped | Aktiv-/Archivansicht für EF Global Query Filter (`ShowArchivedOnly`) |
| `IArchivingService` / `ArchivingService` | Scoped | Soft Delete: Archivieren, Wiederherstellen, Abhängigkeitswarnungen |
| `IdentityRedirectManager` | Scoped | Weiterleitungen nach Login/Logout |
| `IdentityRevalidatingAuthenticationStateProvider` | Scoped | Auth-State-Revalidierung für Blazor |
| `IdentityNoOpEmailSender` | Singleton | Kein echter E-Mail-Versand |

## Authentifizierung und Berechtigungen

### Identity-Konfiguration (`Program.cs`)

- `AddIdentityCore<ApplicationUser>` mit Rollen (`IdentityRole`)
- Passwort: min. 8 Zeichen, Ziffer + Kleinbuchstabe erforderlich
- `RequireConfirmedAccount = false` (Demo/Intern)
- Cookies: `AddIdentityCookies()`
- Kein E-Mail-Versand: `IdentityNoOpEmailSender`

### Rollen (`DsmsRoles`)

| Rolle | Typische Rechte (aus `[Authorize]`, NavMenu, `IUserAccessService`) |
|-------|---------------------------------------------------------------------|
| **Superuser** | Plattform: alle Mandanten (`/tenants`), alle Benutzer; Compliance nur mit eigenem `TenantId` (meist null) |
| **Admin** | Benutzer im eigenen Mandant; **keine** Mandantenverwaltung; Compliance wie bisher für `TenantId` |
| **Auditor** | Audit-Vorlagen, -Durchläufe, VVT, DSFA, TOMs und Dienstleister anlegen/bearbeiten; **keine** Benutzerverwaltung |
| **User** | Listen lesen, Detailansichten, Fragen beantworten, Maßnahmen, Dokumente; **kein** Bearbeiten von Stammdaten/Vorlagen |

**Unterschied Superuser vs. Admin:** Superuser ist mandantenunabhängig (`TenantId` null) und global; Admin ist strikt an einen `TenantId` gebunden. Beide dürfen Benutzer verwalten, aber nur der Superuser sieht fremde Mandanten und darf Superuser anlegen.

**Version 1 – Mandant pro Benutzer:** `ApplicationUser.TenantId` (nullable). Keine `UserTenants`-Tabelle; Architektur über `IUserAccessService`/`UserManagementService` erweiterbar für Multi-Tenant-Zuordnung und Rollen pro Mandant.

### Mandanten- und Archivfilter

- **Global Query Filters** in `ApplicationDbContext.ApplyTenantQueryFilters()`:
  - Mandant: `TenantId == TenantContextAccessor.CurrentTenantId` (ohne gesetzten Kontext: keine Zeilen)
  - Archiv: `IsArchived == ArchiveViewContextAccessor.ShowArchivedOnly` (Standard: nur aktive Einträge)
- `TenantContextAccessor` wird pro Request/Circuit über `TenantInitializationMiddleware` / `TenantContextService` befüllt
- **Mandantenwechsel:** Persistenz in ASP.NET-Session; aus interaktiven Blazor-Komponenten nur per HTTP-Redirect auf `GET /tenant/switch/{tenantId}` (Session ist nach Circuit-Start nicht mehr beschreibbar)
- `ArchiveViewContextAccessor.ShowArchivedOnly` wird über `ArchiveViewToggle` in Listenansichten umgeschaltet
- Archivieren/Wiederherstellen: `IArchivingService` mit `IgnoreQueryFilters()` und expliziter `TenantId`-Prüfung
- Admin-Abfragen (Benutzer-/Mandantenverwaltung): `IgnoreQueryFilters()` wo nötig
- `/tenants` nur Superuser; `/users` gefiltert über `UserManagementService`

### Identity-Endpunkte

`MapAdditionalIdentityEndpoints()` in `IdentityComponentsEndpointRouteBuilderExtensions.cs` – u. a. Logout, externe Logins, Download persönlicher Daten.

## Konfigurationsdateien

| Datei | Inhalt |
|-------|--------|
| `appsettings.json` | Connection String Produktion/Default, Logging |
| `appsettings.Development.json` | `dsms_dev`, detaillierter EF-Logging |
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

- Migrationen: **`InitialCreate`**, **`AddProcessingActivities`**, **`AddToms`**, **`AddServiceProviders`**, **`AddProcessingActivityRelations`**, **`AddDataProtectionImpactAssessments`**, **`AddArchivingSoftDelete`**, **`AddMeasureAuditAnswerLink`** (optionale Spalte `Measures.AuditAnswerId`)
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
| Dokumente | `EvidenceDocument.ProcessingActivityId` | 1:n (optionaler FK), analog zu Audit/Maßnahme/Dienstleister |
| Maßnahmen | `ProcessingActivityMeasure` | Many-to-Many – Maßnahme kann mehreren VVT-Einträgen zugeordnet sein |
| Audit-Antworten | `ProcessingActivityAuditAnswer` | Many-to-Many – keine `ProcessingActivityId` auf `AuditAnswer`, da Antworten über Durchlauf mandantenbezogen bleiben |
| DSFA | `DataProtectionImpactAssessment` (1:n zu `ProcessingActivity`) | Pflicht-FK; `TenantId` + Indexe; Cascade beim Löschen der VVT |

**Seiten:** `ProcessingActivities/Detail.razor`, `ProcessingActivities/Links.razor` (`[Authorize(Roles = Admin,Auditor)]`); DSFA: `Dsfa/Index.razor`, `Dsfa/Detail.razor`, `Dsfa/Edit.razor`.

**Routen DSFA:** `/dsfa`, `/dsfa/{Id}`, `/dsfa/edit`, `/dsfa/edit/{Id}` (Bearbeitung nur Admin/Auditor).

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
| TOMs ohne direkte Dokumenten-Zuordnung | `EvidenceDocument` hat `AuditRunId`/`MeasureId`/`ServiceProviderId`; TOM nutzt Freitext `EvidenceReference` oder Verknüpfung über Dienstleister |
| Keine Lösch-UI | Nur DB-Löschregeln definiert |
| Register nicht verlinkt | Identity-Seite existiert, Produktfluss unklar |
| Englische Enum-Labels | UX-Verbesserung durch Display-Namen |
| `DatabaseSeeder` in Produktion | **Noch zu klären:** Seed nur für Dev oder abschaltbar machen |
| Identity-Seiten nicht lokalisiert | Inkonsistente Sprache |
| Keine automatisierten Tests im Repo | **Noch zu klären:** Teststrategie |

## Deployment-Hinweise (aus Code)

- MySQL 8 erforderlich
- Connection String und Upload-Ordner `Data/Uploads` beschreibbar
- HTTPS empfohlen (`UseHttpsRedirection`, HSTS in Production)
- **Annahme:** Einzelinstanz-Deployment; Blazor Server und SignalR erfordern Sticky Sessions bei Skalierung – im Code nicht dokumentiert

## Verwandte Dokumentation

- [Project_Overview.md](./Project_Overview.md) – fachliche Gesamtübersicht
- [README.md](./README.md) – Schnellstart für Entwickler
- [Changelog.md](./Changelog.md) – Änderungshistorie
