# Provisioning-App – Architektur (Dsms.Provisioning)

> **Stand:** 2026-06-15  
> **Bezug:** [OpenSource_Readiness.md](./OpenSource_Readiness.md), [Provisioning_Extraction.md](./Provisioning_Extraction.md)

## Überblick

`Dsms.Provisioning` wurde als **eigenständiges Blazor-Server-Projekt** in derselben Solution (`Dsms.sln`) wie `Dsms.Web` angelegt.

| Merkmal | Wert |
|---------|------|
| **Zweck** | Private Provisioning-/SaaS-App (Signup, Pläne, Rabattcodes, …) |
| **Repository-Plan** | Späteres Auslagern in privates Repo `datenschutz-cloud-provisioning` |
| **Abhängigkeit zu Dsms.Web** | **Keine** – kein ProjectReference, keine `using Dsms.Web.*` |
| **Datenbank V1** | Gleiche MySQL-Instanz / gleicher Connection String wie Fachanwendung |
| **Migrationen** | Nur `Dsms.Web` wendet EF-Migrationen an; Provisioning-App: `RunMigrationsOnStartup=false` |

## Aktueller Funktionsumfang (Phase C–J)

- [x] Projektstruktur und Konfiguration (`AppUrls`, `Database`, `DataProtection`)
- [x] Identity gegen bestehende `AspNetUsers`-Tabelle
- [x] Superuser-Login (`/Account/Login`)
- [x] Geschützte Plattform-Startseite (`/platform`)
- [x] Logout und AccessDenied
- [x] Data Protection mit konfigurierbarem `ApplicationName` (Token-Kompatibilität mit Fach-App)
- [x] **Phase E:** Tarif-/Planverwaltung (`SubscriptionPlan`)
- [x] **Phase F:** Rabattcode-Verwaltung (`DiscountCode`) inkl. Validierungsservice für späteren Signup
- [x] **Phase G:** PendingSignup-/Registrierungsverwaltung (Plattform-UI, Service, `ValidateForProvisioningAsync`)
- [x] **Phase H:** `ProvisioningService` – manuelle Provisionierung aus PendingSignup
- [x] **Phase I:** Willkommensmail mit Passwortvergabe-Link zur Fachanwendung
- [x] **Phase J:** Öffentlicher Signup (`/signup`) inkl. `PublicSignupService` und End-to-End-Flow
- [x] **Phase K:** Rechtliche Signup-Dokumentation (`LegalAcceptance`, Legal-PDFs, `SignupLegalConfirmation`, öffentliche Legal-Seiten)
- [x] **Phase L:** Kaufmännische Lizenzverwaltung (`License`, Listen-/Detail-/Bearbeiten-UI, Lizenz aus Plan)
- [x] **Phase M:** UI an `Dsms.Web` angeglichen (Design-System, Sidebar, Signup-Look) + Preis-/Decimal-Bugfix
- [x] **Phase N:** Interne Signup-Benachrichtigung (Template `SignupNotification`, Superuser-Einstellungen)
- [x] **Phase O:** Legal-Seiten optisch an `Dsms.Web` angeglichen (`LegalLayout`, `dsms-legal.css`, Markdig, PDF-Download)
- [x] **Phase P:** Registrierungsübersicht und -details an `Dsms.Web` angeglichen (Spalten, Legal, Lizenz, Filter)

## UI und Branding (Phase M)

Die Provisioning-App nutzt dasselbe visuelle Design-System wie `Dsms.Web` (kopiert, nicht referenziert):

| Bereich | Dateien / Komponenten |
|---------|----------------------|
| CSS | `wwwroot/css/dsms-tokens.css`, `dsms-components.css`, `dsms-layout.css`, `wwwroot/app.css` |
| Layout | `MainLayout.razor`, `NavMenu.razor` + `NavMenu.razor.css`, `LoginLayout.razor`, `PublicSignupLayout.razor` |
| Branding | `BrandLogo.razor`, `BrandedPageTitle.razor`, `IApplicationInfoService`, `AppBrandingOptions` |
| Assets | `wwwroot/datenschutz-cloud-logo.png`, `wwwroot/favicon.png` |

Plattform-Seiten verwenden `dsms-card`, `dsms-table`, `dsms-page-header`, `dsms-loading`, `dsms-alert`. Die öffentliche Signup-Seite nutzt dieselben Tarifkarten- und Header-Stile wie in der Fachanwendung.

### Preis-/Decimal-Bugfix (Planverwaltung)

**Ursache:** Eingabefelder wurden mit `InvariantCulture` als `"15.00"` angezeigt, beim Speichern aber zuerst mit `de-DE` geparst. In deutscher Kultur ist `.` Tausendertrennzeichen → `"15.00"` wurde als **15000** interpretiert.

**Behebung:**

- `Services/Shared/MoneyInputHelper.cs` – robustes Parsen (Komma/Punkt, keine Tausendertrennzeichen), Anzeige in `de-DE` ohne Gruppierung
- `Services/Shared/MoneyDisplayHelper.cs` – zentrale Anzeigeformatierung (nur Format, keine Skalierung)
- Verwendung in Plan-Bearbeitung, Rabattcode-Bearbeitung (fester Betrag), Registrierungs-Rechnungsbetrag

