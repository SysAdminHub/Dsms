namespace Dsms.Web.Domain.Entities;

/// <summary>Wiederverwendbarer Fragenkatalog für Audits (pro Mandant, versioniert).</summary>
public class AuditTemplate : EntityBase
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Version { get; set; } = "1.0";
    public bool IsActive { get; set; } = true;

    public ICollection<AuditQuestion> Questions { get; set; } = [];
    public ICollection<AuditRun> AuditRuns { get; set; } = [];
}
