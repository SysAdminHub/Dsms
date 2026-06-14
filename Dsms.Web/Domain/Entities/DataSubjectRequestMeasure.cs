namespace Dsms.Web.Domain.Entities;

/// <summary>Many-to-Many: Betroffenenanfrage ↔ Maßnahme.</summary>
public class DataSubjectRequestMeasure
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int DataSubjectRequestId { get; set; }
    public DataSubjectRequest DataSubjectRequest { get; set; } = null!;

    public int MeasureId { get; set; }
    public Measure Measure { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByUserId { get; set; }
}