**Wichtig:** Bereits falsch gespeicherte Testpreise in der DB (z. B. 15000 statt 15) werden **nicht** automatisch korrigiert – manuell in der Planverwaltung anpassen.

**Testfälle Preise:** `15`, `15,50`, `15.50`, `0`, leer/null, Sonderpreise monatlich/jährlich.

## Noch nicht enthalten

- Docker-Image / Compose
- Audit-Logging (`LogService` – siehe TODOs)
- E-Mail-Einstellungs-UI in Provisioning (SMTP-Verwaltung weiter in Dsms.Web; **interne Benachrichtigung** unter `/platform/email/notifications`)
- Zahlungsanbieter (Mollie/Stripe)
- **Fach-Limitprüfung** (VVT/TOM/DSFA etc.) – bleibt in `Dsms.Web` bis zur Bereinigung der Fachanwendung
- **Lizenz-Löschung** – bewusst nicht implementiert

## Abgrenzung Lizenzverwaltung vs. Fachanwendung

| Aspekt | `Dsms.Provisioning` | `Dsms.Web` (Fachanwendung) |
|--------|----------------------|----------------------------|
| Kaufmännische Lizenzverwaltung | Ja – `/platform/licenses/*` | Enthält noch parallele Seiten (wird später entfernt) |
| Lizenz-Limits pflegen | Ja (Stammdaten, Gültigkeit, Status) | Ja (aktuell noch vorhanden) |
| Limit-Prüfung bei Fachobjekten (VVT, TOM, DSFA, …) | Nein | Ja (`ILicenseService` mit Fachzählungen) |
| Mandantenkontext | Nein | Ja |

**Hinweis:** `Dsms.Web` wird in diesem Schritt **nicht bereinigt**. Die Lizenzseiten in der Fachanwendung bleiben vorerst bestehen, bis Docker/Staging getestet ist.

## Routen (Planverwaltung, Phase E)

| Route | Seite | Zugriff |
|-------|-------|---------|
| `/platform/plans` | Tarifübersicht | Superuser |
| `/platform/plans/edit` | Neuen Plan anlegen | Superuser |
| `/platform/plans/edit/{id}` | Plan bearbeiten | Superuser |
| `/platform/plans/{id}` | Tarifdetails | Superuser |

## Routen (Rabattcodes, Phase F)

| Route | Seite | Zugriff |
|-------|-------|---------|
| `/platform/discount-codes` | Rabattcode-Übersicht | Superuser |
| `/platform/discount-codes/edit` | Neuen Rabattcode anlegen | Superuser |
| `/platform/discount-codes/edit/{id}` | Rabattcode bearbeiten | Superuser |
| `/platform/discount-codes/{id}` | Rabattcode-Details | Superuser |

## Routen (Registrierungen, Phase G)

| Route | Seite | Zugriff |
|-------|-------|---------|
| `/platform/signups` | Registrierungsübersicht | Superuser |
| `/platform/signups/create` | Manuelle Registrierung anlegen | Superuser |
| `/platform/signups/{id}` | Registrierungsdetails | Superuser |

**Registrierungsübersicht (Phase P):** Spalten wie in `Dsms.Web`: Erstellt, Status, Kunde, Mandant, Admin, Plan, Quelle, Betrag, Rechnung, Nächste Rechnung, Legal, Lizenz, Provisioniert, Fehler, Aktionen. Filter: Suche, Status, Tarif, Quelle, Zeitraum (Von/Bis), Rechnungsstatus, Nächste Rechnung bis. LegalAcceptance wird per `ILegalAcceptanceService` geladen (keine DB-Änderung).

**Registrierungsdetails:** Abschnitte Basis, Plan, Kunde/Mandant/Admin, Rabattcode, Rechnung (Signup + aktuelle Abrechnung + Adresse), Payment, Rechtliche Zustimmung, Provisioning-Ergebnis (inkl. Lizenz-Link `/platform/licenses/{id}`). Provisioning-spezifisch: manuelle Provisionierung, Willkommensmail erneut senden. Keine Links zur Fach-Mandanten-/Benutzerverwaltung.

**Bewusst nicht übernommen:** Mail-Status (Welcome/Legal) persistent in Details – nicht in DB gespeichert (TODO). Mandanten-/User-Bearbeitungslinks aus Dsms.Web.

## Routen (Öffentlicher Signup, Phase J)

| Route | Seite | Zugriff |
|-------|-------|---------|
| `/signup` | Tarifwahl, Formular, Rabattcode, Absenden | Anonym (wenn `Features:PublicSignupEnabled=true`) |
| `/signup/success` | Erfolgsmeldung, Willkommens- und Legal-Mail-Status | Anonym |
| `/signup/paid` | Redirect auf `/signup?preferPaid=true` | Anonym |

## Routen (Legal-Dokumente, Phase K)

