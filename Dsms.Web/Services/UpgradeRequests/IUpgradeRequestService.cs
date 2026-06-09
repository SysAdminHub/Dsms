using Dsms.Web.Services.Licenses;

namespace Dsms.Web.Services.UpgradeRequests;

public interface IUpgradeRequestService
{
    Task<IReadOnlyList<UpgradeTargetPlanDto>> GetUpgradeTargetPlansAsync(string currentPlanName);

    Task<UpgradeRequestResult> SubmitUpgradeRequestAsync(
        LicenseDetailsDto license,
        UpgradeRequestInput input);
}
