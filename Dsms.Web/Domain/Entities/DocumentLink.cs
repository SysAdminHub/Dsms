using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Many-to-Many-Verknüpfung zwischen Nachweisdokument und Fachobjekt.
/// Ein Dokument kann mehrere Bezüge haben; jeder Bezug ist eindeutig pro Mandant.
/// </summary>
public class DocumentLink
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int DocumentId { get; set; }
    public EvidenceDocument Document { get; set; } = null!;

    public DocumentLinkedEntityType LinkedEntityType { get; set; }
    public int LinkedEntityId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByUserId { get; set; }
}