| Route | Seite / Endpoint | Zugriff |
|-------|------------------|---------|
| `/legal/{route}` | Markdown-Anzeige (AGB, Datenschutz, AVV, TOM, Unterauftragnehmerliste, Impressum) | Anonym |
| `/legal/{route}/pdf` | PDF-Download einzelnes Dokument | Anonym |

**Routen-Aliase:** `datenschutz` und `datenschutzerklaerung`; `unterauftragnehmer` und `unterauftragnehmerliste`.

**Darstellung (Phase O):** `LegalLayout` + `dsms-legal.css` (wie `Dsms.Web`); Markdown via Markdig; doppelte H1 im Dokument wird beim Rendern entfernt, wenn sie dem Seitentitel entspricht.

## Routen (Lizenzverwaltung, Phase L)

| Route | Seite | Zugriff |
|-------|-------|---------|
| `/platform/licenses` | Lizenzübersicht (Suche, Status, „läuft bald ab“) | Superuser |
| `/platform/licenses/{id}` | Lizenzdetails inkl. Nutzung, Mandanten, PendingSignups | Superuser |
| `/platform/licenses/edit` | Neue Lizenz manuell anlegen | Superuser |
| `/platform/licenses/edit/{id}` | Lizenz bearbeiten (Stammdaten, Status, Limits) | Superuser |
| `/platform/licenses/create-from-plan` | Lizenz aus Tarifvorlage erstellen (nur License, kein Tenant/Admin/Mail) | Superuser |

## Services (Phase E–L)

| Service | Namespace | Aufgabe |
|---------|-----------|---------|
| `ISubscriptionPlanService` / `SubscriptionPlanService` | `Dsms.Provisioning.Services.SubscriptionPlans` | CRUD, Liste, öffentliche Signup-Pläne (für spätere Signup-Phase) |
| `SubscriptionPlanDisplayHelper` | `Dsms.Provisioning.Services.SubscriptionPlans` | Preis- und Limitformatierung (de-DE) |
| `IDiscountCodeService` / `DiscountCodeService` | `Dsms.Provisioning.Services.DiscountCodes` | CRUD, Aktivieren/Deaktivieren, Planoptionen |
| `IDiscountCodeValidationService` / `DiscountCodeValidationService` | `Dsms.Provisioning.Services.DiscountCodes` | Signup- und Provisioning-Validierung; **Einlösung nur im ProvisioningService** |
| `DiscountCodeDisplayHelper` | `Dsms.Provisioning.Services.DiscountCodes` | Formatierung und Status-Hinweise |
| `IPendingSignupService` / `PendingSignupService` | `Dsms.Provisioning.Services.PendingSignups` | Listen-/Detailverwaltung, manuelle Anlage, Status- und Billing-Aktionen |
| `PendingSignupDisplayHelper` | `Dsms.Provisioning.Services.PendingSignups` | Status-, Quellen- und Betragsformatierung; `CanProvision` für UI |
| `BillingStatusDisplayHelper` | `Dsms.Provisioning.Services.PendingSignups` | Anzeige von Rechnungsstatus |
| `BillingCycleDisplayHelper` | `Dsms.Provisioning.Services.PendingSignups` | Anzeige von Abrechnungszyklen |
| `EmailValidationHelper` | `Dsms.Provisioning.Services` | E-Mail-Formatprüfung (Create/Update) |
| `IProvisioningService` / `ProvisioningService` | `Dsms.Provisioning.Services.Provisioning` | Provisionierung + Willkommensmail nach Commit |
| `IProvisioningWelcomeEmailService` / `ProvisioningWelcomeEmailService` | `Dsms.Provisioning.Services.Provisioning` | Token erzeugen, Link bauen, Template `WelcomeSetPassword` versenden |
| `IEmailService` / `EmailService` | `Dsms.Provisioning.Services.Email` | SMTP-Versand (MailKit), Template-Rendering |
| `IEmailSettingsService` / `EmailSettingsService` | `Dsms.Provisioning.Services.Email` | SMTP aus `ProvisioningEmail` (Standard) oder DB (`UseDatabaseSettings=true`) |
| `ProvisioningEmailOptions` | `Dsms.Provisioning.Configuration` | Optionale eigene SMTP-Konfiguration der Provisioning-App |
| `IEmailTemplateService` / `EmailTemplateService` | `Dsms.Provisioning.Services.Email` | Email-Vorlagen aus DB laden |
| `IEmailTemplateRenderer` / `EmailTemplateRenderer` | `Dsms.Provisioning.Services.Email` | Platzhalterersetzung `{{Key}}` |
| `IEmailSecretProtector` / `EmailSecretProtector` | `Dsms.Provisioning.Services.Email` | SMTP-Passwort entschlüsseln (Purpose: `Dsms.Email.SmtpPassword.v1`) |
| `LicenseNumberGenerator` | `Dsms.Provisioning.Services.Licenses` | Eindeutige Lizenznummern (`LIC-{Jahr}-{Sequenz}`) |
| `PlanToLicenseMapper` / `PlanToLicenseValidator` | `Dsms.Provisioning.Services.Licenses` | Plan → License-Mapping und Validierung |
| `ILicenseManagementService` / `LicenseManagementService` | `Dsms.Provisioning.Services.Licenses` | Kaufmännische CRUD, Liste, Nutzung (Mandanten/Admins/Benutzer), Superuser-only |
| `IPlanToLicenseService` / `PlanToLicenseService` | `Dsms.Provisioning.Services.Licenses` | Preview und Erstellung einer Lizenz aus Plan |
| `LicenseDtos` / `LicenseLimitHelper` | `Dsms.Provisioning.Services.Licenses` | DTOs und Anzeige-Helfer für Limits und Status |
| `MoneyInputHelper` / `MoneyDisplayHelper` | `Dsms.Provisioning.Services.Shared` | Preis-Parsing und -Anzeige (EUR, de-DE, ohne Skalierung) |
| `IApplicationInfoService` / `ApplicationInfoService` | `Dsms.Provisioning.Services` | Produktname, Version, Logo-URL aus Config |
| `DocumentCategorySeeder` | `Dsms.Provisioning.Data.Seed` | 8 Standard-Dokumentkategorien pro neuem Mandanten |
| `IPublicSignupService` / `PublicSignupService` | `Dsms.Provisioning.Services.Signup` | Öffentliche Pläne, Rabattprüfung, Signup-Absenden, Provisionierung |
| `PublicSignupPricingHelper` | `Dsms.Provisioning.Services.Signup` | Effektivpreise (Sonderpreise) für Signup |
| `FeaturesOptions` | `Dsms.Provisioning.Configuration` | Feature-Flag `PublicSignupEnabled` |
| `ILegalDocumentService` / `LegalDocumentService` | `Dsms.Provisioning.Services.Legal` | Legal-Metadaten und Markdown aus `Legal/` |
| `ILegalPlaceholderService` / `LegalPlaceholderService` | `Dsms.Provisioning.Services.Legal` | Platzhalter in Legal-Markdown (ProviderName aus Config) |
| `ILegalPdfService` / `LegalPdfService` | `Dsms.Provisioning.Services.Legal` | PDF-Erzeugung (QuestPDF, Community-Lizenz) |
| `ILegalAcceptanceService` / `LegalAcceptanceService` | `Dsms.Provisioning.Services.Legal` | Speichern der Signup-Zustimmung in `LegalAcceptances` |
| `IIpAnonymizationService` / `IpAnonymizationService` | `Dsms.Provisioning.Services.Privacy` | IPv4/IPv6-Anonymisierung für LegalAcceptance |
| `ISignupLegalEmailService` / `SignupLegalEmailService` | `Dsms.Provisioning.Services.Signup` | Template `SignupLegalConfirmation` mit PDF-Anhängen |
| `ISignupNotificationService` / `SignupNotificationService` | `Dsms.Provisioning.Services.Signup` | Interne Signup-Benachrichtigung (Template `SignupNotification`) |
| `EmailTemplateSeeder` | `Dsms.Provisioning.Data.Seed` | Legt Provisioning-Templates an (ohne Überschreiben angepasster Vorlagen) |

