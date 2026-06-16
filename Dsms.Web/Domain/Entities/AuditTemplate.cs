using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Wiederverwendbarer Fragenkatalog für Audits.
/// Mandantenvorlagen (<see cref="AuditTemplateType.Tenant"/>) sind mandantenbezogen;
/// offizielle und Community-Vorlagen sind plattformweit sichtbar.
/// </summary>
public class AuditTemplate : ArchivableEntityBase
{
    /// <summary>Mandant bei eigenen Vorlagen; null bei globalen Vorlagen.</summary>
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public AuditTemplateType TemplateType { get; set; } = AuditTemplateType.Tenant;
    public CommunityStatus CommunityStatus { get; set; } = CommunityStatus.None;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Version { get; set; } = "1.0";
    public bool IsActive { get; set; } = true;

    public DateTime? SubmittedAt { get; set; }
    public string? SubmittedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedByUserId { get; set; }
    public string? ReviewComment { get; set; }

    /// <summary>Ursprünglicher Mandant bei freigegebenen Community-Kopien.</summary>
    public int? OriginalTenantId { get; set; }

    /// <summary>Ursprüngliche Mandantenvorlage bei freigegebenen Community-Kopien.</summary>
    public int? OriginalTemplateId { get; set; }

    public ICollection<AuditQuestion> Questions { get; set; } = [];
    public ICollection<AuditRun> AuditRuns { get; set; } = [];
}
