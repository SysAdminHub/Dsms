# Changelog

Alle nennenswerten Änderungen an diesem Projekt werden in dieser Datei dokumentiert.

## [Unreleased]

### Behoben

- **Dienstleister bearbeiten – Verarbeitungstätigkeiten zuordnen:** `ObjectDisposedException` beim An-/Abhaken von Verarbeitungstätigkeiten behoben. Ursache: dynamisches `InputSelect` mit `ValueExpression` auf Dictionary-Einträge und fehlende `@key` in der Checkbox-Liste destabilisierten den Blazor-Render-Tree. Auswahlzustand läuft jetzt über stabile Zeilenobjekte; TOM-Liste mit `@key` abgesichert; Speichern mit try/catch, Logging und deutscher Fehlermeldung.

- **Legal-Dokumente im Docker-Deployment (Dsms.Web + Dsms.Provisioning):** Markdown-Dateien unter `Legal/current/` werden per `.csproj` als Content in Build- und Publish-Output kopiert (`CopyToOutputDirectory` / `CopyToPublishDirectory`). Ursache: Lokal las die App aus dem Projektverzeichnis (`ContentRootPath`), im Container fehlten die `.md`-Dateien trotz vorhandener `legal-documents.json`. Technisches Logging in `LegalDocumentService` bei fehlenden Metadaten, ungültigen Pfaden oder fehlenden Dateien (App, DocumentKey, FileName, Path, Exists). Provisioning-`Dockerfile` baut wieder `Dsms.Provisioning` statt fälschlich `Dsms.Web`.

### Hinzugefügt

- **Schulungsmodul als Lizenz-Feature:** Neues boolesches Feld `HasTrainingModule` auf `SubscriptionPlan` und `License`. Pläne definieren Standardwerte; Lizenzen sind die wirksame Wahrheit. Provisioning: Planverwaltung, Signup-Anzeige, Lizenzerstellung und manuelle Lizenzbearbeitung inkl. Audit-Log (`LicenseTrainingModuleEnabled`/`LicenseTrainingModuleDisabled`). Fachanwendung: `ILicenseFeatureService`, `TrainingModuleAccessGate`, Upgrade-Hinweis; mandantenspezifische Schulungsfunktionen serverseitig geschützt; globale Superuser-/Community-Vorlagen unverändert zugänglich. Migration `AddHasTrainingModuleToPlansAndLicenses` (Default `true`; bestehende Pläne `free`/`basic` → `false`). Anzeige in `/admin/license` (Enthalten/Nicht enthalten) ergänzt.

### Geändert

- **Signup: Rechnungsadresse optional:** Checkbox „Abweichende Rechnungsadresse“ auf `/signup` (Standard: deaktiviert). Ohne Checkbox werden Rechnungsadressfelder ausgeblendet; beim Absenden übernimmt der Server die Unternehmensadresse. Rechnungs-E-Mail, USt-ID und Bestellnummer bleiben sichtbar. Admin-Detailansicht und interne Benachrichtigungs-E-Mail zeigen „Rechnungsadresse entspricht Unternehmensadresse“, wenn keine abweichende Adresse gewählt wurde. Flag in `MetadataJson` (`HasDifferentBillingAddress`), keine DB-Migration.

- **Kontoeinstellungen (Dsms.Web):** Account-Manage-Bereich (`/Account/Manage/*`) optisch an die Datenschutz-Cloud-UI angepasst (PageHeader, Kartenlayout, Seitennavigation, DSMS-Formularstile). Sichtbare Texte, Statusmeldungen und Validierungshinweise auf Deutsch übersetzt. Betroffen: Profil, E-Mail, Passwort, Zwei-Faktor, Persönliche Daten sowie zugehörige Unterseiten (Authenticator, Wiederherstellungscodes, externe Anmeldungen).

- **Docker-Deployment (zwei Apps):** `docker-compose.yml` startet `db`, `dsms-web` (ehemals `dsms`) und `dsms-provisioning` mit gemeinsamer MySQL-Datenbank und geteiltem Data-Protection-Volume. `.env.example` und `Production_Deployment.md` für Zwei-Container-Betrieb aktualisiert. Neues `Dsms.Provisioning/Dockerfile` (Multi-Stage, Port 8080).

- **DataProtection-Kompatibilität (Dsms.Web + Dsms.Provisioning):** Beide Apps nutzen `ApplicationName=DatenschutzCloud` und gemeinsamen Key-Ring (`../DataProtection-Keys`). Dsms.Web: konfigurierbarer DataProtection-Block, Start-Diagnose-Log. Behebt ungültige Passwort-Links aus Provisioning-Willkommensmails.

### Geändert (Dsms.Provisioning)

- **SMTP-Konfiguration:** Diagnose-Logging beim Start und beim ersten Versand; präzisere Fehlermeldungen mit `Source=ProvisioningEmailOptions` / `Source=DatabaseEmailSettings`. `appsettings.Development.json` überschreibt `ProvisioningEmail` nicht mehr (Ursache für „Emailversand ist deaktiviert“ trotz `Enabled=true` in appsettings.json). SMTP-Diagnose-Karte auf `/platform`.

### Hinzugefügt (Dsms.Provisioning)

- **Eigene SMTP-Konfiguration:** `ProvisioningEmailOptions` – Provisioning-App kann eigene SMTP-Einstellungen aus appsettings/Umgebungsvariablen nutzen (`UseDatabaseSettings=false`, Standard) oder weiterhin DB-`EmailSettings` (`UseDatabaseSettings=true`). `EmailSendingSettings` als aufgelöstes Versandprofil; konfigurierbarer SMTP-Timeout.

### Hinzugefügt (Dsms.Provisioning, Phase K)

- **Rechtliche Signup-Dokumentation:** `LegalAcceptance`-Entity und EF-Mapping; Speicherung innerhalb der Provisioning-Transaktion beim öffentlichen Signup. Legal-Infrastruktur (`Legal/`, `ILegalDocumentService`, `ILegalPdfService`, QuestPDF). Öffentliche Seiten `/legal/{route}` und PDF-Endpunkt `/legal/{route}/pdf`. IP-Anonymisierung (`IIpAnonymizationService`). E-Mail `SignupLegalConfirmation` mit PDF-Anhängen (AGB, Datenschutz, AVV-Paket). Success-Seite mit Status für Willkommens- und Legal-Mail. `ProviderName` aus `AppBranding:ProviderName`.

### Dokumentation

- **Marketing-Webseite (Briefing):** Neue Datei [`website.md`](./website.md) – Konzept, Seitenstruktur, CTAs, Domain-Plan (`datenschutz-cloud.eu`, `app.datenschutz-cloud.eu`, `demo.datenschutz-cloud.eu`), App-Routen für Login/Registrierung, Modulübersicht für Marketing, Design-Tokens, DB-Entscheidung V1 ohne DB. Kein separates Blazor-Projekt in diesem Schritt.
- **Project_Overview.md / Architecture.md:** Abgleich mit aktuellem Stand (Schulungen, Supportzugriff, Plattform-Protokoll, öffentliche Registrierung, Marketing-Architektur); Demo-Zugangsdaten aus Projektübersicht entfernt (Verweis auf README für lokale Entwicklung).

### Hinzugefügt

- **Plattform-Protokoll mandantenübergreifend:** Superuser sehen unter `/platform/logs` alle `LogEntries` aller Mandanten als Metadaten (ohne TenantContext). Fachliche Audit-Events (z. B. `MeasureCreated`, `TomUpdated`, `DocumentLinked`) erscheinen neben Plattform- und Security-Events. Filter erweitert um Mandant Name, Modul, Ergebnis und Supportmodus. Datenschutzhinweis in der UI. Details-Ansicht zeigt nur Audit-Metadaten – keine Feldänderungen (`OldValues`/`NewValues`) und keine Fachinhalte (`EntityName` bei Business-Entities redigiert). `AuditLogPresentationHelper` für Modul-Zuordnung, Metadaten-Parsing und Redaktion. Auditlog für Dokument-Verknüpfungen (`DocumentLinked`/`DocumentUnlinked`) in `DocumentLinksService`.

- **Globale und Community-Schulungsvorlagen:** Mandanten-Admins können eigene Schulungsvorlagen als Community-Vorlage einreichen (Modal mit Datenschutz-Bestätigung). Superuser prüfen Einreichungen unter `/platform/training-templates/community`, geben frei (neue globale Kopie mit `SourceTemplateId`) oder lehnen ab. Superuser verwalten globale Vorlagen ohne Mandantenkontext unter `/training-templates` (Plattform-Navigation). Neue Berechtigungen: `CanManageGlobalTrainingTemplatesAsync`, `CanReviewCommunityTrainingTemplatesAsync`, `CanAccessTenantTrainingsAsync`. Routen `/training-templates` von Mandanten-Fachmodulen getrennt (`RouteAccessClassifier.IsGlobalTrainingTemplateRoute`). Migration `AddTrainingTemplateCommunityFields` (`SourceTemplateId`, `CommunitySubmissionNote`, `CommunityRejectionReason`, `CommunitySubmittedByTenantId`). Auditlog für Einreichung, Freigabe, Ablehnung und Prüfung. Fix: Community-Prüfung ohne parallele DbContext-Zugriffe; globale Vorlagen inkl. Karten/Assets/Quiz ohne Mandantenkontext bearbeitbar (`RequiresUnfilteredQueriesAsync`, `GetTemplateForMutationAsync`).

