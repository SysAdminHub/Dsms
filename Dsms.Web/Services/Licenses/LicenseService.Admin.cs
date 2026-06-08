using Dsms.Web.Data;
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
        var details = MapToDetails(license, usage);
        var tenantLimits = usage.Tenants
            .Select(t => LicenseLimitHelper.BuildTenantLimitUsage(t, details))
            .ToList();

        return AdminLicenseOverviewResult.Success(details, tenantLimits);
    }
}
