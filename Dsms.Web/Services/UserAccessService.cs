using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Services.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

/// <inheritdoc />
public class UserAccessService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ICurrentUserContext currentUser,
    UserManager<ApplicationUser> userManager,
    ITenantContextService tenantContext,
    ISupportAccessService supportAccess) : IUserAccessService
{
    /// <inheritdoc />
    public Task<bool> IsSuperuserAsync() => currentUser.IsInRoleAsync(DsmsRoles.Superuser);

    /// <inheritdoc />
    public async Task<bool> IsTenantUserAsync()
    {
        if (await IsSuperuserAsync())
        {
            return false;
        }

        var userId = await currentUser.GetUserIdAsync();
        if (userId is null)
        {
            return false;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.UserTenants
            .IgnoreQueryFilters()
            .AnyAsync(ut => ut.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<bool> HasTenantContextAsync()
    {
        return (await GetCurrentTenantIdAsync()).HasValue;
    }

    /// <inheritdoc />
    public Task<bool> CanAccessPlatformAdministrationAsync() => IsSuperuserAsync();

    /// <inheritdoc />
    public async Task<bool> CanAccessTenantBusinessModulesAsync()
    {
        if (await IsSuperuserAsync())
        {
            return await supportAccess.HasValidSupportAccessForCurrentTenantAsync();
        }

        var tenantId = await GetCurrentTenantIdAsync();
        if (!tenantId.HasValue)
        {
            return false;
        }

        return await CanAccessTenantMembershipAsync(tenantId.Value);
    }

    /// <inheritdoc />
    public Task<bool> CanManageGlobalAuditTemplatesAsync() => IsSuperuserAsync();

    /// <inheritdoc />
    public Task<bool> CanManageGlobalTrainingTemplatesAsync() => IsSuperuserAsync();

    /// <inheritdoc />
    public Task<bool> CanReviewCommunityTrainingTemplatesAsync() => IsSuperuserAsync();

    /// <inheritdoc />
    public Task<bool> CanAccessTenantTrainingsAsync() => CanAccessTenantBusinessModulesAsync();

    /// <inheritdoc />
    public async Task<bool> CanAccessTenantAuditsAsync()
    {
        if (await IsSuperuserAsync())
        {
            return await supportAccess.HasValidSupportAccessForCurrentTenantAsync();
        }

        var tenantId = await GetCurrentTenantIdAsync();
        if (!tenantId.HasValue)
        {
            return false;
        }

        return await CanAccessTenantMembershipAsync(tenantId.Value);
    }

    /// <inheritdoc />
    public Task<bool> IsSupportModeAsync() => supportAccess.IsSupportModeActiveAsync();

    /// <inheritdoc />
    public Task<bool> CanManageSupportAccessAsync() => supportAccess.CanManageSupportAccessForCurrentTenantAsync();

    /// <inheritdoc />
    public async Task<bool> IsTenantAdminAsync() =>
        await currentUser.IsInRoleAsync(DsmsRoles.Admin) && !await IsSuperuserAsync();

    /// <inheritdoc />
    public async Task<bool> HasEffectiveTenantAdminPermissionsAsync()
    {
        if (await IsTenantAdminAsync())
        {
            var tenantId = await GetCurrentTenantIdAsync();
            if (!tenantId.HasValue)
            {
                return false;
            }

            return await CanAccessTenantMembershipAsync(tenantId.Value);
        }

        if (await IsSuperuserAsync())
        {
            return await supportAccess.HasValidSupportAccessForCurrentTenantAsync();
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> HasEffectiveTenantAdminPermissionsForTenantAsync(int tenantId)
    {
        if (await IsTenantAdminAsync())
        {
            var currentTenantId = await GetCurrentTenantIdAsync();
            return currentTenantId == tenantId && await CanAccessTenantMembershipAsync(tenantId);
        }

        if (await IsSuperuserAsync())
        {
            if (!await supportAccess.HasValidSupportAccessForCurrentTenantAsync())
            {
                return false;
            }

            var currentTenantId = await GetCurrentTenantIdAsync();
            return currentTenantId == tenantId;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<string> GetTenantBusinessWriteDeniedMessageAsync()
    {
        if (await IsSuperuserAsync() && !await HasEffectiveTenantAdminPermissionsAsync())
        {
            return SupportAccessService.InvalidGrantMessage;
        }

        return "Keine Berechtigung für diese Aktion.";
    }

    /// <inheritdoc />
    public async Task<bool> CanManageUsersAsync() =>
        await IsSuperuserAsync() || await currentUser.IsInRoleAsync(DsmsRoles.Admin);

    /// <inheritdoc />
    public Task<bool> CanManageTenantsAsync() => IsSuperuserAsync();

    /// <inheritdoc />
    public async Task<bool> CanManageTenantDataAsync() =>
        await HasEffectiveTenantAdminPermissionsAsync();

    /// <inheritdoc />
    public Task<int?> GetCurrentTenantIdAsync() => tenantContext.GetCurrentTenantIdAsync();

    /// <inheritdoc />
    public async Task<bool> CanAccessTenantAsync(int tenantId)
    {
        if (await IsSuperuserAsync())
        {
            if (!await supportAccess.HasValidSupportAccessForCurrentTenantAsync())
            {
                return false;
            }

            var current = await GetCurrentTenantIdAsync();
            return current == tenantId;
        }

        return await CanAccessTenantMembershipAsync(tenantId);
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

    /// <inheritdoc />
    public async Task<bool> IsAuditorAsync() =>
        await currentUser.IsInRoleAsync(DsmsRoles.Auditor)
        && !await IsSuperuserAsync()
        && !await currentUser.IsInRoleAsync(DsmsRoles.Admin);

    /// <inheritdoc />
    public async Task<bool> CanEditComplianceContentAsync()
    {
        if (await IsAuditorAsync())
        {
            return false;
        }

        return await HasEffectiveTenantAdminPermissionsAsync();
    }

    /// <inheritdoc />
    public async Task<bool> CanEditTenantOperationalContentAsync()
    {
        if (await IsAuditorAsync())
        {
            return false;
        }

        if (await HasEffectiveTenantAdminPermissionsAsync())
        {
            return true;
        }

        if (await IsSuperuserAsync())
        {
            return false;
        }

        return await currentUser.IsInRoleAsync(DsmsRoles.Admin)
            || await currentUser.IsInRoleAsync(DsmsRoles.User);
    }

    /// <inheritdoc />
    public async Task<bool> CanCreatePrivacyIncidentsAsync()
    {
        if (await IsAuditorAsync())
        {
            return false;
        }

        return await HasEffectiveTenantAdminPermissionsAsync();
    }

    /// <inheritdoc />
    public async Task<bool> CanEditPrivacyIncidentsAsync() => await CanEditTenantOperationalContentAsync();

    /// <inheritdoc />
    public async Task<bool> CanCreateDataSubjectRequestsAsync() => await CanCreatePrivacyIncidentsAsync();

    /// <inheritdoc />
    public async Task<bool> CanEditDataSubjectRequestsAsync() => await CanEditTenantOperationalContentAsync();

    /// <inheritdoc />
    public async Task<bool> CanAnonymizeDataSubjectRequestsAsync()
    {
        if (await IsAuditorAsync())
        {
            return false;
        }

        return await HasEffectiveTenantAdminPermissionsAsync();
    }

    /// <inheritdoc />
    public Task<bool> CanManageDocumentCategoriesAsync() => CanManageTenantDataAsync();

    /// <inheritdoc />
    public Task<bool> CanManageDataProtectionRolesAsync() => CanManageTenantDataAsync();

    private async Task<bool> CanAccessTenantMembershipAsync(int tenantId)
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
}
