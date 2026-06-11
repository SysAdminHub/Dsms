namespace Dsms.Web.Domain.Entities;

/// <summary>Many-to-Many: Datenschutzvorfall ↔ Verarbeitungstätigkeit.</summary>
public class PrivacyIncidentProcessingActivity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int PrivacyIncidentId { get; set; }
    public PrivacyIncident PrivacyIncident { get; set; } = null!;

    public int ProcessingActivityId { get; set; }
    public ProcessingActivity ProcessingActivity { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
