using Dsms.Web.Data;
using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Konkrete Schulungsdurchführung oder dokumentierte Awareness-Maßnahme eines Mandanten.
/// TODO: Später Inhaltssnapshot der Vorlage einführen, damit Änderungen an TrainingTemplate
/// bereits gestartete/abgeschlossene Schulungen nicht beeinflussen.
/// </summary>
public class Training : ArchivableEntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Optionale Referenz auf die verwendete Schulungsvorlage (V1: keine Snapshot-Versionierung).</summary>
    public int? TrainingTemplateId { get; set; }
    public TrainingTemplate? TrainingTemplate { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TrainingType TrainingType { get; set; } = TrainingType.PrivacyBasics;
    public string? TargetAudience { get; set; }

    public DateTime? ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? RepeatDueAt { get; set; }

    public TrainingStatus Status { get; set; } = TrainingStatus.Inactive;

    public string? ResponsibleUserId { get; set; }
    public ApplicationUser? ResponsibleUser { get; set; }
    public string? ResponsibleName { get; set; }

    public int ParticipantCount { get; set; }
    public bool ProofMissing { get; set; }
    public string? Notes { get; set; }

    /// <summary>Anzahl Tage, die neu erzeugte Zugangscodes für diese Schulung gültig sind.</summary>
    public int AccessCodeValidityDays { get; set; } = 14;

    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }

    public ICollection<TrainingAssignment> Assignments { get; set; } = [];
}
