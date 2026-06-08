using Dsms.Web.Data;
using Dsms.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

/// <inheritdoc />
public class UserAccessService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ICurrentUserContext currentUser,
    UserManager<ApplicationUser> userManager,
    ITenantContextService tenantContext) : IUserAccessService
{
    /// <inheritdoc />
    public Task<bool> IsSuperuserAsync() => currentUser.IsInRoleAsync(DsmsRoles.Superuser);

    /// <inheritdoc />
    public async Task<bool> IsTenantAdminAsync() =>
        await currentUser.IsInRoleAsync(DsmsRoles.Admin) && !await IsSuperuserAsync();

    /// <inheritdoc />
    public async Task<bool> CanManageUsersAsync() =>
        await IsSuperuserAsync() || await currentUser.IsInRoleAsync(DsmsRoles.Admin);

    /// <inheritdoc />
    public Task<bool> CanManageTenantsAsync() => IsSuperuserAsync();

    /// <inheritdoc />
    public async Task<bool> CanManageTenantDataAsync()
    {
        if (!await IsSuperuserAsync() && !await IsTenantAdminAsync())
        {
            return false;
        }

        var tenantId = await GetCurrentTenantIdAsync();
        if (!tenantId.HasValue)
        {
            return false;
        }

        return await CanAccessTenantAsync(tenantId.Value);
    }

    /// <inheritdoc />
    public Task<int?> GetCurrentTenantIdAsync() => tenantContext.GetCurrentTenantIdAsync();

    /// <inheritdoc />
    public async Task<bool> CanAccessTenantAsync(int tenantId)
    {
        if (await IsSuperuserAsync())
        {
            return true;
        }

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

    /// <inheritdoc />
    public async Task<bool> CanManageUserAsync(ApplicationUser target)
    {
        if (!await CanManageUsersAsync())
        {
            return false;
        }

        if (await IsSuperuserAsync())
        {
            return true;
        }

        if (await userManager.IsInRoleAsync(target, DsmsRoles.Superuser))
        {
            return false;
        }

        var ownTenant = await GetCurrentTenantIdAsync();
        if (!ownTenant.HasValue)
        {
            return false;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        return target.TenantId == ownTenant
            || await db.UserTenants
                .IgnoreQueryFilters()
                .AnyAsync(ut => ut.UserId == target.Id && ut.TenantId == ownTenant.Value);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetAssignableRolesAsync()
    {
        if (await IsSuperuserAsync())
        {
            return DsmsRoles.AssignableBySuperuser;
        }

        if (await currentUser.IsInRoleAsync(DsmsRoles.Admin))
        {
            return DsmsRoles.AssignableByTenantAdmin;
        }

        return Array.Empty<string>();
    }
}

