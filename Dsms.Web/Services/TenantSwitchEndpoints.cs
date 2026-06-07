namespace Dsms.Web.Services;

/// <summary>
/// HTTP-Endpunkte für Mandantenwechsel. Session ist nur vor Response-Start beschreibbar –
/// daher kein direkter Wechsel aus interaktiven Blazor-Komponenten.
/// </summary>
public static class TenantSwitchEndpoints
{
    public const string SwitchRoute = "/tenant/switch";

    public static string BuildSwitchUrl(int tenantId, string? returnUrl = null)
    {
        var path = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        return $"{SwitchRoute}/{tenantId}?returnUrl={Uri.EscapeDataString(path)}";
    }

    public static bool IsLocalReturnUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return url.StartsWith('/')
            && !url.StartsWith("//", StringComparison.Ordinal)
            && !url.StartsWith("/\\", StringComparison.Ordinal);
    }

    public static string ResolveReturnUrl(HttpContext context, string? returnUrl)
    {
        if (IsLocalReturnUrl(returnUrl))
        {
            return returnUrl!;
        }

        if (context.Request.Headers.Referer.FirstOrDefault() is { } referer
            && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
        {
            var localPath = refererUri.PathAndQuery;
            if (IsLocalReturnUrl(localPath))
            {
                return localPath;
            }
        }

        return "/";
    }

    public static async Task<IResult> SwitchTenantAsync(
        int tenantId,
        ITenantService tenantService,
        HttpContext context,
        string? returnUrl)
    {
        var result = await tenantService.SwitchTenantAsync(tenantId);
        if (!result.Succeeded)
        {
            return Results.Redirect("/select-tenant");
        }

        return Results.Redirect(ResolveReturnUrl(context, returnUrl));
    }
}
