# Website-Konzept Datenschutz-Cloud

> **Zweck dieser Datei:** Vollständiges Briefing für ein späteres separates Blazor-Marketingprojekt (`Dsms.Marketing` / `DatenschutzCloud.Marketing`).  
> **Stand:** Abgeleitet aus dem Code und der Dokumentation in `Dsms.Web` (Juni 2026). Keine Codeänderungen in diesem Schritt.

---

## 1. Ziel der Marketingseite

Die öffentliche Marketingseite unter **https://datenschutz-cloud.eu** soll:

- als **Landingpage / OnePager** Vertrauen schaffen und das Produkt verständlich erklären,
- **Leads und Registrierungen** zur SaaS-App lenken,
- die **Demo-Umgebung** erklären und verlinken,
- **keine interne App-Funktion** und **keine Mandantendaten** enthalten,
- **ohne Authentifizierung** auskommen,
- **schnell, schlank und sicher** sein (statische Inhalte, kein DB-Zugriff in V1).

**Verlinkte Zielsysteme (nicht Teil des Marketingprojekts):**

| System | Geplante Domain | Rolle |
|--------|-----------------|-------|
| Marketing | `datenschutz-cloud.eu` | Öffentliche Produktseite |
| SaaS-App | `app.datenschutz-cloud.eu` | Login, Registrierung, Mandantenbetrieb |
| Demo | `demo.datenschutz-cloud.eu` | Vorbereitete Demo-Instanz (nur Beispieldaten) |

---

## 2. Zielgruppen

Aus Produktkontext und vorhandener Zielgruppendefinition (`Project_Overview.md`, README):

| Zielgruppe | Relevanz | Typischer Nutzen |
|------------|----------|------------------|
| **KMU** | Hoch | Strukturierte DSGVO-Dokumentation ohne Excel-Chaos |
| **Vereine** | Hoch | Einfaches DSMS für überschaubare Organisationen |
| **Externe Datenschutzbeauftragte (DSB)** | Hoch | Mehrere Mandanten/Lizenzen zentral verwalten |
| **Interne Datenschutzkoordinatoren** | Hoch | VVT, Maßnahmen, Nachweise im Team |
| **IT-Dienstleister / MSPs** | Mittel | Mandantenfähigkeit, Supportzugriff, Lizenzmodell |
| **Organisationen mit pragmatischem DSMS-Bedarf** | Hoch | Fokus auf Umsetzung statt Enterprise-Komplexität |

**Abgrenzung:** Kein Enterprise-Compliance-Suite-Marketing; Zielgruppe sind pragmatische Anwender im **DACH-Raum**.

---

## 3. Produktpositionierung

**Datenschutz-Cloud** ist ein mandantenfähiges Datenschutzmanagementsystem für den deutschsprachigen Raum.

Es unterstützt Organisationen dabei,

- Datenschutzprozesse **strukturiert zu dokumentieren** (z. B. Verarbeitungsverzeichnis, TOMs, DSFAs),
- **Maßnahmen nachzuverfolgen** und Audits durchzuführen,
- **Schulungen zu verwalten** inklusive Teilnehmerportal und Nachweisen,
- **Nachweise zentral bereitzustellen** und bei Bedarf exportierbar zu halten.

Technischer Projektname: `Dsms.Web` (.NET 9, Blazor Server). Sichtbarer Produktname: **Datenschutz-Cloud** (`AppBranding:ProductName`).

---

## 4. Hauptnutzen / Value Proposition

| Nutzen | Kurzbeschreibung |
|--------|------------------|
| Weg von Excel & Ordnern | Zentrale, durchsuchbare Datenschutzdokumentation |
| Klare Verantwortlichkeiten | Rollen, Organisation/Datenschutzrollen, Mandantenstruktur |
| Nachvollziehbare Maßnahmen | Maßnahmenmodul, Verknüpfungen zu Audits und Vorfällen |
| Audit- & Protokollfähigkeit | Audit-Durchläufe, Auditlog, Plattform-Protokoll (Metadaten) |
| Schulungen & Nachweise | Vorlagen, Durchführungen, Teilnehmerportal, Quiz |
| Mandantenfähig | Für DSBs, MSPs und Mehr-Mandanten-Lizenzen |
| Datenschutzfreundlicher Support | Supportzugriff nur nach Freigabe, zeitlich begrenzt, protokolliert |
| Globale & Community-Vorlagen | Plattform-Audit- und Schulungsvorlagen, Community-Einreichungen |
| Demo-Umgebung | Produkt unverbindlich kennenlernen (geplante Domain, siehe Abschnitt 9) |
| Öffentliche Registrierung | Tarifauswahl und Self-Service-Provisioning über `/signup` |

