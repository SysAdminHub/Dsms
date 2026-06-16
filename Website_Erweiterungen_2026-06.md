# Website-Erweiterungen nach Provisioning-Split und Produktupdates

> **Stand:** Juni 2026  
> **Bezug:** Ergänzung zu [`Website.md`](Website.md) – **ersetzt diese Datei nicht**, sondern dokumentiert notwendige Korrekturen und neue Marketing-Inhalte.  
> **Hinweis:** Keine Codeänderungen; reines Marketing-/Briefing-Dokument.

---

## 1. Zusammenfassung der notwendigen Änderungen

Seit Erstellung von `Website.md` hat sich die Produktarchitektur grundlegend geändert. Die Marketing-Webseite muss folgende Punkte berücksichtigen:

| Thema | Änderung |
|-------|----------|
| **Registrierung** | Nicht mehr in `Dsms.Web` unter `/signup`, sondern über **`Dsms.Provisioning`** auf `https://signup.datenschutz-cloud.eu` |
| **Domainstruktur** | Vier Zielsysteme statt drei: Marketing, App, Provisioning/Signup, Demo (+ GitHub) |
| **Open Source** | Datenschutz-Cloud soll als Open-Source-Projekt kommuniziert werden (GitHub: `https://github.com/SysAdminHub/Dsms`) |
| **Externer DSB** | Zusatzleistung: Unterstützung als externer Datenschutzbeauftragter auf Anfrage |
| **Schulungsmodul** | Stärker vermarkten: PDF-Teilnahmebescheinigungen, Ablage im Dokumentenmodul, Verknüpfung mit Schulung/Teilnahme |
| **Lizenz-/Planlogik** | Funktionsmodule (insb. Schulungen) über Pläne/Lizenzen steuerbar |
| **Trust-Argumente** | Mandantenlöschung nur auf Anfrage, manuelle Prüfung durch Superuser |
| **CTA-Ziele** | Alle Registrierungs-CTAs auf Signup-Domain umstellen |

---

## 2. Korrektur der Zielsysteme und Domains

### Aktualisierte Zielsystem-Tabelle

| System | Domain | Rolle | Projekt |
|--------|--------|-------|---------|
| **Marketing** | `https://datenschutz-cloud.eu` | Öffentliche Landingpage, OnePager, Preise, FAQ, Kontakt | Geplant: `Dsms.Marketing` |
| **Fachanwendung** | `https://app.datenschutz-cloud.eu` | Login, Mandantenbetrieb, Datenschutzmodule | `Dsms.Web` |
| **Provisioning / Signup** | `https://signup.datenschutz-cloud.eu` | Öffentliche Registrierung, Pläne, Rabattcodes, Lizenzen, kaufmännische SaaS-Funktionen | `Dsms.Provisioning` |
| **Demo** | `https://demo.datenschutz-cloud.eu` | Vorbereitete Demo-Instanz (nur Beispieldaten) | `Dsms.Web` (eigene Instanz) |
| **GitHub (Open Source)** | `https://github.com/SysAdminHub/Dsms` | Quellcode, Issues, Community | Repository |

### Veraltete Aussagen in `Website.md`

| Stelle in `Website.md` | Problem | Korrektur |
|------------------------|---------|-----------|
| Abschnitt 1, Tabelle Zeile 24 | „SaaS-App: Login, **Registrierung**, Mandantenbetrieb" | Registrierung entfernen; eigene Zeile für Provisioning ergänzen |
| Abschnitt 4, Zeile 74 | „Öffentliche Registrierung … über `/signup`" | Auf Signup-Domain verweisen, nicht auf App-Route |
| Abschnitt 5, Zeilen 117–119 | Lizenzen, Rabattcodes, Registrierung als App-Routen (`/platform/*`, `/signup`) | In Provisioning-App verschieben oder als externes System beschreiben |
| Abschnitt 7, Zeile 157 | `/preise` → Link zu `/signup` | Link zu `https://signup.datenschutz-cloud.eu` |
| Abschnitt 7, Zeile 168 | „Registrierungs-Link → `/signup`" | → `https://signup.datenschutz-cloud.eu` |
| Abschnitt 8, Zeile 207 | CTA „Jetzt starten" → App-Registrierung | → Signup-Domain |
| Abschnitt 9, Zeilen 269–280 | Registrierungs-URLs unter `app.datenschutz-cloud.eu/signup` | → `signup.datenschutz-cloud.eu` |
| Abschnitt 9, Zeile 279 | „Primärer Registrierungsflow: Login verlinkt auf signup" | Login verlinkt ggf. auf externe Signup-URL (`AppUrls:SignupAppBaseUrl`) |
| Abschnitt 11, Zeilen 362–366 | Deployment nur für drei Hosts | Vierten Host `signup.datenschutz-cloud.eu` für `Dsms.Provisioning` ergänzen |
| Anhang, Zeilen 487–488 | Signup-Seiten in `Dsms.Web` als Referenz | Als veraltet markieren; Signup liegt in `Dsms.Provisioning` |