- **Teilnehmerübersicht (Schulungen):** Seite `/trainings/participants` mit Suche, Filtern, Schulungsstatistiken und Detailansicht inkl. Schulungshistorie. Bearbeitung unter `/trainings/participants/edit/{id}`. `NormalizedEmail` mit eindeutigem Index pro Mandant. Sidebar: „Teilnehmer“ unter Schulungen, „Maßnahmen“ zurück unter Compliance.

- **Teilnehmerportal (Schulungen):** Öffentlicher Zugang unter `/schulung/teilnahme` (E-Mail + 6-stelliger Code, kein App-Login). Schulungsansicht unter `/schulung/teilnahme/inhalt` mit Karten, Markdown/Assets, Fortschritt, Quiz und Teilnahmebestätigung. Signierte HttpOnly-Cookie-Session (2 h). Neue Tabellen `TrainingAssignmentSectionProgress`, `TrainingQuizAttempts`, `TrainingQuizAnswers`. Geschützter Asset-Endpunkt `/training-portal-assets/{assetKey}`. Layout `TrainingParticipantLayout`. Admin-Teilnehmerliste erweitert (Gestartet, Quiz, Score, Detailmodal). Migration `AddTrainingParticipantPortalProgressAndQuiz`.

- **Teilnehmerverwaltung (Schulungen):** Entities `TrainingParticipant` und `TrainingAssignment`, Enum `TrainingAssignmentStatus`. Migration `AddTrainingParticipantsAndAssignments`. 6-stelliger Zugangscode (kryptografisch, nur Hash), Einladungs-E-Mails, Admin-Tab „Teilnehmer“ in Schulungsdetail inkl. Bulk-Import. Keine App-Rolle für Teilnehmer.

### Geändert

- **Superuser als Plattform-Administrator (ohne Fachdatenzugriff):** Superuser arbeiten ohne Mandantenkontext (`TenantId` null, kein Auto-Select, kein Mandantenwechsel). Zentrale Prüfungen in `IUserAccessService` (`CanAccessPlatformAdministrationAsync`, `CanAccessTenantBusinessModulesAsync`, `CanManageGlobalAuditTemplatesAsync`, `CanAccessTenantAuditsAsync`). Routen-Klassifizierung via `RouteAccessClassifier`; serverseitige Absicherung in `BusinessModuleAccessGate` mit Security-Auditlog bei Verweigerung. Navigation: nur Plattform-Funktionen + globale Audit-Vorlagen; Fachmodule ausgeblendet. Plattform-Dashboard auf `/` für Superuser. Globale Audit-Vorlagen weiterhin unter `/audit-templates` (nur Official/Community); mandantenspezifische Audit-Durchläufe gesperrt. Superuser-Bypass in Compliance-Services (Audit, Training) entfernt.

- **Globale Audit-Vorlagen-Fragen (Superuser):** Fragen einer globalen Vorlage können ohne TenantContext verwaltet werden (`LoadTemplateByIdAsync` mit `IgnoreQueryFilters` in `GetTemplateForQuestionMutationAsync`). Audit-Log für Erstellen, Bearbeiten, Löschen globaler Fragen sowie Verweigerung mandantenspezifischer Fragen.

- **Temporärer Supportzugriff (V1):** Mandanten-Admins können unter `/admin/support-access` zeitlich begrenzte Supportfreigaben erteilen (1 h / 4 h / 24 h / 7 Tage). Superuser sehen Freigaben unter `/platform/support-access` und starten den Supportmodus per HTTP-Endpunkt. Session-gebundener Mandantenkontext nur für den freigegebenen Mandanten; Ablauf und Widerruf wirken serverseitig. Entity `SupportAccessGrant`, Services `ISupportAccessService` / `SupportContextService`, Banner und erweiterte Berechtigungsprüfungen in `UserAccessService`.

- **Supportmodus: Bearbeitungsrechte wie Mandanten-Admin:** Im gültigen Supportmodus erhält der Superuser über `HasEffectiveTenantAdminPermissionsAsync()` effektive Admin-Rechte für den freigegebenen Mandanten (Anlegen, Bearbeiten, Archivieren in Fachmodulen). Zentrale Methoden `CanEditComplianceContentAsync`, `CanEditTenantOperationalContentAsync`, `CanManageTenantDataAsync` u. a. nutzen diese Prüfung. Bearbeitungsseiten (VVT, DSFA, TOMs, Dienstleister, Audit-Durchläufe, Verknüpfungen) prüfen Berechtigung zur Laufzeit statt `[Authorize(Roles = ComplianceEditor)]`. Schreibaktionen validieren Supportfreigabe erneut; Auditlog-Einträge mit `[Supportmodus]` und `SupportAccessGrantId`. Keine dauerhafte Mandantenzuordnung; globale Audit-Vorlagen unverändert ohne Supportmodus.

- **Teilnehmerpflege zentralisiert (Schulungen):** Neue Teilnehmer werden ausschließlich unter `/trainings/participants` angelegt (einzeln und per Bulk-Import). Schulungsdetail weist nur noch vorhandene aktive Teilnehmer per Mehrfachauswahl zu; Einladungslogik bleibt in der Schulung. Dublettenprüfung über `NormalizedEmail` mit Link zum bestehenden Teilnehmer.

- **Zugangscode-Gültigkeit pro Schulung:** Feld `Training.AccessCodeValidityDays` (1–90 Tage, Standard 14). Migration `AddTrainingAccessCodeValidityDays`. `appsettings.json`: `DefaultValidityDays`/`MaxValidityDays` nur noch Default/Fallback. UI in Anlegen, Bearbeiten, Detail und Teilnehmerbereich.

- **Schulungen & Awareness (konkrete Schulungen):** Entity `Training` und Enum `TrainingStatus` für dokumentierte Schulungsdurchführungen je Mandant. Migration `AddTrainings`. Schulungen frei anlegen oder aus Vorlagen (`TrainingTemplateId`, V1-Referenz ohne Inhaltssnapshot – TODO für später). Listen-, Bearbeit- und Detailansicht unter `/trainings` mit Suche, Filtern, Schnellfiltern, Status-Badges, Wiederholungsfälligkeit (berechnet), Archivierung. Nachweise über `DocumentLinks` (`DocumentLinkedEntityType.Training`); Auswahl „Schulungen auswählen“ im Dokumentenmodul; Vorauswahl per `?prefillTrainingId=`. Dashboard-Kachel „Schulungen“ (geplant/aktiv, Wiederholung fällig, bald fällig, Nachweis fehlt). Navigation: Bereich „Schulungen“ mit Unterpunkten Schulungen und Schulungsvorlagen. PageHelp-Key `trainings`. Auditlog für Schulungs-CRUD, Status, Nachweis-Verknüpfungen. Tenant-Export `trainings.json`. Demo-Seed „Grundlagenschulung Datenschutz 2026“.

- **Schulungen & Awareness (Admin-UI):** Admin-Oberfläche für Schulungsvorlagen unter `/training-templates` mit Listenansicht (Suche, Filter, Archiv), Erstellen/Bearbeiten mit Tabs (Stammdaten, Karten/Markdown, Bilder/Medien, Quiz, Vorschau). Markdown-Editor mit Vorschau und Asset-Platzhaltern, direkter Bild-Upload als `TrainingTemplateAsset`, Quiz-Editor (Single-/Multiple-Choice), Validierung „Vorlage prüfen“, Kopieren globaler Vorlagen in den Mandanten. Navigation: „Schulungsvorlagen“ im Compliance-Bereich. PageHelp-Key `training-templates`.