**Formulierungshilfen (ohne Rechtsgarantien):**

- „unterstützt bei der DSGVO-Dokumentation“
- „hilft bei der strukturierten Nachweisführung“
- „datenschutzfreundlich konzipiert“
- „mit kontrolliertem Supportzugriff“

---

## 5. Wichtige Produktmodule

Abgeleitet aus `NavMenu.razor`, Fachseiten und `Architecture.md`.

| Modul | Route(n) | Zweck | Mandant / Plattform | Marketing-tauglich |
|-------|----------|-------|---------------------|----------------------|
| **Dashboard** | `/` | Kennzahlen, Donut-Kacheln, „Erste Schritte“, offene Maßnahmen/Audits | Mandant | Ja – Überblick |
| **VVT** | `/processing-activities` | Verzeichnis von Verarbeitungstätigkeiten (Art. 30) | Mandant | Ja – Kernmodul |
| **DSFA** | `/dsfa` | Datenschutz-Folgenabschätzungen | Mandant | Ja |
| **TOMs** | `/toms` | Technische und organisatorische Maßnahmen | Mandant | Ja |
| **Dienstleister** | `/service-providers` | Auftragsverarbeiter, AVV, Drittland | Mandant | Ja |
| **Datenschutzvorfälle** | `/incidents` | Vorfallregister, Meldebewertung, Verknüpfungen | Mandant | Ja |
| **Betroffenenanfragen** | `/data-subject-requests` | DSGVO-Anfragen dokumentieren | Mandant | Ja |
| **Maßnahmen** | `/measures` | Aufgaben, Fälligkeiten, Audit-Verknüpfung | Mandant | Ja |
| **Organisation** | `/organization` | Datenschutzrollen, Organigramm | Mandant | Ja – Verantwortlichkeiten |
| **Audit-Vorlagen** | `/audit-templates` | Eigene, offizielle, Community-Vorlagen | Mandant + global | Ja – Vorlagen-Story |
| **Audit-Durchläufe** | `/audit-runs` | Audits durchführen, Fragen beantworten | Mandant | Ja |
| **Dokumente** | `/documents` | Nachweisdokumente, Kategorien, Verknüpfungen | Mandant | Ja |
| **Schulungen** | `/trainings` | Konkrete Schulungsdurchführungen | Mandant | Ja – USP |
| **Schulungsvorlagen** | `/training-templates` | Markdown-Karten, Quiz, Assets | Mandant + global | Ja |
| **Teilnehmerverwaltung** | `/trainings/participants` | Stammdaten, Bulk-Import | Mandant | Teilweise (B2B-Detail) |
| **Teilnehmerportal** | `/schulung/teilnahme` | Öffentlicher Zugang E-Mail + Code (kein App-Login) | Mandant | Ja – Demo-Story |
| **Globale Audit-Vorlagen** | `/audit-templates` (Superuser) | Offizielle Plattformvorlagen | Plattform | Ja |
| **Community Audit-Vorlagen** | `/platform/audit-templates/community` | Prüfung eingereichter Vorlagen | Plattform | Ja – Community |
| **Globale Schulungsvorlagen** | `/training-templates` (Superuser) | Plattform-Schulungsvorlagen | Plattform | Ja |
| **Community-Schulungsvorlagen** | `/platform/training-templates/community` | Prüfung eingereichter Schulungsvorlagen | Plattform | Ja |
| **Supportzugriff (Admin)** | `/admin/support-access` | Zeitlich begrenzte Freigabe für Support | Mandant | Ja – Trust |
| **Supportzugriff (Plattform)** | `/platform/support-access` | Superuser-Übersicht, Supportmodus | Plattform | Intern, nicht im Detail |
| **Auditlog (Admin)** | `/admin/auditlog` | Mandanten-Auditlog inkl. Feldänderungen | Mandant | Nein (intern) |
| **Plattform-Protokoll** | `/platform/logs` | Mandantenübergreifend, Metadaten-only | Plattform | Nein (intern) |
| **Benutzerverwaltung** | `/users` | Konten, Rollen | Mandant / Plattform | Nein |
| **Mandantenverwaltung** | `/tenants` | Organisationseinheiten | Plattform | Nein |
| **Lizenzen / Pläne** | `/platform/licenses`, `/platform/plans` | SaaS-Abrechnungsgrundlage | Plattform | Ja – Preisseite (Limits) |
| **Rabattcodes** | `/platform/discount-codes` | Aktionen beim Signup | Plattform | Optional Marketing |
| **Öffentliche Registrierung** | `/signup` | Tarifwahl, Provisioning | Plattform → Mandant | Ja – CTA |
| **Legal (in App)** | `/legal/{route}` | Impressum, AGB, AVV, … | Plattform | Inhalte wiederverwendbar |
| **Demo-Umgebung** | `demo.datenschutz-cloud.eu` | Separate Instanz | — | Ja – eigene Demo-Seite |

