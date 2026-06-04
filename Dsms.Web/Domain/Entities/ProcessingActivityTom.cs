namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Verknüpfung zwischen TOM und Verarbeitungstätigkeit (Many-to-Many).
/// TenantId dient der mandantenbezogenen Absicherung – Verknüpfungen dürfen nur innerhalb eines Mandanten bestehen.
/// </summary>
public class ProcessingActivityTom
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int ProcessingActivityId { get; set; }
    public ProcessingActivity ProcessingActivity { get; set; } = null!;

    public int TomId { get; set; }
    public Tom Tom { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
