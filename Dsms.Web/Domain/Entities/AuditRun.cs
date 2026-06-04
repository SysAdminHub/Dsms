using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Konkreter Audit-Durchlauf auf Basis einer Vorlage.
/// <see cref="Status"/> steuert Lebenszyklus; Zeitstempel werden in der UI beim Speichern gesetzt.
/// </summary>
public class AuditRun : EntityBase
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int AuditTemplateId { get; set; }
    public AuditTemplate AuditTemplate { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public AuditRunStatus Status { get; set; } = AuditRunStatus.Draft;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? AssignedUserId { get; set; }

    public ICollection<AuditAnswer> Answers { get; set; } = [];
    public ICollection<Measure> Measures { get; set; } = [];
    public ICollection<EvidenceDocument> Documents { get; set; } = [];
}
