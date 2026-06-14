using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Support;

/// <inheritdoc />
public sealed class SupportAccessService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ICurrentUserContext currentUser,
    ITenantContextService tenantContext,
    ISupportContextService supportContext,
    ILogService logService) : ISupportAccessService
{
    public const string InvalidGrantMessage =
        "Der Supportzugriff ist nicht mehr gültig oder wurde widerrufen.";

    public const string NoActiveGrantMessage =
        "Für diesen Mandanten liegt kein aktiver Supportzugriff vor.";

    /// <inheritdoc />
    public async Task<bool> CanManageSupportAccessForCurrentTenantAsync()
    {
        if (await currentUser.IsInRoleAsync(DsmsRoles.Superuser))
        {
            return false;
        }

        if (!await currentUser.IsInRoleAsync(DsmsRoles.Admin))
        {
            return false;
        }

        var tenantId = await tenantContext.GetCurrentTenantIdAsync();
        if (!tenantId.HasValue)
        {
            return false;
        }

        return await HasTenantMembershipAsync(tenantId.Value);
    }

    /// <inheritdoc />
    public async Task<SupportAccessGrantDetail?> GetActiveGrantForCurrentTenantAsync()
    {
        if (!await CanManageSupportAccessForCurrentTenantAsync())
        {
            return null;
        }

        var tenantId = (await tenantContext.GetCurrentTenantIdAsync())!.Value;
        var grant = await GetActiveGrantEntityForTenantAsync(tenantId);
        return grant is null ? null : await MapDetailAsync(grant);
    }

    /// <inheritdoc />
    public async Task<SupportAccessOperationResult> GrantAccessAsync(TimeSpan duration, string? reason)
    {
        var userId = await currentUser.GetUserIdAsync();
        if (userId is null)
        {
            return SupportAccessOperationResult.Fail("Nicht angemeldet.");
        }

        if (await currentUser.IsInRoleAsync(DsmsRoles.Superuser))
        {
            await LogSecurityAsync(
                "SupportAccessGrantDenied",
                "Superuser-Versuch, Supportzugriff selbst freizugeben.",
                tenantId: null,
                grantId: null,
                result: "Denied");
            return SupportAccessOperationResult.Fail("Plattform-Administratoren können keinen Supportzugriff freigeben.");
        }

        if (!await CanManageSupportAccessForCurrentTenantAsync())
        {
            return SupportAccessOperationResult.Fail("Keine Berechtigung für Supportzugriff.");
        }

        var tenantId = (await tenantContext.GetCurrentTenantIdAsync())!.Value;
        var utcNow = DateTime.UtcNow;

        await using var db = await dbFactory.CreateDbContextAsync();

        var activeGrants = await db.SupportAccessGrants
            .Where(g => g.TenantId == tenantId && g.RevokedAt == null && g.ValidUntil > utcNow)
            .ToListAsync();

        foreach (var existing in activeGrants)
        {
            existing.RevokedAt = utcNow;
            existing.RevokedByUserId = userId;
            existing.UpdatedAt = utcNow;
        }

        var grant = new SupportAccessGrant
        {
            TenantId = tenantId,
            GrantedByUserId = userId,
            GrantedAt = utcNow,
            ValidUntil = utcNow.Add(duration),
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        db.SupportAccessGrants.Add(grant);
        await db.SaveChangesAsync();

        await LogAuditAsync(
            "SupportAccessGranted",
            "Mandanten-Admin hat Supportzugriff freigegeben.",
            tenantId,
            grant.Id,
            result: "Success",
            metadata: new { grant.ValidUntil, DurationHours = duration.TotalHours, grant.Reason });

        return SupportAccessOperationResult.Ok(grant, "Supportzugriff wurde freigegeben.");
    }

    /// <inheritdoc />
    public async Task<SupportAccessOperationResult> RevokeAccessAsync(int grantId)
    {
        var userId = await currentUser.GetUserIdAsync();
        if (userId is null)
        {
            return SupportAccessOperationResult.Fail("Nicht angemeldet.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var grant = await db.SupportAccessGrants
            .Include(g => g.Tenant)
            .FirstOrDefaultAsync(g => g.Id == grantId);

        if (grant is null)
        {
            return SupportAccessOperationResult.Fail("Supportzugriff nicht gefunden.");
        }

        var isSuperuser = await currentUser.IsInRoleAsync(DsmsRoles.Superuser);
        var canManage = await CanManageSupportAccessForCurrentTenantAsync()
            && (await tenantContext.GetCurrentTenantIdAsync()) == grant.TenantId;

        if (!isSuperuser && !canManage)
        {
            return SupportAccessOperationResult.Fail("Keine Berechtigung zum Widerruf.");
        }

        if (grant.RevokedAt is not null)
        {
            return SupportAccessOperationResult.Fail("Supportzugriff wurde bereits widerrufen.");
        }

        var utcNow = DateTime.UtcNow;
        grant.RevokedAt = utcNow;
        grant.RevokedByUserId = userId;
        grant.UpdatedAt = utcNow;
        await db.SaveChangesAsync();

        var action = isSuperuser ? "SupportAccessRevokedByPlatform" : "SupportAccessRevoked";
        var description = isSuperuser
            ? "Superuser hat Supportzugriff widerrufen."
            : "Mandanten-Admin hat Supportzugriff widerrufen.";

        await LogAuditAsync(action, description, grant.TenantId, grant.Id, result: "Success");

        var activeGrantId = await supportContext.GetActiveGrantIdAsync();
        if (activeGrantId == grantId)
        {
            await supportContext.ClearAsync();
            await tenantContext.ClearCurrentTenantIdAsync();
        }

        return SupportAccessOperationResult.Ok(grant, "Der Supportzugriff wurde widerrufen.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SupportAccessGrantListItem>> ListGrantsForPlatformAsync()
    {
        if (!await currentUser.IsInRoleAsync(DsmsRoles.Superuser))
        {
            return [];
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var utcNow = DateTime.UtcNow;

        var grants = await db.SupportAccessGrants
            .AsNoTracking()
            .Include(g => g.Tenant)
            .Include(g => g.GrantedByUser)
            .OrderByDescending(g => g.GrantedAt)
            .Take(200)
            .ToListAsync();

        await LogAuditAsync(
            "SupportAccessListViewed",
            "Superuser hat Supportzugriffsliste geöffnet.",
            tenantId: null,
            grantId: null,
            result: "Success");

        return grants.Select(g => new SupportAccessGrantListItem(
            g.Id,
            g.TenantId,
            g.Tenant.Name,
            g.IsActive(utcNow),
            g.ValidUntil,
            g.GrantedAt,
            g.GrantedByUser.Email ?? g.GrantedByUser.UserName ?? g.GrantedByUserId,
            g.Reason,
            g.RevokedAt is not null)).ToList();
    }

    /// <inheritdoc />
    public async Task<SupportAccessOperationResult> ActivateSupportModeAsync(int grantId)
    {
        if (!await currentUser.IsInRoleAsync(DsmsRoles.Superuser))
        {
            return SupportAccessOperationResult.Fail("Nur Plattform-Administratoren können den Supportmodus starten.");
        }

        var grant = await ValidateGrantAsync(grantId);
        if (grant is null)
        {
            await LogSecurityAsync(
                "SupportModeStartDenied",
                NoActiveGrantMessage,
                tenantId: null,
                grantId,
                result: "Denied");
            return SupportAccessOperationResult.Fail(NoActiveGrantMessage);
        }

        await supportContext.SetActiveGrantIdAsync(grant.Id);
        await tenantContext.SetCurrentTenantIdAsync(grant.TenantId);

        await LogAuditAsync(
            "SupportModeStarted",
            "Superuser hat Supportmodus gestartet.",
            grant.TenantId,
            grant.Id,
            result: "Success",
            metadata: new { grant.ValidUntil });

        return SupportAccessOperationResult.Ok(grant);
    }

    /// <inheritdoc />
    public async Task ExitSupportModeAsync()
    {
        var grantId = await supportContext.GetActiveGrantIdAsync();
        var tenantId = await tenantContext.GetCurrentTenantIdAsync();

        await supportContext.ClearAsync();
        await tenantContext.ClearCurrentTenantIdAsync();

        if (grantId.HasValue)
        {
            await LogAuditAsync(
                "SupportModeEnded",
                "Superuser hat Supportmodus beendet.",
                tenantId,
                grantId,
                result: "Success");
        }
    }

    /// <inheritdoc />
    public async Task<int?> EnsureSuperuserSupportContextAsync()
    {
        if (!await currentUser.IsInRoleAsync(DsmsRoles.Superuser))
        {
            return null;
        }

        var grantId = await supportContext.GetActiveGrantIdAsync();
        if (!grantId.HasValue)
        {
            await tenantContext.ClearCurrentTenantIdAsync();
            return null;
        }

        var grant = await ValidateGrantAsync(grantId.Value);
        if (grant is null)
        {
            await supportContext.ClearAsync();
            await tenantContext.ClearCurrentTenantIdAsync();

            await LogSecurityAsync(
                "SupportModeInvalidated",
                InvalidGrantMessage,
                tenantId: null,
                grantId,
                result: "Denied");
            return null;
        }

        var currentTenantId = await tenantContext.GetCurrentTenantIdAsync();
        if (currentTenantId != grant.TenantId)
        {
            await tenantContext.SetCurrentTenantIdAsync(grant.TenantId);
        }

        return grant.TenantId;
    }

    /// <inheritdoc />
    public async Task<bool> HasValidSupportAccessForCurrentTenantAsync()
    {
        if (!await currentUser.IsInRoleAsync(DsmsRoles.Superuser))
        {
            return false;
        }

        var grantId = await supportContext.GetActiveGrantIdAsync();
        var tenantId = await tenantContext.GetCurrentTenantIdAsync();
        if (!grantId.HasValue || !tenantId.HasValue)
        {
            return false;
        }

        var grant = await ValidateGrantAsync(grantId.Value);
        return grant is not null && grant.TenantId == tenantId.Value;
    }

    /// <inheritdoc />
    public async Task<bool> IsSupportModeActiveAsync() =>
        await HasValidSupportAccessForCurrentTenantAsync();

    /// <inheritdoc />
    public async Task<SupportAccessSessionInfo?> GetCurrentSupportSessionAsync()
    {
        if (!await HasValidSupportAccessForCurrentTenantAsync())
        {
            return null;
        }

        var grantId = (await supportContext.GetActiveGrantIdAsync())!.Value;
        var grant = await ValidateGrantAsync(grantId);
        if (grant is null)
        {
            return null;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var tenantName = await db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == grant.TenantId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync() ?? $"Mandant #{grant.TenantId}";

        return new SupportAccessSessionInfo(grant.Id, grant.TenantId, tenantName, grant.ValidUntil);
    }

    /// <inheritdoc />
    public async Task<SupportAccessGrant?> ValidateGrantAsync(int grantId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var grant = await db.SupportAccessGrants
            .AsNoTracking()
            .Include(g => g.Tenant)
            .FirstOrDefaultAsync(g => g.Id == grantId);

        if (grant is null || !grant.IsActive(DateTime.UtcNow))
        {
            return null;
        }

        return grant;
    }

    private async Task<SupportAccessGrant?> GetActiveGrantEntityForTenantAsync(int tenantId)
    {
        var utcNow = DateTime.UtcNow;
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.SupportAccessGrants
            .Include(g => g.Tenant)
            .Include(g => g.GrantedByUser)
            .Where(g => g.TenantId == tenantId && g.RevokedAt == null && g.ValidUntil > utcNow)
            .OrderByDescending(g => g.GrantedAt)
            .FirstOrDefaultAsync();
    }

    private async Task<SupportAccessGrantDetail?> MapDetailAsync(SupportAccessGrant grant)
    {
        string? revokedByDisplay = null;
        if (grant.RevokedByUserId is not null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var revoker = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == grant.RevokedByUserId);
            revokedByDisplay = revoker?.Email ?? revoker?.UserName ?? grant.RevokedByUserId;
        }

        return new SupportAccessGrantDetail(
            grant.Id,
            grant.TenantId,
            grant.Tenant.Name,
            grant.IsActive(DateTime.UtcNow),
            grant.ValidUntil,
            grant.GrantedAt,
            grant.GrantedByUser.Email ?? grant.GrantedByUser.UserName ?? grant.GrantedByUserId,
            grant.Reason,
            grant.RevokedAt,
            revokedByDisplay);
    }

    private async Task<bool> HasTenantMembershipAsync(int tenantId)
    {
        var userId = await currentUser.GetUserIdAsync();
        if (userId is null)
        {
            return false;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.UserTenants
            .IgnoreQueryFilters()
            .AnyAsync(ut => ut.UserId == userId && ut.TenantId == tenantId);
    }

    private Task LogAuditAsync(
        string action,
        string description,
        int? tenantId,
        int? grantId,
        string result,
        object? metadata = null) =>
        logService.LogAuditAsync(
            action,
            description,
            entityType: "SupportAccessGrant",
            entityId: grantId?.ToString(),
            tenantId: tenantId,
            metadata: MergeMetadata(metadata, grantId, result),
            isVisibleToAdmin: true);

    private Task LogSecurityAsync(
        string action,
        string description,
        int? tenantId,
        int? grantId,
        string result) =>
        logService.LogSecurityAsync(
            action,
            description,
            metadata: MergeMetadata(null, grantId, result, tenantId));

    private static object MergeMetadata(object? metadata, int? grantId, string result, int? tenantId = null)
    {
        if (metadata is null && grantId is null && tenantId is null)
        {
            return new { Result = result, SupportAccessGrantId = grantId };
        }

        return new { Result = result, SupportAccessGrantId = grantId, TenantId = tenantId, Extra = metadata };
    }
}