---

## 3. Aktualisierte Call-to-Actions

| CTA-Text | Ziel-URL | Verwendung |
|----------|----------|------------|
| **Jetzt starten** | `https://signup.datenschutz-cloud.eu` | Hero, Footer, OnePager-Abschluss |
| **Kostenlos registrieren** | `https://signup.datenschutz-cloud.eu` | Preisseite, FAQ, wiederholte Conversion-Punkte |
| **Einloggen** | `https://app.datenschutz-cloud.eu/Account/Login` | Header, Footer |
| **Demo ansehen** / **Demo öffnen** | `https://demo.datenschutz-cloud.eu` (oder Marketing-Seite `/demo` mit Weiterleitung) | Hero, Demo-Bereich |
| **GitHub-Repository ansehen** | `https://github.com/SysAdminHub/Dsms` | Open-Source-Sektion |
| **Anfrage stellen** | `/kontakt` | Externer DSB, allgemeine Anfragen |
| **Erstgespräch anfragen** | `/kontakt` | Optional, externer DSB |
| **Demo anfordern** | `/kontakt` | Wenn keine öffentlichen Demo-Zugangsdaten |

### CTAs, die entfernt oder ersetzt werden müssen

- ~~`https://app.datenschutz-cloud.eu/signup`~~ → **`https://signup.datenschutz-cloud.eu`**
- ~~`https://app.datenschutz-cloud.eu/signup/success`~~ → Erfolgsseite liegt in Provisioning (Route dort prüfen, z. B. `/signup/success`)
- ~~`https://app.datenschutz-cloud.eu/Account/Register`~~ → Legacy; nicht mehr als Registrierungs-CTA verwenden

---

## 4. Neue Sektion: Open Source

### Positionierung auf der Marketingseite

Empfohlene Platzierung: Nach dem Trust-Bereich oder als eigene Sektion vor dem Footer – zwischen „Datenschutz / Trust" und „Preise / Pläne".

### Titelvorschläge

- **Open Source statt Blackbox** (prägnant)
- **Transparente Datenschutzsoftware** (alternativ)

### Textbausteine (direkt verwendbar)

**Kurzversion (Hero-Nebenbox oder Trust-Badge):**

> Datenschutz-Cloud ist Open Source. Der Quellcode ist öffentlich auf GitHub einsehbar. So bleibt die Lösung transparent, nachvollziehbar und unabhängig prüfbar.

**Ausführliche Version (eigene Sektion):**

> **Open Source statt Blackbox**
>
> Datenschutz-Software lebt von Vertrauen. Deshalb ist Datenschutz-Cloud als Open-Source-Projekt verfügbar. Der Quellcode liegt auf GitHub und kann von Organisationen, Datenschutzbeauftragten und IT-Verantwortlichen eingesehen werden.
>
> Transparenz schafft Vertrauen: Sie können nachvollziehen, wie die Software grundsätzlich aufgebaut ist – von der Mandantentrennung über Protokollierung bis zu den Datenschutzmodulen. Open Source eignet sich besonders für Datenschutzsoftware, weil Nachvollziehbarkeit und Unabhängigkeit wichtige Voraussetzungen sind.
>
> Ob Sie die Cloud-Variante nutzen oder die Software selbst betreiben möchten: Der Quellcode bleibt zugänglich.

**Bullet-Points (optional):**

- Quellcode öffentlich auf GitHub
- Transparente Architektur und nachvollziehbare Funktionsweise
- Unabhängig prüfbar – ohne Blackbox
- Für Organisationen, die Wert auf Offenheit legen

### CTA

| Button | Ziel |
|--------|------|
| GitHub-Repository ansehen | `https://github.com/SysAdminHub/Dsms` |

### Hinweis zur Lizenz

