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
}
