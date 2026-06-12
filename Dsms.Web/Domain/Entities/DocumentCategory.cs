namespace Dsms.Web.Domain.Entities;

/// <summary>Mandantenbezogene Kategorie für Nachweisdokumente.</summary>
public class DocumentCategory : EntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystemDefault { get; set; }

    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }

    public ICollection<EvidenceDocument> Documents { get; set; } = [];
}
