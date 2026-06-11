namespace Dsms.Web.Domain.Entities;

/// <summary>Many-to-Many: Datenschutzvorfall ↔ Dienstleister.</summary>
public class PrivacyIncidentServiceProvider
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int PrivacyIncidentId { get; set; }
    public PrivacyIncident PrivacyIncident { get; set; } = null!;

    public int ServiceProviderId { get; set; }
    public ServiceProvider ServiceProvider { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
