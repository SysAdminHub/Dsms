using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Datenschutz-Folgenabschätzung (DSFA) zu einer Verarbeitungstätigkeit.
/// Mandantenbezogen; eine Verarbeitungstätigkeit kann mehrere DSFA-Einträge haben (z. B. Versionen).
/// </summary>
public class DataProtectionImpactAssessment : ArchivableEntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Zugehörige Verarbeitungstätigkeit (Pflicht; muss demselben Mandanten angehören).</summary>
    public int ProcessingActivityId { get; set; }
    public ProcessingActivity ProcessingActivity { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    /// <summary>Beschreibung der betrachteten Verarbeitung im DSFA-Kontext.</summary>
    public string? ProcessingDescription { get; set; }

    /// <summary>Grund, warum eine DSFA durchgeführt wird.</summary>
    public string? ReasonForDpia { get; set; }

    /// <summary>Notwendigkeit und Verhältnismäßigkeit der Verarbeitung.</summary>
    public string? NecessityAndProportionality { get; set; }

    /// <summary>Bewertung der Risiken für betroffene Personen.</summary>
    public string? RiskAssessment { get; set; }

    /// <summary>Geplante oder umgesetzte Schutzmaßnahmen zur Risikoreduktion.</summary>
    public string? ProtectiveMeasures { get; set; }

    public DpiaResidualRisk ResidualRisk { get; set; } = DpiaResidualRisk.NotEvaluated;

    public DpiaOutcome Outcome { get; set; } = DpiaOutcome.NotEvaluated;

    public DpiaStatus Status { get; set; } = DpiaStatus.Draft;

    /// <summary>Verantwortliche Person für die DSFA (Freitext).</summary>
    public string? ResponsiblePerson { get; set; }

    /// <summary>Person / Rolle, die die DSFA geprüft hat.</summary>
    public string? ReviewedBy { get; set; }

    public DateOnly? ReviewedAt { get; set; }

    public DateOnly? NextReviewAt { get; set; }

}