Im Repository liegt eine **`LICENSE.txt`** mit der **GNU Affero General Public License v3 (AGPL-3.0)**. Vor Veröffentlichung der Marketingseite sollte die konkrete Lizenzformulierung nochmals anhand der Repository-Lizenz geprüft und ggf. rechtlich abgestimmt werden.

**Empfohlene vorsichtige Formulierung auf der Webseite:**

> Der Quellcode steht unter einer Open-Source-Lizenz (AGPL-3.0). Details finden Sie in der LICENSE-Datei im Repository.

**Nicht ohne Prüfung behaupten:** spezifische Nutzungsrechte, kommerzielle Einschränkungen oder „frei für jeden Zweck" – das hängt von der konkreten Lizenzinterpretation ab.

---

## 5. Neue Sektion: Externer Datenschutzbeauftragter

### Positionierung

Eigene Sektion auf der Startseite oder Unterseite `/leistungen` / Abschnitt auf `/kontakt`. Ziel: Besucher verstehen, dass Datenschutz-Cloud **Software plus optionale fachliche Begleitung** sein kann.

### Titelvorschläge

- **Software plus Datenschutzberatung**
- **Externer Datenschutzbeauftragter auf Anfrage**
- **Mehr als Software: Unterstützung durch einen externen Datenschutzbeauftragten**

### Textbausteine (direkt verwendbar)

**Kurzversion:**

> Sie haben keinen eigenen Datenschutzbeauftragten oder möchten Ihr Datenschutzmanagement strukturiert aufbauen? Auf Anfrage unterstütze ich Sie auch als externer Datenschutzbeauftragter. Der Leistungsumfang und die Preise werden individuell anhand Ihrer Organisation, Größe und Anforderungen abgestimmt.

**Ausführliche Version:**

> **Mehr als Software: Unterstützung durch einen externen Datenschutzbeauftragten**
>
> Nicht jede Organisation verfügt über einen eigenen Datenschutzbeauftragten – und nicht jede möchte das Datenschutzmanagement allein aus dem Stand heraus aufbauen. Wenn Sie fachliche Begleitung benötigen, können Sie Datenschutz-Cloud mit externer Datenschutzbeauftragten-Unterstützung kombinieren.
>
> Die Software dient dabei als Werkzeug für die laufende Dokumentation: Verarbeitungsverzeichnis, Maßnahmen, Schulungsnachweise und Audits bleiben zentral und nachvollziehbar. Die fachliche Betreuung – etwa als externer Datenschutzbeauftragter – ergänzt das technische Fundament.
>
> **Geeignet für:** KMU, Vereine und Organisationen ohne eigene Datenschutzrolle, die pragmatische Unterstützung statt Enterprise-Beratungspakete suchen.
>
> **Preise:** Werden individuell nach Betrieb, Größe, Komplexität und Betreuungsumfang kalkuliert. Es gibt keine pauschalen Festpreise auf der Webseite – jede Organisation hat andere Anforderungen.

**Bullet-Points (optional):**

- Externer Datenschutzbeauftragter auf Anfrage
- Geeignet für KMU, Vereine und Organisationen ohne eigene Datenschutzrolle
- Datenschutz-Cloud als Werkzeug für laufende Dokumentation
- Individuelle Preisgestaltung nach Aufwand und Organisation

### CTAs

| Button | Ziel |
|--------|------|
| Anfrage stellen | `/kontakt` |
| Erstgespräch anfragen (optional) | `/kontakt` |

### Formulierungshinweise

- Seriös und sachlich formulieren – keine Übertreibung
- Keine Rechtsgarantien („100 % DSGVO-konform", „rechtssicher garantiert")
- Keine pauschalen Preise versprechen, solange diese nicht final definiert sind

---

## 6. Erweiterung: Schulungen mit PDF-Nachweis

### Aktualisierung in `Website.md`

Die bestehenden Schulungs-Beschreibungen in Abschnitt 4 (Zeile 54, 69) und Abschnitt 8 (Zeile 224–225) sind unvollständig. Folgende Funktionen fehlen:

| Funktion | Marketing-relevant |
|----------|-------------------|
| Schulungsvorlagen | Ja |
| Schulungen anlegen | Ja |
| Teilnehmerverwaltung | Teilweise (B2B) |
| Teilnehmerportal (E-Mail + Code) | Ja – USP |
| Quiz / Lernerfolgskontrolle | Ja |
| Abschlussdokumentation | Ja |
| **PDF-Teilnahmebescheinigung** | **Ja – neu hervorheben** |
| **Ablage im Dokumentenmodul** | **Ja – neu hervorheben** |
| **Verknüpfung mit Schulung/Teilnahme** | **Ja – neu hervorheben** |

