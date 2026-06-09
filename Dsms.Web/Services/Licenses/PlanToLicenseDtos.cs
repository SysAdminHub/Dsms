namespace Dsms.Web.Services.Licenses;

public sealed class CreateLicenseFromPlanDto
{
    public Guid PlanId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? InternalNote { get; set; }
    public string? LicenseNumber { get; set; }
    public string? OverridePlanName { get; set; }
}

public sealed class LicenseFromPlanPreviewDto
{
    public Guid PlanId { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public string PlanDisplayName { get; init; } = string.Empty;
    public string? PlanDescription { get; init; }
    public bool IsFree { get; init; }
    public decimal? PriceMonthly { get; init; }
    public decimal? PriceYearly { get; init; }
    public string Currency { get; init; } = "EUR";

    public string PlanNameForLicense { get; init; } = string.Empty;
    public string Status { get; init; } = "Active";
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidUntil { get; init; }

    public int? MaxTenants { get; init; }
    public int? MaxAdmins { get; init; }
    public int? MaxUsersPerTenant { get; init; }
    public int? MaxAuditorsPerTenant { get; init; }
    public int? MaxCustomAuditTemplatesPerTenant { get; init; }
    public int? MaxActiveAuditsPerTenant { get; init; }
    public int? MaxProcessingActivitiesPerTenant { get; init; }
    public int? MaxDpiaPerTenant { get; init; }
    public int? MaxTomsPerTenant { get; init; }
    public int? MaxProcessorsPerTenant { get; init; }
    public int? MaxActiveMeasuresPerTenant { get; init; }
    public int? MaxStorageMb { get; init; }
    public int? MaxEmailRemindersPerMonth { get; init; }
}
