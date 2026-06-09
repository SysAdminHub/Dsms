namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Kundenlizenz – zentrale kaufmännische und technische Einheit.
/// Limits mit <c>null</c> bedeuten unbegrenzt.
/// </summary>
public class License
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string LicenseNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string PlanName { get; set; } = "Manual";
    public string Status { get; set; } = "Active";
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? InternalNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Lizenzweite Limits
    public int? MaxTenants { get; set; }
    public int? MaxAdmins { get; set; }

    // Mandantenbezogene Limits (gelten je Mandant)
    public int? MaxUsersPerTenant { get; set; }
    public int? MaxAuditorsPerTenant { get; set; }
    public int? MaxCustomAuditTemplatesPerTenant { get; set; }
    public int? MaxActiveAuditsPerTenant { get; set; }
    public int? MaxProcessingActivitiesPerTenant { get; set; }
    public int? MaxDpiaPerTenant { get; set; }
    public int? MaxTomsPerTenant { get; set; }
    public int? MaxProcessorsPerTenant { get; set; }
    public int? MaxActiveMeasuresPerTenant { get; set; }

    // Sonstige Limits
    public int? MaxStorageMb { get; set; }
    public int? MaxEmailRemindersPerMonth { get; set; }

    public ICollection<Tenant> Tenants { get; set; } = [];
}
