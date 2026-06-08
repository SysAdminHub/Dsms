using Dsms.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.TenantDeletion;

public class TenantDeletionService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IUserAccessService access,
    ILogger<TenantDeletionService> logger) : ITenantDeletionService
{
    private static readonly TimeSpan DeletionGracePeriod = TimeSpan.FromDays(7);

    public async Task<TenantDeletionResult> RequestDeletionAsync(
        int tenantId, string confirmedTenantName, string userId, CancellationToken ct = default)
    {
        if (!await access.CanManageTenantDataAsync())
        {
            return TenantDeletionResult.Fail("Keine Berechtigung für diese Aktion.");
        }

        var currentTenantId = await access.GetCurrentTenantIdAsync();
        if (!currentTenantId.HasValue || currentTenantId.Value != tenantId)
        {
            return TenantDeletionResult.Fail("Ungültiger Mandantenkontext.");
        }

        if (!await access.CanAccessTenantAsync(tenantId))
        {
            return TenantDeletionResult.Fail("Kein Zugriff auf diesen Mandanten.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null)
        {
            return TenantDeletionResult.Fail("Mandant nicht gefunden.");
        }

        if (tenant.IsDeletionRequested)
        {
            return TenantDeletionResult.Fail("Für diesen Mandanten wurde bereits eine Löschung angefordert.");
        }

        if (!TenantNamesMatch(tenant.Name, confirmedTenantName))
        {
            return TenantDeletionResult.Fail("Der eingegebene Mandantenname stimmt nicht überein.");
        }

        var now = DateTime.UtcNow;
        tenant.IsDeletionRequested = true;
        tenant.DeletionRequestedAt = now;
        tenant.DeletionRequestedByUserId = userId;
        tenant.DeletionScheduledAt = now.Add(DeletionGracePeriod);
        tenant.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        logger.LogWarning(
            "Löschung angefordert: TenantId={TenantId}, UserId={UserId}, ScheduledAt={ScheduledAt}",
            tenantId, userId, tenant.DeletionScheduledAt);

        return TenantDeletionResult.Ok(
            "Die Löschung des Mandanten wurde angefordert. Bitte stellen Sie sicher, dass Sie vorher einen Datenexport heruntergeladen haben.");
    }

    public async Task<TenantDeletionResult> CancelDeletionRequestAsync(int tenantId, CancellationToken ct = default)
    {
        if (!await access.CanManageTenantsAsync())
        {
            return TenantDeletionResult.Fail("Nur Superuser können eine Löschanforderung abbrechen.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null)
        {
            return TenantDeletionResult.Fail("Mandant nicht gefunden.");
        }

        if (!tenant.IsDeletionRequested)
        {
            return TenantDeletionResult.Fail("Für diesen Mandanten liegt keine Löschanforderung vor.");
        }

        tenant.IsDeletionRequested = false;
        tenant.DeletionRequestedAt = null;
        tenant.DeletionRequestedByUserId = null;
        tenant.DeletionScheduledAt = null;
        tenant.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Löschanforderung abgebrochen: TenantId={TenantId}", tenantId);

        return TenantDeletionResult.Ok("Die Löschanforderung wurde abgebrochen.");
    }

    internal static bool TenantNamesMatch(string actual, string confirmed)
    {
        return string.Equals(
            NormalizeTenantName(actual),
            NormalizeTenantName(confirmed),
            StringComparison.Ordinal);
    }

    private static string NormalizeTenantName(string name) =>
        name.Trim();
}
