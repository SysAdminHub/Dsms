namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Stammdatensatz eines Schulungsteilnehmers pro Mandant.
/// Kein App-Benutzer — Zugang erfolgt später per E-Mail + Zugangscode.
/// </summary>
public class TrainingParticipant : ArchivableEntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string? Name { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? ExternalReference { get; set; }

    public bool IsActive { get; set; } = true;

    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }

    public ICollection<TrainingAssignment> Assignments { get; set; } = [];
}