Superuser-Prüfung erfolgt serverseitig in allen Plattform-Services über `IUserAccessService.EnsureSuperuserAsync()`. Der öffentliche Signup ruft `ProvisionPendingSignupAsync(id, requireSuperuser: false)` auf – nur für PendingSignups mit `Source=PublicSignup`.

## ApplicationDbContext (V1)

Der DbContext erbt von `IdentityDbContext<ApplicationUser>` und mappt:

- Identity-Tabellen (`ApplicationUser`)
- **`SubscriptionPlans`** – Fluent API kompatibel zu `Dsms.Web`
- **`DiscountCodes`** – Fluent API kompatibel zu `Dsms.Web` (Unique Index auf `Code`, FK zu `SubscriptionPlan`)
- **`PendingSignups`** – Fluent API kompatibel zu `Dsms.Web`
- **`Licenses`** – Fluent API kompatibel zu `Dsms.Web`
- **`Tenants`** – Fluent API kompatibel zu `Dsms.Web` (FK zu License)
- **`UserTenants`** – Composite PK `(UserId, TenantId)`
- **`DocumentCategories`** – Fluent API kompatibel zu `Dsms.Web` (FK zu Tenant, Unique `(TenantId, Name)`)
- **`ApplicationUser.LicenseId`** – optional FK zu License

- **`EmailSettings`** / **`EmailTemplates`** – Fluent API kompatibel zu `Dsms.Web`
- **`LegalAcceptances`** – Fluent API kompatibel zu `Dsms.Web` (FK zu Tenant, User, Index auf PendingSignupId)

**Später zu ergänzende Entities:**

- `LogEntry`

Entities werden per **Kopie** aus der Fachanwendung übernommen (Namespaces `Dsms.Provisioning.*`), nicht per ProjectReference.

## Domain-Entities (Phase E–I)

