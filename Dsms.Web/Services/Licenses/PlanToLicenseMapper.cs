using Dsms.Web.Domain.Entities;

namespace Dsms.Web.Services.Licenses;

internal static class PlanToLicenseMapper
{
    public static License MapToLicense(
        SubscriptionPlan plan,
        CreateLicenseFromPlanDto input,
        string licenseNumber) => new()
    {
        LicenseNumber = licenseNumber,
        CustomerName = input.CustomerName.Trim(),
        CustomerEmail = NormalizeOptional(input.CustomerEmail),
        PlanName = ResolvePlanNameForLicense(plan, input.OverridePlanName),
        Status = input.Status,
        ValidFrom = input.ValidFrom,
        ValidUntil = input.ValidUntil,
        InternalNote = NormalizeOptional(input.InternalNote),
        MaxTenants = plan.MaxTenants,
        MaxAdmins = plan.MaxAdmins,
        MaxUsersPerTenant = plan.MaxUsersPerTenant,
        MaxAuditorsPerTenant = plan.MaxAuditorsPerTenant,
        MaxCustomAuditTemplatesPerTenant = plan.MaxCustomAuditTemplatesPerTenant,
        MaxActiveAuditsPerTenant = plan.MaxActiveAuditsPerTenant,
        MaxProcessingActivitiesPerTenant = plan.MaxProcessingActivitiesPerTenant,
        MaxDpiaPerTenant = plan.MaxDpiaPerTenant,
        MaxTomsPerTenant = plan.MaxTomsPerTenant,
        MaxProcessorsPerTenant = plan.MaxProcessorsPerTenant,
        MaxActiveMeasuresPerTenant = plan.MaxActiveMeasuresPerTenant,
        MaxStorageMb = plan.MaxStorageMb,
        MaxEmailRemindersPerMonth = plan.MaxEmailRemindersPerMonth
    };

    public static string ResolvePlanNameForLicense(SubscriptionPlan plan, string? overridePlanName) =>
        string.IsNullOrWhiteSpace(overridePlanName) ? plan.DisplayName : overridePlanName.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
