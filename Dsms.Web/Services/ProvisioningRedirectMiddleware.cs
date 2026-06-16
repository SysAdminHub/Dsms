using Dsms.Web.Configuration;
using Microsoft.Extensions.Options;

namespace Dsms.Web.Services;

/// <summary>
/// Leitet alte Signup- und Plattform-Provisioning-Routen der Fachanwendung zur Provisioning-App um.
/// </summary>
public sealed class ProvisioningRedirectMiddleware(RequestDelegate next)
{
    private static readonly string[] ProvisioningPlatformPrefixes =
    [
        "platform/licenses",
        "platform/plans",
        "platform/discount-codes",
        "platform/provisioning",
        "platform/signups",
    ];

    public async Task InvokeAsync(HttpContext context, IOptions<AppUrlOptions> urlOptions)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            && TryGetRedirectTarget(context, urlOptions.Value, out var target))
        {
            context.Response.Redirect(target);
            return;
        }

        await next(context);
    }

    internal static bool TryGetRedirectTarget(HttpContext context, AppUrlOptions options, out string target)
    {
        target = "";

        var path = context.Request.Path.Value?.TrimStart('/') ?? "";
        if (path.Length == 0)
        {
            return false;
        }

        var normalized = path.ToLowerInvariant();
        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null;

        if (normalized == "signup")
        {
            target = options.ResolveSignupUrl();
            return true;
        }

        if (normalized.StartsWith("signup/", StringComparison.Ordinal))
        {
            target = options.BuildProvisioningUrl(path, query);
            return true;
        }

        foreach (var prefix in ProvisioningPlatformPrefixes)
        {
            if (normalized == prefix || normalized.StartsWith(prefix + "/", StringComparison.Ordinal))
            {
                target = options.BuildProvisioningUrl(path, query);
                return true;
            }
        }

        return false;
    }
}

public static class ProvisioningRedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseProvisioningRedirects(this IApplicationBuilder app) =>
        app.UseMiddleware<ProvisioningRedirectMiddleware>();
}