| Entity | Pfad | Tabelle |
|--------|------|---------|
| `SubscriptionPlan` | `Domain/Entities/SubscriptionPlan.cs` | `SubscriptionPlans` |
| `DiscountCode` | `Domain/Entities/DiscountCode.cs` | `DiscountCodes` |
| `PendingSignup` | `Domain/Entities/PendingSignup.cs` | `PendingSignups` |
| `License` | `Domain/Entities/License.cs` | `Licenses` |
| `Tenant` | `Domain/Entities/Tenant.cs` | `Tenants` |
| `UserTenant` | `Domain/Entities/UserTenant.cs` | `UserTenants` |
| `DocumentCategory` | `Domain/Entities/DocumentCategory.cs` | `DocumentCategories` |
| `EmailSettings` | `Domain/Entities/EmailSettings.cs` | `EmailSettings` |
| `EmailTemplate` | `Domain/Entities/EmailTemplate.cs` | `EmailTemplates` |
| `LegalAcceptance` | `Domain/Entities/LegalAcceptance.cs` | `LegalAcceptances` |
| `EntityBase` | `Domain/Entities/EntityBase.cs` | (Basisklasse für int-PK-Entities) |

Hilfsklassen: `BillingCycles`, `BillingStatuses`, `PendingSignupStatuses`, `DocumentCategoryColors`, `EmailTemplateKeys`, `DiscountCodeLabels`, `DiscountCodeType`, `SmtpEncryption`.

## Provisioning- und Willkommensmail-Flow (Phase H/I)

Auslöser: Superuser klickt **Provisionieren** in `/platform/signups/{id}`.

1. Superuser-Prüfung (`IUserAccessService`)
2. PendingSignup laden; Status provisionierbar (`Draft`, `Paid`, `PendingPayment`, `Provisioning`)
3. Admin-E-Mail darf noch nicht existieren; Plan muss aktiv sein
4. Rabattcode via `ValidateForProvisioningAsync` prüfen
5. DB-Transaktion: License → Tenant → Admin → UserTenant → Rabattcode-Einlösung → **LegalAcceptance** (optional) → Dokumentkategorien → PendingSignup `Provisioned`
6. **Commit** (bei Fehler Rollback, Status `Failed`)
7. **Nach Commit:** `IProvisioningWelcomeEmailService.SendWelcomeEmailAsync` – **kein Rollback** bei Mail-Fehler
8. Ergebnis: `EmailSent` / `EmailErrorMessage` in `ProvisionCustomerResultDto`

**Resend:** Bei provisioniertem PendingSignup Button „Willkommensmail erneut senden“ in Details.

## Öffentlicher Signup-Flow (Phase J/K)

End-to-End ohne Superuser-Login:

1. Nutzer öffnet `/signup` (Feature-Flag `Features:PublicSignupEnabled=true`)
2. Aktive Pläne mit `IsPublicSignupEnabled=true` werden angezeigt
3. Formular: Kundendaten, Admin, optional Rechnungsdaten (Paid), Rabattcode, **Pflicht-Legal-Checkboxen** (Links zu `/legal/*`)
4. Honeypot + Doppelabsende-Schutz; serverseitige Validierung der Legal-Zustimmungen
5. `PendingSignupService.CreateForPublicSignupAsync` → Status `Draft`, Snapshots
6. `ProvisioningService.ProvisionPendingSignupAsync(id, requireSuperuser: false, legalAcceptance: …)` → License, Tenant, Admin, **LegalAcceptance** (in Transaktion), Kategorien, Rabatt-Einlösung, Status `Provisioned`
7. Willkommensmail nach Commit (Fehler blockiert Signup nicht)
8. `SignupLegalConfirmation`-Mail mit PDF-Anhängen (AGB, Datenschutz, AVV-Paket) – Fehler blockiert Signup nicht
9. **Interne Signup-Benachrichtigung** über `ISignupNotificationService` (Fehler blockiert Signup nicht)
10. Redirect `/signup/success?planName=…&paid=…&emailSent=…&legalEmailSent=…`

**LegalAcceptance:** Wird innerhalb der Provisioning-Transaktion gespeichert (benötigt `TenantId` und `UserId`). Scheitert die Speicherung, wird die gesamte Provisionierung zurückgerollt. Scheitert die Legal-Bestätigungsmail danach, bleibt die Registrierung erfolgreich (`LegalEmailSent=false`).

**ProviderName:** Aus `AppBranding:ProviderName` (Fallback `"Anbieter"`), nicht hardcoded im Code.

## Interne Signup-Benachrichtigung

Nach erfolgreichem Public Signup bzw. manueller Provisionierung kann eine **interne E-Mail** an einen konfigurierbaren Empfänger gesendet werden (Betrieb/Backoffice).

