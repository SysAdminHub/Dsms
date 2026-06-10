namespace Dsms.Web.Services.Tenants;

/// <summary>
/// Mandanten-Stammdaten für VVT (Art. 30 DSGVO) – lesen und bearbeiten im Kontext des aktuellen Mandanten.
/// </summary>
public interface ITenantComplianceInfoService
{
    /// <summary>Lädt die DSGVO-Stammdaten des aktuellen Mandanten (Superuser: gewählter Mandant; Admin: eigener Mandant).</summary>
    Task<TenantComplianceInfoDto?> GetForCurrentTenantAsync();

    /// <summary>Aktualisiert nur DSGVO-Stammdaten; TenantId wird serverseitig ermittelt.</summary>
    Task<TenantOperationResult> UpdateForCurrentTenantAsync(TenantComplianceInfoSaveModel model);
}