---

## 6. Vertrauens- und Datenschutzargumente

Aus implementierter Architektur (`Architecture.md`, `Logging.md`, Support-Services):

| Argument | Umsetzung im Projekt |
|----------|----------------------|
| Mandantenfähige Architektur | `TenantId`-Filter, Session-Mandantenkontext |
| Superuser ohne Fachdaten-Zugriff | Superuser standardmäßig ohne Mandantenkontext; Fachmodule gesperrt |
| Supportzugriff nur nach Freigabe | Mandanten-Admin unter `/admin/support-access` (1 h / 4 h / 24 h / 7 Tage) |
| Supportzugriff zeitlich begrenzt | `SupportAccessGrant.ValidUntil`, serverseitige Prüfung |
| Supportzugriff widerrufbar | `RevokedAt` auf Grant |
| Supportmodus protokolliert | Auditlog mit `[Supportmodus]` und `SupportAccessGrantId` |
| Plattform-Protokoll ohne Fachinhalte | `/platform/logs`: Metadaten-only; Business-Entity-Namen redigiert |
| Admin-Auditlog | `/admin/auditlog`: Feldänderungen für eigenen Mandanten |
| Globale Vorlagen = Plattformdaten | `TenantId = null` bei offiziellen/globalen Vorlagen |
| Mandantendaten geschützt | Kein Marketing-DB-Zugriff geplant |
| IP-Anonymisierung | `IIpAnonymizationService` / `LogIpAnonymizer` (IPv4 /24, IPv6 /64) bei LegalAcceptance und Logs |
| Legal-Dokumente vorhanden | Markdown unter `Dsms.Web/Legal/current/`; App-Routen `/legal/*` |
| Demo nur Beispieldaten | **Annahme / TODO:** Konzept für `demo.datenschutz-cloud.eu` separat definieren |

**Nicht formulieren:** „100 % DSGVO-konform“, „garantiert rechtsicher“.

---

## 7. Seitenstruktur der Marketingseite

Geplante Routen im Marketingprojekt (statisch, ohne DB in V1):

| Route | Ziel | Inhalt |
|-------|------|--------|
| `/` | Conversion & Orientierung | OnePager (siehe Abschnitt 8) |
| `/funktionen` | Produktverständnis | Modulübersicht mit Nutzen je Bereich |
| `/preise` | Tarifentscheidung | Pläne, Limits, Link zu `/signup` – **TODO:** finale Preise (siehe Abschnitt Preise) |
| `/demo` | Demo erklären | Text zur Demo-Umgebung, Link zu `https://demo.datenschutz-cloud.eu`, Hinweis auf Beispieldaten |
| `/kontakt` | Lead / Anfrage | V1: Mailto oder statische Kontaktdaten; später optional Formular |
| `/faq` | Einwände klären | Häufige Fragen zu DSMS, Mandanten, Support, Demo |
| `/impressum` | Rechtlich | Inhalt aus Legal-Markdown oder eigene Pflege |
| `/datenschutz` | Rechtlich | Datenschutzerklärung für Marketingseite (ggf. abweichend von App) |
| `/agb` | Rechtlich | AGB-Verweis oder Kurzfassung + Link zur App-Version |
| `/avv` | B2B-Vertrauen | AVV-Übersicht / Link zur App-Version `/legal/avv` |
| `/unterauftragnehmer` | Optional | Liste/Unterauftragnehmer – Inhalt in `Legal/current/unterauftragnehmerliste.md` |
| `/toms` | Optional | TOM-Übersicht – Inhalt in `Legal/current/tom.md` |
| Login-Link | App-Weiterleitung | Direktlink zu App-Login (kein Redirect-Proxy nötig) |
| Registrierungs-Link | App-Weiterleitung | Direktlink zu `/signup` |

