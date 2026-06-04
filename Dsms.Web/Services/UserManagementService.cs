using Dsms.Web.Data;
using Dsms.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

/// <inheritdoc />
public class UserManagementService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IUserAccessService access,
    ICurrentUserContext currentUser) : IUserManagementService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserListItem>> ListUsersAsync(bool includeInactive)
    {
        if (!await access.CanManageUsersAsync())
        {
            return [];
        }

        var tenants = await db.Tenants.ToDictionaryAsync(t => t.Id, t => t.Name);
        IQueryable<ApplicationUser> query = userManager.Users;

        if (!await access.IsSuperuserAsync())
        {
            var tenantId = await access.GetCurrentTenantIdAsync();
            if (!tenantId.HasValue)
            {
                return [];
            }

            query = query.Where(u => u.TenantId == tenantId);
        }

        if (!includeInactive)
        {
            query = query.Where(u => u.IsActive);
        }

        var users = await query.OrderBy(u => u.Email).ToListAsync();
        var result = new List<UserListItem>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var tenantName = user.TenantId is int tid && tenants.TryGetValue(tid, out var name) ? name : "—";
            result.Add(new UserListItem(user, tenantName, roles.ToList(), user.IsActive));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetUserForEditAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !await access.CanManageUserAsync(user))
        {
            return null;
        }

        return user;
    }

    /// <inheritdoc />
    public async Task<UserOperationResult> CreateUserAsync(UserCreateModel model)
    {
        if (!await access.CanManageUsersAsync())
        {
            return UserOperationResult.Fail("Keine Berechtigung zur Benutzerverwaltung.");
        }

        var validation = await ValidateRoleAndTenantAsync(model.Role, model.TenantId, isNewUser: true);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var tenantId = await ResolveTenantIdForSaveAsync(model.Role, model.TenantId);
        if (tenantId is null && !IUserAccessService.RoleRequiresNoTenant(model.Role))
        {
            return UserOperationResult.Fail("Für diese Rolle ist ein Mandant erforderlich.");
        }

        var creatorId = await currentUser.GetUserIdAsync();
        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = true,
            DisplayName = model.DisplayName.Trim(),
            TenantId = tenantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = creatorId
        };

        var createResult = await userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            return UserOperationResult.FromIdentity(createResult);
        }

        await userManager.AddToRoleAsync(user, model.Role);
        return UserOperationResult.Ok();
    }

    /// <inheritdoc />
    public async Task<UserOperationResult> UpdateUserAsync(string userId, UserEditModel model)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return UserOperationResult.Fail("Benutzer nicht gefunden.");
        }

        if (!await access.CanManageUserAsync(user))
        {
            return UserOperationResult.Fail("Keine Berechtigung, diesen Benutzer zu bearbeiten.");
        }

        var validation = await ValidateRoleAndTenantAsync(model.Role, model.TenantId, isNewUser: false);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var tenantId = await ResolveTenantIdForSaveAsync(model.Role, model.TenantId);
        if (tenantId is null && !IUserAccessService.RoleRequiresNoTenant(model.Role))
        {
            return UserOperationResult.Fail("Für diese Rolle ist ein Mandant erforderlich.");
        }

        user.DisplayName = model.DisplayName.Trim();
        user.TenantId = tenantId;
        user.IsActive = model.IsActive;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return UserOperationResult.FromIdentity(updateResult);
        }

        // Version 1: genau eine Rolle pro Benutzer (später: Rollen pro Mandant).
        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, model.Role);

        return UserOperationResult.Ok();
    }

    /// <inheritdoc />
    public async Task<UserOperationResult> SetUserActiveAsync(string userId, bool isActive)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return UserOperationResult.Fail("Benutzer nicht gefunden.");
        }

        if (!await access.CanManageUserAsync(user))
        {
            return UserOperationResult.Fail("Keine Berechtigung.");
        }

        user.IsActive = isActive;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded ? UserOperationResult.Ok() : UserOperationResult.FromIdentity(result);
    }

    private async Task<UserOperationResult> ValidateRoleAndTenantAsync(string role, int? tenantId, bool isNewUser)
    {
        var assignable = await access.GetAssignableRolesAsync();
        if (!assignable.Contains(role))
        {
            return UserOperationResult.Fail("Diese Rolle darf nicht zugewiesen werden.");
        }

        if (IUserAccessService.RoleRequiresNoTenant(role))
        {
            if (tenantId.HasValue)
            {
                return UserOperationResult.Fail("Superuser sind keinem Mandanten zugeordnet.");
            }

            return UserOperationResult.Ok();
        }

        if (!tenantId.HasValue)
        {
            return UserOperationResult.Fail("Bitte einen Mandanten auswählen.");
        }

        if (!await access.CanAccessTenantAsync(tenantId.Value))
        {
            return UserOperationResult.Fail("Mandant ist nicht zugänglich.");
        }

        var tenantExists = await db.Tenants.AnyAsync(t => t.Id == tenantId.Value);
        if (!tenantExists)
        {
            return UserOperationResult.Fail("Mandant existiert nicht.");
        }

        return UserOperationResult.Ok();
    }

    private async Task<int?> ResolveTenantIdForSaveAsync(string role, int? requestedTenantId)
    {
        if (IUserAccessService.RoleRequiresNoTenant(role))
        {
            return null;
        }

        if (await access.IsSuperuserAsync())
        {
            return requestedTenantId;
        }

        // Mandanten-Admin: immer eigener Mandant, unabhängig von manipulierten Formularwerten.
        return await access.GetCurrentTenantIdAsync();
    }
}
