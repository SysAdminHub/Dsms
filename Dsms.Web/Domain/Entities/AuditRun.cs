using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Konkreter Audit-Durchlauf auf Basis einer Vorlage.
/// <see cref="Status"/> steuert Lebenszyklus; Zeitstempel werden in der UI beim Speichern gesetzt.
/// </summary>
public class AuditRun : ArchivableEntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int AuditTemplateId { get; set; }
    public AuditTemplate AuditTemplate { get; set; } = null!;

    /// <summary>Snapshot des Vorlagentitels beim Auditstart.</summary>
    public string? TemplateTitleSnapshot { get; set; }

    /// <summary>Snapshot der Vorlagenversion beim Auditstart.</summary>
    public string? TemplateVersionSnapshot { get; set; }

    public string Title { get; set; } = string.Empty;
    public AuditRunStatus Status { get; set; } = AuditRunStatus.Draft;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? AssignedUserId { get; set; }

    public ICollection<AuditAnswer> Answers { get; set; } = [];
    public ICollection<Measure> Measures { get; set; } = [];
}