### `/demo`

- Erklärt die Demo-Umgebung und deren Zweck (Kennenlernen ohne Vertragsbindung).
- Verlinkt auf **https://demo.datenschutz-cloud.eu**.
- Betont: nur **Beispieldaten**, keine echten Kundendaten.
- **Keine Demo-Zugangsdaten** in dieser Datei oder auf der Marketingseite ohne separates öffentliches Konzept.

### `/preise`

**Im Projekt vorhanden (`SubscriptionPlanSeeder`):**

| Plan | `IsPublicSignupEnabled` | Preise im Seed | Limits (Auszug) |
|------|-------------------------|----------------|-----------------|
| Free | Ja | 0 EUR | 1 Mandant, 2 User, 5 VVT, … |
| Basic | Nein | `null` (nicht gesetzt) | 1 Mandant, 5 User, … |
| Pro | Nein | `null` | 5 Mandanten, 25 User/Mandant, … |
| Business | Nein | `null` | 25 Mandanten, 100 User/Mandant, unbegrenzte VVT/TOMs |

**TODO:** Finale Marketing-Preise für Basic/Pro/Business festlegen und pflegen.  
**TODO:** Ob Preise dynamisch aus API/DB geladen werden sollen (Marketing V2) oder statisch gepflegt werden.

Sonderpreise/Rabatte existieren in der App (`IsPromotionalPriceEnabled`, `DiscountCode`), sind für Marketing optional.

### `/kontakt`

- V1 ohne Datenbank: `support@datenschutz-cloud.eu` (`AppBranding:SupportEmail`) oder Kontaktformular später.
- Demo-Anfrage vs. allgemeiner Kontakt bewusst trennen (später).

---

## 8. OnePager-Aufbau (Startseite `/`)

Reihenfolge und Textideen:

1. **Hero**
   - Headline: „Datenschutz strukturiert managen – in einer Cloud.“
   - Subline: „VVT, TOMs, DSFA, Audits, Schulungen und Nachweise – zentral für Ihre Organisation.“
   - CTAs: „Jetzt starten“ → App-Registrierung; „Demo ansehen“ → `/demo`

2. **Problem**
   - „Excel-Listen, E-Mail-Anhänge, unklare Zuständigkeiten – so entstehen Lücken in der DSGVO-Dokumentation.“

3. **Lösung**
   - „Datenschutz-Cloud bündelt Ihre Datenschutzprozesse in einem mandantenfähigen System.“

4. **Modulübersicht**
   - Karten: VVT, TOMs, DSFA, Dienstleister, Audits, Maßnahmen, Dokumente, Schulungen, Vorfälle

5. **Datenschutz / Trust**
   - Mandantentrennung, anonymisierte Protokoll-IPs, Legal-Dokumente, Hosting in EU (nur wenn verifiziert – **TODO**)

6. **Supportzugriff**
   - „Support nur mit Ihrer Freigabe – zeitlich begrenzt und protokolliert.“

7. **Schulungen & Community-Vorlagen**
   - Online-Schulungen mit Quiz; globale und Community-Vorlagen für Audits und Schulungen

8. **Demo-Bereich**
   - „Datenschutz-Cloud unverbindlich kennenlernen“
   - „Testen Sie die wichtigsten Funktionen in einer vorbereiteten Demo-Umgebung.“
   - Button → `https://demo.datenschutz-cloud.eu`

9. **Zielgruppen**
   - KMU, Vereine, DSB, interne Koordinatoren, IT-Dienstleister

10. **Preise / Pläne**
    - Free-Einstieg hervorheben; weitere Pläne **TODO** (Preise)

11. **FAQ**
    - Kurz-Ausschnitt, Link zu `/faq`

12. **Call-to-Action**
    - „Kostenlos registrieren“ / „Jetzt starten“

13. **Footer**
    - Legal-Links, App-Login, Support-E-Mail, Version optional nicht nötig

---

## 9. Call-to-Actions und Ziel-URLs

