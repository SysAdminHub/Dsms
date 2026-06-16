using Microsoft.Extensions.Logging;namespace Dsms.Web.Services;

/// <summary>
/// Initialisiert den Mandantenkontext einmal pro HTTP-Request, bevor Blazor-Komponenten rendern.
/// Fehler werden geloggt – der Request wird nicht abgebrochen.
/// </summary>
public class TenantInitializationMiddleware(
    RequestDelegate next,
    ILogger<TenantInitializationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                await tenantService.InitializeContextAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Tenant-Middleware-Init fehlgeschlagen");
            }
        }
        try
        {
            await next(context);
        }
        catch(Exception ex)
        {
            logger.LogWarning(ex.Message);
        }
    }
}

public static class TenantInitializationMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantInitialization(this IApplicationBuilder app) =>
        app.UseMiddleware<TenantInitializationMiddleware>();
}