| Aspekt | Details |
|--------|---------|
| **Zweck** | Interne Info über neue Registrierung inkl. Tarif, Kunde, Lizenz, Mail-Status |
| **Einstellung** | `/platform/email/notifications` (nur Superuser) |
| **Felder (DB `EmailSettings`)** | `SystemNotificationsEnabled`, `SystemNotificationRecipientEmail` |
| **SMTP-Transport** | Über bestehenden `IEmailService` / `ProvisioningEmail` bzw. DB-`EmailSettings` |
| **Template-Key** | `SignupNotification` in Tabelle `EmailTemplates` |
| **Seeding** | `EmailTemplateSeeder` beim App-Start – legt fehlende Vorlagen an, überschreibt keine angepassten |
| **Versandzeitpunkt (Public Signup)** | Nach Willkommens- und Legal-Mail in `PublicSignupService` |
| **Versandzeitpunkt (manuell)** | Nach erfolgreicher manueller Provisionierung in `Signups/Details.razor` |
| **Doppelversand** | Public Signup sendet nur aus `PublicSignupService`; manuelle Provisionierung nur aus Details – kein automatischer Versand in `ProvisioningService` |
| **Fehlerverhalten** | Fehler führen **nicht** zu Rollback; nur Logging / internes Result |
| **Sicherheit** | Keine Tokens, keine Passwortlinks, keine SMTP-Secrets in Mail oder Logs |

**Hinweis zu Dsms.Web:** Die Fachanwendung baut die interne Benachrichtigung bisher als rohes HTML in `SignupNotificationService` (ohne DB-Template). In Provisioning wurde daraus eine template-basierte Lösung mit Key `SignupNotification` abgeleitet.

**Betriebsanforderung:** Template `SignupNotification` muss in `EmailTemplates` vorhanden und aktiv sein (wird standardmäßig beim Start angelegt, falls noch nicht vorhanden).

**Services:** `ISignupNotificationService`, `SignupNotificationService`, `SignupNotificationDiscountHelper`, `SignupNotificationVariablesBuilder`, `SignupNotificationResult`.

**EmailSettingsService:** `GetNotificationSettingsAsync()`, `UpdateNotificationSettingsAsync()` – ändert nur Benachrichtigungsfelder, nicht SMTP-Transport.

## DataProtection-Kompatibilität (Passwort-Token)

Tokens für `GeneratePasswordResetTokenAsync` (Willkommensmail aus Provisioning) werden in **Dsms.Provisioning** erzeugt und in **Dsms.Web** validiert. Beide Apps müssen denselben Key-Ring und denselben Application-Namen verwenden.

| Aspekt | Anforderung |
|--------|-------------|
| **Token-Erzeugung** | `Dsms.Provisioning`: `UserManager.GeneratePasswordResetTokenAsync` |
| **Token-Validierung** | `Dsms.Web`: `/passwort-zuruecksetzen?userId=…&token=…&mode=invite` |
| **Token-Codierung** | `WebEncoders.Base64UrlEncode(UTF8.GetBytes(token))` – identisch in beiden Apps |
| **ApplicationName** | `DataProtection:ApplicationName` muss identisch sein (`DatenschutzCloud`) |
| **Key-Ring** | Gleicher physischer Ordner für beide Apps (lokal: `../DataProtection-Keys` = Solution-Root) |
| **Token-Lifespan** | `DataProtectionTokenProviderOptions.TokenLifespan` (60 Min.) in beiden Apps |
| **SMTP-Secrets** | Purpose `Dsms.Email.SmtpPassword.v1` in `EmailSecretProtector` (nutzt ebenfalls Data Protection) |

### Lokale Entwicklung

Beide Projekte liegen unter `Dsms.Web/` und `Dsms.Provisioning/`. Empfohlener `KeysPath` in **beiden** `appsettings.json`:

```json
"DataProtection": {
  "ApplicationName": "DatenschutzCloud",
  "KeysPath": "../DataProtection-Keys"
}
```

Damit zeigen beide Apps auf `c:\code\DS\DataProtection-Keys` (Solution-Root).

**Nach Umstellung:** Beide Apps neu starten und eine **neue** Willkommensmail auslösen (alte Links wurden mit einem anderen Key-Ring signiert).

Start-Log prüfen (beide Apps):

```
DataProtection-Konfiguration geladen: Environment=Development, ApplicationName=DatenschutzCloud, KeysPath=..., KeyFileCount=...
```

`KeyFileCount` sollte in beiden Apps nach dem ersten Start > 0 sein und derselbe Pfad sein.

**Wenn der Link ungültig ist:**

1. `ApplicationName` in beiden Apps identisch?
2. `KeysPath` zeigt auf **denselben** Ordner (Start-Log vergleichen)?
3. Token abgelaufen (> 60 Min.)?
4. Neue Mail nach Key-Ring-Wechsel angefordert?
5. Beide Apps neu gestartet nach Konfigurationsänderung?

### Docker / Production

Gemeinsames Volume mounten, z. B. `/app/DataProtection-Keys` in beiden Containern (siehe `docker-compose.yml`, Volume `dsms_dataprotection`).

## UI-Komponenten (Phase E–J)

Minimale Shared-Komponenten in `Components/Shared/`:

- `PageHeader.razor` – Seitentitel und Aktionen
- `StatusBadge.razor` – Bootstrap-Badge für Statusanzeigen
- `LegalFooter.razor` – Footer-Links zu allen Legal-Seiten (Signup + Legal-Layout)
- `BrandedPageTitle.razor` – Browser-Titel mit Produktname

