using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Umsetzungsmaßnahme aus einem Audit oder freistehend im Mandanten.
/// Optional verknüpft mit <see cref="AuditRun"/> und/oder <see cref="AuditAnswer"/>.
/// </summary>
public class Measure : ArchivableEntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int? AuditRunId { get; set; }
    public AuditRun? AuditRun { get; set; }

    /// <summary>Konkrete Audit-Antwort, aus der die Maßnahme entstanden ist (optional).</summary>
    public int? AuditAnswerId { get; set; }
    public AuditAnswer? AuditAnswer { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MeasureStatus Status { get; set; } = MeasureStatus.Open;
    public DateOnly? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? AssignedUserId { get; set; }

    public ICollection<EvidenceDocument> Documents { get; set; } = [];

    /// <summary>Verarbeitungstätigkeiten, denen diese Maßnahme zugeordnet ist.</summary>
    public ICollection<ProcessingActivityMeasure> ProcessingActivityLinks { get; set; } = [];
}
