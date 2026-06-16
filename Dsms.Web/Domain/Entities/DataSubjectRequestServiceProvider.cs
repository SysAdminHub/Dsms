namespace Dsms.Web.Domain.Entities;

/// <summary>Many-to-Many: Betroffenenanfrage ↔ Dienstleister.</summary>
public class DataSubjectRequestServiceProvider
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int DataSubjectRequestId { get; set; }
    public DataSubjectRequest DataSubjectRequest { get; set; } = null!;

    public int ServiceProviderId { get; set; }
    public ServiceProvider ServiceProvider { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByUserId { get; set; }
}
