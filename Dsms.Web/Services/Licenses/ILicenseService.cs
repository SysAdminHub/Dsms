namespace Dsms.Web.Services.Licenses;

public interface ILicenseService
{
    Task<IReadOnlyList<LicenseListItemDto>> GetAllLicensesWithUsageAsync(
        string? search = null,
        string? statusFilter = null,
        string sortBy = "CreatedAt",
        bool sortDescending = true);

    Task<LicenseDetailsDto?> GetLicenseDetailsAsync(Guid licenseId);
    Task<LicenseUsageDto> GetUsageAsync(Guid licenseId);
    Task<Guid> CreateLicenseAsync(LicenseEditDto dto);
    Task<bool> UpdateLicenseAsync(Guid licenseId, LicenseEditDto dto);
    Task<IReadOnlyList<LicenseOptionDto>> GetActiveLicenseOptionsAsync();

    Task<LicenseLimitCheckResult> CanCreateTenantAsync(Guid licenseId);
    Task<LicenseLimitCheckResult> CanCreateAdminAsync(Guid licenseId);
    Task<LicenseLimitCheckResult> CanCreateUserAsync(int tenantId);
    Task<LicenseLimitCheckResult> CanCreateAuditorAsync(int tenantId);
    Task<LicenseLimitCheckResult> CanCreateCustomAuditTemplateAsync(int tenantId);
    Task<LicenseLimitCheckResult> CanCreateActiveAuditAsync(int tenantId);
    Task<LicenseLimitCheckResult> CanCreateProcessingActivityAsync(int tenantId);
    Task<LicenseLimitCheckResult> CanCreateDpiaAsync(int tenantId);
    Task<LicenseLimitCheckResult> CanCreateTomAsync(int tenantId);
    Task<LicenseLimitCheckResult> CanCreateProcessorAsync(int tenantId);
    Task<LicenseLimitCheckResult> CanCreateMeasureAsync(int tenantId);
    Task<LicenseLimitCheckResult> CanUseStorageAsync(Guid licenseId, long additionalBytes);
    Task<LicenseLimitCheckResult> CanSendEmailReminderAsync(Guid licenseId);

    /// <summary>
    /// Prüft, ob eine Lizenz grundsätzlich neue Objekte erstellen darf (Status Active, nicht abgelaufen).
    /// </summary>
    Task<LicenseLimitCheckResult> CheckLicenseUsableForCreationAsync(Guid licenseId);

    /// <summary>
    /// Read-only Lizenzübersicht für den eingeloggten Kunden-Admin (Lizenz aus ApplicationUser.LicenseId).
    /// </summary>
    Task<AdminLicenseOverviewResult> GetCurrentAdminLicenseOverviewAsync();
}
