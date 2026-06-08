using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

/// <inheritdoc />
/// <remarks>
/// Nutzt IDbContextFactory – jede Operation erhält einen eigenen DbContext.
/// Verhindert Concurrency-Fehler bei paralleler Komponenten-Initialisierung (F5).
/// </remarks>
public class TenantService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ITenantContextService tenantContext,
    ICurrentUserContext currentUser,
    IUserAccessService access,
    ILogger<TenantService> logger) : ITenantService
{
    private bool _contextInitialized;
    private IReadOnlyList<Tenant>? _cachedAccessibleTenants;

    /// <inheritdoc />
    public async Task InitializeContextAsync()
    {
        if (_contextInitialized)
        {
            return;
        }

        try
        {
            await tenantContext.LoadFromSessionAsync();
            _cachedAccessibleTenants ??= await LoadAccessibleTenantsSafeAsync();

            if (!await tenantContext.HasTenantSelectedAsync() && _cachedAccessibleTenants.Count == 1)
            {
                await tenantContext.SetCurrentTenantIdAsync(_cachedAccessibleTenants[0].Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "InitializeContextAsync: Teilinitialisierung fehlgeschlagen");
        }
        finally
        {
            _contextInitialized = true;
        }
    }

    /// <inheritdoc />
    public async Task<TenantBootstrapState> LoadTenantAsync()
    {
        try
        {
            await tenantContext.LoadFromSessionAsync();

            if (!await tenantContext.HasTenantSelectedAsync())
            {
                await InitializeContextAsync();
            }

            var tenants = (await GetAccessibleTenantsAsync()).ToList();
            var tenantId = await tenantContext.GetCurrentTenantIdAsync();

            if (!tenantId.HasValue && tenants.Count == 1)
            {
                await tenantContext.SetCurrentTenantIdAsync(tenants[0].Id);
                tenantId = tenants[0].Id;
            }

            Tenant? current = null;
            if (tenantId.HasValue)
            {
                current = tenants.FirstOrDefault(t => t.Id == tenantId.Value)
                    ?? await GetTenantByIdSafeAsync(tenantId.Value);
            }

            logger.LogInformation(
                "Tenant bootstrap: TenantId={TenantId}, Name={Name}, Options={Count}",
                tenantId, current?.Name ?? "—", tenants.Count);

            return new TenantBootstrapState
            {
                Succeeded = true,
                AccessibleTenants = tenants,
                CurrentTenantId = tenantId,
                CurrentTenant = current
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LoadTenantAsync fehlgeschlagen");

            int? fallbackId = null;
            try
            {
                fallbackId = await tenantContext.GetCurrentTenantIdAsync();
            }
            catch
            {
                // UI soll trotzdem rendern.
            }

            return new TenantBootstrapState
            {
                Succeeded = false,
                CurrentTenantId = fallbackId,
                AccessibleTenants = _cachedAccessibleTenants ?? []
            };
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Tenant>> GetAccessibleTenantsAsync()
    {
        if (_cachedAccessibleTenants is { Count: > 0 })
        {
            return _cachedAccessibleTenants;
        }

        try
        {
            _cachedAccessibleTenants = await LoadAccessibleTenantsSafeAsync();
            return _cachedAccessibleTenants;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetAccessibleTenantsAsync fehlgeschlagen");
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<Tenant?> GetCurrentTenantAsync()
    {
        try
        {
            var tenantId = await tenantContext.GetCurrentTenantIdAsync();
            if (!tenantId.HasValue)
            {
                return null;
            }

            if (_cachedAccessibleTenants is { Count: > 0 })
            {
                var cached = _cachedAccessibleTenants.FirstOrDefault(t => t.Id == tenantId.Value);
                if (cached is not null)
                {
                    return cached;
                }
            }

            return await GetTenantByIdSafeAsync(tenantId.Value);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetCurrentTenantAsync fehlgeschlagen");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<TenantSwitchResult> SwitchTenantAsync(int tenantId)
    {
        try
        {
            if (!await CanAccessTenantAsync(tenantId))
            {
                return TenantSwitchResult.Fail("Kein Zugriff auf diesen Mandanten.");
            }

            await using var db = await dbFactory.CreateDbContextAsync();
            var exists = await db.Tenants
                .IgnoreQueryFilters()
                .AnyAsync(t => t.Id == tenantId && t.IsActive);

            if (!exists)
            {
                return TenantSwitchResult.Fail("Mandant existiert nicht oder ist inaktiv.");
            }

            await tenantContext.SetCurrentTenantIdAsync(tenantId);
            logger.LogInformation("Mandant gewechselt zu {TenantId}", tenantId);
            return TenantSwitchResult.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SwitchTenantAsync fehlgeschlagen für {TenantId}", tenantId);
            return TenantSwitchResult.Fail("Mandantenwechsel fehlgeschlagen.");
        }
    }

    /// <inheritdoc />
    public async Task<bool> CanAccessTenantAsync(int tenantId)
    {
        try
        {
            if (await access.IsSuperuserAsync())
            {
                return true;
            }

            var accessible = _cachedAccessibleTenants ?? await LoadAccessibleTenantsSafeAsync();
            _cachedAccessibleTenants ??= accessible;
            return accessible.Any(t => t.Id == tenantId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CanAccessTenantAsync fehlgeschlagen für {TenantId}", tenantId);
            return false;
        }
    }

    /// <inheritdoc />
    public bool IsTenantRequiredForRoute(string relativePath)
    {
        var path = relativePath.Trim('/').ToLowerInvariant();

        if (path.StartsWith("account", StringComparison.Ordinal)
            || path == "select-tenant"
            || path.StartsWith("tenants", StringComparison.Ordinal)
            || path.StartsWith("users", StringComparison.Ordinal)
            || path.StartsWith("admin/erinnerungen", StringComparison.Ordinal)
            || path.StartsWith("platform/email", StringComparison.Ordinal)
            || path.StartsWith("platform/licenses", StringComparison.Ordinal)
            || path.StartsWith("platform/plans", StringComparison.Ordinal)
            || path.StartsWith("platform/provisioning", StringComparison.Ordinal)
            || path == "passwort-vergessen"
            || path == "passwort-zuruecksetzen"
            || path == "not-found"
            || path == "error")
        {
            return false;
        }

        return true;
    }

    private async Task<IReadOnlyList<Tenant>> LoadAccessibleTenantsSafeAsync()
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();

            if (await access.IsSuperuserAsync())
            {
                return await db.Tenants
                    .IgnoreQueryFilters()
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }

            var userId = await currentUser.GetUserIdAsync();
            if (userId is null)
            {
                return [];
            }

            return await db.UserTenants
                .IgnoreQueryFilters()
                .Where(ut => ut.UserId == userId)
                .Select(ut => ut.Tenant)
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LoadAccessibleTenantsSafeAsync fehlgeschlagen");
            return [];
        }
    }

    private async Task<Tenant?> GetTenantByIdSafeAsync(int tenantId)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            return await db.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetTenantByIdSafeAsync fehlgeschlagen für {TenantId}", tenantId);
            return null;
        }
    }
}
