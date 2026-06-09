# Production Deployment mit Docker

Diese Anleitung beschreibt, wie DSMS als Open-Source-taugliches Docker-Deployment betrieben wird. Produktive Secrets gehören **nicht** ins Repository oder ins Image, sondern werden über `.env` und Environment Variables gesetzt.

## 1. Ziel

DSMS soll als Docker Image gebaut und z. B. über Docker Hub veröffentlicht werden können. Im Production-Betrieb laufen:

- die **DSMS-Blazor-Server-Anwendung** als Container
- **MySQL 8** als separater Datenbank-Container
- **persistente Volumes** für Datenbank, Dateiuploads und ASP.NET Data Protection Keys

## 2. Voraussetzungen

- [Docker](https://docs.docker.com/get-docker/)
- [Docker Compose](https://docs.docker.com/compose/) (in Docker Desktop enthalten)
- MySQL 8 wird **über Docker Compose** gestartet – keine separate MySQL-Installation nötig

Optional für den Image-Build:

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (nur wenn das Image lokal gebaut wird)

## 3. Relevante Dateien

| Datei | Zweck |
|-------|--------|
| `docker-compose.yml` | Startet App + MySQL mit Volumes und Environment Variables |
| `.env` | Lokale/produktive Secrets und Einstellungen (**nicht committen**) |
| `.env.example` | Vorlage mit Platzhalterwerten (**im Repository**) |
| `Dsms.Web/Dockerfile` | Multi-Stage Build für das Production-Image |
| `Dsms.Web/appsettings.json` | Basis-Konfiguration mit **lokalem** Development-Fallback |

## 4. Warum keine produktiven Secrets in appsettings.json?

`appsettings.json` liegt im Repository und wird ins Docker Image kopiert. Echte Passwörter, API Keys oder produktive Connection Strings wären damit für jeden sichtbar und blieben in Git-Historie und Images erhalten.

Stattdessen:

- `appsettings.json` enthält nur unkritische Defaults und Platzhalter
- Production-Werte kommen aus Environment Variables (über `.env` + `docker-compose.yml`)

Die in der Sidebar angezeigte Version wird aus `Application:Version` in `appsettings.json` gelesen (kein Secret; darf im Repository stehen). Für Production kann der Wert bei Bedarf über `Application__Version` als Environment Variable überschrieben werden.

## 5. Lokaler Default-ConnectionString (Entwicklung)

In `Dsms.Web/appsettings.json` bleibt dieser Fallback für lokales `dotnet run` erhalten:

```
Server=localhost;Port=3306;Database=dsms_dev;User=root;Password=changeme;CharSet=utf8mb4;
```

`changeme` ist ein bewusster **lokaler Platzhalter**, kein produktives Secret.

Für lokale Entwicklung kann zusätzlich nur die Datenbank per Compose gestartet werden:

```bash
docker compose up -d db
```

## 6. Production: Connection String per Environment Variable

Im Docker-Production-Betrieb überschreibt diese Variable den lokalen Default:

```
ConnectionStrings__DefaultConnection
```

Die Anwendung lädt den Wert über den Standard-.NET-Weg:

```csharp
builder.Configuration.GetConnectionString("DefaultConnection")
```

In `docker-compose.yml` wird der Wert gesetzt (ohne echte Passwörter in der Datei):

```yaml
ConnectionStrings__DefaultConnection: Server=db;Port=3306;Database=${MYSQL_DATABASE};User=${MYSQL_USER};Password=${MYSQL_PASSWORD};CharSet=utf8mb4;
```

## 7. `.env` aus `.env.example` erstellen

```bash
cp .env.example .env        # Linux/macOS
copy .env.example .env      # Windows
```

Dann in `.env` starke Passwörter setzen:

```env
MYSQL_DATABASE=dsms
MYSQL_USER=dsms_user
MYSQL_PASSWORD=<starkes-passwort>
MYSQL_ROOT_PASSWORD=<starkes-root-passwort>
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
```

**`.env` niemals committen.**

## 8. Environment Variables

| Variable | Beschreibung |
|----------|----------------|
| `MYSQL_DATABASE` | Datenbankname |
| `MYSQL_USER` | Anwendungsbenutzer für MySQL |
| `MYSQL_PASSWORD` | Passwort des Anwendungsbenutzers |
| `MYSQL_ROOT_PASSWORD` | MySQL-Root-Passwort |
| `MYSQL_PORT` | Host-Port für MySQL (Default: `3306`) |
| `ASPNETCORE_ENVIRONMENT` | z. B. `Production` |
| `ASPNETCORE_URLS` | z. B. `http://+:8080` |
| `DSMS_PORT` | Host-Port für die App (Default: `8080`) |
| `DSMS_IMAGE` | Image-Name/Tag (Default: `dsms:latest`) |
| `STORAGE_UPLOAD_PATH` | Relativer Upload-Pfad (Default: `Data/Uploads`) |
| `ConnectionStrings__DefaultConnection` | Wird in `docker-compose.yml` aus MySQL-Variablen zusammengesetzt |

SMTP- und E-Mail-Einstellungen werden **nicht** über `appsettings.json` konfiguriert, sondern über die DSMS-Oberfläche unter `/platform/email/settings` (in der Datenbank, verschlüsselt via Data Protection).

## 9. Docker Volumes

| Volume | Mount im Container | Inhalt |
|--------|-------------------|--------|
| `dsms_mysql_data` | `/var/lib/mysql` | MySQL-Datenbankdateien |
| `dsms_uploads` | `/app/Data/Uploads` | Hochgeladene Nachweisdokumente |
| `dsms_dataprotection` | `/app/DataProtection-Keys` | ASP.NET Data Protection Keys (Login, SMTP-Verschlüsselung, Tokens) |

Ohne persistente Volumes gehen Daten bei Container-Neustarts oder Image-Updates verloren.

## 10. System starten

```bash
docker compose up -d
```

Nur die Datenbank (z. B. für lokales `dotnet run`):

```bash
docker compose up -d db
```

Erster Start: Migrationen und Seeding laufen automatisch beim App-Start.

Die App ist erreichbar unter `http://localhost:8080` (oder dem in `DSMS_PORT` gesetzten Port).

## 11. Logs ansehen

```bash
docker compose logs -f dsms
```

MySQL-Logs:

```bash
docker compose logs -f db
```

## 12. Updates einspielen

**Bei lokalem Build:**

```bash
docker compose build dsms
docker compose up -d
```

**Bei Image aus Registry (z. B. Docker Hub):**

In `.env` den Image-Namen setzen:

```env
DSMS_IMAGE=ihr-benutzername/dsms:latest
```

Dann:

```bash
docker compose pull dsms
docker compose up -d
```

## 13. Backups

Regelmäßig sichern:

1. **MySQL-Volume** (`dsms_mysql_data`) – enthält alle Anwendungsdaten
2. **Upload-Volume** (`dsms_uploads`) – enthält hochgeladene Dateien
3. **Data-Protection-Volume** (`dsms_dataprotection`) – ohne diese Keys sind verschlüsselte SMTP-Passwörter und Auth-Cookies nach Neustart ggf. ungültig

Beispiel (Volume-Pfad ermitteln und archivieren):

```bash
docker volume inspect dsms_mysql_data
```

Für produktive Umgebungen empfiehlt sich zusätzlich ein dediziertes MySQL-Backup (z. B. `mysqldump`).

## 14. Sicherheitshinweise

- Starke, eindeutige Passwörter in `.env` verwenden
- `.env` **nicht** ins Repository committen (steht in `.gitignore`)
- `appsettings.Production.json` mit echten Secrets **nicht** committen
- HTTPS über einen **Reverse Proxy** (nginx, Traefik, Caddy) vor dem Container terminieren
- MySQL-Port (`3306`) in Production ggf. nicht nach außen veröffentlichen
- SMTP-Zugangsdaten nur über die DSMS-Admin-Oberfläche pflegen
- Falls früher echte Secrets committed wurden: aus Git-Historie entfernen und betroffene Passwörter rotieren

## 15. Image bauen und veröffentlichen

```bash
docker build -f Dsms.Web/Dockerfile -t ihr-benutzername/dsms:latest .
docker push ihr-benutzername/dsms:latest
```

Das Image enthält:

- `appsettings.json` mit lokalem Development-Fallback (`localhost` / `changeme`)
- **keine** `.env`
- **keine** `appsettings.Production.json` mit Secrets
- **keine** Upload-Dateien
