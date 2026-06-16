using Dsms.Web.Data;
using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Verknüpfung eines Schulungsteilnehmers mit einer konkreten Schulung inkl. Zugangscode und Einladungsstatus.
/// </summary>
public class TrainingAssignment : ArchivableEntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;

    public int? TrainingParticipantId { get; set; }
    public TrainingParticipant? TrainingParticipant { get; set; }

    public string? ParticipantNameSnapshot { get; set; }
    public string ParticipantEmailSnapshot { get; set; } = string.Empty;

    public string? AccessCodeHash { get; set; }
    public DateTime? AccessCodeGeneratedAtUtc { get; set; }
    public DateTime? AccessCodeExpiresAtUtc { get; set; }
    public DateTime? AccessCodeSentAtUtc { get; set; }

    public DateTime? InvitationSentAtUtc { get; set; }
    public string? InvitationSentByUserId { get; set; }
    public ApplicationUser? InvitationSentByUser { get; set; }
    public int InvitationSendCount { get; set; }

    public DateTime? LastAccessAttemptAtUtc { get; set; }
    public int FailedAccessAttempts { get; set; }
    public DateTime? LockedUntilUtc { get; set; }

    public TrainingAssignmentStatus Status { get; set; } = TrainingAssignmentStatus.Assigned;

    public DateTime? StartedAtUtc { get; set; }
    public DateTime? LastAccessAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? ParticipationConfirmedAtUtc { get; set; }

    /// <summary>Automatisch erzeugte Teilnahmebescheinigung im Dokumentenmodul.</summary>
    public int? CertificateDocumentId { get; set; }
    public EvidenceDocument? CertificateDocument { get; set; }

    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }

    public ICollection<TrainingAssignmentSectionProgress> SectionProgress { get; set; } = [];
    public ICollection<TrainingQuizAttempt> QuizAttempts { get; set; } = [];
}
