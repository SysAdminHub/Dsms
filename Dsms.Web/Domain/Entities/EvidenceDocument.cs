namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Metadaten zu einer hochgeladenen Nachweisdatei.
/// Binärdaten liegen im Dateisystem (<see cref="StoragePath"/>), nicht in der DB.
/// Bezüge zu Fachobjekten werden über <see cref="DocumentLink"/> verwaltet.
/// </summary>
public class EvidenceDocument : ArchivableEntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public string StoragePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    public string? UploadedByUserId { get; set; }

    public ICollection<DocumentLink> Links { get; set; } = [];
}
