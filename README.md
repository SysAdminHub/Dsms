# Datenschutz-Cloud

Open-Source-Datenschutzmanagementsystem für den deutschsprachigen Raum – mandantenfähig, auf Blazor Server und .NET 9.

**Repository:** [github.com/SysAdminHub/Dsms](https://github.com/SysAdminHub/Dsms)

## Open-Source-Umfang

**Dieses Repository enthält ausschließlich die Fachanwendung `Dsms.Web`** unter der GNU Affero General Public License v3.0 (AGPL-3.0).

Die separate **Provisioning-App** (`Dsms.Provisioning`) für öffentliche Registrierung, Tarifpläne, Rabattcodes, Lizenzen und Mandantenanlage ist **nicht Open Source** und gehört **nicht** in dieses Repository. Sie wird im kommerziellen SaaS-Betrieb von Datenschutz-Cloud separat und privat betrieben.

| Komponente | Status in diesem Repository |
|------------|----------------------------|
| `Dsms.Web` | Open Source (AGPL-3.0) |
| `Dsms.Provisioning` | Privat – nicht enthalten |

## Überblick

Datenschutz-Cloud ist ein mandantenfähiges Datenschutzmanagementsystem für den deutschsprachigen Raum. Die Open-Source-Fachanwendung unterstützt Organisationen dabei, Datenschutzprozesse wie VVT, TOMs, DSFA, Dienstleisterverwaltung, Audits, Schulungen und Nachweise strukturiert zu dokumentieren und nachzuverfolgen.

Das System richtet sich an KMU, Vereine, Datenschutzbeauftragte, Datenschutzkoordinatoren und IT-Dienstleister, die praktische DSGVO-Dokumentation benötigen. Es ist ein Werkzeug zur Unterstützung von Datenschutzprozessen – **kein Ersatz für individuelle Rechtsberatung**.

Technisch basiert `Dsms.Web` auf **Blazor Server**, **ASP.NET Core Identity**, **Entity Framework Core** und **MySQL 8**.

## Projektstatus

Dieses Projekt befindet sich in **aktiver Entwicklung**. APIs, Datenmodell und Deployment-Struktur können sich noch ändern. Es gibt derzeit keine Garantie für produktionsreife Stabilität im Eigenbetrieb.

## Selfhosting-Status

Eine Selfhosting-Variante der Datenschutz-Cloud ist geplant und wird schrittweise vorbereitet. Dieses Repository enthält bereits Docker- und Deployment-Bausteine für **`Dsms.Web`**, die für **Entwicklung, Tests und eigene Experimente** genutzt werden können.

Eine **offiziell unterstützte und vollständig dokumentierte Selfhosting-Variante für produktive Umgebungen ist derzeit jedoch noch nicht verfügbar**. Produktiver Eigenbetrieb erfolgt daher aktuell auf **eigene Verantwortung**. An einer sauberen Selfhosting-Dokumentation wird gearbeitet.

- Selfhosting bezieht sich auf die **Fachanwendung `Dsms.Web`** – nicht auf die private Provisioning-App.
- Vorhandene Docker-Beispiele dienen primär Entwicklung, Test und technischem Verständnis.
- Keine produktiven Secrets oder Zugangsdaten gehören ins Repository oder in diese Dokumentation.

## Funktionsumfang

Der folgende Umfang ist Bestandteil der Open-Source-Fachanwendung **`Dsms.Web`**:

| Bereich | Beschreibung |
|--------|--------------|
| Dashboard | Kennzahlen und Übersichten zu Maßnahmen, Audits, TOMs, Dienstleistern, DSFA und VVT |
| VVT | Verzeichnis von Verarbeitungstätigkeiten inkl. Stammdaten und Verknüpfungen; TOMs, Dienstleister und Maßnahmen können direkt in der VVT-Maske angelegt und sofort verknüpft werden |
| TOMs | Technische und organisatorische Maßnahmen; mandantenfähige, durch Mandanten-Admins verwaltbare TOM-Kategorien (Aktiv/Inaktiv statt Löschen) |
| DSFA | Datenschutz-Folgenabschätzungen |
| Dienstleister | Auftragsverarbeiter und externe Dienstleister |
| Datenschutzvorfälle | Vorfallregister mit Verknüpfungen |
| Betroffenenanfragen | Dokumentation von DSGVO-Anfragen und Fristen |
| Maßnahmen | Maßnahmenverfolgung mit Status und Fälligkeit |
| Organisation | Datenschutzrollen, Organigramm, Berichtslinien |
| Audits | Audit-Durchläufe mit Fragen und Antworten |
| Auditvorlagen | Eigene, offizielle und Community-Vorlagen |
| Dokumente | Nachweisdokumente mit Upload und Verknüpfungen |
| Schulungen | Schulungsdurchführungen und Teilnehmerverwaltung |
| Schulungsvorlagen | Vorlagen mit Markdown, Assets und Quiz |
| Teilnehmerportal | Externes Schulungsportal ohne App-Login |
| Quiz | Lernerfolgskontrolle in Schulungen |
| PDF-Bescheinigungen | Teilnahmebescheinigungen für Schulungen |
| Supportzugriff | Zeitlich begrenzte Supportfreigabe durch Mandanten-Admins |
| Auditlog | Admin-Protokollierung und Plattform-Logs |
| Lizenzanzeige | Anzeige der Mandanten-Lizenz und Limits (`/admin/license`) |
| Benutzer & Mandanten | Benutzer- und Mandantenverwaltung, Mandanten-Export |

**Nicht Bestandteil dieses Open-Source-Repositories:** öffentliche Registrierung (Signup), Tarifpläne, Rabattcodes, kaufmännische Lizenzverwaltung und automatisierte Mandantenanlage – diese Funktionen liegen in der privaten Provisioning-App.

Ausführlichere fachliche und technische Details: [Project_Overview.md](./Project_Overview.md)

## Architektur

### In diesem Repository (Open Source)

```
Dsms/
├── Dsms.Web/              # Fachanwendung – Open Source (AGPL-3.0)
├── docker-compose.yml     # MySQL + Dsms.Web (Entwicklung/Test)
├── .env.example           # Vorlage für Docker-Umgebungsvariablen
└── Dsms.sln               # Solution mit Dsms.Web
```

**`Dsms.Web`** ist die mandantenfähige Fachanwendung:

- Datenschutzmodule (VVT, TOMs, DSFA, Dienstleister, Vorfälle, Betroffenenanfragen)
- Audits, Maßnahmen, Dokumente, Organisation
- Schulungen und Teilnehmerportal
- Benutzer- und Mandantenverwaltung
- Supportzugriff, Auditlog, Lizenzanzeige
- **Schema-Migrationen** werden beim Start ausgeführt

Separate Projekte `Dsms.Core` oder `Dsms.Infrastructure` existieren **derzeit nicht**.

### Private Provisioning-App (nicht Open Source)

Im SaaS-Betrieb von Datenschutz-Cloud existiert zusätzlich **`Dsms.Provisioning`** als **separate, private Anwendung**. Sie ist **nicht** Teil dieses Repositories und steht **nicht** unter der AGPL-3.0-Lizenz dieses Projekts.

Typische Aufgaben der privaten Provisioning-App:

- Öffentliche Registrierung (Signup)
- Mandantenanlage und Provisioning
- Tarifpläne, Rabattcodes und Lizenzen
- Systembenachrichtigungen im Signup-Kontext

`Dsms.Web` kann für den SaaS-Betrieb so konfiguriert werden, dass Signup-Links auf eine externe Provisioning-URL verweisen. Für Selfhosting und Eigenbetrieb ist die Provisioning-App **nicht erforderlich**; Mandanten und Benutzer werden direkt in `Dsms.Web` angelegt.

Technische Details zur Fachanwendung: [Architecture.md](./Architecture.md)

## Screenshots

Screenshots folgen.

## Voraussetzungen

| Tool | Zweck |
|------|-------|
| [.NET 9 SDK](https://dotnet.microsoft.com/download) | Build und lokale Ausführung |
| [MySQL 8](https://dev.mysql.com/downloads/) | Datenbank (lokal oder per Docker) |
| [Docker](https://docs.docker.com/get-docker/) und [Docker Compose](https://docs.docker.com/compose/) | Optional: MySQL-Container oder Docker-Setup für `Dsms.Web` |
| Git | Repository klonen |

Prüfen der Installation:

```bash
dotnet --version
docker --version
docker compose version
```

## Lokale Entwicklung

### 1. Repository klonen

```bash
git clone https://github.com/SysAdminHub/Dsms.git
cd Dsms
```

### 2. Konfiguration vorbereiten

**Datenbankverbindung** in `Dsms.Web/appsettings.Development.json` anpassen (nur Platzhalter verwenden):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3306;Database=dsms_dev;User=root;Password=change-me;CharSet=utf8mb4;"
}
```

Alternativ können sensible Werte über **User Secrets** oder **Umgebungsvariablen** gesetzt werden, z. B.:

```bash
ConnectionStrings__DefaultConnection=Server=localhost;Port=3306;Database=dsms_dev;User=root;Password=change-me;CharSet=utf8mb4;
```

**Hinweis:** Committen Sie keine echten Passwörter oder Connection Strings.

### 3. Datenbank bereitstellen

**Option A – nur MySQL per Docker:**

```bash
cp .env.example .env
# .env anpassen (Platzhalter durch eigene Werte ersetzen)
docker compose up -d db
```

**Option B – MySQL manuell:**

```sql
CREATE DATABASE dsms_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### 4. Anwendung starten

Migrationen und Demo-Seeding laufen beim Start von `Dsms.Web` automatisch (`DatabaseSeeder`).

```bash
cd Dsms.Web
dotnet run
```

Fachanwendung (Standard): `http://localhost:5295`

Die Solution kann auch in Visual Studio 2022+ über `Dsms.sln` geöffnet werden.

### 5. EF-Migrationen (manuell, optional)

```bash
cd Dsms.Web
dotnet ef migrations add MeinMigrationName
dotnet ef database update
```

## Start mit Docker Compose

Das Docker-Setup in diesem Repository startet **MySQL** und **`Dsms.Web`**:

```bash
cp .env.example .env
# .env anpassen: Datenbank-Passwörter und ggf. App-URLs
docker compose up -d db dsms-web
```

Standard-Ports (über `.env` änderbar):

| Dienst | Host-Port (Default) |
|--------|---------------------|
| MySQL | 3306 |
| Dsms.Web | 8081 |

**Wichtige Hinweise:**

- `.env` muss angepasst werden; committen Sie diese Datei **nicht**.
- MySQL-Passwörter selbst setzen (Platzhalter in `.env.example`).
- **DataProtection-Keys** werden über das Volume `dsms_dataprotection` persistiert.
- **Uploads** werden über das Volume `dsms_uploads` persistiert.
- `Dsms.Web` führt Schema-Migrationen beim Start aus.

> **Hinweis:** Das vorhandene Docker-Setup ist eine technische Grundlage für Entwicklung, Tests und eigene Experimente mit **`Dsms.Web`**. Eine offiziell unterstützte Selfhosting-Dokumentation für produktive Umgebungen ist noch in Arbeit. Details: [Production_Deployment.md](./Production_Deployment.md)

## Konfiguration

Wichtige Konfigurationsbereiche für **`Dsms.Web`** (nur Beispielwerte):

| Bereich | Beispiel |
|---------|----------|
| Datenbankverbindung | `ConnectionStrings__DefaultConnection=Server=localhost;Port=3306;Database=dsms;User=dsms;Password=change-me;CharSet=utf8mb4;` |
| Data Protection | `DataProtection__ApplicationName=DatenschutzCloud` |
| Data Protection Keys | `DataProtection__KeysPath=/app/DataProtection-Keys` |
| Upload-Pfad | `Storage__UploadPath=/app/Data/Uploads` |
| Fach-App-URL | `AppUrls__MainAppBaseUrl=https://app.example.com` |
| Externe Signup-URL (optional, SaaS) | `AppUrls__ProvisioningSignupUrl=https://signup.example.com/signup` |

Konfigurationsquellen:

- `Dsms.Web/appsettings.json` / `appsettings.Development.json`
- Umgebungsvariablen (Production, Docker)
- User Secrets (lokale Entwicklung)
- `.env` für Docker Compose (siehe `.env.example`)

## Demo- und Seed-Daten

Das Projekt enthält Demo-/Seed-Daten für Entwicklungs- und Demo-Umgebungen. Diese werden beim Start von `Dsms.Web` idempotent angelegt (`Data/Seed/DatabaseSeeder.cs`).

**Zugangsdaten und Passwörter werden aus Sicherheitsgründen nicht in dieser README veröffentlicht.** Prüfen Sie den Seeder-Code lokal in einer isolierten Entwicklungsumgebung.

## Sicherheit

- Keine echten Secrets, Passwörter oder API-Keys ins Repository committen.
- `.env` nicht committen; `.env.example` nur mit Platzhaltern pflegen.
- Produktive Passwörter immer durch starke, eigene Werte ersetzen.
- SMTP- und Datenbank-Zugangsdaten nicht veröffentlichen.
- **DataProtection-Keys** in Produktion persistent speichern.
- **HTTPS** und einen Reverse Proxy in Produktion verwenden.
- **Backups** für Datenbank und Uploads selbst einrichten.
- Zugriff auf Produktionssysteme absichern (Firewall, Updates, Least Privilege).

## Mitwirken

Issues und Pull Requests sind willkommen – **ausschließlich für den Open-Source-Umfang (`Dsms.Web`)**.

- Bugreports bitte mit Beschreibung, Schritten zur Reproduktion und relevanten Logs – **ohne Secrets**.
- Feature-Vorschläge gerne als Issue.
- Bitte achten Sie darauf, **keine personenbezogenen Daten, Passwörter, Tokens oder Kundendaten** in Issues oder Pull Requests zu veröffentlichen.

Eine separate `CONTRIBUTING.md` ist derzeit noch nicht vorhanden.

## Roadmap

Geplante oder mögliche Erweiterungen (ohne feste Zusage):

- Offiziell dokumentierte und unterstützte Selfhosting-Variante für `Dsms.Web`
- Weitere Vorlagen (Audit, Schulung, Community)
- Erweiterungen im Schulungsmodul
- Ausbau der Dokumentenverknüpfungen
- Weitere Exporte
- Verbesserungen an Demo- und Deployment-Dokumentation
- `CONTRIBUTING.md` und erweiterte Entwickler-Dokumentation

## Lizenz

**`Dsms.Web`** in diesem Repository steht unter der **GNU Affero General Public License v3.0**. Details finden Sie in der Datei [`LICENSE.txt`](./LICENSE.txt).

Die private Provisioning-App **`Dsms.Provisioning`** ist **nicht** Bestandteil dieses Repositories und **nicht** unter dieser Lizenz veröffentlicht.

Wenn Sie Datenschutz-Cloud in einem eigenen SaaS- oder Netzwerkbetrieb einsetzen oder verändern möchten, prüfen Sie bitte die Pflichten der AGPL-3.0 sorgfältig.

## Rechtlicher Hinweis

Datenschutz-Cloud ist ein Werkzeug zur Unterstützung von Datenschutzprozessen und ersetzt keine individuelle Rechtsberatung. Die Verantwortung für die rechtliche Bewertung und Umsetzung bleibt beim jeweiligen Betreiber bzw. bei der jeweiligen Organisation.

## Kontakt

- **Technische Fragen:** [GitHub Issues](https://github.com/SysAdminHub/Dsms/issues)
- **Webseite:** [datenschutz-cloud.eu](https://datenschutz-cloud.eu)

## Weitere Dokumentation

| Datei | Inhalt |
|-------|--------|
| [Project_Overview.md](./Project_Overview.md) | Fachliche Gesamtübersicht |
| [Architecture.md](./Architecture.md) | Technische Architektur |
| [Production_Deployment.md](./Production_Deployment.md) | Docker-Deployment (technische Referenz) |
| [Changelog.md](./Changelog.md) | Änderungshistorie |
