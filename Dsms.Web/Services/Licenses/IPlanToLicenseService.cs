namespace Dsms.Web.Services.Licenses;

public interface IPlanToLicenseService
{
    Task<LicenseFromPlanPreviewDto> PreviewLicenseFromPlanAsync(
        Guid planId,
        CreateLicenseFromPlanDto? input = null);

    Task<Guid> CreateLicenseFromPlanAsync(CreateLicenseFromPlanDto dto);
}