- **Schulungen & Awareness (Datenmodell):** Technische Grundlage für Schulungsvorlagen ohne Admin-UI oder Teilnehmerportal. Neue Entities `TrainingTemplate`, `TrainingTemplateSection`, `TrainingTemplateAsset`, `TrainingQuestion`, `TrainingQuestionOption` mit Enums `TrainingType`, `TrainingQuestionType`, `CommunityTemplateStatus`. Mandanteneigene und globale Vorlagen (`TenantId` nullable, `IsGlobal`), Community-Felder vorbereitet, Markdown-Karten mit Asset-Platzhaltern `{{asset:asset-key}}`, Quiz mit Single-/Multiple-Choice, Bestehensgrenze und Quizpflicht am Template. Services: `TrainingTemplateService`, `TrainingTemplateAssetService`, `TrainingQuestionService`, `TrainingTemplateAccessService`, `TrainingAssetStorageService` (Speicher unter `Data/training-assets/`), sicherer Endpoint `/training-assets/{templateId}/{assetKey}`. Migration `AddTrainingTemplates`. Idempotenter globaler Demo-Seed „Grundlagenschulung Datenschutz“. Auditlog-Events für Vorlagen, Karten, Assets und Fragen integriert. `CopyTemplateAsync` inkl. Datei-Kopie der Assets implementiert.

- **Organisation / Datenschutzrollen:** Neues Modul zur Pflege organisatorischer Datenschutzrollen und Zuständigkeiten je Mandant (`DataProtectionRole`). Listen-, Erstell-, Bearbeit- und Detailansicht unter `/organization` mit Suche, Filtern, optionaler App-Benutzer-Verknüpfung, Berichtslinie und Vertretung (Freitext und Rollenreferenz). Kompakte Anzeige in Mandanten-Stammdaten; Export in Tenant-ZIP als `data-protection-roles.json`. Migration `AddDataProtectionRoles`. Demo-Seed für Demo-Mandanten.

- **Datenschutz-Organigramm:** … Organigramm ist Standardansicht unter `/organization`; Listenansicht unter `/organization/list`.

- **Mandantenfähige Dokumentkategorien:** Enum `DocumentCategory` durch Entity `DocumentCategory` mit mandantenspezifischer Verwaltung ersetzt. Standard-Kategorien werden pro Mandant automatisch angelegt; Admins können Kategorien anlegen, bearbeiten, sortieren und deaktivieren. Migration `ReplaceDocumentCategoryEnumWithTenantEntity`. Verwaltung unter `/documents/categories`.

- **Dokumententypen:** `EvidenceDocument` um Pflichtfeld `DocumentType` (Enum) erweitert; farbige Typ-Badges in Liste und Detailseiten. Migration `AddEvidenceDocumentTypeAndCategory`.

- **Betroffenenanfragen:** Neues Modul zur Dokumentation und Bearbeitung von Anfragen betroffener Personen nach DSGVO (Auskunft, Löschung, Berichtigung, Widerspruch u. a.). Listen-, Erstell-, Bearbeit- und Detailansicht mit Fristüberwachung, Verknüpfungen zu VVT, Maßnahmen, Dienstleistern und Dokumenten sowie endgültiger Anonymisierung personenbezogener Falldaten. Migration `AddDataSubjectRequests`.

- **Rechtliche Zustimmung bei Registrierung (Schritt 2):** Drei Pflicht-Checkboxen auf `/signup` … Migrationen `AddLegalAcceptances`, `RenameLegalAcceptanceIpToAnonymized`.

- **Legal-Dokumente (Schritt 3):** PDF-Generierung (QuestPDF) für alle Legal-Dokumente, Download-Button auf `/legal/*` und Endpoint `/legal/{route}/pdf`. Registrierungs-Bestätigungsmail mit PDF-Anhängen (AGB, Datenschutz, kombiniertes AVV/TOM/Unterauftragnehmer-Paket). E-Mail-Vorlage `SignupLegalConfirmation`.

- **Registrierungen:** Editierbare aktuelle Abrechnungsdaten für Superuser (aktuell gültiger Betrag, Währung, Abrechnungszeitraum). Der historische Plan-Snapshot bleibt unverändert. Migration `AddPendingSignupCurrentBillingAmount`.

- **Öffentliche Registrierung / Provisioning:** Rabattcodes werden beim erfolgreichen Public-Signup-Provisioning final eingelöst. Nach erfolgreicher Provisionierung wird der Nutzungszähler erhöht und die Einlösung protokolliert (`DiscountCodeRedeemed`). Migration `AddPendingSignupDiscountRedemptionFields`.

- **Öffentliche Registrierung / Provisioning:** Rabattcodes vom Typ „Kostenlose Monate“ setzen beim Provisioning die Lizenzlaufzeit auf die konfigurierte kostenlose Laufzeit und verschieben das nächste Rechnungsdatum entsprechend.

- **Öffentliche Registrierung:** Rabattcode-Feld ergänzt. Rabattcodes werden serverseitig gegen Plan, Abrechnung, Gültigkeit und Nutzungslimit geprüft und in der Preisvorschau berücksichtigt.

- **PendingSignup:** Speichert angewendete Rabattinformationen als Snapshot für spätere Rechnung und Provisioning. Migration `AddPendingSignupDiscountFields`.

- **Rabattcodes:** Rabattcode-Grundmodell für SaaS-Aktionen ergänzt: Superuser können Rabattcodes mit Typ, Wert, Planbindung, Gültigkeit und Nutzungslimit im Plattformbereich verwalten. Noch keine Anwendung in der öffentlichen Registrierung. Migration `AddDiscountCodes`.

- **Tarif-/Planverwaltung:** Optionale Sonderpreise für monatliche und jährliche Preise ergänzt. Pläne können ein frei editierbares Angebots-Badge anzeigen, z. B. „Limitiertes Angebot“. Migration `AddSubscriptionPlanPromotionalPrices`.

### Geändert

- **Dokumentenverknüpfungen:** Many-to-Many über neue Tabelle `DocumentLinks` statt einzelner FK-Felder auf `EvidenceDocument`. Auswahlmodal mit Checkboxen, Suche und TOM-Unterstützung. Migration `AddDocumentLinksManyToMany` (idempotent für teilweise angewendete DB-Stände). Upload und Bearbeiten mit je einem Auswahl-Modal pro Bezugstyp.

- **Registrierungsdetails (Superuser):** Trennung von Plan-Snapshot, Signup-Betrag/Rabatt und aktueller Abrechnungsbasis. Bei kostenlosen Startmonaten ist der spätere aktuell gültige Folgepreis sichtbar und editierbar.

- **Interne Signup-Benachrichtigung:** Enthält nun Informationen zu verwendeten Rabattcodes, Rabattbetrag, finalem Betrag, kostenlosen Monaten und aktuell gültigem Folgepreis.

- **Registrierungsdetails (Superuser):** Kostenpflichtige Pläne mit kostenlosen Monaten werden transparenter dargestellt: statt dauerhaft „Kostenlos“ werden „Heute zu zahlen“, kostenlose Laufzeit, nächste Rechnung und Folgeabrechnung angezeigt.

- **Öffentliche Registrierung / Provisioning:** Public Signup berücksichtigt gespeicherte Rabatt-Snapshots konsistent. Prozent- und Betragsrabatte behalten die berechneten Finalbeträge; kostenlose Monate werden erst beim erfolgreichen Provisioning final angewendet.

- **Öffentliche Registrierung:** Preisvorschau für Rabattcodes mit kostenlosen Monaten transparenter dargestellt. Neben „Heute zu zahlen“ werden kostenlose Laufzeit und die anschließende Monats- bzw. Jahresrechnung angezeigt.

- **Öffentliche Registrierung:** Public Signup berücksichtigt bei der Betragsermittlung aktive Sonderpreise und angewendete Rabattcodes. Der finale Betrag wird als effektiver Signup-Betrag gespeichert.

- **Öffentliche Registrierung:** Sonderpreis-Ribbon auf Tarifkarten optisch vergrößert und mit auffälligerem Aktionsstil hervorgehoben.

- **Öffentliche Registrierung:** Angebots-Badge für aktive Sonderpreise wird jetzt als schräges Ribbon auf der Tarifkarte dargestellt.

- **Öffentliche Registrierung:** Tarifkarten zeigen aktive Sonderpreise mit durchgestrichenem regulärem Preis, hervorgehobenem Sonderpreis und Angebots-Badge an. Der effektive Registrierungspreis berücksichtigt aktive Sonderpreise.

- **Audit-Durchläufe:** Starten automatisch beim ersten Speichern einer Auditantwort – Status wird auf „Laufend“ gesetzt und `StartedAt` einmalig gesetzt (`AuditRunLifecycle`).

- **Dashboard:** „Erste Schritte“-Box startet nun immer eingeklappt und wird nicht mehr dauerhaft über localStorage geöffnet gehalten.

- **Dashboard (UX):** Dashboard weiter beruhigt: redundante obere Kurzkennzahlen-Zeile entfernt, da die Informationen bereits in den Donut-Kacheln enthalten sind. „Erste Schritte“-Box unter die Dashboard-Kacheln verschoben, auf Desktop kompakter dargestellt und bei vollständig erledigter Einrichtung standardmäßig eingeklappt.

### Hinzugefügt

