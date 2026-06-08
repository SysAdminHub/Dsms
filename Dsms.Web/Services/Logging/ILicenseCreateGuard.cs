using Dsms.Web.Services.Licenses;

namespace Dsms.Web.Services.Logging;

/// <summary>
/// Prüft Lizenzlimits und protokolliert blockierte Erstellungen zentral.
/// </summary>
public interface ILicenseCreateGuard
{
    Task<bool> IsAllowedAsync(LicenseLimitCheckResult check, string? entityType = null);
}