### Geplante Domainstruktur

| Zweck | URL |
|-------|-----|
| Marketing | https://datenschutz-cloud.eu |
| SaaS-App | https://app.datenschutz-cloud.eu |
| Demo | https://demo.datenschutz-cloud.eu |

**Hinweis:** In `appsettings.json` steht `AppBranding:WebsiteUrl` als `https://www.datenschutz-cloud.eu` (mit `www`).  
**TODO:** Einheitliche Canonical-Domain (`www` vs. ohne `www`) für Marketing und App-Branding festlegen.

### App-Routen (aus Projekt ermittelt)

Quellen: `@page`-Direktiven, `Login.razor`, `NavMenu.razor`, `IdentityComponentsEndpointRouteBuilderExtensions.cs`, `appsettings.json`.

| Aktion | Route | Vollständige URL (Produktion) |
|--------|-------|-------------------------------|
| **Login** | `/Account/Login` | https://app.datenschutz-cloud.eu/Account/Login |
| **Registrierung (öffentlich)** | `/signup` | https://app.datenschutz-cloud.eu/signup |
| **Registrierung Erfolg** | `/signup/success` | https://app.datenschutz-cloud.eu/signup/success |
| **Legacy Identity-Register** | `/Account/Register` | https://app.datenschutz-cloud.eu/Account/Register |
| **Passwort vergessen** | `/passwort-vergessen` | https://app.datenschutz-cloud.eu/passwort-vergessen |
| **Passwort setzen** | `/passwort-zuruecksetzen` | https://app.datenschutz-cloud.eu/passwort-zuruecksetzen |
| **Logout** | `POST /Account/Logout` | Form-POST (kein Marketing-Link) |
| **2FA Login** | `/Account/LoginWith2fa` | Nach Login-Flow |
| **Schulungs-Teilnehmerlogin** | `/schulung/teilnahme` | Separates Portal, nicht App-Hauptlogin |
| **Legal in App** | `/legal/impressum`, `/legal/datenschutzerklaerung`, `/legal/agb`, `/legal/avv`, `/legal/tom`, `/legal/unterauftragnehmerliste` | In App erreichbar; Marketing kann eigene Legal-Seiten haben |

**Primärer Registrierungsflow:** Login verlinkt auf `signup` („Kostenlos registrieren“ in `Login.razor`).  
`/signup/paid` leitet auf `/signup?preferPaid=true` weiter (Legacy).

**Demo:**

| Punkt | Status |
|-------|--------|
| Domain `demo.datenschutz-cloud.eu` | **TODO:** Nicht im Code/Deployment dokumentiert |
| Demo-Login-Route | Gleiche App-Routen wie Produktion (`/Account/Login`), sofern separate Instanz |
| Demo-Zugangsdaten | **Nicht dokumentieren** – lokaler Seed (`DatabaseSeeder`) nutzt `@demo.local`-Konten nur für Entwicklung |

### Mögliche CTAs

| CTA-Text | Ziel |
|----------|------|
| Jetzt starten | https://app.datenschutz-cloud.eu/signup |
| Kostenlos registrieren | https://app.datenschutz-cloud.eu/signup |
| Einloggen | https://app.datenschutz-cloud.eu/Account/Login |
| Demo ansehen / Demo öffnen | `/demo` → Link zu demo.datenschutz-cloud.eu |
| Demo anfordern | `/kontakt` (später Formular) |
| Datenschutz-Cloud kennenlernen | `/` oder `/demo` |

---

## 10. Design / Branding

Aus `AppBrandingOptions`, `appsettings.json`, CSS:

| Element | Wert / Pfad |
|---------|-------------|
| Produktname | Datenschutz-Cloud |
| Kurzname (Fallback) | DC |
| Tagline | „Datenschutzmanagement einfach verwalten“ |
| Description (Login) | „Ihr Datenschutz-Management-System für VVT, TOMs, DSFA, Dienstleister, Audits und Nachweise.“ |
| Logo | `/datenschutz-cloud-logo.png` → `Dsms.Web/wwwroot/datenschutz-cloud-logo.png` |
| Favicon | `Dsms.Web/wwwroot/favicon.png` |
| Support-E-Mail | support@datenschutz-cloud.eu |
| Tonalität | Professionell, deutsch, verständlich, vertrauenswürdig |
| Zielmarkt | DACH / deutschsprachiger Raum |