- **Audit-Durchläufe:** Beim Abschließen wird `CompletedAt` gesetzt (Button „Audit abschließen“ und manueller Statuswechsel); fehlendes `StartedAt` wird beim Abschluss nachgezogen. Antwortseite und Liste zeigen Start- und Abschlussdatum.

- **Audit-Durchläufe:** Audit-Durchläufe können als „Abgeschlossen“ markiert werden (`AuditRunStatus.Completed` im Bearbeiten-Formular und Button „Audit abschließen“ auf der Antwortseite); Listen-, Detail- und Dashboard-Anzeige berücksichtigen den Abschlussstatus mit deutschen Labels (`AuditRunLabels`).

### Behoben

- **Schulungen aus Vorlage:** Vorlagenauswahl beim Anlegen/Bearbeiten von Schulungen ergänzt. Dropdown „Vorlage auswählen“ mit eigenen und globalen aktiven Vorlagen; Felder werden aus Vorlage vorbelegt (mit Bestätigung bei bestehenden Eingaben). Query-Parameter `?templateId=` und Route `/trainings/create`; Button „Schulung erstellen“ in der Vorlagenliste. Serverseitige Validierung von `TrainingTemplateId` beim Speichern. Detailansicht zeigt verknüpfte Vorlage inkl. „Vorlage anzeigen“. Schulungstyp bei verknüpfter Vorlage read-only und serverseitig aus Vorlage erzwungen; ohne Vorlage weiterhin Pflicht-Dropdown. Terminblock aus Anlegen-/Bearbeiten-Maske entfernt; Steuerung über Status für Online-Schulungen. Statusmodell vereinfacht auf Aktiv/Inaktiv/Archiviert; Checkbox „Nachweis fehlt“ und Feld „Teilnehmeranzahl“ aus UI entfernt; Migration `SimplifyTrainingStatus` mappt Legacy-Statuswerte.

- **Feedback-Modal:** Overlay-/Z-Index-Problem behoben, sodass Dashboard-Donut-Charts und andere Seiteninhalte nicht mehr über dem Feedback-Dialog liegen. Das Modal wird nun im MainLayout gerendert (außerhalb der Sidebar) und nutzt erhöhte z-index-Werte.

- **Öffentliche Registrierung / Provisioning:** Rabattcodes werden nicht mehr nur anhand der Preisvorschau betrachtet, sondern vor der Provisionierung erneut serverseitig validiert. Einlösung und `CurrentRedemptions` erfolgen erst nach erfolgreichem Provisioning.

- **Archivansicht Audit-Durchläufe:** Archivierte Audit-Durchläufe werden wieder zuverlässig im Archiv angezeigt. Ursache war ein `Include` auf `AuditTemplate`, dessen Global Query Filter in der Archivansicht nur archivierte Vorlagen zulässt – mandantensichere Abfrage mit `IgnoreQueryFilters()` und separater Vorlagen-Titel-Auflösung wie bei DSFA.

- **Dashboard:** Von vielen einzelnen Kennzahlenkarten auf kompakte Donut-Kacheln je Fachbereich umgestellt. Donut-Kacheln zeigen Gesamtzahl, Statusgruppen (Kritisch/Hinweis/Gut/Neutral), Legende mit Detailkennzahlen und Mouseover-Informationen. Kennzahlen werden weiterhin mandantensicher über den `DashboardService` geladen. Kurzzeile mit wichtigsten Alarmen (offene Maßnahmen, Vorfälle, überfällige Prüfungen) ergänzt. „Erste Schritte“-Box und Listen für offene Maßnahmen/Audits bleiben erhalten.

### Hinzugefügt

- **Dashboard-Kennzahlen:** Fehlende Zähler für Statusgruppen ergänzt – u. a. TOMs umgesetzt, Maßnahmen gesamt, überfällige Maßnahmen, Vorfälle gesamt, abgeschlossene Vorfälle, Audits gesamt, abgeschlossene Audits sowie mutually exclusive Statusgruppen-Zählungen je Fachbereich.
- **Dashboard-Komponenten:** Wiederverwendbare Blazor-Komponente `DonutDashboardCard` mit SVG-Donut-Diagramm; DTOs `DashboardDonutSegment`, `DashboardLegendItem`, `DashboardStatusGroupCounts`.

### Geändert

- **Datenschutzvorfälle – Maßnahmen-UI:** Abschnitt „Maßnahmen / Abschluss“ mit Freitextfeldern (Sofort-/Abhilfe-/Präventionsmaßnahmen, Abschlusszusammenfassung, Abgeschlossen am) aus Bearbeitungs- und Detailseite entfernt. Maßnahmen werden nur noch über verknüpfte Maßnahmen im Modul Maßnahmen dokumentiert. Datenbankfelder bleiben erhalten; PageHelp-Standardtext angepasst.

### Hinzugefügt

- **Datenschutzvorfälle – Maßnahme aus Vorfall (Audit-Muster):** Button auf Detail- und Bearbeitungsseite (`measures/edit?privacyIncidentId=`), Vorausfüllung und automatische `PrivacyIncidentMeasures`-Verknüpfung analog Audit-Antworten; Rücknavigation zum Vorfall; Checkboxen für bestehende Maßnahmen bleiben erhalten.

- **Datenschutzvorfälle – TOM-Verknüpfung und Maßnahmen aus Vorfall:** Many-to-Many `PrivacyIncidentToms`; TOM-Auswahl auf Bearbeitungsseite; Anzeige auf Detailseite; Button „Maßnahme aus Vorfall erstellen“ mit Vorausfüllung über `measures/edit?privacyIncidentId=` und automatischer Verknüpfung; Tenant-Export `LinkedTomIds`. Migration `AddPrivacyIncidentToms`.

- **Modul Datenschutzvorfälle (`/incidents`):** Mandantenbezogenes Vorfallregister mit Liste, Detail, Anlegen (Admin/Superuser) und Bearbeiten (Admin/Superuser/User). Entity `PrivacyIncident` inkl. Meldebewertung, Risiko, Verknüpfungen zu VVT/Dienstleistern/Maßnahmen und Dokumenten (`EvidenceDocument.PrivacyIncidentId`). Rollenlogik über `CanCreatePrivacyIncidentsAsync` / `CanEditPrivacyIncidentsAsync`. Sidebar-Menüpunkt „Vorfälle“, PageHelp `privacy-incidents`, Dashboard-Kennzahlen, Tenant-Export `privacy-incidents.json`, Compliance-Auditlog. Migration `AddPrivacyIncidents`.

### Geändert

- **Sidebar-Branding:** Logo-Bild und DC-Kürzel in der Sidebar entfernt; oben links nur noch der Produktname „Datenschutz-Cloud“. Logo über `AppBranding:LogoUrl` bleibt auf Login-/Auth-Seiten erhalten.

- **Logo-Branding in der UI:** Sichtbares Kürzel „DC“ (Login, Passwort-Seiten, Registrierung) wird durch das Logo-Bild `/datenschutz-cloud-logo.png` ersetzt, sofern `AppBranding:LogoUrl` gesetzt ist. Fallback auf `ShortName` bleibt erhalten. Wiederverwendbare Komponente `BrandLogo`. Favicon und technische Namen unverändert.

### Hinzugefügt

- **Feedback senden:** Dezenter Sidebar-Link im Bereich „Konto“ (`FeedbackButton`) öffnet ein Modal mit Kategorie, Betreff und Nachricht. Versand per `IFeedbackService` an `AppBranding.SupportEmail` über den zentralen E-Mail-Service und Vorlage `FeedbackMessageToSupport`. Enthält Benutzer-, Mandanten-, Seiten- und Versionskontext. Keine Datenbankpersistenz.

### Behoben

- **Feedback senden (Sidebar):** `@onclick` funktionierte nicht, weil `NavMenu`/`MainLayout` statisch gerendert werden. `FeedbackButton` nutzt jetzt `InteractiveServerRenderMode` (wie `TenantSwitcher`). Button-Styling an `nav-link`/`dsms-nav-logout` angeglichen — gleiche Optik wie „Abmelden“.

- **Feedback-Modal (Layout):** Modal wurde in der Sidebar gerendert und lag im Stacking Context unter der sticky Topbar. Eigenes Fixed-Overlay (`dsms-feedback-modal-backdrop`, `z-index: 2000`) über der gesamten Viewport-Fläche.

- **Seiten-Hilfe (Info-Button):** Zentraler Hilfetext für Fachseiten über `PageHelpContent` (plattformweit, eindeutiger `Key`). Wiederverwendbare Komponente `PageHelpButton` neben dem Seitentitel in `PageHeader` (`HelpKey`). Modal mit rechtlichem Bezug, Kurzbeschreibung und Erklärungstext (Plaintext, absatzweise). Superuser können Texte direkt im Modal bearbeiten; andere Rollen nur lesen. Standardtexte per `PageHelpContentSeeder` (idempotent, überschreibt keine Anpassungen). Migration `AddPageHelpContents`. Integriert auf: Verarbeitungstätigkeiten, TOMs, DSFA, Dienstleister, Audit-Vorlagen, Audit-Durchläufe, Maßnahmen, Dokumente, Tenant-Daten, Benutzer, Meine Lizenz.

