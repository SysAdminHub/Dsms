using Dsms.Web.Domain.Entities;

namespace Dsms.Web.Services.Tenants;

public interface ITenantManagementService
{
    Task<IReadOnlyList<TenantListItemDto>> ListTenantsAsync();
    Task<Tenant?> GetTenantForEditAsync(int tenantId);
    Task<IReadOnlyList<Tenant>> GetTenantsByLicenseAsync(Guid licenseId);
    Task<TenantOperationResult> CreateTenantAsync(TenantSaveModel model);
    Task<TenantOperationResult> UpdateTenantAsync(int tenantId, TenantSaveModel model);
}
