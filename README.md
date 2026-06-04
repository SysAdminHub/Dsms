# DSMS – Datenschutz-Management (Version 1)

Einfaches Grundgerüst für ein Datenschutzmanagementsystem als **Blazor Server**-Webanwendung mit **Entity Framework Core**, **MySQL** (Pomelo) und **ASP.NET Core Identity**.

## Voraussetzungen

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (oder neuer)
- [MySQL 8](https://dev.mysql.com/downloads/) (lokal oder Docker)
- Visual Studio 2022+ oder `dotnet` CLI

## Schnellstart

1. **Connection String** in `Dsms.Web/appsettings.Development.json` anpassen:

```json
"DefaultConnection": "Server=localhost;Port=3306;Database=dsms_dev;User=root;Password=IHR_PASSWORT;CharSet=utf8mb4;"
```

2. **MySQL starten** (optional per Docker):

```bash
docker compose up -d
```

Oder Datenbank manuell anlegen:

```sql
CREATE DATABASE dsms_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

3. **Projekt öffnen**: `Dsms.sln` in Visual Studio öffnen, oder per CLI:

```bash
cd Dsms.Web
dotnet run
```

Migrationen werden beim ersten Start automatisch angewendet (inkl. Demo-Daten).

4. Im Browser anmelden mit einem Demo-Benutzer:

| E-Mail | Passwort | Rolle |
|--------|----------|-------|
| admin@demo.local | Demo123! | Admin |
| auditor@demo.local | Demo123! | Auditor |
| user@demo.local | Demo123! | User |

## Projektstruktur

```
Dsms.Web/
  Domain/          Kernmodell (Entities, Enums, Rollen)
  Data/            DbContext, Identity, Seed
  Services/        Anwendungslogik (Dashboard, Dateien, User-Kontext)
  Components/      Blazor UI (Layout, Pages, Account)
```

## Code-Dokumentation

Öffentliche Klassen und zentrale Logik sind mit **XML-Kommentaren** (`///`) dokumentiert (deutsch).
Wichtige Razor-Seiten haben eine kurze Kopfzeile (`@* … *@`); komplexe Stellen sind inline kommentiert.

| Bereich | Wo nachlesen |
|---------|----------------|
| Mandant / Benutzerkontext | `Services/ICurrentUserContext.cs`, `Data/ApplicationUser.cs` |
| Authentifizierung | `Program.cs`, `Components/Account/Identity*.cs`, `Routes.razor` |
| Datenmodell & EF | `Domain/`, `Data/ApplicationDbContext.cs` |
| Demo-Daten | `Data/Seed/DatabaseSeeder.cs` |
| Audit-Lebenszyklus | `Components/Pages/AuditRuns/Edit.razor`, `Answers.razor` |

## EF-Migrationen (manuell)

```bash
cd Dsms.Web
dotnet ef migrations add MeinMigrationName
dotnet ef database update
```

## Hinweise

- Keine Enterprise-Mandantenisolation: Benutzer sind einem `TenantId` zugeordnet.
- Rollen: `Admin`, `Auditor`, `User`.
- Uploads liegen unter `Dsms.Web/Data/Uploads/` (gitignored).

## Lizenz

Internes Projekt – Lizenz nach Bedarf ergänzen.
