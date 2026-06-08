namespace Dsms.Web.Services.TenantExport;

public interface ITenantExportService
{
    /// <summary>
    /// Erstellt einen vollständigen ZIP-Export für den angegebenen Mandanten.
    /// Der Aufrufer muss Berechtigung und Tenant-Kontext vorher prüfen.
    /// </summary>
    Task<TenantExportResult?> CreateExportAsync(int tenantId, string userId, CancellationToken ct = default);
}