- **Dashboard „Erste Schritte“:** Einklappbare Checkliste mit sechs mandantenbezogenen Standardaufgaben (`TenantOnboardingTask`, `ITenantOnboardingService`). Manuelles Abhaken, Fortschrittsanzeige, Links zu Modulen. Einklapp-Zustand per localStorage. Migration `AddTenantOnboardingTasks`.

### Geändert

- **Dashboard „Erste Schritte“ (UI):** Kompakteres Card-Layout im Stil der Upgrade-Card — kleinerer Header, Fortschritts-Badge, dezente 6px-Progressbar, Aufgabenzeilen ohne Bulletpoints, kurze Beschreibungstexte nur in der Anzeige. Halbe Breite auf Desktop im Zwei-Spalten-Grid neben „Offene Maßnahmen“.

### Geändert

- **Sichtbares Produkt-Branding:** Der sichtbare Produktname wurde von „DSMS“ auf **Datenschutz-Cloud** umgestellt. Zentrale Konfiguration unter `AppBranding` in `appsettings.json` (`AppBrandingOptions`). Betrifft Sidebar, Topbar, Login, Browser-Titel (`BrandedPageTitle`), Versionsanzeige, E-Mail-Vorlagen/Platzhalter, Mandanten-Export-Metadaten und interne Systemmails. Technische Projektnamen (z. B. `Dsms.Web`) und Favicon unverändert.

### Hinzugefügt

- **Mandanten-Stammdaten für Admins unter `/tenant-daten`:** Mandanten-Admins und Superuser können DSGVO-Stammdaten (Verantwortlicher, DSB) des aktuellen Mandanten bearbeiten (`ITenantComplianceInfoService`). TenantId wird serverseitig ermittelt; Lizenz- und Plattformfelder bleiben gesperrt. Audit-Log-Aktion `TenantComplianceInfoUpdated`.

- **Mandanten-Stammdaten für VVT (Art. 30 DSGVO):** Zentrale Angaben zum Verantwortlichen und zur Datenschutzbeauftragten Person auf `Tenant` (Wiederverwendung von `LegalName` als Name des Verantwortlichen). Bearbeitung in `/tenants/edit`, Export in `tenant.json` des Mandanten-ZIP. Migration `AddTenantControllerAndDpoFields`.

### Hinzugefügt

- **Mandanten-Recovery für Blazor Server:** `ITenantService.EnsureTenantContextAsync()` stellt den Mandant aus der Session wieder her, wenn der Circuit-Cache (`TenantContextAccessor`) leer ist; Zugriffsprüfung inklusive. Genutzt von `TenantContextGate`, `TenantSwitcher` und Middleware.

### Behoben

- **Mandant nach Inaktivität verloren:** `_contextInitialized` in `TenantService` entfernt – leerer Accessor löst erneutes Session-Laden aus. `TenantContextGate` und `TenantSwitcher` synchronisieren den UI-State nach Recovery.

### Hinzugefügt

- **Sidebar-Versionsanzeige:** Anwendungsversion aus `Application:Version` in `appsettings.json`, Anzeige unten in der Sidebar für angemeldete Benutzer (`IApplicationInfoService`).

### Behoben

- **Auditor: reine Leserolle im Mandantenbereich**
  - Neue Rollen-Konstanten `ComplianceEditor` (nur Admin) und `ComplianceViewer` (Admin, Auditor, User)
  - `IUserAccessService`: `IsAuditorAsync`, `CanEditComplianceContentAsync`, `CanEditTenantOperationalContentAsync`
  - Auditor aus allen Bearbeitungs-Routen entfernt (VVT, DSFA, TOMs, Dienstleister, Audit-Durchläufe, Verknüpfungen)
  - Listen/Detailseiten: Bearbeiten/Archivieren-Buttons nur noch für berechtigte Rollen
  - Maßnahmen, Dokumente, Audit-Antworten: Auditor sieht Inhalte, kann aber nicht speichern/hochladen/archivieren
  - Audit-Antworten (`/audit-runs/answers/{id}`): Lesemodus für Auditor mit deaktivierten Eingaben
  - Serverseitig: `ArchivingService`, `AuditTemplateService`, `DocumentUploadComponent`, `DocumentLinksEditModal`, `Measures/Edit`, `AuditRuns/Answers`

### Behoben

- **Lizenz-Nutzungszählung auf `/users`:** `CountRoleUsersForTenantAsync` zählte Benutzer doppelt, wenn sie sowohl in `UserTenants` als auch über Legacy-`TenantId` am Mandanten verknüpft waren. Die Zählung nutzt jetzt dieselbe Distinct-Logik wie die Admin-Lizenzübersicht (`CountUsersByRolePerTenantAsync`).

### Hinzugefügt

- **Docker-Production-Deployment (Open Source):**
  - `docker-compose.yml` – App-Service `dsms`, MySQL 8 `db`, Volumes für DB/Uploads/Data-Protection-Keys
  - `.env.example` mit Platzhalterwerten; `.env` in `.gitignore`
  - `Production_Deployment.md` – Anleitung für produktiven Docker-Betrieb
  - `Dsms.Web/Dockerfile` – Multi-Stage Release-Build (Port 8080)
  - Data Protection Keys persistent (`DataProtection-Keys` + Docker-Volume)
  - Upload-Pfad konfigurierbar (`Storage:UploadPath`, Default `Data/Uploads`)
  - Production Connection String über `ConnectionStrings__DefaultConnection`

### Geändert

- Tabellen-Aktionsspalten: Einheitliches vertikales Button-Layout über `dsms-table-action-stack` in Verarbeitungstätigkeiten, DSFA, Dienstleister, Audit-Vorlagen, Audit-Durchläufe, Maßnahmen, Dokumente, Benutzer und TOMs
- `appsettings.json`: lokaler Default-ConnectionString auf `dsms_dev` vereinheitlicht; `Storage:UploadPath` ergänzt
- `README.md`: Verweis auf `Production_Deployment.md`; lokaler Compose-Start nur `db`
- `Architecture.md`: Docker-Deployment und Konfiguration aktualisiert

### Hinzugefügt

- **Admin-Lizenzübersicht: Upgrade-Anfrage (`/admin/license`):**
  - Erweiterte Tarif-/Billing-Anzeige in „Lizenzdaten“ (Plan-Anzeigename, Beschreibung, Abrechnung, Betrag, Nächste Rechnung – falls verfügbar)
  - Button „Upgrade anfragen“ mit aufklappbarem Formular (Zieltarif oder individuelles Upgrade)
  - `IUpgradeRequestService` / `UpgradeRequestService` – Validierung, interne E-Mail an Systembenachrichtigungsadresse, Audit-/Systemlog
  - Zielpläne: aktive kostenpflichtige Pläne außer aktuellem Tarif (`GetActivePaidPlansAsync`)
  - Tarifinfo im Formular mit Preisen, Beschreibung und kompakten Limits; Preise auch in der internen Upgrade-Mail
  - **Keine** automatische Lizenz-/Planänderung, **keine** Zahlung, **keine** Rechnung

### Hinzugefügt

- **Manuelle Rechnungsverwaltung für Registrierungen:**
  - `BillingStatuses` (NotRequired, InvoicePending, InvoiceSent, Paid, PaymentOverdue, Cancelled) und `BillingStatusDisplayHelper`
  - Neue Felder auf `PendingSignup`: `BillingStatus`, `InvoiceSentAt`, `InvoicePaidAt`, `NextInvoiceDate` (DateOnly), `BillingNote` (Migration `AddPendingSignupBillingManagement`)
  - Beim Public Signup: Free → `NotRequired`, kein Datum; Paid monatlich → `NextInvoiceDate = Registrierung + 1 Monat`; Paid jährlich → + 1 Jahr
  - `/platform/signups`: Spalten Rechnungsstatus und Nächste Rechnung; Filter Rechnungsstatus und Nächste Rechnung bis
  - Detailseite `/platform/signups/{id}`: Abschnitt „Rechnung“, Bearbeitung Nächste Rechnung + interne Rechnungsnotiz, Aktionen (gesendet/bezahlt/überfällig/offen)
  - Service-Methoden: `UpdateBillingDetailsAsync`, `MarkInvoiceSentAsync`, `MarkInvoicePaidAsync`, `MarkPaymentOverdueAsync`, `MarkInvoicePendingAsync` (nur Superuser, mit Auditlog)
  - Interne Signup-E-Mail enthält „Nächste Rechnung am“
  - **Keine** automatische Lizenzverlängerung bei Rechnungsaktionen

