using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Eintrag im Verzeichnis von Verarbeitungstätigkeiten (VVT) gemäß Art. 30 DSGVO.
/// Mandantenbezogen; zentrale Dokumentation einer Datenverarbeitung in der Organisation.
/// </summary>
public class ProcessingActivity : ArchivableEntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Bezeichnung / Name der Verarbeitungstätigkeit.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Allgemeine Beschreibung der Verarbeitung.</summary>
    public string? Description { get; set; }

    /// <summary>Zweck der Verarbeitung personenbezogener Daten.</summary>
    public string? Purpose { get; set; }

    /// <summary>Verantwortlicher Bereich oder Abteilung in der Organisation.</summary>
    public string? ResponsibleDepartment { get; set; }

    /// <summary>
    /// Ergänzende Freitextangaben zur Rechtsgrundlage. Strukturierte Rechtsgrundlagen
    /// werden über <see cref="LegalBasisLinks"/> erfasst; dieses Feld bleibt für ergänzende
    /// Angaben sowie zur Erhaltung bestehender Freitext-Rechtsgrundlagen erhalten.
    /// </summary>
    public string? LegalBasis { get; set; }

    /// <summary>Strukturiert ausgewählte Standard-Rechtsgrundlagen (DSGVO) zu dieser Verarbeitungstätigkeit.</summary>
    public ICollection<ProcessingActivityLegalBasis> LegalBasisLinks { get; set; } = [];

    /// <summary>Kategorien der betroffenen Personen (z. B. Mitarbeiter, Kunden).</summary>
    public string? DataSubjectCategories { get; set; }

    /// <summary>Kategorien der verarbeiteten personenbezogenen Daten.</summary>
    public string? PersonalDataCategories { get; set; }

    /// <summary>Empfänger oder Empfängerkategorien der Daten.</summary>
    public string? Recipients { get; set; }

    /// <summary>Ob eine Übermittlung in Drittländer stattfindet.</summary>
    public bool ThirdCountryTransfer { get; set; }

    /// <summary>Beschreibung der Drittlandübermittlung inkl. Garantien, falls zutreffend.</summary>
    public string? ThirdCountryTransferDescription { get; set; }

    /// <summary>Lösch- bzw. Aufbewahrungsfristen für die Daten.</summary>
    public string? RetentionPeriod { get; set; }

    /// <summary>Ob eine Datenschutz-Folgenabschätzung (DSFA) erforderlich ist.</summary>
    public bool DpiaRequired { get; set; }

    public ProcessingActivityStatus Status { get; set; } = ProcessingActivityStatus.Draft;

    /// <summary>Verantwortliche Person / fachlicher Owner (Freitext, z. B. Name oder Rolle).</summary>
    public string? Owner { get; set; }

    /// <summary>Verknüpfte technische und organisatorische Maßnahmen (TOMs).</summary>
    public ICollection<ProcessingActivityTom> TomLinks { get; set; } = [];

    /// <summary>Externe Dienstleister und Auftragsverarbeiter zu dieser Verarbeitungstätigkeit.</summary>
    public ICollection<ProcessingActivityServiceProvider> ServiceProviderLinks { get; set; } = [];

    /// <summary>Zugeordnete Maßnahmen (Many-to-Many).</summary>
    public ICollection<ProcessingActivityMeasure> MeasureLinks { get; set; } = [];

    /// <summary>Verknüpfte Audit-Antworten (Many-to-Many).</summary>
    public ICollection<ProcessingActivityAuditAnswer> AuditAnswerLinks { get; set; } = [];

    /// <summary>Datenschutz-Folgenabschätzungen (DSFA) zu dieser Verarbeitungstätigkeit.</summary>
    public ICollection<DataProtectionImpactAssessment> DpiaAssessments { get; set; } = [];
}
