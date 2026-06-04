namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Verknüpfung zwischen Verarbeitungstätigkeit und Audit-Antwort (Many-to-Many).
/// Ermöglicht die Nachverfolgung, welche Audit-Ergebnisse zu welcher Verarbeitung gehören.
/// </summary>
public class ProcessingActivityAuditAnswer
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int ProcessingActivityId { get; set; }
    public ProcessingActivity ProcessingActivity { get; set; } = null!;

    public int AuditAnswerId { get; set; }
    public AuditAnswer AuditAnswer { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
