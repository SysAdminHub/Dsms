using Dsms.Web.Services.Support;

namespace Dsms.Web.Services;

/// <summary>HTTP-Endpunkte für Supportmodus (Session vor Response-Start).</summary>
public static class SupportAccessEndpoints
{
    public const string EnterRoute = "/platform/support-access/enter";
    public const string ExitRoute = "/platform/support-access/exit";

    public static string BuildEnterUrl(int grantId, string? returnUrl = null)
    {
        var path = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        return $"{EnterRoute}/{grantId}?returnUrl={Uri.EscapeDataString(path)}";
    }

    public static string BuildExitUrl(string? returnUrl = null)
    {
        var path = string.IsNullOrWhiteSpace(returnUrl) ? "/platform/support-access" : returnUrl;
        return $"{ExitRoute}?returnUrl={Uri.EscapeDataString(path)}";
    }

    public static async Task<IResult> EnterSupportModeAsync(
        int grantId,
        ISupportAccessService supportAccess,
        HttpContext context,
        string? returnUrl)
    {
        var result = await supportAccess.ActivateSupportModeAsync(grantId);
        if (!result.Succeeded)
        {
            return Results.Redirect("/platform/support-access?error=invalid");
        }

        return Results.Redirect(TenantSwitchEndpoints.ResolveReturnUrl(context, returnUrl));
    }

    public static async Task<IResult> ExitSupportModeAsync(
        ISupportAccessService supportAccess,
        HttpContext context,
        string? returnUrl)
    {
        await supportAccess.ExitSupportModeAsync();
        var target = string.IsNullOrWhiteSpace(returnUrl) ? "/platform/support-access" : returnUrl;
        if (!TenantSwitchEndpoints.IsLocalReturnUrl(target))
        {
            target = "/platform/support-access";
        }

        return Results.Redirect(target);
    }
}
