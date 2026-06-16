using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Support;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

/// <inheritdoc />
/// <remarks>
/// Nutzt IDbContextFactory – jede Operation erhält einen eigenen DbContext.
/// Verhindert Concurrency-Fehler bei paralleler Komponenten-Initialisierung (F5).
/// Der Scoped <see cref="TenantContextAccessor"/> ist ein Cache; die Session ist die persistente Quelle.
/// </remarks>
public class TenantService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ITenantContextService tenantContext,
    ICurrentUserContext currentUser,
    IUserAccessService access,
    ISupportAccessService supportAccess,
    ILogger<TenantService> logger) : ITenantService
{
    private IReadOnlyList<Tenant>? _cachedAccessibleTenants;

    /// <inheritdoc />
    public Task InitializeContextAsync() => EnsureTenantContextAsync();

    /// <inheritdoc />
    public async Task<int?> EnsureTenantContextAsync()
    {
        try
        {
            if (await access.IsSuperuserAsync())
            {
                return await supportAccess.EnsureSuperuserSupportContextAsync();
            }

            var tenantId = await tenantContext.GetCurrentTenantIdAsync();
            if (tenantId.HasValue)
            {
                if (await IsActiveAccessibleTenantAsync(tenantId.Value))
                {
                    logger.LogDebug("Mandantenkontext bereits im Cache: {TenantId}", tenantId);
                    return tenantId;
                }

                logger.LogWarning(
                    "Mandant {TenantId} im Cache ohne gültigen Zugriff – Cache und Session werden geleert",
                    tenantId);
                await tenantContext.ClearCurrentTenantIdAsync();
            }

            // Cache leer oder ungültig: Session erneut lesen (Recovery nach Circuit-Verlust).
            tenantId = await tenantContext.GetCurrentTenantIdAsync();
            if (tenantId.HasValue)
            {
                if (await IsActiveAccessibleTenantAsync(tenantId.Value))
                {
                    logger.LogInformation(
                        "Mandantenkontext aus Session wiederhergestellt: {TenantId}",
                        tenantId);
                    return tenantId;
                }

                logger.LogWarning(
                    "Mandant {TenantId} in Session ohne gültigen Zugriff – Eintrag wird entfernt",
                    tenantId);
                await tenantContext.ClearCurrentTenantIdAsync();
            }

            var tenants = await GetAccessibleTenantsAsync();
            if (tenants.Count == 1)
            {
                await tenantContext.SetCurrentTenantIdAsync(tenants[0].Id);
                logger.LogDebug("Mandant automatisch gewählt (einziger zugewiesener Mandant): {TenantId}", tenants[0].Id);
                return tenants[0].Id;
            }

            logger.LogDebug("Kein aktiver Mandant im Cache oder in der Session");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "EnsureTenantContextAsync fehlgeschlagen");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<TenantBootstrapState> LoadTenantAsync()
    {
        try
        {
            var tenantId = await EnsureTenantContextAsync();
            var tenants = (await GetAccessibleTenantsAsync()).ToList();

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
            var tenantId = await EnsureTenantContextAsync();
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
            if (await access.IsSuperuserAsync())
            {
                if (await supportAccess.IsSupportModeActiveAsync())
                {
                    return TenantSwitchResult.Fail(
                        "Beenden Sie zuerst den Supportmodus, bevor Sie den Mandanten wechseln.");
                }

                logger.LogWarning("Superuser-Versuch, Mandant {TenantId} zu wechseln – abgelehnt", tenantId);
                return TenantSwitchResult.Fail(
                    "Plattform-Administratoren können nicht in Mandanten wechseln.");
            }

            if (!await CanAccessTenantAsync(tenantId))
            {
                return TenantSwitchResult.Fail("Kein Zugriff auf diesen Mandanten.");
            }

            await using var db = await dbFactory.CreateDbContextAsync();
            var exists = await db.Tenants
                .IgnoreQueryFilters()
                .AnyAsync(t => t.Id == tenantId && t.IsActive && !t.IsDeletionRequested);

            if (!exists)
            {
                return TenantSwitchResult.Fail("Mandant existiert nicht, ist inaktiv oder zur Löschung vorgemerkt.");
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
                if (!await supportAccess.HasValidSupportAccessForCurrentTenantAsync())
                {
                    return false;
                }

                var current = await tenantContext.GetCurrentTenantIdAsync();
                return current == tenantId;
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
    public bool IsTenantRequiredForRoute(string relativePath) =>
        RouteAccessClassifier.IsTenantRequiredForRoute(relativePath);

    /// <summary>Prüft Zugriff und Aktiv-Status – Basis für Session-Recovery ohne Sicherheitslücke.</summary>
    private async Task<bool> IsActiveAccessibleTenantAsync(int tenantId)
    {
        if (!await CanAccessTenantAsync(tenantId))
        {
            return false;
        }

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            return await db.Tenants
                .IgnoreQueryFilters()
                .AnyAsync(t => t.Id == tenantId && t.IsActive && !t.IsDeletionRequested);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "IsActiveAccessibleTenantAsync fehlgeschlagen für {TenantId}", tenantId);
            return false;
        }
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
                .Where(t => t.IsActive && !t.IsDeletionRequested)
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
