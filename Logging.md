# DSMS – Logging und Auditlog

Diese Dokumentation beschreibt das zentrale Protokoll-/Auditlog-System des DSMS.

## Überblick

Alle Protokolleinträge werden in der Tabelle `LogEntries` gespeichert. Audit- und Systemlogs nutzen **dieselbe Struktur** und werden über das Feld `LogCategory` unterschieden.

| Kategorie | Zweck | `IsVisibleToAdmin` |
|-----------|-------|-------------------|
| `Audit` | Fachliche Benutzeraktionen | Standard: `true` (Login/Logout: `false`) |
| `System` | Technische Ereignisse, Fehler | Immer `false` |
| `Security` | Sicherheitsrelevante Ereignisse (z. B. fehlgeschlagene Logins) | Immer `false` |

## Service verwenden

Der zentrale Service ist `ILogService` / `LogService`. Er ist per DI in allen Services und Razor-Komponenten verfügbar.

**Wichtig:** Logging-Fehler brechen die eigentliche Fachaktion nicht ab. Der Service fängt interne Fehler ab.

### Auditlog manuell ergänzen

```csharp
await logService.LogAuditAsync(
    action: "DpiaCreated",
    description: "DSFA wurde erstellt.",
    entityType: "Dpia",
    entityId: dpia.Id.ToString(),
    entityName: dpia.Title,
    tenantId: tenantId,
    licenseId: licenseId);
```

### Systemfehler manuell loggen

```csharp
await logService.LogSystemErrorAsync(
    action: "EmailSendFailed",
    description: "E-Mail konnte nicht versendet werden.",
    exception: ex,
    tenantId: tenantId,
    licenseId: licenseId,
    metadata: new { Recipient = recipientEmail });
```

### Systemereignis (ohne Exception)

```csharp
await logService.LogSystemAsync(
    action: "BackgroundJobStarted",
    description: "Hintergrundjob wurde gestartet.",
    severity: "Info",
    metadata: new { JobName = "Export" });
```

### Security-Log

```csharp
await logService.LogSecurityAsync(
    action: "UserLoginFailed",
    description: "Anmeldung fehlgeschlagen.",
    severity: "Warning",
    userEmail: email,
    metadata: new { Reason = "InvalidCredentials" });
```

### Blockierte Lizenz-Erstellung

Verwenden Sie `ILicenseCreateGuard` statt direkter `IsAllowed`-Prüfung – der Guard protokolliert automatisch:

```csharp
if (!await licenseCreateGuard.IsAllowedAsync(limitCheck, "Dpia"))
{
    return Fail(limitCheck.Message);
}
```

Oder direkt:

```csharp
await logService.LogBlockedCreationAsync(limitCheck, entityType: "Dpia");
```

## Automatische Kontextanreicherung

`LogService` ergänzt automatisch (wenn verfügbar):

- aktueller Benutzer (`UserId`, `UserEmail`, `UserDisplayName`)
- `TenantId` / `TenantName` (aus Parameter, Session oder Mandantentabelle)
- `LicenseId` / `LicenseNumber` (aus Parameter, Mandant oder `ApplicationUser.LicenseId`)
- anonymisierte IP-Adresse
- `UserAgent`, `RequestPath`, `CorrelationId`

Explizit übergebene `tenantId` / `licenseId` haben Vorrang.

## IP-Anonymisierung

Zentrale Methode: `LogIpAnonymizer.AnonymizeIpAddress(string? ipAddress)`

| Typ | Beispiel Eingabe | Beispiel Ausgabe |
|-----|------------------|-----------------|
| IPv4 | `192.168.178.123` | `192.168.178.xxx` |
| IPv6 | `2001:db8:abcd:1234:5678:90ab:cdef:1234` | `2001:db8:abcd:1234:xxxx:xxxx:xxxx:xxxx` |

## Sensible Daten

**Nicht loggen:** Passwörter, Tokens, API-Keys, Cookies, Resetlinks, vollständige IP-Adressen, Dokumenteninhalte.

`LogJsonHelper` filtert bekannte sensible Eigenschaftsnamen bei der JSON-Serialisierung (`[REDACTED]`).

## UI-Zugriff

| Rolle | Route | Sichtbarkeit |
|-------|-------|--------------|
| Superuser | `/platform/logs` | Alle `LogEntries` mandantenübergreifend (Audit, System, Security); nur Metadaten, keine Fachinhalte in der Details-Ansicht |
| Admin (Kundenadmin) | `/admin/auditlog` | Nur `Audit` mit `IsVisibleToAdmin == true` der eigenen Lizenz |
| User / Auditor | – | Keine Logansicht in V1 |

### Plattform-Protokoll (Superuser)

- **Query:** `LogQueryService.GetPlatformLogsAsync` – keine Filterung auf `CurrentTenantId`; alle Mandanten-Events sichtbar.
- **Datenschutz:** In der Details-Ansicht werden `OldValuesJson`/`NewValuesJson` nicht angezeigt; `EntityName` bei Fachmodulen redigiert (`AuditLogPresentationHelper`).
- **Filter:** Zeitraum, Kategorie, Severity, Benutzer, Aktion, Lizenz, Mandant-ID/-Name, Modul, Entitätstyp, Ergebnis, Supportmodus, Beschreibung.
- **Modul:** Wird aus `EntityType` abgeleitet oder aus `MetadataJson.Module` gelesen; fachliche Logs schreiben `Module` und `Result` über `ComplianceAuditLogService`.

