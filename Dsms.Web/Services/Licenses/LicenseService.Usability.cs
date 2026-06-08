using Dsms.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Licenses;

public sealed partial class LicenseService
{
    public async Task<LicenseLimitCheckResult> CheckLicenseUsableForCreationAsync(Guid licenseId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var license = await db.Licenses.AsNoTracking().FirstOrDefaultAsync(l => l.Id == licenseId);
        if (license is null)
        {
            return LicenseLimitHelper.LicenseNotFound();
        }

        return BuildUsabilityCheckResult(license, licenseId);
    }

    private static LicenseLimitCheckResult BuildUsabilityCheckResult(License license, Guid licenseId, int? tenantId = null)
    {
        var block = LicenseLimitHelper.BuildUsabilityBlockResult(
            license.Status,
            license.ValidUntil,
            licenseId,
            tenantId);
        if (block is not null)
        {
            return block;
        }

        return new LicenseLimitCheckResult
        {
            IsAllowed = true,
            LicenseId = licenseId,
            TenantId = tenantId,
            LicenseStatus = license.Status,
            ValidUntil = license.ValidUntil,
            LimitName = "Lizenz"
        };
    }

    private static LicenseLimitCheckResult? TryBuildUsabilityBlock(License license, Guid licenseId, int? tenantId = null) =>
        LicenseLimitHelper.BuildUsabilityBlockResult(
            license.Status,
            license.ValidUntil,
            licenseId,
            tenantId);
}