Layouts: `PublicSignupLayout.razor` für `/signup` und `/signup/success`; `LegalLayout.razor` für `/legal/*` (beide mit `LegalFooter`).

CSS: `wwwroot/css/dsms-legal.css` (Legal-Seiten, Footer, Markdown-Inhalt).

Legal-Dateien: `Legal/legal-documents.json`, `Legal/current/*.md` (private Anbieter-Rechtstexte).

**Legal-Services:** `ILegalDocumentService` / `LegalDocumentService` (Metadaten, Markdown, HTML); `ILegalPdfService` (PDF); `LegalDocumentEndpoints` für `/legal/{route}/pdf`. Registrierung in `Program.cs`.

## Bekannte TODOs

- **LogService / Audit-Logging:** Provisioning-, E-Mail-, Plan- und PendingSignup-Änderungen werden noch nicht protokolliert.
- **Docker** / Deployment-Image für Provisioning-App (gemeinsames DataProtection-Volume).
- **E-Mail-Einstellungs-UI** in Provisioning (optional; aktuell Verwaltung in Dsms.Web).
- **Zahlungsanbieter** (Mollie/Stripe).

## Testhinweise (Phase I + J)

### Manuelle Provisionierung (Phase I)

1. In `Dsms.Web`: SMTP in EmailSettings konfigurieren, Template `WelcomeSetPassword` aktiv, Testmail optional prüfen.
2. `AppUrls:MainAppBaseUrl` in `Dsms.Provisioning/appsettings.Development.json` auf lokale Fach-App setzen (z. B. `http://localhost:5295`).
3. `DataProtection:ApplicationName` und `KeysPath` müssen mit `Dsms.Web` übereinstimmen.
4. PendingSignup provisionieren → Erfolgsmeldung „Willkommensmail wurde versendet“ oder Warnung.
5. Link in Fach-App öffnen → Passwort setzen → Login als neuer Admin.

### Öffentlicher Signup (Phase J)

1. `Features:PublicSignupEnabled=true` in `appsettings.Development.json` (Default in `appsettings.json` bleibt `false`).
2. Mindestens ein Plan: `IsActive=true`, `IsPublicSignupEnabled=true`.
3. `dotnet run` in `Dsms.Provisioning` → `http://localhost:5296/signup`.
4. Free- oder Paid-Tarif wählen, Formular ausfüllen, Legal-Checkboxen setzen, absenden.
5. Erfolgsseite prüfen; E-Mail mit Passwort-Link zur Fach-App.
6. Optional: `/signup/paid` → bevorzugt kostenpflichtigen Tarif.
7. Bei deaktiviertem Feature: Hinweis „Die öffentliche Registrierung ist derzeit deaktiviert.“

## Identity-Kompatibilität

`ApplicationUser` enthält dieselben Zusatzspalten wie in `Dsms.Web`:

- `DisplayName`, `TenantId`, `LicenseId`, `IsActive`, `CreatedAt`, `CreatedByUserId`

Login ist nur für Benutzer mit Rolle **Superuser** zulässig. Andere Rollen werden abgemeldet und sehen `/Account/AccessDenied`.

Passwort vergessen verweist auf die Fachanwendung: `AppUrls:MainAppBaseUrl + /passwort-vergessen`.

## Konfiguration

| Schlüssel | Zweck |
|-----------|-------|
| `ConnectionStrings:DefaultConnection` | MySQL (gleiche DB wie Dsms.Web) |
| `AppUrls:MainAppBaseUrl` | Fachanwendung (Passwort-Links in Willkommensmail) |
| `AppBranding:ProductName` | App-Name in Email-Vorlagen |
| `AppBranding:SupportEmail` | Support-Adresse in Email-Vorlagen |
| `AppUrls:ProvisioningAppBaseUrl` | Diese App (Canonical-URL) |
| `Database:RunMigrationsOnStartup` | Immer `false` in Production |
| `DataProtection:ApplicationName` | Muss mit Dsms.Web übereinstimmen (`DatenschutzCloud`) |
| `DataProtection:KeysPath` | Persistente Keys (Volume im Deployment) |
| `Features:PublicSignupEnabled` | Öffentlicher Signup (`/signup`); Default `false`, für lokale Tests in Development aktivieren |
| `ProvisioningEmail:UseDatabaseSettings` | `true` = SMTP aus DB-Tabelle `EmailSettings`; Default `false` |
| `ProvisioningEmail:Enabled` | E-Mail-Versand aktivieren (nur bei `UseDatabaseSettings=false`) |
| `ProvisioningEmail:SmtpHost` | SMTP-Server (z. B. über Env `ProvisioningEmail__SmtpHost`) |
| `ProvisioningEmail:SmtpPort` | SMTP-Port (Default `587`) |
| `ProvisioningEmail:Encryption` | `None`, `SslOnConnect`, `StartTls`, `StartTlsWhenAvailable` |
| `ProvisioningEmail:SmtpUsername` / `SmtpPassword` | SMTP-Anmeldung (Passwort nicht loggen; besser per Umgebungsvariable) |
| `ProvisioningEmail:SenderEmail` / `SenderName` | Absender für Registrierungs-Mails |
| `ProvisioningEmail:TimeoutSeconds` | SMTP-Timeout (Default `30`) |