### Titelvorschlag

**Schulungen mit Nachweis**

### Textbausteine (direkt verwendbar)

**Kurzversion:**

> Planen Sie Datenschutzschulungen, laden Sie Teilnehmer ein und dokumentieren Sie erfolgreiche Abschlüsse. Nach Abschluss kann automatisch eine Teilnahmebescheinigung als PDF erstellt und im Dokumentenbereich abgelegt werden.

**Ausführliche Version:**

> **Schulungen mit Nachweis**
>
> Datenschutzschulungen sind Pflicht – aber oft fehlt die saubere Dokumentation. Mit dem Schulungsmodul planen Sie Schulungen auf Basis von Vorlagen, verwalten Teilnehmer und nutzen ein Teilnehmerportal mit Lernerfolgskontrolle per Quiz.
>
> Nach erfolgreichem Abschluss wird automatisch eine **Teilnahmebescheinigung als PDF** erzeugt. Die Bescheinigung wird im **Dokumentenmodul abgelegt** und mit der jeweiligen **Schulung und Teilnahme verknüpft** – Nachweise bleiben mandantenbezogen auffindbar.
>
> **Ihr Nutzen:**
> - Nachweise zentral dokumentiert – kein Ordner-Chaos
> - Weniger manuelle PDF-Erstellung
> - Bessere Vorbereitung auf Audits und Nachfragen
> - Schulungsnachweise bleiben mandantenbezogen auffindbar und verknüpft

**Bullet-Points für Modulkarte:**

- Schulungsvorlagen und Durchführungen
- Teilnehmerportal mit Quiz
- Automatische PDF-Teilnahmebescheinigung
- Ablage und Verknüpfung im Dokumentenmodul

### Hinweis zur Lizenzierung

Das Schulungsmodul kann über Plan/Lizenz als Feature freigeschaltet werden. Auf der Marketingseite kann erwähnt werden:

> Das Schulungsmodul ist je nach Tarif enthalten oder als Zusatzmodul verfügbar.

Keine finalen Preise erfinden – nur als Konzept positionieren (siehe Abschnitt 8).

---

## 7. Aktualisierung: Provisioning-Plattform

### Neue Systembeschreibung für `Website.md`

**`Dsms.Provisioning`** (Domain: `https://signup.datenschutz-cloud.eu`) verwaltet:

- Öffentliche Registrierung
- Registrierungen / Pending Signups
- Mandantenanlage (Provisioning)
- Pläne (Subscription Plans)
- Rabattcodes
- Lizenzen
- Systembenachrichtigungen
- SMTP-Diagnose
- Kaufmännische SaaS-Funktionen
- Produktive Legal-Texte für Signup (Impressum, AGB, …)

**`Dsms.Web`** (Domain: `https://app.datenschutz-cloud.eu`) enthält:

- Fachanwendung für Mandanten
- Datenschutzmodule (VVT, TOMs, DSFA, …)
- Schulungen (wenn lizenziert)
- Dokumente
- Benutzerverwaltung
- Mandantendaten
- „Meine Lizenz" (Anzeige, kein kaufmännisches Management)
- Supportzugriff, Auditlog (Mandant)
- Upgrade-Hinweis, wenn Schulungsmodul nicht in der Lizenz enthalten ist

### Textbaustein für Marketingseite

> Registrierung, Pläne, Rabattcodes und Lizenzen werden über die separate Provisioning-Plattform verwaltet. Die Fachanwendung bleibt dadurch klar auf das Datenschutzmanagement des Mandanten fokussiert.

### Abschnitte in `Website.md`, die angepasst werden müssen

| Abschnitt | Anpassung |
|-----------|-----------|
| **1. Ziel der Marketingseite** | Provisioning als viertes Zielsystem |
| **5. Produktmodule** | Zeilen 117–119: Plattform-Routen (`/platform/licenses`, `/platform/plans`, `/platform/discount-codes`, `/signup`) aus App-Tabelle entfernen oder als „extern (Provisioning)" markieren |
| **5. Produktmodule** | Neue Zeile: „Meine Lizenz" (`/admin/license`) – Anzeige in Fachanwendung |
| **9. CTA und Ziel-URLs** | Signup-Routen auf Provisioning-Domain |
| **11. Technische Architektur** | `Dsms.Provisioning` in Projektstruktur und Deployment-Tabelle |
| **11. Deployment** | Vier Hosts statt drei (nginx-Konfiguration) |
| **Anhang** | Signup-Referenzen auf `Dsms.Provisioning` umstellen |

