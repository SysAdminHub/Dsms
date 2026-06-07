using Dsms.Web.Services.TenantExport;
using Microsoft.AspNetCore.Antiforgery;

namespace Dsms.Web.Services;

public static class TenantDataEndpoints
{
    public static IEndpointRouteBuilder MapTenantDataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/tenant-daten/export", async (
            HttpContext context,
            IUserAccessService access,
            ICurrentUserContext currentUser,
            ITenantExportService exportService,
            IAntiforgery antiforgery,
            ILoggerFactory loggerFactory) =>
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch
            {
                return Results.BadRequest("Ungültiges Antiforgery-Token.");
            }

            if (!await access.CanManageTenantDataAsync())
            {
                return Results.Forbid();
            }

            var tenantId = await access.GetCurrentTenantIdAsync();
            if (!tenantId.HasValue)
            {
                return Results.BadRequest("Kein Mandant ausgewählt.");
            }

            if (!await access.CanAccessTenantAsync(tenantId.Value))
            {
                return Results.Forbid();
            }

            var userId = await currentUser.GetUserIdAsync();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var logger = loggerFactory.CreateLogger("TenantDataExport");
            logger.LogInformation("Tenant-Export angefordert: TenantId={TenantId}, UserId={UserId}", tenantId, userId);

            var result = await exportService.CreateExportAsync(tenantId.Value, userId);
            if (result is null)
            {
                return Results.Problem("Export konnte nicht erstellt werden.", statusCode: 500);
            }

            return TypedResults.File(result.Content, result.ContentType, result.FileName);
        }).RequireAuthorization();

        return endpoints;
    }
}