### Geändert

- **Public Signup: Abrechnungszeitraum Monatlich/Jährlich:**
  - `BillingCycles` (Monthly/Yearly) und `BillingCycleDisplayHelper`
  - Auswahl im Signup-Formular bei kostenpflichtigen Plänen; Validierung und Amount nach Nutzerwahl
  - Feld `BillingCycle` auf `PendingSignup` (Migration `AddPendingSignupBillingCycle`)
  - Anzeige in `/platform/signups`, Detailseite und interner E-Mail

- **Systembenachrichtigungen:** Empfänger nicht mehr in `appsettings.json`, sondern in `EmailSettings` unter `/platform/email/settings` (`SystemNotificationsEnabled`, `SystemNotificationRecipientEmail`); Migration `AddEmailSettingsSystemNotifications`

### Hinzugefügt

- **Signup: Superuser-Ansicht Rechnungsdaten + interne Benachrichtigung:**
  - Rechnungsdaten in `PendingSignupListDto` (BillingEmail, PaymentProvider, MetadataJson)
  - Spalte „Rechnung“ in `/platform/signups` (kompakt: E-Mail + Status)
  - Abschnitt „Rechnungsdaten“ auf `/platform/signups/{id}`; Lizenzstatus und Ablaufdatum aus `ILicenseService`
  - `SignupNotificationService` – interne E-Mail nach erfolgreichem Public Signup
  - Systembenachrichtigungen konfigurierbar unter `/platform/email/settings` (`SystemNotificationsEnabled`, `SystemNotificationRecipientEmail`)
  - Systemlogs: `SignupNotificationSent`, `SignupNotificationFailed`, `SignupNotificationSkipped`

- **Public Signup: Direkte Provisionierung für alle Pläne:**
  - Free- und kostenpflichtige Pläne werden nach Absenden direkt provisioniert (License, Mandant, Admin, Passwortmail)
  - Einheitlicher Flow in `PublicSignupService.SubmitWithProvisioningAsync`: PendingSignup → Provisioning → Status Provisioned/Failed
  - `CreateForPublicSignupAsync`, `SetStatusForPublicSignupAsync`, `MarkAsProvisionedForPublicSignupAsync`, `MarkAsFailedForPublicSignupAsync` in `PendingSignupService`
  - Neuer Status `Provisioning` in `PendingSignupStatuses`
  - Automatische Lizenzlaufzeit beim Public Signup: 1 Monat (`ValidFrom` = UTC-Datum heute, `ValidUntil` = +1 Monat)
  - Billing: Free → `PaymentProvider = None`, Amount = 0; Paid → `PaymentProvider = ManualInvoice`, Amount = jährlicher Preis falls gesetzt, sonst monatlich
  - `BillingStatus` und `BillingCycle` in `MetadataJson` (Legacy-Fallback); zusätzlich eigenes DB-Feld `BillingStatus` und Rechnungsverwaltungsfelder (s. unten)
  - Erfolgsseite `/signup/success` für Free und Paid (Query-Parameter `paid`, `planName`, `emailSent`)
  - **Ohne** Mollie, Online-Zahlung, automatische Rechnungserstellung

- **Signup: Rechnungsdaten bei kostenpflichtigen Plänen:**
  - Rechnungsfelder in `PublicSignupFormDto` und bedingte Anzeige auf `/signup` (nur wenn `IsFree == false`)
  - Validierung in `PublicSignupService` nur für kostenpflichtige Pläne
  - Speicherung in `PendingSignup` (Migration `AddPendingSignupBillingFields`)
  - Sinnvolle Vorausfüllung aus Unternehmens-/Admin-Daten beim Wechsel auf kostenpflichtigen Plan

### Geändert

- **Signup-Header:** Helle Schriftfarben auf blauem Hintergrund für Titel, Untertitel und „Tarif auswählen“

### Hinzugefügt

- **Öffentliche Registrierung mit Tarifauswahl:**
  - Neues Feld `IsPublicSignupEnabled` auf `SubscriptionPlan` (Migration `AddSubscriptionPlanIsPublicSignupEnabled`)
  - Bestehende aktive Free-Pläne werden per Migration auf öffentlich registrierbar gesetzt; bezahlte Pläne standardmäßig nicht
  - `IPublicSignupService` / `PublicSignupService` – lädt öffentliche Pläne, validiert Auswahl, verzweigt Free → `ProvisioningService`, Paid → `PendingSignupService.CreatePublicAsync`
  - DTOs `PublicSignupPlanDto`, `PublicSignupFormDto`, `PublicSignupSubmitResult`
  - `GetPublicSignupPlansAsync()` und `GetPublicSignupPlanByIdAsync()` in `SubscriptionPlanService`
  - `/signup` zeigt alle aktiven, öffentlich registrierbaren Pläne als wählbare Karten; Formular erst nach Planwahl
  - Superuser-Planverwaltung: Checkbox „Öffentlich registrierbar“, Spalte in `/platform/plans`
  - `/signup/paid` leitet auf `/signup?preferPaid=true` weiter (Vorauswahl bezahlter öffentlicher Pläne)
  - Systemlogs `PublicSignupSubmitted`, `PublicSignupFailed`; Quelle `PublicSignup` für Paid-PendingSignups
  - **Ohne** Mollie, Online-Zahlung, Rechnungslogik, Upgrade-Funktion

### Geändert

- **Signup vereinheitlicht:** `IFreeSignupService` / `FreeSignupService` durch `IPublicSignupService` / `PublicSignupService` ersetzt; `/signup` nicht mehr nur für kostenlosen Plan

### Hinzugefügt

- **Öffentlicher Paid-Signup (ohne Mollie):**
  - Seiten `/signup/paid` und `/signup/paid/success` (ohne Login, LoginLayout)
  - `IPaidSignupService` / `PaidSignupService` – lädt aktive bezahlte Pläne, validiert Formular, ruft `PendingSignupService.CreatePublicAsync` auf
  - `GetActivePaidPlansAsync()` in `SubscriptionPlanService`
  - `CreatePublicAsync` in `PendingSignupService` – ohne Superuser-Prüfung, nur aktive bezahlte Pläne, Status Draft, Plan-Snapshots
  - Duplikatprüfung: bestehender Benutzer und offene PendingSignups (Draft, PendingPayment, Paid) zentral im Service
  - Auditlog `PaidSignupSubmitted`, Systemlog `PaidSignupFailed`
  - Quelle `PaidSignup` in Superuser-Übersicht `/platform/signups` (Spalte Quelle)
  - Verlinkung zwischen `/signup` und `/signup/paid`
  - **Ohne** Mollie, Checkout, Webhook, License/Tenant/Admin-Erstellung, Passwortmail, ProvisioningService

### Hinzugefügt

- **PendingSignup (Vorbereitung bezahlte Registrierungen):**
  - Entity `PendingSignup` mit Plan-Snapshots, Kundendaten, Payment-Vorbereitung und Provisioning-Ergebnis
  - Statuswerte: Draft, PendingPayment, Paid, Provisioned, Failed, Cancelled, Expired
  - `IPendingSignupService` / `PendingSignupService`
  - Superuser-Seiten: `/platform/signups`, `/platform/signups/{id}`, `/platform/signups/create`
  - EF-Migration `AddPendingSignups`
  - Auditlogs: PendingSignupCreated, PendingSignupStatusChanged, PendingSignupMarkedFailed, PendingSignupCancelled, PendingSignupExpired
  - **Ohne** Mollie, Webhook, automatische Provisionierung; Free-Signup unverändert

### Hinzugefügt

- **Öffentlicher Free-Signup:**
  - Seiten `/signup` und `/signup/success` (ohne Login, LoginLayout)
  - `IFreeSignupService` / `FreeSignupService` – lädt aktiven Free-Plan, validiert Formular, ruft `ProvisioningService` auf
  - `GetActiveFreePlanAsync()` in `SubscriptionPlanService`
  - Systemlogs `FreeSignupSubmitted`, `FreeSignupFailed`
  - Honeypot- und Doppelabsende-Schutz
  - **Ohne** Mollie, PendingSignup, Webhook, bezahlte Pläne, Auto-Login

### Hinzugefügt

