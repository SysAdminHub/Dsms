using Dsms.Web.Data;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Tenants;

public sealed class TenantComplianceInfoService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IUserAccessService access,
    ITenantService tenantService,
    ILogService logService) : ITenantComplianceInfoService
{
    public async Task<TenantComplianceInfoDto?> GetForCurrentTenantAsync()
    {
        if (!await access.CanManageTenantDataAsync())
        {
            return null;
        }

        var tenant = await tenantService.GetCurrentTenantAsync();
        if (tenant is null || !await access.CanAccessTenantAsync(tenant.Id))
        {
            return null;
        }

        return TenantComplianceFields.ToDto(tenant);
    }

    public async Task<TenantOperationResult> UpdateForCurrentTenantAsync(TenantComplianceInfoSaveModel model)
    {
        if (!await access.CanManageTenantDataAsync())
        {
            return TenantOperationResult.Fail("Keine Berechtigung zum Bearbeiten der Mandanten-Stammdaten.");
        }

        var tenantId = await access.GetCurrentTenantIdAsync();
        if (!tenantId.HasValue)
        {
            return TenantOperationResult.Fail("Bitte wählen Sie zuerst einen Mandanten aus.");
        }

        if (!await access.CanAccessTenantAsync(tenantId.Value))
        {
            return TenantOperationResult.Fail("Kein Zugriff auf diesen Mandanten.");
        }

        var validationError = TenantComplianceFields.Validate(model);
        if (validationError is not null)
        {
            return validationError;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId.Value);
        if (tenant is null)
        {
            return TenantOperationResult.Fail("Mandant nicht gefunden.");
        }

        var oldValues = TenantComplianceFields.Snapshot(tenant);
        TenantComplianceFields.Apply(tenant, model);
        tenant.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await logService.LogAuditAsync(
            action: "TenantComplianceInfoUpdated",
            description: "Mandanten-Stammdaten aktualisiert.",
            entityType: "Tenant",
            entityId: tenant.Id.ToString(),
            entityName: tenant.Name,
            tenantId: tenant.Id,
            licenseId: tenant.LicenseId,
            oldValues: oldValues,
            newValues: TenantComplianceFields.Snapshot(tenant));

        return TenantOperationResult.Ok();
    }
}