### Farben & UI (CSS-Variablen)

Datei: `Dsms.Web/wwwroot/css/dsms-tokens.css`

| Token | Wert |
|-------|------|
| `--dsms-primary` | `#1e4d8c` |
| `--dsms-primary-hover` | `#163a6b` |
| `--dsms-accent` | `#0d9488` |
| `--dsms-bg` | `#f4f6f9` |
| `--dsms-surface` | `#ffffff` |
| `--dsms-text` | `#0f172a` |

Weitere Styles: `dsms-layout.css`, `dsms-components.css`, `app.css`, Bootstrap 5.

Wiederverwendbare UI-Komponenten (Referenz für späteres `Dsms.SharedUi`): `BrandLogo`, `PageHeader`, `StatusBadge`, Card-Patterns aus Signup/Login.

**Marketing-UI:** Helle Flächen, klare Cards, dezente Akzentfarben, SaaS-typischer Aufbau – angelehnt an Login/Signup (`PublicSignupLayout`, `LoginLayout`), nicht an die dunkle App-Sidebar.

---

## 11. Technische Architektur für Marketingprojekt

### Empfohlene Architektur

- Separates Blazor-Projekt in derselben Solution (`Dsms.sln`)
- **Keine** Vermischung mit `Dsms.Web` (kein Shared DbContext)
- **Keine** Mandantenlogik, **keine** Authentifizierung in V1
- Login/Register/Demo nur als **externe Links**
- Legal-Seiten: Markdown oder statische Razor-Komponenten (Inhalte aus `Dsms.Web/Legal/current/` als Referenz, nicht 1:1 kopieren ohne Rechtsprüfung)
- Später optional: Kontaktformular mit eigener DB

### Projektstruktur (Ziel)

```
Dsms.sln
├── Dsms.Web              # SaaS-App (bestehend)
├── Dsms.Marketing        # Öffentliche Marketingseite (geplant)
└── Dsms.SharedUi         # Optional: BrandLogo, Tokens, Layout-Basis
```

### Deployment-Ziel

| Host | Dienst |
|------|--------|
| datenschutz-cloud.eu | Dsms.Marketing |
| app.datenschutz-cloud.eu | Dsms.Web (Produktion) |
| demo.datenschutz-cloud.eu | Dsms.Web (Demo-Instanz, eigene DB) |

### Deployment-Hinweise aus bestehendem Projekt

| Datei | Inhalt |
|-------|--------|
| `docker-compose.yml` | App + MySQL 8, Port 8080, Volumes |
| `Dsms.Web/Dockerfile` | Multi-Stage Production-Image |
| `Production_Deployment.md` | Docker-Betrieb, `.env`, Connection String |
| `Properties/launchSettings.json` | Dev: `https://localhost:7245` |

**TODO:** nginx/Reverse-Proxy-Konfiguration für drei Hosts (Marketing, App, Demo) erstellen – im Repo **nicht** vorhanden.

**TODO:** Demo-Instanz-Konfiguration (eigene DB, Seed-only, kein Public-Signup?) definieren.

---

## 12. Datenbank-Entscheidung

### V1 Marketing

- **Keine Datenbank** im Marketingprojekt
- Inhalte statisch in Blazor/Razor/Markdown
- Kontaktformular später

### Später (optional)

- Gleicher MySQL-Container möglich, aber **eigene Datenbank** z. B. `dsms_marketing`
- Eigener DB-Benutzer
- **Kein** Zugriff auf SaaS-Mandantendatenbank
- **Kein** gemeinsamer `ApplicationDbContext` mit `Dsms.Web`

### Empfohlene Lead-Tabelle (nur Dokumentation)

```
MarketingLead:
  Id, Name, Email, Company, Message,
  ConsentAccepted, PrivacyVersion, CreatedAt,
  Source, IpAddressAnonymized (optional)
```

---

## 13. Datenschutz für Kontakt-/Demoformular (später)

Wenn Kontakt- oder Demo-Anfrageformular gebaut wird:

- Datenschutzhinweis anzeigen
- Pflicht-Checkbox für Datenschutzinformation
- Keine unnötigen Pflichtfelder
- Keine vollständige IP speichern (Anonymisierung wie in App)
- Spam-Schutz und Rate-Limiting
- E-Mail-Versand oder Lead-Tabelle
- Keine Drittanbieter-Übergabe ohne Dokumentation
- Demo-Anfragen nicht mit Mandantendaten vermischen

