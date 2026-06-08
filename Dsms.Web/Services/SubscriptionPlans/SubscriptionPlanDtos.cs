namespace Dsms.Web.Services.SubscriptionPlans;

public sealed class SubscriptionPlanListDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsFree { get; init; }
    public int SortOrder { get; init; }
    public decimal? PriceMonthly { get; init; }
    public decimal? PriceYearly { get; init; }
    public string Currency { get; init; } = "EUR";
    public int? MaxTenants { get; init; }
    public int? MaxAdmins { get; init; }
    public int? MaxUsersPerTenant { get; init; }
}

public sealed class SubscriptionPlanDetailsDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public bool IsFree { get; init; }
    public int SortOrder { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public decimal? PriceMonthly { get; init; }
    public decimal? PriceYearly { get; init; }
    public string Currency { get; init; } = "EUR";

    public string? ExternalProductId { get; init; }
    public string? ExternalMonthlyPriceId { get; init; }
    public string? ExternalYearlyPriceId { get; init; }
    public string? InternalNote { get; init; }

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

public sealed class SubscriptionPlanEditDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFree { get; set; }
    public int SortOrder { get; set; }

    public decimal? PriceMonthly { get; set; }
    public decimal? PriceYearly { get; set; }
    public string Currency { get; set; } = "EUR";

    public string? ExternalProductId { get; set; }
    public string? ExternalMonthlyPriceId { get; set; }
    public string? ExternalYearlyPriceId { get; set; }
    public string? InternalNote { get; set; }

    public int? MaxTenants { get; set; }
    public int? MaxAdmins { get; set; }
    public int? MaxUsersPerTenant { get; set; }
    public int? MaxAuditorsPerTenant { get; set; }
    public int? MaxCustomAuditTemplatesPerTenant { get; set; }
    public int? MaxActiveAuditsPerTenant { get; set; }
    public int? MaxProcessingActivitiesPerTenant { get; set; }
    public int? MaxDpiaPerTenant { get; set; }
    public int? MaxTomsPerTenant { get; set; }
    public int? MaxProcessorsPerTenant { get; set; }
    public int? MaxActiveMeasuresPerTenant { get; set; }
    public int? MaxStorageMb { get; set; }
    public int? MaxEmailRemindersPerMonth { get; set; }
}

public sealed class SubscriptionPlanOptionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal? PriceMonthly { get; init; }
    public decimal? PriceYearly { get; init; }
    public string Currency { get; init; } = "EUR";
    public bool IsFree { get; init; }
    public int SortOrder { get; init; }
}
