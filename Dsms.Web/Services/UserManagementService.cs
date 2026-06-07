using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.PasswordReset;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

/// <inheritdoc />
public class UserManagementService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    UserManager<ApplicationUser> userManager,
    IUserAccessService access,
    ICurrentUserContext currentUser,
    IPasswordResetService passwordReset) : IUserManagementService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserListItem>> ListUsersAsync(bool includeInactive)
    {
        if (!await access.CanManageUsersAsync())
        {
            return [];
        }

        await using var db = await dbFactory.CreateDbContextAsync();

        var tenants = await db.Tenants.IgnoreQueryFilters().ToDictionaryAsync(t => t.Id, t => t.Name);
        IQueryable<ApplicationUser> query = userManager.Users;

        if (!await access.IsSuperuserAsync())
        {
            var tenantId = await access.GetCurrentTenantIdAsync();
            if (!tenantId.HasValue)
            {
                return [];
            }

            var userIdsInTenant = await db.UserTenants
                .IgnoreQueryFilters()
                .Where(ut => ut.TenantId == tenantId.Value)
                .Select(ut => ut.UserId)
                .ToListAsync();

            query = query.Where(u => u.TenantId == tenantId || userIdsInTenant.Contains(u.Id));
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
            var userTenantIds = await db.UserTenants
                .IgnoreQueryFilters()
                .Where(ut => ut.UserId == user.Id)
                .Select(ut => ut.TenantId)
                .ToListAsync();

            if (userTenantIds.Count == 0 && user.TenantId is int legacyTenantId)
            {
                userTenantIds.Add(legacyTenantId);
            }

            var tenantNames = userTenantIds
                .Select(id => tenants.TryGetValue(id, out var name) ? name : "?")
                .Distinct()
                .OrderBy(n => n);

            result.Add(new UserListItem(user, string.Join(", ", tenantNames), roles.ToList(), user.IsActive));
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
    public async Task<IReadOnlyList<int>> GetUserTenantIdsAsync(string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var ids = await db.UserTenants
            .IgnoreQueryFilters()
            .Where(ut => ut.UserId == userId)
            .Select(ut => ut.TenantId)
            .ToListAsync();

        if (ids.Count == 0)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user?.TenantId is int legacyId)
            {
                ids.Add(legacyId);
            }
        }

        return ids;
    }

    /// <inheritdoc />
    public async Task<UserOperationResult> CreateUserAsync(UserCreateModel model)
    {
        if (!await access.CanManageUsersAsync())
        {
            return UserOperationResult.Fail("Keine Berechtigung zur Benutzerverwaltung.");
        }

        var tenantIds = await ResolveTenantIdsForSaveAsync(model.Role, model.TenantIds, model.TenantId);
        var validation = await ValidateRoleAndTenantsAsync(model.Role, tenantIds, isNewUser: true);
        if (!validation.Succeeded)
        {
            return validation;
        }

        if (tenantIds.Count == 0 && !IUserAccessService.RoleRequiresNoTenant(model.Role))
        {
            return UserOperationResult.Fail("Für diese Rolle ist mindestens ein Mandant erforderlich.");
        }

        var creatorId = await currentUser.GetUserIdAsync();
        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = true,
            DisplayName = model.DisplayName.Trim(),
            TenantId = tenantIds.FirstOrDefault(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = creatorId
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            return UserOperationResult.FromIdentity(createResult);
        }

        await userManager.AddToRoleAsync(user, model.Role);
        await SyncUserTenantsAsync(user.Id, tenantIds);

        var inviteResult = await passwordReset.SendWelcomeInvitationAsync(user.Id);
        if (inviteResult.Succeeded)
        {
            return UserOperationResult.OkWithInfo(
                "Benutzer wurde angelegt und die Willkommensmail wurde versendet.");
        }

        return UserOperationResult.OkWithInfo(
            "Benutzer wurde angelegt, aber die Willkommensmail konnte nicht gesendet werden. Bitte Email-Einstellungen prüfen oder Einladung erneut senden.");
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

        var tenantIds = await ResolveTenantIdsForSaveAsync(model.Role, model.TenantIds, model.TenantId);
        var validation = await ValidateRoleAndTenantsAsync(model.Role, tenantIds, isNewUser: false);
        if (!validation.Succeeded)
        {
            return validation;
        }

        if (tenantIds.Count == 0 && !IUserAccessService.RoleRequiresNoTenant(model.Role))
        {
            return UserOperationResult.Fail("Für diese Rolle ist mindestens ein Mandant erforderlich.");
        }

        user.DisplayName = model.DisplayName.Trim();
        user.TenantId = tenantIds.FirstOrDefault();
        user.IsActive = model.IsActive;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return UserOperationResult.FromIdentity(updateResult);
        }

        await SyncUserTenantsAsync(user.Id, tenantIds);

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

    private async Task SyncUserTenantsAsync(string userId, IReadOnlyList<int> tenantIds)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var existing = await db.UserTenants
            .IgnoreQueryFilters()
            .Where(ut => ut.UserId == userId)
            .ToListAsync();

        db.UserTenants.RemoveRange(existing);

        foreach (var tenantId in tenantIds.Distinct())
        {
            db.UserTenants.Add(new UserTenant
            {
                UserId = userId,
                TenantId = tenantId,
                AssignedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task<UserOperationResult> ValidateRoleAndTenantsAsync(
        string role,
        IReadOnlyList<int> tenantIds,
        bool isNewUser)
    {
        var assignable = await access.GetAssignableRolesAsync();
        if (!assignable.Contains(role))
        {
            return UserOperationResult.Fail("Diese Rolle darf nicht zugewiesen werden.");
        }

        if (IUserAccessService.RoleRequiresNoTenant(role))
        {
            if (tenantIds.Count > 0)
            {
                return UserOperationResult.Fail("Superuser sind keinem Mandanten zugeordnet.");
            }

            return UserOperationResult.Ok();
        }

        if (tenantIds.Count == 0)
        {
            return UserOperationResult.Fail(
                "Kein Mandant zugeordnet. Bitte wählen Sie oben im Header einen aktiven Mandanten aus.");
        }

        foreach (var tenantId in tenantIds)
        {
            if (!await access.CanAccessTenantAsync(tenantId))
            {
                return UserOperationResult.Fail("Ein ausgewählter Mandant ist nicht zugänglich.");
            }

            await using var db = await dbFactory.CreateDbContextAsync();
            var tenantExists = await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == tenantId);
            if (!tenantExists)
            {
                return UserOperationResult.Fail("Ein ausgewählter Mandant existiert nicht.");
            }
        }

        return UserOperationResult.Ok();
    }

    private async Task<IReadOnlyList<int>> ResolveTenantIdsForSaveAsync(
        string role,
        IList<int> requestedTenantIds,
        int? legacyTenantId)
    {
        if (IUserAccessService.RoleRequiresNoTenant(role))
        {
            return [];
        }

        var ids = requestedTenantIds.Count > 0
            ? requestedTenantIds.ToList()
            : legacyTenantId is int tid ? [tid] : [];

        if (await access.IsSuperuserAsync())
        {
            return ids;
        }

        // Mandanten-Admin: aktiver Mandant aus TenantContextService (zentrale Wahrheit).
        var ownTenant = await access.GetCurrentTenantIdAsync();
        if (ownTenant.HasValue)
        {
            return [ownTenant.Value];
        }

        // Fallback: beim Formular-Init aus dem Mandantenkontext befüllte IDs.
        return ids;
    }
}