- **ProvisioningService:**
  - `IProvisioningService` / `ProvisioningService` mit `ProvisionCustomerAsync` – erstellt License (via Plan-Mapping), Tenant, Admin und sendet Passwortvergabe-Mail
  - DTOs `ProvisionCustomerRequestDto`, `ProvisionCustomerResultDto`
  - Gemeinsame Plan-Mapping-Logik in `PlanToLicenseMapper` / `PlanToLicenseValidator`
  - `SendProvisioningWelcomeEmailAsync` in `IPasswordResetService` (ohne Benutzerverwaltungs-Prüfung)
  - Superuser-Seite `/platform/provisioning/create` (Navigation „Provisionierung“)
  - Auditlog `CustomerProvisioned`, Systemlogs bei Fehlern (`ProvisioningFailed`, `PasswordSetupEmailFailed`)
  - **Ohne** Public Signup, Mollie, PendingSignup, Webhook; wiederverwendbar für spätere Signup-/Webhook-Flows

### Hinzugefügt

- **Plan-to-License Mapping:**
  - `IPlanToLicenseService` / `PlanToLicenseService` – kopiert Tarifvorlagen-Werte in neue `License`-Einträge
  - `PreviewLicenseFromPlanAsync`, `CreateLicenseFromPlanAsync`, DTOs `CreateLicenseFromPlanDto`, `LicenseFromPlanPreviewDto`
  - Gemeinsame Lizenznummern-Generierung in `LicenseNumberGenerator` (wiederverwendet von `LicenseService`)
  - Superuser-Seite `/platform/licenses/create-from-plan` („Neue Lizenz aus Plan“)
  - Auditlog `LicenseCreatedFromPlan` (`IsVisibleToAdmin = false`)
  - **Ohne** Public Signup, Mollie, ProvisioningService, Mandanten-/Admin-Anlage, Passwortmail; bestehende Lizenzen unverändert

### Hinzugefügt

- **Tarif-/Planverwaltung (Grundlage):**
  - Neue Entity `SubscriptionPlan` (Guid-Id) als Tarifvorlage mit Preisen, Limits und optionalen externen Billing-Feldern
  - `ISubscriptionPlanService` / `SubscriptionPlanService` mit CRUD und `GetActivePlansAsync()` (für späteren Signup vorbereitet)
  - Superuser-Seiten: `/platform/plans` (Übersicht), `/platform/plans/edit`, `/platform/plans/{id}` (Details)
  - Navigation „Pläne“ im Plattform-Bereich (nur Superuser)
  - EF-Migration `AddSubscriptionPlans`
  - Idempotentes Seeding der Demo-Tarife `free`, `basic`, `pro`, `business` (`SubscriptionPlanSeeder`)
  - Plattform-Auditlog: `SubscriptionPlanCreated`, `SubscriptionPlanUpdated`, `SubscriptionPlanActivated`, `SubscriptionPlanDeactivated` (`IsVisibleToAdmin = false`)
  - **Ohne** Public Signup, Mollie, ProvisioningService, automatische Lizenzanlage, Featurelocks; bestehende `License`-Logik unverändert

### Geändert

- **Audit-Diffs lesbar:** `AuditDiffHelper`, `ComplianceAuditDiffBuilder` und `AuditLogChangeParser` – Update-Logs speichern nur geänderte Felder als lesbare Strings (Enums/Status nicht mehr als `{}`); leere Updates werden nicht geschrieben
- **Audit-Detailansicht:** `LogEntryChangesView` in Admin- (`/admin/auditlog`) und Superuser-Protokoll (`/platform/logs`) mit Änderungstabelle; alte Logeinträge mit Roh-JSON weiterhin anzeigbar
- **Login-Logs für Admins ausgeblendet:** `UserLoginSuccessful` mit `IsVisibleToAdmin = false` (Superuser-Nutzungsanalyse unverändert)
- **Fachliche Auditlogs:** `IComplianceAuditLogService` für VVT, DSFA, TOMs, Dienstleister, Maßnahmen, Audits, Auditvorlagen und Nachweisdokumente (Create/Update/Archive/Status)

### Hinzugefügt

- **Zentrales Protokoll-/Auditlog-System:**
  - Entity `LogEntry` mit Kategorien Audit, System, Security
  - `ILogService` / `LogService` mit `LogAuditAsync`, `LogSystemAsync`, `LogSystemErrorAsync`, `LogSecurityAsync`
  - `ILogQueryService` für Superuser- (`/platform/logs`) und Admin-Auditlog-Ansicht (`/admin/auditlog`)
  - `ILicenseCreateGuard` für zentrale Protokollierung blockierter Lizenz-Erstellungen
  - IP-Anonymisierung (`LogIpAnonymizer`), sichere JSON-Serialisierung (`LogJsonHelper`)
  - Login-Protokollierung (erfolgreich / fehlgeschlagen) in `Login.razor`
  - Automatische Logpunkte: Lizenzen, Mandanten, Benutzer, E-Mail-Fehler, Reminder-Fehler
  - Dokumentation: `Logging.md`
  - EF-Migration `AddLogEntries`

### Hinzugefügt

- **Lizenzstatus und Ablaufdatum bei Neuanlage:**
  - Zentrale Nutzbarkeitsprüfung (`CheckLicenseUsableForCreationAsync`, `LicenseUsabilityInfo`, `LicenseBlockReason`)
  - Alle `CanCreate*`-Methoden prüfen zuerst Status (Active) und `ValidUntil`, danach Mengenlimits
  - UI blockiert Neu-Buttons über bestehende `LicenseLimitAlert`-Logik; Bearbeiten/Archivieren unverändert
  - Admin- und Superuser-Lizenzübersicht zeigen Nutzbarkeit und Hinweise

### Hinzugefügt

- **Admin-Lizenzübersicht (read-only):**
  - Neue Seite `/admin/license` für Kunden-Admins („Meine Lizenz“)
  - `GetCurrentAdminLicenseOverviewAsync()` – Lizenz aus `ApplicationUser.LicenseId`, ohne URL-Parameter
  - Anzeige von Basisdaten, lizenzweiter Nutzung und Nutzung je Mandant; Navigationspunkt nur für Tenant-Admins
- **Lizenz-Limits (Durchsetzung beim Anlegen):**
  - `LicenseLimitCheckResult` und zentrale `CanCreate*`-Methoden in `LicenseService` (lizenzweit und mandantenbezogen)
  - Wiederverwendbare UI-Komponenten `LicenseUsageBadge` und `LicenseLimitAlert`
  - Limit-Anzeige und deaktivierte Neu-Buttons in Listen-/Formularseiten (Mandanten, Benutzer, VVT, DSFA, TOMs, Dienstleister, Maßnahmen, Audits, eigene Auditvorlagen)
  - Serverseitige Prüfung beim Speichern in Services (`TenantManagementService`, `UserManagementService`, `AuditTemplateService.CopyToTenantAsync`) und Edit-Page-Handlern
  - Keine Featurelocks, kein Billing, keine automatische Datenlöschung; Bearbeiten/Archivieren/Löschen bestehender Objekte unverändert möglich
  - `CanSendEmailReminderAsync` vorbereitet (TODO, aktuell ohne Blockierung)

### Hinzugefügt

- **Lizenzverwaltung (Grundlage):**
  - Neue Entity `License` (Guid-Id) als zentrale kaufmännische/technische Kundeneinheit
  - Optionale `LicenseId` auf `Tenant` und `ApplicationUser` (nullable FK, bestehende Daten unverändert)
  - `ILicenseService` / `LicenseService` mit Usage-Counts und Limit-Anzeige-Hilfen (`LicenseLimitHelper`)
  - Superuser-Seiten: `/platform/licenses` (Übersicht), `/platform/licenses/edit`, `/platform/licenses/{id}` (Details)
  - Navigation „Lizenzen“ im Plattform-Bereich (nur Superuser)
  - EF-Migration `AddLicenses`
  - Limit-Blockierung beim Anlegen neuer Objekte siehe Eintrag „Lizenz-Limits“; weiterhin **ohne** Featurelocks und Billing-Anbindung
- **Demo-Seeding:** Idempotente Demo-Lizenz `LIC-DEMO-000001` mit Limits, Zuordnung zu Demo-Admins/Mandanten, zwei Demo-Mandanten (Hauptsitz + Niederlassung Süd)
- **Lizenzzuordnung in Mandanten- und Benutzerverwaltung (Superuser):**
  - Mandantenübersicht/-bearbeitung mit Lizenz-Spalte und -Dropdown
  - Benutzerübersicht/-anlage/-bearbeitung mit Lizenz-Spalte, Konsistenzprüfung und mandantenabhängigem Dropdown
  - `ITenantManagementService`, `LicenseOptionDto`, `GetActiveLicenseOptionsAsync()`

### Geändert

- **Erinnerungen:** Zugriff auf `/admin/erinnerungen` nur noch für **Superuser** (Seite, Navigation, `ReminderService`)

### Behoben