---

## 8. Aktualisierung: Preise und Pläne

### Konzept (ohne finale Preise)

Funktionsmodule werden über **Pläne und Lizenzen** gesteuert. Provisioning verwaltet die kaufmännische Seite; die Fachanwendung zeigt den Mandanten den aktuellen Lizenzumfang an.

| Aspekt | Beschreibung |
|--------|--------------|
| **Plan-basierte Features** | Pläne können unterschiedliche Feature-Umfänge definieren (z. B. `HasTrainingModule`) |
| **Schulungsmodul** | Lizenzierbares Feature – in höheren Plänen enthalten oder als Zusatzmodul positionierbar |
| **Anzeige „Meine Lizenz"** | Mandanten sehen in der Fachanwendung ihren aktuellen Plan, Limits und enthaltene Module |
| **Upgrade-Hinweis** | Wenn Schulungsmodul nicht enthalten: Hinweis in der App mit Verweis auf Upgrade/Kontakt |
| **Rabattcodes** | Werden beim Signup in Provisioning angewendet |
| **Free-Plan** | Weiterhin als Einstieg denkbar (Seed: 0 EUR, Limits) |

### Textbausteine für `/preise` (Konzept)

**Einleitung:**

> Wählen Sie den Plan, der zu Ihrer Organisation passt. Die Registrierung erfolgt über unsere Signup-Plattform – dort sehen Sie die aktuell verfügbaren Tarife und können direkt starten.

**Hinweis zu Modulen (ohne Preise):**

> Je nach Tarif sind unterschiedliche Funktionsumfänge enthalten – etwa das Schulungsmodul mit PDF-Teilnahmebescheinigungen. Details zu den einzelnen Plänen finden Sie bei der Registrierung.

**CTA auf Preisseite:**

| Button | Ziel |
|--------|------|
| Plan wählen und registrieren | `https://signup.datenschutz-cloud.eu` |
| Individuelle Anfrage | `/kontakt` |

### Was in `Website.md` bleibt (TODO)

- Finale Marketing-Preise für Basic/Pro/Business festlegen
- Entscheidung: Preise statisch auf Marketingseite oder dynamisch aus Provisioning
- Positionierung Schulungsmodul: Inklusive vs. Zusatzmodul

---

## 9. Aktualisierung: Trust und Datenschutz

### Bestehende Argumente (beibehalten)

Aus `Website.md` Abschnitt 6 – weiterhin gültig und marketing-tauglich:

- Mandantenfähige Architektur
- Superuser ohne Fachdaten-Zugriff
- Supportzugriff nur nach Freigabe, zeitlich begrenzt, widerrufbar, protokolliert
- Plattform-Protokoll ohne Fachinhalte (Metadaten-only)
- Admin-Auditlog mit Feldänderungen
- IP-Anonymisierung
- Legal-Dokumente vorhanden
- Demo nur Beispieldaten

### Neue Trust-Argumente

#### Mandantenlöschung (kleiner Trust-Hinweis)

**Textbaustein:**

> Mandantenlöschungen erfolgen nicht unkontrolliert per Klick. Eine Löschung kann angefordert werden und wird anschließend manuell geprüft.

**Ausführlicher (optional, z. B. FAQ):**

> Wenn Sie Ihren Mandanten löschen möchten, können Sie dies anfordern. Die Löschung wird nicht automatisch ausgeführt, sondern manuell geprüft. Dabei werden Systembenachrichtigungen ausgelöst und der Vorgang protokolliert. Vor einer Löschung empfehlen wir, relevante Daten zu exportieren.

**Platzierung:** Trust-Bereich auf Startseite (1–2 Sätze) oder FAQ-Eintrag „Wie funktioniert die Mandantenlöschung?"

#### Open Source (siehe Abschnitt 4)

Transparenz als Vertrauensargument ergänzen.

### Hosting-Aussagen – vorsichtig formulieren

In `Website.md` Abschnitt 8 (Zeile 219) steht bereits:

> „Hosting in EU (nur wenn verifiziert – **TODO**)"

