using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Licenses;
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
    IPasswordResetService passwordReset,
    ILicenseService licenseService) : IUserManagementService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserListItem>> ListUsersAsync(bool includeInactive)
    {
        if (!await access.CanManageUsersAsync())
        {
            return [];
        }

        await using var db = await dbFactory.CreateDbContextAsync();

        var tenants = await db.Tenants
            .IgnoreQueryFilters()
            .Select(t => new { t.Id, t.Name, t.LicenseId })
            .ToDictionaryAsync(t => t.Id, t => new TenantLicenseRow(t.Name, t.LicenseId));

        var licenses = await db.Licenses
            .AsNoTracking()
            .ToDictionaryAsync(l => l.Id);

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
                .Select(id => tenants.TryGetValue(id, out var t) ? t.Name : "?")
                .Distinct()
                .OrderBy(n => n);

            var licenseInfo = BuildUserLicenseInfo(user, roles, userTenantIds, tenants, licenses);

            result.Add(new UserListItem(
                user,
                string.Join(", ", tenantNames),
                roles.ToList(),
                user.IsActive,
                licenseInfo.LicenseId,
                licenseInfo.LicenseNumber,
                licenseInfo.LicenseCustomerName,
                licenseInfo.LicensePlanName,
                licenseInfo.LicenseDisplayName,
                licenseInfo.HasLicenseConflict,
                licenseInfo.LicenseConflictMessage));
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

        var isSuperuserManaging = await access.IsSuperuserAsync();
        var tenantIds = await ResolveTenantIdsForSaveAsync(model.Role, model.TenantIds, model.TenantId);

        var validation = await ValidateRoleAndTenantsAsync(model.Role, tenantIds, isNewUser: true);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var licenseValidation = await ValidateLicenseAsync(model.Role, model.LicenseId, tenantIds, isSuperuserManaging);
        if (!licenseValidation.Succeeded)
        {
            return licenseValidation;
        }

        if (tenantIds.Count == 0
            && RequiresTenantAssignment(model.Role, isSuperuserManaging))
        {
            return UserOperationResult.Fail("Für diese Rolle ist mindestens ein Mandant erforderlich.");
        }

        var createLimitCheck = await ValidateCreateLimitAsync(model.Role, model.LicenseId, tenantIds, isSuperuserManaging);
        if (!createLimitCheck.Succeeded)
        {
            return createLimitCheck;
        }

        var creatorId = await currentUser.GetUserIdAsync();
        var resolvedLicenseId = await ResolveLicenseIdForSaveAsync(
            model.Role, model.LicenseId, tenantIds, isSuperuserManaging);

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = true,
            DisplayName = model.DisplayName.Trim(),
            TenantId = tenantIds.FirstOrDefault(),
            LicenseId = resolvedLicenseId,
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

        var isSuperuserManaging = await access.IsSuperuserAsync();
        var tenantIds = await ResolveTenantIdsForSaveAsync(model.Role, model.TenantIds, model.TenantId);

        var validation = await ValidateRoleAndTenantsAsync(model.Role, tenantIds, isNewUser: false);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var licenseValidation = await ValidateLicenseAsync(model.Role, model.LicenseId, tenantIds, isSuperuserManaging);
        if (!licenseValidation.Succeeded)
        {
            return licenseValidation;
        }

        if (tenantIds.Count == 0
            && RequiresTenantAssignment(model.Role, isSuperuserManaging))
        {
            return UserOperationResult.Fail("Für diese Rolle ist mindestens ein Mandant erforderlich.");
        }

        user.DisplayName = model.DisplayName.Trim();
        user.TenantId = tenantIds.FirstOrDefault();
        user.IsActive = model.IsActive;
        user.LicenseId = await ResolveLicenseIdForSaveAsync(
            model.Role, model.LicenseId, tenantIds, isSuperuserManaging);

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

    private async Task<UserOperationResult> ValidateCreateLimitAsync(
        string role,
        Guid? licenseId,
        IReadOnlyList<int> tenantIds,
        bool isSuperuserManaging)
    {
        if (role == DsmsRoles.Superuser)
        {
            return UserOperationResult.Ok();
        }

        if (role == DsmsRoles.Admin)
        {
            var adminLicenseId = await ResolveAdminLicenseIdForLimitAsync(role, licenseId, tenantIds, isSuperuserManaging);
            if (!adminLicenseId.HasValue)
            {
                return UserOperationResult.Ok();
            }

            var check = await licenseService.CanCreateAdminAsync(adminLicenseId.Value);
            return check.IsAllowed
                ? UserOperationResult.Ok()
                : UserOperationResult.Fail(check.Message);
        }

        foreach (var tenantId in tenantIds)
        {
            var check = role switch
            {
                DsmsRoles.Auditor => await licenseService.CanCreateAuditorAsync(tenantId),
                _ => await licenseService.CanCreateUserAsync(tenantId)
            };

            if (!check.IsAllowed)
            {
                return UserOperationResult.Fail(check.Message);
            }
        }

        return UserOperationResult.Ok();
    }

    private async Task<Guid?> ResolveAdminLicenseIdForLimitAsync(
        string role,
        Guid? licenseId,
        IReadOnlyList<int> tenantIds,
        bool isSuperuserManaging)
    {
        if (role != DsmsRoles.Admin)
        {
            return null;
        }

        if (isSuperuserManaging)
        {
            return licenseId;
        }

        return await ResolveLicenseIdForSaveAsync(role, licenseId, tenantIds, isSuperuserManaging);
    }

    private static bool RequiresTenantAssignment(string role, bool isSuperuserManaging) =>
        role switch
        {
            DsmsRoles.Superuser => false,
            DsmsRoles.Admin => !isSuperuserManaging,
            _ => true
        };

    private async Task<Guid?> ResolveLicenseIdForSaveAsync(
        string role,
        Guid? requestedLicenseId,
        IReadOnlyList<int> tenantIds,
        bool isSuperuserManaging)
    {
        if (role == DsmsRoles.Superuser)
        {
            return null;
        }

        if (isSuperuserManaging)
        {
            return role == DsmsRoles.Admin ? requestedLicenseId : null;
        }

        if (role == DsmsRoles.Admin)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var tenantId = tenantIds.FirstOrDefault();
            if (tenantId == 0)
            {
                tenantId = await access.GetCurrentTenantIdAsync() ?? 0;
            }

            if (tenantId > 0)
            {
                var tenant = await db.Tenants.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Id == tenantId);
                return tenant?.LicenseId;
            }
        }

        return null;
    }

    private async Task<UserOperationResult> ValidateLicenseAsync(
        string role,
        Guid? licenseId,
        IReadOnlyList<int> tenantIds,
        bool isSuperuserManaging)
    {
        if (role == DsmsRoles.Superuser)
        {
            if (licenseId.HasValue)
            {
                return UserOperationResult.Fail("Superuser benötigen keine Kundenlizenz.");
            }

            return UserOperationResult.Ok();
        }

        if (!isSuperuserManaging)
        {
            return UserOperationResult.Ok();
        }

        if (!licenseId.HasValue)
        {
            return UserOperationResult.Fail("Bitte wählen Sie eine Lizenz aus.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.Licenses.AnyAsync(l => l.Id == licenseId.Value))
        {
            return UserOperationResult.Fail("Die ausgewählte Lizenz existiert nicht.");
        }

        if (role == DsmsRoles.Admin)
        {
            foreach (var tenantId in tenantIds)
            {
                var tenantLicenseId = await GetTenantLicenseIdAsync(db, tenantId);
                if (tenantLicenseId != licenseId)
                {
                    return UserOperationResult.Fail("Der ausgewählte Mandant gehört nicht zur ausgewählten Lizenz.");
                }
            }

            return UserOperationResult.Ok();
        }

        if (tenantIds.Count == 0)
        {
            return UserOperationResult.Fail(
                "Benutzer und Auditoren müssen einem Mandanten der ausgewählten Lizenz zugeordnet sein.");
        }

        foreach (var tenantId in tenantIds)
        {
            var tenantLicenseId = await GetTenantLicenseIdAsync(db, tenantId);
            if (tenantLicenseId != licenseId)
            {
                return UserOperationResult.Fail("Der ausgewählte Mandant gehört nicht zur ausgewählten Lizenz.");
            }
        }

        return UserOperationResult.Ok();
    }

    private static async Task<Guid?> GetTenantLicenseIdAsync(ApplicationDbContext db, int tenantId) =>
        await db.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Id == tenantId)
            .Select(t => t.LicenseId)
            .FirstOrDefaultAsync();

    private sealed record UserLicenseInfo(
        Guid? LicenseId,
        string? LicenseNumber,
        string? LicenseCustomerName,
        string? LicensePlanName,
        string LicenseDisplayName,
        bool HasLicenseConflict,
        string? LicenseConflictMessage);

    private sealed record TenantLicenseRow(string Name, Guid? LicenseId);

    private static UserLicenseInfo BuildUserLicenseInfo(
        ApplicationUser user,
        IList<string> roles,
        IReadOnlyList<int> userTenantIds,
        Dictionary<int, TenantLicenseRow> tenants,
        Dictionary<Guid, Domain.Entities.License> licenses)
    {
        if (roles.Contains(DsmsRoles.Superuser))
        {
            return new UserLicenseInfo(
                null, null, null, null,
                LicenseDisplayHelper.SystemLicenseText,
                user.LicenseId.HasValue,
                user.LicenseId.HasValue ? "Superuser sollten keine Kundenlizenz haben." : null);
        }

        if (roles.Contains(DsmsRoles.Admin))
        {
            if (user.LicenseId is Guid adminLicenseId
                && licenses.TryGetValue(adminLicenseId, out var adminLicense))
            {
                var tenantLicenseIds = userTenantIds
                    .Where(tenants.ContainsKey)
                    .Select(id => tenants[id].LicenseId)
                    .Where(id => id.HasValue)
                    .Distinct()
                    .ToList();

                var hasConflict = tenantLicenseIds.Any(id => id != adminLicenseId);
                return new UserLicenseInfo(
                    adminLicenseId,
                    adminLicense.LicenseNumber,
                    adminLicense.CustomerName,
                    adminLicense.PlanName,
                    LicenseDisplayHelper.FormatCompactDisplay(
                        adminLicense.LicenseNumber, adminLicense.CustomerName, adminLicense.PlanName),
                    hasConflict,
                    hasConflict ? "Lizenz stimmt nicht mit Mandant überein" : null);
            }

            return new UserLicenseInfo(
                user.LicenseId, null, null, null,
                user.LicenseId.HasValue
                    ? LicenseDisplayHelper.NoLicenseText
                    : "Keine Lizenz zugeordnet",
                user.LicenseId is null,
                user.LicenseId is null ? "Für Kundenadmins muss eine Lizenz ausgewählt werden." : null);
        }

        var tenantLicenses = userTenantIds
            .Where(tenants.ContainsKey)
            .Select(id => tenants[id].LicenseId)
            .Distinct()
            .ToList();

        if (tenantLicenses.Count == 0 || tenantLicenses.All(id => id is null))
        {
            return new UserLicenseInfo(
                null, null, null, null,
                LicenseDisplayHelper.NoLicenseText,
                user.LicenseId.HasValue,
                user.LicenseId.HasValue ? "Lizenzkonflikt" : null);
        }

        var distinctLicenseIds = tenantLicenses.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (distinctLicenseIds.Count > 1)
        {
            return new UserLicenseInfo(
                null, null, null, null,
                LicenseDisplayHelper.MultipleLicensesText,
                true,
                "Lizenz stimmt nicht mit Mandant überein");
        }

        var primaryLicenseId = distinctLicenseIds[0];
        var license = licenses[primaryLicenseId];
        var userLicenseConflict = user.LicenseId.HasValue && user.LicenseId != primaryLicenseId;

        return new UserLicenseInfo(
            primaryLicenseId,
            license.LicenseNumber,
            license.CustomerName,
            license.PlanName,
            LicenseDisplayHelper.FormatCompactDisplay(
                license.LicenseNumber, license.CustomerName, license.PlanName),
            userLicenseConflict,
            userLicenseConflict ? "Die Benutzerlizenz stimmt nicht mit der Mandantenlizenz überein." : null);
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

        var isSuperuserManaging = await access.IsSuperuserAsync();
        if (tenantIds.Count == 0 && !isSuperuserManaging)
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

        var ownTenant = await access.GetCurrentTenantIdAsync();
        if (ownTenant.HasValue)
        {
            return [ownTenant.Value];
        }

        return ids;
    }
}