---

## 14. SEO-Grundlagen

- Pro Seite: `<title>`, Meta Description, eine H1
- Sprechende URLs (`/funktionen`, `/preise`, …)
- Schnelle Ladezeiten (statisches Blazor/WebAssembly oder SSR ohne schwere API-Calls)
- Strukturierte Inhalte, FAQ für Rich Results
- OpenGraph: Titel, Description, og:image (Logo)
- **Später:** `sitemap.xml`, `robots.txt`
- **Entscheidung TODO:** Demo-Seite indexierbar ja/nein
- App-/Login-/Register-URLs nicht als Marketing-SEO-Ziele behandeln

---

## 15. Inhalte, die NICHT auf die Marketingseite gehören

- Mandantendaten, echte Benutzerdaten
- Audit-Log-Inhalte, Supportzugriff-Details einzelner Mandanten
- Interne IDs, Admin-Routen (`/platform/*`, `/tenants`, …)
- Superuser-Funktionen im Detail
- Interne Architektur, Secrets, Connection Strings, API Keys
- Demo-Zugangsdaten aus `DatabaseSeeder` / README
- Nicht freigegebene Community-Inhalte

**Sicherheitshinweis:** In `Project_Overview.md` und `README.md` stehen Demo-Zugangsdaten für lokale Entwicklung.  
**TODO:** Prüfen, ob diese ausschließlich in Entwickler-Dokumentation bleiben sollen und nicht in öffentliche Artefakte gelangen.

---

## 16. Offene TODOs für spätere Umsetzung

### Marketingprojekt

- [ ] Blazor-Marketingprojekt `Dsms.Marketing` anlegen
- [ ] Optional `Dsms.SharedUi` für Branding-Komponenten
- [ ] Layout/Header/Footer bauen
- [ ] Startseite (OnePager) bauen
- [ ] Seiten: Funktionen, Preise, Demo, FAQ, Kontakt
- [ ] Legal-Seiten einbinden (Impressum, Datenschutz, AGB, AVV, optional TOM/Unterauftragnehmer)
- [ ] Login-/Register-Links aus `AppBranding` / Konfiguration übernehmen
- [ ] Kontakt-/Demoformular entscheiden und ggf. Lead-DB planen
- [ ] Canonical-Domain (`www` vs. ohne) klären
- [ ] Finale Preise für Basic/Pro/Business festlegen
- [ ] Sitemap und robots.txt
- [ ] Performance- und Mobile-Tests

### Deployment & Domains

- [ ] Deployment Marketing auf datenschutz-cloud.eu
- [ ] App auf app.datenschutz-cloud.eu (bestehendes Docker-Setup erweitern)
- [ ] Demo-Instanz auf demo.datenschutz-cloud.eu (Konzept + DB + Seed)
- [ ] nginx-Konfiguration für drei Hosts
- [ ] Demo-Datenkonzept (keine echten Kundendaten, ggf. öffentlicher Lese-Account ohne Passwort-Leak)

### Dokumentation

- [ ] `Project_Overview.md`, `Architecture.md`, `Changelog.md` bei größeren Produktänderungen fortführen

---

## Anhang: Geprüfte Projektdateien (Routen & Konfiguration)

| Datei | Erkenntnis |
|-------|------------|
| `Dsms.Web/Components/Account/Pages/Login.razor` | Login `/Account/Login`, Link zu `signup` |
| `Dsms.Web/Components/Pages/Signup/Index.razor` | Registrierung `/signup` |
| `Dsms.Web/Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs` | Logout `POST /Account/Logout` |
| `Dsms.Web/appsettings.json` | `AppBranding`, `WebsiteUrl`, `AppUrl` |
| `Dsms.Web/Configuration/AppBrandingOptions.cs` | Branding-Defaults |
| `Dsms.Web/Legal/legal-documents.json` | Legal-Routen |
| `Dsms.Web/Components/Pages/Legal/LegalDocumentPage.razor` | `/legal/{DocumentRoute}` |
| `docker-compose.yml`, `Production_Deployment.md` | Docker, keine Domain-Mappings |
| `Dsms.Web/Data/Seed/SubscriptionPlanSeeder.cs` | Demo-Tarife und Preise |
