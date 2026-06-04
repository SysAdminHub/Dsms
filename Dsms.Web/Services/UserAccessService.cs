using Dsms.Web.Data;
using Dsms.Web.Domain;
using Microsoft.AspNetCore.Identity;

namespace Dsms.Web.Services;

/// <inheritdoc />
public class UserAccessService(
    ICurrentUserContext currentUser,
    UserManager<ApplicationUser> userManager) : IUserAccessService
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
    public Task<int?> GetCurrentTenantIdAsync() => currentUser.GetTenantIdAsync();

    /// <inheritdoc />
    public async Task<bool> CanAccessTenantAsync(int tenantId)
    {
        if (await IsSuperuserAsync())
        {
            return true;
        }

        var ownTenant = await GetCurrentTenantIdAsync();
        return ownTenant == tenantId;
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

        // Mandanten-Admin: nur Benutzer des eigenen Mandanten, kein Superuser.
        if (await userManager.IsInRoleAsync(target, DsmsRoles.Superuser))
        {
            return false;
        }

        var ownTenant = await GetCurrentTenantIdAsync();
        return ownTenant.HasValue && target.TenantId == ownTenant;
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
