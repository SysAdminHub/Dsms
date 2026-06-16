using Dsms.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Licenses;

public sealed class LicenseFeatureService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ITenantContextService tenantContext) : ILicenseFeatureService
{
    public async Task<bool> HasTrainingModuleAsync(CancellationToken ct = default)
    {
        var tenantId = await tenantContext.GetCurrentTenantIdAsync();
        if (!tenantId.HasValue)
        {
            return false;
        }

        return await HasTrainingModuleAsync(tenantId.Value, ct);
    }

    public async Task<bool> HasTrainingModuleAsync(int tenantId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var license = await db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.License)
            .FirstOrDefaultAsync(ct);

        return license?.HasTrainingModule ?? false;
    }
}