**Status:** Im Code/Deployment ist **kein verifizierter Hosting-Standort** (Deutschland/EU) dokumentiert. Die Aussage darf **nicht** als fertiger Marketingclaim übernommen werden.

| Formulierung | Status |
|--------------|--------|
| „datenschutzfreundlich konzipiert" | ✅ Erlaubt |
| „für den deutschsprachigen Raum entwickelt" | ✅ Erlaubt |
| „mit Fokus auf transparente Dokumentation und kontrollierten Supportzugriff" | ✅ Erlaubt |
| „Hosting in Deutschland" | ❌ TODO – vor Veröffentlichung verifizieren |
| „Hosting in der EU" | ❌ TODO – vor Veröffentlichung verifizieren |
| „deutsche Rechenzentren" | ❌ TODO – vor Veröffentlichung verifizieren |

**Empfehlung für Trust-Bereich (ohne Hosting-Claim):**

> Datenschutz-Cloud ist datenschutzfreundlich konzipiert – mit Mandantentrennung, anonymisierten Protokoll-IPs, kontrolliertem Supportzugriff und transparenten Legal-Dokumenten. Entwickelt für den deutschsprachigen Raum.

---

## 10. Konkrete Änderungen an Website.md

Checkliste für die spätere Überarbeitung von `Website.md`:

### Zielsysteme und Domains

- [ ] Abschnitt 1: Tabelle um Provisioning-Zeile ergänzen
- [ ] Abschnitt 1: Bei SaaS-App „Registrierung" entfernen
- [ ] Abschnitt 9: Domainstruktur-Tabelle um `signup.datenschutz-cloud.eu` und GitHub ergänzen
- [ ] Abschnitt 11: Deployment-Tabelle um vierten Host erweitern
- [ ] Canonical-Domain (`www` vs. ohne `www`) – TODO beibehalten

### Registrierung und CTAs

- [ ] Abschnitt 4, Zeile 74: „Öffentliche Registrierung über `/signup`" → Signup-Domain
- [ ] Abschnitt 7, Zeile 157: `/preise`-Link auf Signup-Domain
- [ ] Abschnitt 7, Zeile 168: Registrierungs-Link auf Signup-Domain
- [ ] Abschnitt 8, Zeile 207: Hero-CTA „Jetzt starten" → Signup-Domain
- [ ] Abschnitt 8, Zeile 242: „Kostenlos registrieren" → Signup-Domain
- [ ] Abschnitt 9: Alle `app.datenschutz-cloud.eu/signup`-URLs ersetzen
- [ ] Abschnitt 9: CTA-Tabelle aktualisieren (GitHub-CTA ergänzen)

### Produktmodule

- [ ] Abschnitt 5: Plattform-Routen (Lizenzen, Pläne, Rabattcodes, Signup) als Provisioning kennzeichnen oder entfernen
- [ ] Abschnitt 5: „Meine Lizenz" (`/admin/license`) als Mandanten-Anzeige ergänzen
- [ ] Abschnitt 5: Schulungsmodul um PDF-Bescheinigung, Dokumentenablage, Verknüpfung erweitern

### Neue Marketing-Sektionen (OnePager)

- [ ] Abschnitt 8: Neue Sektion „Open Source" (nach Trust oder vor Preise)
- [ ] Abschnitt 8: Neue Sektion „Externer Datenschutzbeauftragter"
- [ ] Abschnitt 8: Schulungs-Sektion um PDF-Nachweis erweitern
- [ ] Abschnitt 8: Trust-Bereich um Mandantenlöschung ergänzen
- [ ] Abschnitt 8: Hosting-EU-TODO beibehalten oder durch vorsichtige Formulierung ersetzen

### Seitenstruktur

- [ ] Abschnitt 7: Optional neue Route `/leistungen` oder DSB-Abschnitt auf `/kontakt`
- [ ] Abschnitt 7: Footer um GitHub-Link ergänzen

### Technische Architektur (Briefing)

- [ ] Abschnitt 11: Projektstruktur um `Dsms.Provisioning` ergänzen
- [ ] Abschnitt 11: nginx-Konfiguration für vier Hosts
- [ ] Anhang: Signup-Referenzen auf `Dsms.Provisioning` aktualisieren

### Formulierungen

- [ ] Keine Rechtsgarantien (bereits in Website.md – beibehalten)
- [ ] Open-Source-Lizenz vor Veröffentlichung final prüfen

