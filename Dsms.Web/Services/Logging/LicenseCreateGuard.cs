using Dsms.Web.Services.Licenses;

namespace Dsms.Web.Services.Logging;

public sealed class LicenseCreateGuard(ILogService logService) : ILicenseCreateGuard
{
    public async Task<bool> IsAllowedAsync(LicenseLimitCheckResult check, string? entityType = null)
    {
        if (check.IsAllowed)
        {
            return true;
        }

        await logService.LogBlockedCreationAsync(check, entityType);
        return false;
    }
}
