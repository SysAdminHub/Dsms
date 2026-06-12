namespace Dsms.Web.Domain.Entities;

/// <summary>Many-to-Many: Betroffenenanfrage ↔ Verarbeitungstätigkeit.</summary>
public class DataSubjectRequestProcessingActivity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int DataSubjectRequestId { get; set; }
    public DataSubjectRequest DataSubjectRequest { get; set; } = null!;

    public int ProcessingActivityId { get; set; }
    public ProcessingActivity ProcessingActivity { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByUserId { get; set; }
}
