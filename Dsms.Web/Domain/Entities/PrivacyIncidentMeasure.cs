namespace Dsms.Web.Domain.Entities;

/// <summary>Many-to-Many: Datenschutzvorfall ↔ Maßnahme.</summary>
public class PrivacyIncidentMeasure
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int PrivacyIncidentId { get; set; }
    public PrivacyIncident PrivacyIncident { get; set; } = null!;

    public int MeasureId { get; set; }
    public Measure Measure { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