- **DSFA-Archivansicht:** Archivierte DSFAs erscheinen wieder in der Archiv-Liste
  - Ursachen: (1) Join auf VVT unterlag dem Archiv-Query-Filter; (2) Status „Archiviert“ (DpiaStatus) und `IsArchived` waren nicht synchron
  - Listenabfrage in `Dsfa/Index.razor` mit explizitem Mandanten-/Archiv-Filter und `IgnoreQueryFilters()` für VVT-Namen
  - Synchronisation Status/`IsArchived` in `ArchivingService` (DSFA) und `Dsfa/Edit.razor`
  - Migration `SyncDpiaStatusArchivedWithIsArchived` bereinigt bestehende Datensätze mit Status Archiviert ohne `IsArchived`

- **Tenant-Export / Mandantenabfrage:** `InvalidCastException: Can't convert NULL to Int32` behoben
  - Export lädt Verknüpfungen jetzt mit separaten `IgnoreQueryFilters()`-Abfragen statt gefilterter EF-Includes
  - Audit-Vorlagen/Fragen im Export per SQL-Projektion (nur benötigte Felder)
  - Migration `FixTenantDeletionRequestNulls`: NULL-Werte in `Tenants.IsDeletionRequested` / `IsActive` bereinigt
  - Migration `FixArchivableNullBooleanColumns`: NULL-Werte in `IsArchived`/`IsActive` aller Archiv-Tabellen bereinigt

### Hinzugefügt

- **Tenant-Daten (Export und Löschanforderung):**
  - Neue Seite `/tenant-daten` im Bereich Verwaltung (Superuser und Mandanten-Admin)
  - Vollständiger Mandanten-Export als ZIP (`ITenantExportService` / `TenantExportService`)
  - Export enthält: Stammdaten, Benutzer, VVT, DSFA, TOMs, Dienstleister, Maßnahmen, Audit-Vorlagen, Audit-Durchläufe inkl. Fragen/Antworten, Dokument-Metadaten und Dateien
  - Export enthält **keine** Passwort-Hashes, Tokens, Secrets oder SMTP-Passwörter
  - Download über `POST /tenant-daten/export` (direkter Stream, keine dauerhafte Speicherung)
  - Löschanforderung über `ITenantDeletionService` – markiert Tenant mit `IsDeletionRequested`, `DeletionRequestedAt`, `DeletionRequestedByUserId`, `DeletionScheduledAt` (jetzt + 7 Tage)
  - Keine automatische Hard-Delete in Version 1
  - Warnhinweis `TenantDeletionBanner` bei Mandanten mit Löschanforderung
  - Superuser kann Löschanforderung abbrechen
  - EF-Migration `AddTenantDeletionRequestFields`
  - Berechtigung über `IUserAccessService.CanManageTenantDataAsync()`
- **Audit-Vorlagen: Fragen bearbeiten und löschen:**
  - Bestehende Fragen in bearbeitbaren Vorlagen können inline bearbeitet (Nr., Kategorie, Text) und gelöscht werden
  - Serverseitige Prüfung über `CanEditAsync` / `AddQuestionAsync`, `UpdateQuestionAsync`, `DeleteQuestionAsync`
  - Löschen blockiert, wenn Frage in Audit-Durchläufen referenziert ist (`DeleteBehavior.Restrict`); Snapshots schützen laufende Audits
  - Änderungen an Vorlagenfragen wirken nur auf neue Auditdurchläufe

- **Audit-Vorlagen: Community-Einreichungen (Teil 2):**
  - `AuditTemplateType.Community` und `CommunityStatus` (None, Submitted, Approved, Rejected)
  - Mandanten-Admins/Auditoren können eigene aktive Vorlagen zur Community-Prüfung einreichen
  - Eingereichte Vorlagen (`Submitted`) sind nur für den einreichenden Mandanten sichtbar und während der Prüfung schreibgeschützt
  - Superuser-Prüfbereich unter `/platform/audit-templates/community` (Navigation: Plattform → Community-Prüfung)
  - Freigabe erstellt globale Community-Kopie (`TemplateType = Community`, `TenantId = null`); Ursprungsvorlage bleibt im Mandanten erhalten
  - Ablehnung lässt Vorlage privat; Mandant kann bearbeiten und erneut einreichen
  - Badge „Community“ sowie Status-Hinweise „Zur Prüfung eingereicht“ / „Community abgelehnt“
  - Optional: „Kopie erstellen“ für offizielle und Community-Vorlagen im eigenen Mandanten
  - EF-Migration `AddAuditTemplateCommunityFields`
  - Snapshot-Logik beim Auditstart unverändert – laufende Durchläufe bleiben bei Vorlagenänderungen geschützt

- **Audit-Vorlagen: Eigene und offizielle Vorlagen (Teil 1):**
  - `AuditTemplateType` Enum: `Tenant` (eigene Mandantenvorlage) und `Official` (Plattformvorlage)
  - Eigene Vorlagen: `TenantId` gesetzt, nur im eigenen Mandanten sichtbar, bearbeitbar durch **Admin**/**Auditor** (und Superuser)
  - Offizielle Vorlagen: `TenantId` null, für alle Mandanten sichtbar, nur **Superuser** darf erstellen/bearbeiten/archivieren
  - Button „Neue Vorlage“ erstellt weiterhin Mandantenvorlagen im aktuell ausgewählten Mandanten (auch für Superuser)
  - Zusätzlicher Button „Neue globale Vorlage“ nur für **Superuser** → `/audit-templates/edit?type=official` (ohne `TenantId` des Mandantenkontexts)
  - Mandanten sehen offizielle Vorlagen schreibgeschützt („Ansehen“ statt „Bearbeiten“)
  - Badges in der Liste: „Eigene Vorlage“ / „Offiziell“
  - `IAuditTemplateService` / `AuditTemplateService`: serverseitige Sichtbarkeits- und Berechtigungsprüfungen
  - Snapshot beim Auditstart: Fragentext/-metadaten in `AuditAnswer`, Vorlagentitel/-version in `AuditRun` – laufende Durchläufe bleiben bei Vorlagenänderungen unverändert
  - EF-Migration `AddAuditTemplateTypeAndSnapshots` inkl. Backfill bestehender Daten
  - Community-Vorlagen bewusst **nicht** enthalten (geplant für späteren Schritt)

- **Zentrale Erinnerungsfunktion (Version 1 – manueller Versandassistent):**
  - Seite `/admin/erinnerungen` für Superuser und Mandanten-Admins (Verwaltung → Erinnerungen)
  - `IReminderService` / `ReminderService`: Vorschau und manueller Versand, keine History, kein Background-Job
  - Pro Mandant eine Sammelmail an alle aktiven Admins (Rolle `Admin`, keine normalen Benutzer)
  - Reminder-Typen: DSFA `NextReviewAt`, TOM `NextReviewAt`, Dienstleister `DataProcessingAgreementReviewedAt`, Maßnahmen `DueDate`, Audit-Inaktivität (>14 Tage)
  - TemplateKey `Reminder` über bestehenden `IEmailService`
  - Audit-Letztaktivität aus Antworten (`AnsweredAt`/`UpdatedAt`/`CreatedAt`) – keine `LastActivityAt`-Migration
  - Kein EmailLog, keine Reminder-History-Tabelle

- **Benutzeranlage ohne initiales Passwort mit Willkommensmail:**
  - Passwortfeld aus `/users/create` entfernt; `UserManager.CreateAsync(user)` ohne Passwort
  - Automatische Willkommensmail nach Anlage (TemplateKey `WelcomeSetPassword`)
  - Einladungslink nutzt Identity-Passwortreset-Token (`userId` + `token` + `mode=invite`) auf `/passwort-zuruecksetzen`
  - Passwortreset- und Einladungslinks: beide 60 Minuten gültig (gleiche Identity-Token-Lebensdauer)
  - Keine eigene `UserInvitationTokens`-Tabelle
  - „Einladung erneut senden“ in Benutzerverwaltung (Superuser/Admin, mandantensicher)
  - `PasswordResetService` wiederverwendet für Token-Erzeugung, Link-Bau und Emailversand

- **Passwortreset mit ASP.NET Identity:**
  - Self-Service über Login-Link „Passwort vergessen?“ → `/passwort-vergessen`
  - Reset-Seite `/passwort-zuruecksetzen` mit URL-sicher codiertem Identity-Token (`UserManager.GeneratePasswordResetTokenAsync` / `ResetPasswordAsync`)
  - Neutrale UI-Meldung ohne Benutzer-Aufzählung; Token-Lebensdauer 60 Minuten (`DataProtectionTokenProviderOptions`)
  - Admin/Superuser: „Passwortreset-Mail senden“ in Benutzerverwaltung (`/users`, `/users/edit/{UserId}`)
  - Zentraler `PasswordResetService` nutzt `IEmailService` und Vorlage `PasswordReset` – keine eigene Token-Tabelle
  - Einfaches Rate Limiting (5 Min. pro Emailadresse) über `IDistributedCache`
  - Keine Passwörter per Email; kein EmailLog

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
