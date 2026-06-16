namespace Dsms.Web.Services.Licenses;

public enum LicenseBlockReason
{
    None,
    LicenseNotFound,
    LicenseNotAssigned,
    LicenseInactive,
    LicenseSuspended,
    LicenseExpired,
    LimitReached
}

public sealed class LicenseLimitCheckResult
{
    public bool IsAllowed { get; init; } = true;
    public string Message { get; init; } = string.Empty;
    public string LimitName { get; init; } = string.Empty;
    public int CurrentValue { get; init; }
    public int? LimitValue { get; init; }
    public bool IsUnlimited { get; init; }
    public bool IsWarning { get; init; }
    public bool IsExceeded { get; init; }
    public Guid? LicenseId { get; init; }
    public int? TenantId { get; init; }
    public string? LicenseStatus { get; init; }
    public DateTime? ValidUntil { get; init; }
    public LicenseBlockReason BlockReason { get; init; } = LicenseBlockReason.None;

    public LicenseLimitUsageItemDto ToUsageItem() =>
        LicenseLimitHelper.CreateItem(LimitName, CurrentValue, LimitValue);
}

public sealed class LicenseUsabilityInfo
{
    public bool IsUsableForCreation { get; init; }
    public LicenseBlockReason BlockReason { get; init; } = LicenseBlockReason.None;
    public string Status { get; init; } = string.Empty;
    public DateTime? ValidUntil { get; init; }
    public bool IsExpired { get; init; }
    public string SuperuserDisplayLabel { get; init; } = string.Empty;
    public string? AdminHintMessage { get; init; }
}

public sealed class LicenseOptionDto
{
    public Guid Id { get; init; }
    public string LicenseNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string PlanName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class LicenseListItemDto
{
    public Guid Id { get; init; }
    public string LicenseNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? ValidUntil { get; init; }
    public DateTime CreatedAt { get; init; }
    public int? MaxTenants { get; init; }
    public int? MaxAdmins { get; init; }
    public int? MaxUsersPerTenant { get; init; }
    public int? MaxAuditorsPerTenant { get; init; }
    public int? MaxActiveAuditsPerTenant { get; init; }
    public int? MaxActiveMeasuresPerTenant { get; init; }
    public LicenseUsageDto Usage { get; init; } = new();
    public LicenseUsabilityInfo Usability { get; init; } = new();
}

public sealed class LicenseDetailsDto
{
    public Guid Id { get; init; }
    public string LicenseNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public string? PlanDisplayName { get; init; }
    public string? PlanDescription { get; init; }
    public string? BillingCycleDisplay { get; init; }
    public string? AmountDisplay { get; init; }
    public string? NextInvoiceDateDisplay { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidUntil { get; init; }
    public string? InternalNote { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

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
    public bool HasTrainingModule { get; init; }

    public LicenseUsageDto Usage { get; init; } = new();
    public LicenseUsabilityInfo Usability { get; init; } = new();
}

public sealed class LicenseUsageDto
{
    public Guid LicenseId { get; init; }
    public int CurrentTenants { get; init; }
    public int CurrentAdmins { get; init; }
    public int CurrentUsersTotal { get; init; }
    public int CurrentAuditorsTotal { get; init; }
    public int CurrentCustomAuditTemplatesTotal { get; init; }
    public int CurrentActiveAuditsTotal { get; init; }
    public int CurrentProcessingActivitiesTotal { get; init; }
    public int CurrentDpiaTotal { get; init; }
    public int CurrentTomsTotal { get; init; }
    public int CurrentProcessorsTotal { get; init; }
    public int CurrentActiveMeasuresTotal { get; init; }
    public int CurrentStorageMb { get; init; }
    public int CurrentEmailRemindersThisMonth { get; init; }
    public List<TenantUsageDto> Tenants { get; init; } = [];
}

public sealed class TenantUsageDto
{
    public int TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public int CurrentUsers { get; init; }
    public int CurrentAuditors { get; init; }
    public int CurrentCustomAuditTemplates { get; init; }
    public int CurrentActiveAudits { get; init; }
    public int CurrentProcessingActivities { get; init; }
    public int CurrentDpia { get; init; }
    public int CurrentToms { get; init; }
    public int CurrentProcessors { get; init; }
    public int CurrentActiveMeasures { get; init; }
}

public sealed class LicenseLimitUsageItemDto
{
    public string Name { get; init; } = string.Empty;
    public int CurrentValue { get; init; }
    public int? LimitValue { get; init; }
    public bool IsUnlimited { get; init; }
    public int? Percentage { get; init; }
    public bool IsWarning { get; init; }
    public bool IsExceeded { get; init; }

    public string DisplayText => IsUnlimited
        ? $"{CurrentValue} / unbegrenzt"
        : $"{CurrentValue} / {LimitValue}";
}

public sealed class TenantLimitUsageDto
{
    public int TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public List<LicenseLimitUsageItemDto> Items { get; init; } = [];
}

public enum AdminLicenseOverviewStatus
{
    Success,
    IsSuperuser,
    NoLicenseAssigned,
    LicenseNotFound,
    Unauthorized
}

public sealed class AdminLicenseOverviewResult
{
    public AdminLicenseOverviewStatus Status { get; init; }
    public string? Message { get; init; }
    public LicenseDetailsDto? License { get; init; }
    public IReadOnlyList<TenantLimitUsageDto> TenantLimits { get; init; } = [];

    public static AdminLicenseOverviewResult Success(
        LicenseDetailsDto license,
        IReadOnlyList<TenantLimitUsageDto> tenantLimits) => new()
    {
        Status = AdminLicenseOverviewStatus.Success,
        License = license,
        TenantLimits = tenantLimits
    };

    public static AdminLicenseOverviewResult Superuser() => new()
    {
        Status = AdminLicenseOverviewStatus.IsSuperuser,
        Message = "Superuser verwalten Lizenzen über die Plattform-Lizenzverwaltung."
    };

    public static AdminLicenseOverviewResult NoLicenseAssigned(string message) => new()
    {
        Status = AdminLicenseOverviewStatus.NoLicenseAssigned,
        Message = message
    };

    public static AdminLicenseOverviewResult LicenseNotFound(string message) => new()
    {
        Status = AdminLicenseOverviewStatus.LicenseNotFound,
        Message = message
    };

    public static AdminLicenseOverviewResult Unauthorized() => new()
    {
        Status = AdminLicenseOverviewStatus.Unauthorized,
        Message = "Keine Berechtigung für diese Seite."
    };
}

public sealed class LicenseEditDto
{
    public Guid? Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string PlanName { get; set; } = "Manual";
    public string Status { get; set; } = "Active";
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
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
    public bool HasTrainingModule { get; set; } = true;
}
