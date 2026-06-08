namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Mandant / Organisationseinheit – Wurzel der einfachen Mandantentrennung in Version 1.
/// </summary>
public class Tenant : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Mandant wurde zur Löschung angefordert (keine automatische Hard-Delete in V1).</summary>
    public bool IsDeletionRequested { get; set; }

    public DateTime? DeletionRequestedAt { get; set; }
    public string? DeletionRequestedByUserId { get; set; }

    /// <summary>Geplanter Zeitpunkt für spätere endgültige Löschung (V1: nur Markierung).</summary>
    public DateTime? DeletionScheduledAt { get; set; }

    public ICollection<AuditTemplate> AuditTemplates { get; set; } = [];
    public ICollection<AuditRun> AuditRuns { get; set; } = [];
    public ICollection<Measure> Measures { get; set; } = [];
    public ICollection<EvidenceDocument> Documents { get; set; } = [];
    public ICollection<ProcessingActivity> ProcessingActivities { get; set; } = [];
    public ICollection<Tom> Toms { get; set; } = [];
    public ICollection<ServiceProvider> ServiceProviders { get; set; } = [];
    public ICollection<DataProtectionImpactAssessment> DpiaAssessments { get; set; } = [];
    public ICollection<UserTenant> UserTenants { get; set; } = [];
}