---

## 11. Offene Punkte vor Veröffentlichung

| Punkt | Priorität | Hinweis |
|-------|-----------|---------|
| **Finale Preise** Basic/Pro/Business | Hoch | Seed-Daten haben `null`-Preise; Marketing darf keine erfundenen Preise zeigen |
| **Hosting-Aussagen verifizieren** | Hoch | „Hosting in DE/EU" nur nach Deployment-Nachweis |
| **Lizenz des GitHub-Repos prüfen** | Mittel | LICENSE.txt = AGPL-3.0; Marketing-Formulierung abstimmen |
| **Finale Support-/Kontaktadresse** | Mittel | `support@datenschutz-cloud.eu` aus AppBranding – Verfügbarkeit prüfen |
| **Canonical Domain www vs. ohne www** | Mittel | `AppBranding:WebsiteUrl` nutzt `www` – einheitlich festlegen |
| **Demo-Instanz-Konzept** | Mittel | Domain, Seed, kein Public-Signup – noch TODO in Website.md |
| **Signup-Erfolgsseiten-Routen in Provisioning** | Niedrig | Exakte Pfade in `Dsms.Provisioning` dokumentieren (z. B. `/signup/success`) |
| **Rechtliche Prüfung der Marketingtexte** | Hoch | Insbesondere Open-Source-, DSB- und Trust-Formulierungen |
| **Positionierung Schulungsmodul in Plänen** | Mittel | Inklusive vs. Zusatzmodul – Produktentscheidung |
| **Externer DSB: Ansprechpartner und Impressum** | Hoch | Name/Kontakt für DSB-Leistung in Impressum/Kontaktseite |
| **Provisioning vs. Open Source Abgrenzung** | Mittel | Fachanwendung OSS, Provisioning privat – Marketing klar trennen |
| **Sitemap / robots.txt / SEO** | Niedrig | Bereits als TODO in Website.md |

---

## Anhang: Formulierungshinweise (Wichtig)

### Nicht formulieren

- „100 % DSGVO-konform"
- „rechtssicher garantiert"
- „vollständig haftungssicher"
- „ersetzt jede Rechtsberatung"
- „garantiert prüfungssicher"

### Besser formulieren

- „unterstützt bei der strukturierten Datenschutzdokumentation"
- „hilft bei der Nachweisführung"
- „unterstützt Datenschutzprozesse"
- „für die praktische Umsetzung im Alltag"
- „als Werkzeug für Datenschutzbeauftragte, Datenschutzkoordinatoren und Organisationen"

---

## Anhang: Veraltete Stellen in Website.md (Detailanalyse)

| # | Abschnitt | Zeile(n) | Befund |
|---|-----------|----------|--------|
| 1 | 1 – Zielsysteme | 24 | Registrierung fälschlich der App zugeordnet |
| 2 | 1 – Zielsysteme | 21–25 | Provisioning fehlt komplett |
| 3 | 4 – Value Proposition | 74 | Signup über App-Route `/signup` |
| 4 | 5 – Module | 117–119 | Plattform-SaaS-Routen noch in App-Kontext |
| 5 | 7 – Seitenstruktur | 157, 168 | Signup-Links auf App |
| 6 | 8 – OnePager | 207, 242 | CTAs auf App-Registrierung |
| 7 | 8 – OnePager | 219 | Hosting EU als TODO (korrekt markiert, aber riskant wenn übernommen) |
| 8 | 9 – CTAs | 269–280, 294–295 | Alle Signup-URLs veraltet |
| 9 | 9 – CTAs | 279 | Login→Signup-Flow beschreibt alte App-Interna |
| 10 | 11 – Architektur | 354–366 | Nur Dsms.Web + Marketing, kein Provisioning |
| 11 | 11 – Deployment | 377 | nginx für drei Hosts – jetzt vier |
| 12 | — | — | Kein Open-Source-Abschnitt |
| 13 | — | — | Kein externer DSB-Abschnitt |
| 14 | 4, 8 – Schulungen | 54, 69, 224 | Keine PDF-Bescheinigung / Dokumentenverknüpfung |
| 15 | 6 – Trust | — | Keine Mandantenlöschung |
| 16 | 8 – Preise | 236 | Kein Hinweis auf lizenzierbare Feature-Module |
| 17 | Anhang | 487–488 | Signup-Seiten in Dsms.Web als aktiv referenziert |