## Login-Protokollierung

Login-Logs dienen in V1 der **Nutzungsanalyse durch den Superuser**, nicht der Kunden-Auditansicht.

| Ereignis | Kategorie | Action | Admin-sichtbar |
|----------|-----------|--------|----------------|
| Erfolgreiche Anmeldung | Audit | `UserLoginSuccessful` | **Nein** (`IsVisibleToAdmin = false`) |
| Fehlgeschlagene Anmeldung | Security | `UserLoginFailed` | Nein |
| Abmeldung (optional) | Audit | `UserLogout` | Nein |

Implementiert in `Components/Account/Pages/Login.razor`. Superuser sieht Login-Logs unter `/platform/logs`; Admins sehen sie **nicht** in `/admin/auditlog`.

### Login-Log (nur Superuser sichtbar)

```csharp
await logService.LogAuditAsync(
    action: "UserLoginSuccessful",
    description: "Benutzer hat sich angemeldet.",
    entityType: "ApplicationUser",
    entityId: user.Id,
    entityName: user.Email,
    licenseId: licenseId,
    isVisibleToAdmin: false);
```

## Fachliche Auditlogs

Fachliche Änderungen werden über `IComplianceAuditLogService` bzw. `LogAuditAsync` protokolliert.

- **Sichtbar für Admins:** `IsVisibleToAdmin = true`, eigene Lizenz/Mandanten
- **Geschrieben bei:** Create, Update, Archive, Restore, Statusänderung (soweit vorhanden)
- **Nur geänderte, unkritische Felder** in `OldValuesJson`/`NewValuesJson` (Titel, Status, Fälligkeitsdatum – keine Langtexte)
- **Update ohne Änderung:** kein Auditlog-Eintrag (Option A)
- **Enums/Status** als lesbarer String (z. B. `"Entwurf"`), nicht als `{}`
- **ChangeList** in `MetadataJson` für die UI-Änderungstabelle (`AuditDiffHelper.ChangeListMetadataKey`)

### Update-Diffs erstellen

```csharp
var changes = ComplianceAuditDiffBuilder.ForDpia(
    _previousTitle, _previousStatus, _previousResidualRisk, _previousResponsible,
    _previousReviewedAt, _previousNextReviewAt, _model);

await complianceAuditLog.LogDpiaUpdatedAsync(_model.Id, _model.Title, tenantId, changes);
```

`ComplianceAuditLogService` schreibt nur, wenn `changes` nicht leer ist. `OldValuesJson`/`NewValuesJson` enthalten dann nur die geänderten Felder als `Dictionary<string, string?>`.

### Fachliches Auditlog manuell ergänzen

```csharp
await logService.LogAuditAsync(
    action: "DpiaCreated",
    description: "DSFA wurde erstellt.",
    entityType: "Dpia",
    entityId: dpia.Id.ToString(),
    entityName: dpia.Title,
    tenantId: tenantId,
    licenseId: licenseId,
    isVisibleToAdmin: true);
```

Oder über den Compliance-Helper:

```csharp
await complianceAuditLog.LogDpiaCreatedAsync(dpia.Id, dpia.Title, tenantId);
```

## Bereits automatisch geloggte Aktionen

**Plattform / Verwaltung:**
- Lizenz: erstellt, geändert, Status geändert, Limits geändert
- Mandant: erstellt, geändert, Lizenzzuordnung geändert
- Benutzer: erstellt, geändert, Lizenzzuordnung geändert, deaktiviert
- Login: erfolgreich / fehlgeschlagen (nur Superuser sichtbar)
- Lizenzblockierung: `CreateBlockedByLicenseLimit`, …
- E-Mail: `EmailSendFailed` (System)
- Reminder: `ReminderJobFailed` (System)

**Compliance-Kernbereiche** (über `IComplianceAuditLogService`):
- Verarbeitungstätigkeiten: `ProcessingActivityCreated/Updated/Archived/Restored/StatusChanged`
- DSFA: `DpiaCreated/Updated/Archived/Restored/StatusChanged`
- TOMs: `TomCreated/Updated/Archived/Restored/StatusChanged`
- Dienstleister: `ProcessorCreated/Updated/Archived/Restored/StatusChanged`
- Maßnahmen: `MeasureCreated/Updated/Archived/Restored/StatusChanged/Completed`
- Audits: `AuditCreated/Started/Updated/Completed/Archived`, `AuditAnswerUpdated`
- Auditvorlagen: `AuditTemplateCreated/Updated/Archived/Restored/Imported/PublishedToCommunity`
- Nachweisdokumente: `EvidenceDocumentUploaded/Archived/Restored`

Hard-Delete-Aktionen existieren für diese Bereiche nicht – entsprechende `*Deleted`-Actions werden nicht geschrieben.