## Troubleshooting: „Emailversand ist deaktiviert“

### Warum steht „Emailversand ist deaktiviert“, obwohl `Enabled=true` gesetzt wurde?

ASP.NET Core lädt Konfiguration in dieser Reihenfolge (spätere Quellen **überschreiben** frühere):

1. `appsettings.json`
2. `appsettings.{Environment}.json` (z. B. `appsettings.Development.json` bei `dotnet run`)
3. Umgebungsvariablen (`ProvisioningEmail__Enabled`, …)
4. User Secrets (Development)

**Häufige Ursachen:**

1. **`appsettings.Development.json` überschreibt `appsettings.json`** – z. B. `Enabled=false` in Development, obwohl in `appsettings.json` `Enabled=true` steht. **Lösung:** Wert in der Development-Datei anpassen, den Block dort entfernen oder Env-Vars setzen.
2. **Umgebungsvariablen überschreiben appsettings** – z. B. `ProvisioningEmail__Enabled=false` in der Shell/IDE.
3. **Anderes Environment als gedacht** – prüfen: `ASPNETCORE_ENVIRONMENT` (Start-Log zeigt `Environment=…`).
4. **Falscher Section-Name** – muss exakt `ProvisioningEmail` heißen (nicht `ProvisioningEmails`, `Email`, …).
5. **`UseDatabaseSettings=true`** – dann gilt `EmailSettings.IsEnabled` aus der DB, nicht `ProvisioningEmail:Enabled`.
6. **App nach Konfigurationsänderung nicht neu gestartet** – `dotnet run` neu starten.
7. **Falsches Projekt gestartet** – `Dsms.Provisioning` (Port 5296), nicht `Dsms.Web`.

### Diagnose

- **Start-Log:** Zeile `ProvisioningEmail-Konfiguration geladen: Environment=…, Enabled=…, …`
- **Erster Mailversand:** Zeile `E-Mail-Versand (aufgelöst): Source=…, ConfigEnabled=…, ResolvedEnabled=…`
- **Plattform-UI:** `/platform` → Karte „E-Mail (SMTP-Diagnose)“ (Superuser)
- **Fehlermeldung** enthält `Source=ProvisioningEmailOptions` oder `Source=DatabaseEmailSettings`

### PowerShell: Test-Konfiguration per Umgebungsvariable

Keine echten Passwörter in Git committen – Platzhalter `CHANGE_ME` verwenden:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ProvisioningEmail__UseDatabaseSettings = "false"
$env:ProvisioningEmail__Enabled = "true"
$env:ProvisioningEmail__SmtpHost = "smtp.example.com"
$env:ProvisioningEmail__SmtpPort = "587"
$env:ProvisioningEmail__Encryption = "StartTls"
$env:ProvisioningEmail__SmtpUsername = "signup@example.com"
$env:ProvisioningEmail__SmtpPassword = "CHANGE_ME"
$env:ProvisioningEmail__SenderEmail = "signup@example.com"
$env:ProvisioningEmail__SenderName = "Datenschutz-Cloud Registrierung"
$env:ProvisioningEmail__TimeoutSeconds = "30"
dotnet run --project Dsms.Provisioning
```

### Mailversand testen

1. Konfiguration setzen (`Enabled=true`, Host, Zugangsdaten per Env-Var)
2. App neu starten und Start-Log prüfen (`Enabled=true`)
3. Public Signup oder manuelle Provisionierung mit Willkommensmail
4. Bei Fehler: Log auf `Source=…` und `ResolvedEnabled=false` prüfen

## Nächste Schritte

1. **Docker / Staging-Setup** – beide Apps gegen gemeinsame DB deployen und End-to-End testen
2. **Audit-Logging** (`LogService`) in Provisioning
3. **Fachanwendung bereinigen** – Lizenzseiten und kaufmännische Verwaltung aus `Dsms.Web` entfernen (nach erfolgreichem Staging-Test)
4. E-Mail-Einstellungs-UI in Provisioning (optional)
5. Zahlungsanbieter (Mollie/Stripe)

## Start (Entwicklung)

```bash
cd Dsms.Provisioning
dotnet run
```

Standard-URL: `http://localhost:5296`

Voraussetzung: MySQL mit bereits durch `Dsms.Web` migrierter Datenbank und vorhandenem Superuser (z. B. `superuser@demo.local` aus Demo-Seeding).

### Connection String (Entwicklung)

`appsettings.Development.json` nutzt **dieselbe** lokale Verbindung wie `Dsms.Web`:

```
User=root; Password=changeme; Database=dsms_dev
```

Bei **Docker Compose** (MySQL-Container) stattdessen in `appsettings.Development.json` anpassen, z. B.:

```
User=dsms_user; Password=<Wert aus .env MYSQL_PASSWORD>; Database=dsms
```

Ohne passenden Connection String schlägt der Login mit `Access denied for user 'dsms_user'@…` fehl.
