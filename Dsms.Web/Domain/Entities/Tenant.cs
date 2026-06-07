namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Mandant / Organisationseinheit – Wurzel der einfachen Mandantentrennung in Version 1.
/// </summary>
public class Tenant : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public bool IsActive { get; set; } = true;

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
