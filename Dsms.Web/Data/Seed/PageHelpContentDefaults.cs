namespace Dsms.Web.Data.Seed;

/// <summary>Standard-Hilfetexte für Fachseiten. Werden nur angelegt, wenn der Key noch fehlt.</summary>
public static class PageHelpContentDefaults
{
    public sealed record Definition(
        string Key,
        string Title,
        string? LegalReference,
        string? ShortDescription,
        string Content);

    public static readonly IReadOnlyList<Definition> All =
    [
        new(
            Domain.PageHelpContentKeys.ProcessingActivities,
            "Verarbeitungstätigkeiten",
            "Art. 30 DSGVO, Art. 6 DSGVO",
            "Dokumentieren Sie hier die wichtigsten Verarbeitungstätigkeiten Ihrer Organisation.",
            """
            Hier erfassen Sie, welche personenbezogenen Daten in Ihrer Organisation verarbeitet werden.

            Eine Verarbeitungstätigkeit beschreibt zum Beispiel, warum Daten verarbeitet werden, welche Daten betroffen sind, welche Personengruppen betroffen sind, auf welcher Rechtsgrundlage die Verarbeitung erfolgt und welche Schutzmaßnahmen gelten.

            Typische Beispiele sind Personalverwaltung, Kundenverwaltung, Bewerbermanagement, Website-Kontaktformular oder Newsletter.

            Legen Sie für jede wichtige Verarbeitung einen eigenen Eintrag an und verknüpfen Sie passende TOMs, Dienstleister, Dokumente oder DSFAs.
            """),
        new(
            Domain.PageHelpContentKeys.Toms,
            "TOM-Verzeichnis",
            "Art. 32 DSGVO",
            "Dokumentieren Sie technische und organisatorische Maßnahmen zum Schutz personenbezogener Daten.",
            """
            In diesem Bereich erfassen Sie Schutzmaßnahmen, mit denen personenbezogene Daten abgesichert werden.

            Dazu gehören zum Beispiel Zugriffsschutz, Berechtigungskonzepte, Backups, Verschlüsselung, Schulungen, Zutrittskontrolle oder Protokollierung.

            Verknüpfen Sie TOMs mit den passenden Verarbeitungstätigkeiten, damit nachvollziehbar ist, welche Maßnahmen für welche Verarbeitung gelten.
            """),
        new(
            Domain.PageHelpContentKeys.Dsfa,
            "Datenschutz-Folgenabschätzung",
            "Art. 35 DSGVO",
            "Prüfen und dokumentieren Sie hier Datenschutz-Folgenabschätzungen.",
            """
            Eine Datenschutz-Folgenabschätzung wird benötigt, wenn eine Verarbeitung voraussichtlich ein hohes Risiko für betroffene Personen mit sich bringt.

            Hier dokumentieren Sie Risiken, Schutzmaßnahmen, Restrisiko und das Ergebnis der Prüfung.

            Nicht jede Verarbeitung benötigt automatisch eine DSFA. Wichtig ist aber, den Bedarf nachvollziehbar zu prüfen und zu dokumentieren.
            """),
        new(
            Domain.PageHelpContentKeys.ServiceProviders,
            "Dienstleister und Auftragsverarbeiter",
            "Art. 28 DSGVO, Art. 30 DSGVO",
            "Dokumentieren Sie externe Dienstleister, Auftragsverarbeiter und AVV-Informationen.",
            """
            Hier erfassen Sie externe Dienstleister, die für Ihre Organisation Leistungen erbringen oder personenbezogene Daten verarbeiten.

            Bei Auftragsverarbeitern sollte ein Auftragsverarbeitungsvertrag vorhanden sein. Außerdem sollten Drittlandbezug, Unterauftragsverarbeiter, TOM-Prüfung und Risiko dokumentiert werden.

            Verknüpfen Sie Dienstleister mit den passenden Verarbeitungstätigkeiten und TOMs.
            """),
        new(
            Domain.PageHelpContentKeys.AuditTemplates,
            "Audit-Vorlagen",
            "DSGVO Rechenschaftspflicht, Art. 5 Abs. 2 DSGVO",
            "Erstellen und nutzen Sie wiederverwendbare Fragenkataloge für Datenschutz-Audits.",
            """
            Audit-Vorlagen enthalten strukturierte Prüffragen zu Datenschutzthemen.

            Sie können eigene Mandantenvorlagen anlegen oder vordefinierte Vorlagen nutzen. Mit einer Vorlage starten Sie einen Audit-Durchlauf und dokumentieren Antworten nachvollziehbar.

            Prüfen Sie regelmäßig, ob Ihre Vorlagen die relevanten Themen Ihrer Organisation abdecken.
            """),
        new(
            Domain.PageHelpContentKeys.TrainingTemplates,
            "Schulungsvorlagen",
            "Art. 32 DSGVO, Art. 39 DSGVO, Awareness",
            "Erstellen und verwalten Sie wiederverwendbare Schulungsinhalte für Datenschutz und Awareness.",
            """
            Schulungsvorlagen enthalten wiederverwendbare Inhalte für Datenschutz- und Awareness-Schulungen. Eine Vorlage besteht aus mehreren Karten mit Markdown-Inhalten.

            Bilder können direkt in der Vorlage hochgeladen und über Platzhalter wie {{asset:beispielbild}} eingefügt werden. Optional kann ein Multiple-Choice-Quiz gepflegt werden, das später zur Teilnahmebestätigung verwendet wird.

            Eine Karte ist ein einzelner Lernabschnitt. Markdown ermöglicht einfache Textformatierung. Ein Asset ist ein Bild oder Medium innerhalb der Vorlage. Das Quiz ist der Fragenkatalog zur Wissensprüfung. Die Bestehensgrenze ist die Mindestpunktzahl in Prozent.
            """),
        new(
            Domain.PageHelpContentKeys.Trainings,
            "Schulungen",
            "Art. 32 DSGVO, Art. 39 DSGVO, Awareness",
            "Dokumentieren Sie konkrete Datenschutzschulungen und Awareness-Maßnahmen.",
            """
            In diesem Bereich verwalten Sie konkrete Online-Schulungen und Awareness-Maßnahmen. Schulungen können aktiv, inaktiv oder archiviert sein. Im Tab „Teilnehmer“ weisen Sie vorhandene Teilnehmer der Schulung zu und versenden Einladungen mit 6-stelligem Zugangscode. Neue Teilnehmer legen Sie zentral unter Schulungsteilnehmer an. Teilnehmer sind keine App-Benutzer und erhalten kein Passwort. Teilnehmer können Schulungen ohne App-Konto über E-Mail-Adresse und Zugangscode unter /schulung/teilnahme durchführen. Fortschritt, Quizversuche und Teilnahmebestätigung werden pro Zuweisung dokumentiert. Nachweise wie Teilnehmerlisten oder Zertifikate werden über das Dokumentenmodul verknüpft.
            """),
        new(
            Domain.PageHelpContentKeys.TrainingParticipants,
            "Schulungsteilnehmer",
            "Art. 32 DSGVO, Art. 39 DSGVO",
            "Alle Schulungsteilnehmer des Mandanten und deren Schulungshistorie.",
            """
            In der Teilnehmerübersicht sehen Sie alle Schulungsteilnehmer des Mandanten und deren Schulungshistorie. Neue Teilnehmer werden hier zentral angelegt (einzeln oder per Import) – nicht in einzelnen Schulungen. Teilnehmer sind keine normalen Benutzer der Datenschutz-Cloud. Sie erhalten bei Bedarf Zugangscodes für einzelne Schulungen. E-Mail-Adressen werden pro Mandant eindeutig geführt, damit keine doppelten Teilnehmer entstehen.
            """),
        new(
            Domain.PageHelpContentKeys.AuditRuns,
            "Audit-Durchläufe",
            "DSGVO Rechenschaftspflicht, Art. 5 Abs. 2 DSGVO",
            "Führen Sie Datenschutzprüfungen anhand von Audit-Vorlagen durch.",
            """
            Audit-Durchläufe helfen Ihnen dabei, Datenschutzthemen regelmäßig zu prüfen und Antworten nachvollziehbar zu dokumentieren.

            Wählen Sie eine passende Vorlage, beantworten Sie die Prüffragen und legen Sie bei Handlungsbedarf Maßnahmen an.
            """),
        new(
            Domain.PageHelpContentKeys.Measures,
            "Maßnahmen",
            "Art. 32 DSGVO, Art. 5 Abs. 2 DSGVO",
            "Verfolgen Sie Korrektur- und Verbesserungsmaßnahmen aus Audits und Datenschutzprüfungen.",
            """
            Maßnahmen dokumentieren konkrete Handlungsschritte, zum Beispiel aus Audit-Durchläufen oder Datenschutzprüfungen.

            Erfassen Sie Status, Fälligkeit und Verantwortlichkeiten. Verknüpfen Sie Maßnahmen mit Audits, Verarbeitungstätigkeiten oder Dokumenten, damit der Kontext nachvollziehbar bleibt.
            """),
        new(
            Domain.PageHelpContentKeys.PrivacyIncidents,
            "Datenschutzvorfälle",
            "Art. 33 DSGVO, Art. 34 DSGVO",
            "Dokumentieren, bewerten und verfolgen Sie Datenschutzvorfälle und Datenschutzpannen.",
            """
            In diesem Bereich dokumentieren Sie Datenschutzvorfälle, unabhängig davon, ob eine Meldung an die Aufsichtsbehörde erforderlich ist.

            Ein Datenschutzvorfall kann zum Beispiel ein Fehlversand, Datenverlust, unberechtigter Zugriff, Ransomware, falsche Berechtigung oder eine Meldung eines Dienstleisters sein.

            Dokumentieren Sie, was passiert ist, wann der Vorfall bekannt wurde, welche Daten und Personen betroffen sind und welche Folgen möglich sind.

            Prüfen Sie außerdem, ob eine Meldung an die Aufsichtsbehörde oder eine Benachrichtigung betroffener Personen erforderlich ist.

            Konkrete Aufgaben, Abhilfe- und Präventionsmaßnahmen werden als Maßnahmen dokumentiert und mit dem Vorfall verknüpft. Aus einem Vorfall heraus können direkt Maßnahmen erstellt werden. Vorhandene Maßnahmen können zusätzlich verknüpft werden.

            Verknüpfen Sie relevante TOMs, um nachvollziehbar zu machen, welche Schutzmaßnahmen betroffen waren oder angepasst werden müssen.

            Diese Informationen dienen der allgemeinen Orientierung und ersetzen keine Rechtsberatung.
            """),
        new(
            Domain.PageHelpContentKeys.DataSubjectRequests,
            "Betroffenenanfragen",
            "Art. 15–22 DSGVO",
            "Anfragen betroffener Personen dokumentieren, bearbeiten und nachverfolgen.",
            """
            In diesem Modul werden Anfragen betroffener Personen nach DSGVO dokumentiert, z. B. Auskunft, Löschung, Berichtigung, Widerspruch oder Datenübertragbarkeit. Erfassen Sie Eingang, Fristen, Bearbeitung, Ergebnis und Nachweise. Nach Abschluss können personenbezogene Falldaten endgültig anonymisiert werden, um nur noch den Vorgangsnachweis zu behalten.

            Die Anonymisierung überschreibt personenbezogene Falldaten endgültig. Die ursprünglichen Werte werden nicht gespeichert und können nicht wiederhergestellt werden. Verknüpfte Dokumente müssen separat geprüft werden.
            """),
        new(
            Domain.PageHelpContentKeys.Organization,
            "Datenschutzrollen & Zuständigkeiten",
            "Art. 30 DSGVO, organisatorische Verantwortlichkeiten",
            "Organisatorische Datenschutzrollen und Zuständigkeiten je Mandant dokumentieren.",
            """
            In diesem Bereich dokumentieren Sie die organisatorischen Datenschutzrollen und Zuständigkeiten Ihres Unternehmens. Dazu gehören z. B. Geschäftsführung, Datenschutzbeauftragte Person, Datenschutzkoordinator, IT-Verantwortliche, HR-Verantwortliche oder Fachbereichsverantwortliche. Diese Rollen sind organisatorische Zuständigkeiten und nicht identisch mit den technischen Benutzerrollen der Anwendung. Eine Rolle kann optional mit einem App-Benutzer verknüpft oder vollständig als Freitext gepflegt werden.

            Das Datenschutz-Organigramm zeigt die organisatorischen Berichtslinien. Über die Listenansicht können Datenschutzrollen verwaltet und bearbeitet werden.
            """),
        new(
            Domain.PageHelpContentKeys.OrganizationOrgChart,
            "Datenschutz-Organigramm",
            "Organisatorische Berichtslinien",
            "Grafische Darstellung der Datenschutzorganisation und Berichtslinien.",
            """
            Das Datenschutz-Organigramm zeigt die organisatorischen Datenschutzrollen und Berichtslinien des Mandanten. Die Darstellung basiert auf den gepflegten Datenschutzrollen und dem Feld „Berichtet an“. Technische App-Rollen wie Admin, Auditor oder User werden hier nicht als Hierarchie dargestellt. Sie können Rollen in der Tabellenansicht pflegen und über Berichtslinien miteinander verbinden.

            Vertretungen werden als Hinweis angezeigt, aber nicht als Berichtslinie dargestellt.
            """),
        new(
            Domain.PageHelpContentKeys.Documents,
            "Dokumente und Nachweise",
            "DSGVO Rechenschaftspflicht, Art. 5 Abs. 2 DSGVO",
            "Laden Sie Nachweise, Richtlinien, Formulare, Verträge und weitere Dokumente hoch.",
            """
            Im Dokumentenmodul können Nachweise, Richtlinien, Formulare, Verträge, Kommunikationsvorlagen und sonstige Dokumente zentral abgelegt werden. Dokumente können mit Verarbeitungstätigkeiten, TOMs, Maßnahmen, Dienstleistern, DSFA, Datenschutzvorfällen, Betroffenenanfragen und weiteren Objekten verknüpft werden.

            Dokumenttypen sind feste fachliche Gruppen wie Nachweis, Richtlinie, Formular, Vertrag, Kommunikationsvorlage und Sonstiges Dokument. Kategorien können pro Mandant individuell angepasst werden, z. B. Datenschutz, IT-Sicherheit, HR oder Lieferanten. So kann jeder Mandant seine Dokumente passend zur eigenen Organisation strukturieren.

            Dokumenttypen im Überblick:
            - Nachweis: Beleg für Umsetzung oder Prüfung
            - Richtlinie: interne Vorgabe oder Regelung
            - Formular: Vorlage zur Erfassung von Informationen
            - Vertrag: vertragliches oder rechtliches Dokument
            - Kommunikationsvorlage: Textvorlage für wiederkehrende Kommunikation
            - Sonstiges Dokument: sonstige relevante Datei

            Ordnen Sie Dokumente möglichst den passenden Verarbeitungstätigkeiten, Dienstleistern, Audits oder Maßnahmen zu.
            """),
        new(
            Domain.PageHelpContentKeys.TenantData,
            "Tenant-Daten",
            "Art. 30 DSGVO, Art. 13/14 DSGVO",
            "Pflegen Sie Mandanten-Stammdaten, exportieren Sie Daten und verwalten Sie Löschanforderungen.",
            """
            Hier pflegen Sie zentrale Angaben zum Verantwortlichen und zur Datenschutzbeauftragten Person Ihres Mandanten.

            Sie können mandantenbezogene Daten exportieren und bei Bedarf eine Löschanforderung stellen. Halten Sie Stammdaten aktuell, damit sie in Verarbeitungsverzeichnissen und Exporten korrekt erscheinen.
            """),
        new(
            Domain.PageHelpContentKeys.Users,
            "Benutzerverwaltung",
            "Art. 32 DSGVO (Zugriffskontrolle)",
            "Verwalten Sie Benutzerkonten, Rollen und Mandantenzuordnungen.",
            """
            Legen Sie Benutzer an, weisen Sie Rollen zu und ordnen Sie Konten Mandanten zu.

            Achten Sie auf das Prinzip der geringsten Berechtigung – vergeben Sie nur die Rollen, die für die jeweilige Aufgabe erforderlich sind.
            """),
        new(
            Domain.PageHelpContentKeys.License,
            "Meine Lizenz",
            null,
            "Übersicht über Ihre Kundenlizenz und Nutzungslimits.",
            """
            Hier sehen Sie den Status Ihrer Lizenz, gültige Zeiträume und Nutzungslimits für Mandanten, Benutzer und Fachmodule.

            Prüfen Sie regelmäßig, ob Ihre Nutzung innerhalb der Lizenzgrenzen liegt. Bei Fragen zu Upgrades wenden Sie sich an Ihren Ansprechpartner.
            """)
    ];
}
