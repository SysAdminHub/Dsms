using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Licenses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Tenants;

public sealed class TenantManagementService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IUserAccessService access,
    UserManager<ApplicationUser> userManager,
    ILicenseService licenseService) : ITenantManagementService
{
    public async Task<IReadOnlyList<TenantListItemDto>> ListTenantsAsync()
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        return await (
            from t in db.Tenants.AsNoTracking()
            join l in db.Licenses.AsNoTracking() on t.LicenseId equals l.Id into licenseJoin
            from l in licenseJoin.DefaultIfEmpty()
            orderby t.Name
            select new TenantListItemDto
            {
                Id = t.Id,
                Name = t.Name,
                LegalName = t.LegalName,
                IsActive = t.IsActive,
                LicenseId = t.LicenseId,
                LicenseNumber = l != null ? l.LicenseNumber : null,
                LicenseCustomerName = l != null ? l.CustomerName : null,
                LicensePlanName = l != null ? l.PlanName : null,
                LicenseDisplayName = l != null
                    ? LicenseDisplayHelper.FormatCompactDisplay(l.LicenseNumber, l.CustomerName, l.PlanName)
                    : LicenseDisplayHelper.NoLicenseText
            }).ToListAsync();
    }

    public async Task<Tenant?> GetTenantForEditAsync(int tenantId)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
    }

    public async Task<IReadOnlyList<Tenant>> GetTenantsByLicenseAsync(Guid licenseId)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Tenants
            .AsNoTracking()
            .Where(t => t.LicenseId == licenseId && t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<TenantOperationResult> CreateTenantAsync(TenantSaveModel model)
    {
        await EnsureSuperuserAsync();

        if (!model.LicenseId.HasValue)
        {
            return TenantOperationResult.Fail("Bitte wählen Sie eine Lizenz aus.");
        }

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return TenantOperationResult.Fail("Name ist erforderlich.");
        }

        var limitCheck = await licenseService.CanCreateTenantAsync(model.LicenseId.Value);
        if (!limitCheck.IsAllowed)
        {
            return TenantOperationResult.Fail(limitCheck.Message);
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.Licenses.AnyAsync(l => l.Id == model.LicenseId.Value))
        {
            return TenantOperationResult.Fail("Die ausgewählte Lizenz existiert nicht.");
        }

        var tenant = new Tenant
        {
            Name = model.Name.Trim(),
            LegalName = string.IsNullOrWhiteSpace(model.LegalName) ? null : model.LegalName.Trim(),
            IsActive = model.IsActive,
            LicenseId = model.LicenseId
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return TenantOperationResult.Ok();
    }

    public async Task<TenantOperationResult> UpdateTenantAsync(int tenantId, TenantSaveModel model)
    {
        await EnsureSuperuserAsync();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return TenantOperationResult.Fail("Name ist erforderlich.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant is null)
        {
            return TenantOperationResult.Fail("Mandant nicht gefunden.");
        }

        if (model.LicenseId.HasValue
            && !await db.Licenses.AnyAsync(l => l.Id == model.LicenseId.Value))
        {
            return TenantOperationResult.Fail("Die ausgewählte Lizenz existiert nicht.");
        }

        var previousLicenseId = tenant.LicenseId;
        tenant.Name = model.Name.Trim();
        tenant.LegalName = string.IsNullOrWhiteSpace(model.LegalName) ? null : model.LegalName.Trim();
        tenant.IsActive = model.IsActive;
        tenant.LicenseId = model.LicenseId;
        tenant.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        if (previousLicenseId != model.LicenseId)
        {
            await SyncTenantUserLicenseIdsAsync(tenantId, model.LicenseId);
        }

        return TenantOperationResult.Ok();
    }

    /// <summary>
    /// User/Auditoren: LicenseId auf null setzen, wenn sie nicht mehr zur Mandantenlizenz passt.
    /// Admins werden nicht automatisch geändert (lizenzweite Zuordnung über ApplicationUser.LicenseId).
    /// </summary>
    private async Task SyncTenantUserLicenseIdsAsync(int tenantId, Guid? newLicenseId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var userIds = await db.UserTenants
            .IgnoreQueryFilters()
            .Where(ut => ut.TenantId == tenantId)
            .Select(ut => ut.UserId)
            .Distinct()
            .ToListAsync();

        foreach (var userId in userIds)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                continue;
            }

            var roles = await userManager.GetRolesAsync(user);
            if (roles.Contains(DsmsRoles.Superuser) || roles.Contains(DsmsRoles.Admin))
            {
                continue;
            }

            if (user.LicenseId.HasValue && user.LicenseId != newLicenseId)
            {
                user.LicenseId = null;
                await userManager.UpdateAsync(user);
            }
        }
    }

    private async Task EnsureSuperuserAsync()
    {
        if (!await access.IsSuperuserAsync())
        {
            throw new UnauthorizedAccessException("Keine Berechtigung für die Mandantenverwaltung.");
        }
    }
}
