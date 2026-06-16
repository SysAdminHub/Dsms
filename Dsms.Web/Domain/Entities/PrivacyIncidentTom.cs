namespace Dsms.Web.Domain.Entities;

/// <summary>Many-to-Many: Datenschutzvorfall ↔ TOM.</summary>
public class PrivacyIncidentTom
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int PrivacyIncidentId { get; set; }
    public PrivacyIncident PrivacyIncident { get; set; } = null!;

    public int TomId { get; set; }
    public Tom Tom { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
