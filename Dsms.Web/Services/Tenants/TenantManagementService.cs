using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Licenses;
using Dsms.Web.Services.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Tenants;

public sealed class TenantManagementService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IUserAccessService access,
    UserManager<ApplicationUser> userManager,
    ILicenseService licenseService,
    ILogService logService,
    ILicenseCreateGuard licenseCreateGuard) : ITenantManagementService
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
                IsDeletionRequested = t.IsDeletionRequested,
                DeletionRequestedAt = t.DeletionRequestedAt,
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

        var validationError = TenantComplianceFields.Validate(TenantComplianceFields.FromSaveModel(model));
        if (validationError is not null)
        {
            return validationError;
        }

        var limitCheck = await licenseService.CanCreateTenantAsync(model.LicenseId.Value);
        if (!await licenseCreateGuard.IsAllowedAsync(limitCheck, "Tenant"))
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
            IsActive = model.IsActive,
            LicenseId = model.LicenseId
        };
        ApplySaveModel(tenant, model);

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        await logService.LogAuditAsync(
            action: "TenantCreated",
            description: "Mandant wurde erstellt.",
            entityType: "Tenant",
            entityId: tenant.Id.ToString(),
            entityName: tenant.Name,
            tenantId: tenant.Id,
            licenseId: tenant.LicenseId,
            newValues: SnapshotTenantValues(tenant));

        return TenantOperationResult.Ok();
    }

    public async Task<TenantOperationResult> UpdateTenantAsync(int tenantId, TenantSaveModel model)
    {
        await EnsureSuperuserAsync();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return TenantOperationResult.Fail("Name ist erforderlich.");
        }

        var validationError = TenantComplianceFields.Validate(TenantComplianceFields.FromSaveModel(model));
        if (validationError is not null)
        {
            return validationError;
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
        var oldValues = SnapshotTenantValues(tenant);
        ApplySaveModel(tenant, model);
        tenant.IsActive = model.IsActive;
        tenant.LicenseId = model.LicenseId;
        tenant.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await logService.LogAuditAsync(
            action: "TenantUpdated",
            description: "Mandant wurde geändert.",
            entityType: "Tenant",
            entityId: tenant.Id.ToString(),
            entityName: tenant.Name,
            tenantId: tenant.Id,
            licenseId: tenant.LicenseId,
            oldValues: oldValues,
            newValues: SnapshotTenantValues(tenant, tenant.IsActive, tenant.LicenseId));

        if (previousLicenseId != model.LicenseId)
        {
            await logService.LogAuditAsync(
                action: "TenantLicenseChanged",
                description: "Lizenzzuordnung des Mandanten wurde geändert.",
                entityType: "Tenant",
                entityId: tenant.Id.ToString(),
                entityName: tenant.Name,
                tenantId: tenant.Id,
                licenseId: tenant.LicenseId,
                oldValues: new { LicenseId = previousLicenseId },
                newValues: new { tenant.LicenseId });

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

    private static void ApplySaveModel(Tenant tenant, TenantSaveModel model) =>
        TenantComplianceFields.Apply(tenant, model);

    private static object SnapshotTenantValues(
        Tenant tenant,
        bool? isActive = null,
        Guid? licenseId = null) => new
    {
        tenant.Name,
        tenant.LegalName,
        tenant.Street,
        tenant.HouseNumber,
        tenant.PostalCode,
        tenant.City,
        tenant.Phone,
        tenant.Email,
        tenant.Website,
        tenant.DpoName,
        tenant.DpoStreet,
        tenant.DpoHouseNumber,
        tenant.DpoPostalCode,
        tenant.DpoCity,
        tenant.DpoPhone,
        tenant.DpoEmail,
        IsActive = isActive ?? tenant.IsActive,
        LicenseId = licenseId ?? tenant.LicenseId
    };
}
