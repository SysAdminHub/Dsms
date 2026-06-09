using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.PendingSignups;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Licenses;

public sealed partial class LicenseService
{
    public const string AdminNoLicenseMessage =
        "Für Ihr Benutzerkonto ist keine Lizenz zugeordnet. Bitte wenden Sie sich an den Support.";

    public const string AdminLicenseNotFoundMessage =
        "Die zugeordnete Lizenz konnte nicht gefunden werden. Bitte wenden Sie sich an den Support.";

    public async Task<AdminLicenseOverviewResult> GetCurrentAdminLicenseOverviewAsync()
    {
        if (await access.IsSuperuserAsync())
        {
            return AdminLicenseOverviewResult.Superuser();
        }

        if (!await access.IsTenantAdminAsync())
        {
            return AdminLicenseOverviewResult.Unauthorized();
        }

        var userId = await currentUser.GetUserIdAsync();
        if (string.IsNullOrEmpty(userId))
        {
            return AdminLicenseOverviewResult.Unauthorized();
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.LicenseId is not Guid licenseId)
        {
            return AdminLicenseOverviewResult.NoLicenseAssigned(AdminNoLicenseMessage);
        }

        var license = await db.Licenses.AsNoTracking().FirstOrDefaultAsync(l => l.Id == licenseId);
        if (license is null)
        {
            return AdminLicenseOverviewResult.LicenseNotFound(AdminLicenseNotFoundMessage);
        }

        var usage = await BuildUsageAsync(db, licenseId);
        var details = await MapToAdminDetailsAsync(db, license, usage);
        var tenantLimits = usage.Tenants
            .Select(t => LicenseLimitHelper.BuildTenantLimitUsage(t, details))
            .ToList();

        return AdminLicenseOverviewResult.Success(details, tenantLimits);
    }

    private async Task<LicenseDetailsDto> MapToAdminDetailsAsync(
        ApplicationDbContext db,
        License license,
        LicenseUsageDto usage)
    {
        var baseDetails = MapToDetails(license, usage);

        var plan = await db.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name == license.PlanName);

        var signup = await db.PendingSignups
            .AsNoTracking()
            .Where(p => p.ProvisionedLicenseId == license.Id)
            .OrderByDescending(p => p.ProvisionedAt ?? p.CreatedAt)
            .FirstOrDefaultAsync();

        string? billingCycleDisplay = null;
        string? amountDisplay = null;
        string? nextInvoiceDateDisplay = null;

        if (signup is not null)
        {
            var isFree = PendingSignupDisplayHelper.IsFreeSignup(signup.PaymentProvider, signup.Amount);
            billingCycleDisplay = BillingCycleDisplayHelper.GetDisplayName(
                signup.BillingCycle,
                signup.MetadataJson,
                isFree);
            amountDisplay = isFree
                ? "Kostenlos"
                : BillingCycleDisplayHelper.FormatAmountWithCycle(
                    signup.PaymentProvider,
                    signup.Amount,
                    signup.Currency,
                    signup.BillingCycle,
                    signup.MetadataJson);
            nextInvoiceDateDisplay = BillingStatusDisplayHelper.FormatNextInvoiceDate(
                signup.NextInvoiceDate,
                isFree);
        }

        return new LicenseDetailsDto
        {
            Id = baseDetails.Id,
            LicenseNumber = baseDetails.LicenseNumber,
            CustomerName = baseDetails.CustomerName,
            CustomerEmail = baseDetails.CustomerEmail,
            PlanName = baseDetails.PlanName,
            PlanDisplayName = plan?.DisplayName,
            PlanDescription = plan?.Description,
            BillingCycleDisplay = billingCycleDisplay,
            AmountDisplay = amountDisplay,
            NextInvoiceDateDisplay = nextInvoiceDateDisplay,
            Status = baseDetails.Status,
            ValidFrom = baseDetails.ValidFrom,
            ValidUntil = baseDetails.ValidUntil,
            InternalNote = baseDetails.InternalNote,
            CreatedAt = baseDetails.CreatedAt,
            UpdatedAt = baseDetails.UpdatedAt,
            MaxTenants = baseDetails.MaxTenants,
            MaxAdmins = baseDetails.MaxAdmins,
            MaxUsersPerTenant = baseDetails.MaxUsersPerTenant,
            MaxAuditorsPerTenant = baseDetails.MaxAuditorsPerTenant,
            MaxCustomAuditTemplatesPerTenant = baseDetails.MaxCustomAuditTemplatesPerTenant,
            MaxActiveAuditsPerTenant = baseDetails.MaxActiveAuditsPerTenant,
            MaxProcessingActivitiesPerTenant = baseDetails.MaxProcessingActivitiesPerTenant,
            MaxDpiaPerTenant = baseDetails.MaxDpiaPerTenant,
            MaxTomsPerTenant = baseDetails.MaxTomsPerTenant,
            MaxProcessorsPerTenant = baseDetails.MaxProcessorsPerTenant,
            MaxActiveMeasuresPerTenant = baseDetails.MaxActiveMeasuresPerTenant,
            MaxStorageMb = baseDetails.MaxStorageMb,
            MaxEmailRemindersPerMonth = baseDetails.MaxEmailRemindersPerMonth,
            Usage = baseDetails.Usage,
            Usability = baseDetails.Usability
        };
    }
}
