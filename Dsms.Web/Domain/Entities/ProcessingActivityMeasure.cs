namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Verknüpfung zwischen Verarbeitungstätigkeit und Maßnahme (Many-to-Many).
/// TenantId verhindert mandantenübergreifende Zuordnungen.
/// </summary>
public class ProcessingActivityMeasure
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int ProcessingActivityId { get; set; }
    public ProcessingActivity ProcessingActivity { get; set; } = null!;

    public int MeasureId { get; set; }
    public Measure Measure { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
