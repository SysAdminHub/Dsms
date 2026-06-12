using Dsms.Web.Services;
using Microsoft.AspNetCore.Identity;

namespace Dsms.Web.Services.Legal;

public static class LegalDocumentEndpoints
{
    public static void MapLegalDocumentEndpoints(this WebApplication app)
    {
        app.MapGet("/legal/{route}/pdf", DownloadLegalPdfAsync);
    }

    private static async Task<IResult> DownloadLegalPdfAsync(
        string route,
        HttpContext httpContext,
        ILegalPdfService legalPdfService,
        ILegalPlaceholderService placeholderService,
        ILegalDocumentService legalDocumentService,
        ITenantService tenantService,
        IUserAccessService userAccess,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("LegalDocumentEndpoints");

        if (!LegalDocumentKeys.TryResolveKeyFromRoute(route, out var documentKey))
        {
            return Results.NotFound("Das Dokument wurde nicht gefunden.");
        }

        try
        {
            LegalPdfResult? pdf;

            if (string.Equals(documentKey, LegalDocumentKeys.Avv, StringComparison.OrdinalIgnoreCase)
                && httpContext.User.Identity?.IsAuthenticated == true)
            {
                var tenant = await tenantService.GetCurrentTenantAsync();
                if (tenant is not null && await userAccess.CanAccessTenantAsync(tenant.Id))
                {
                    pdf = await legalPdfService.GenerateDocumentPdfForTenantAsync(documentKey, tenant.Id);
                }
                else
                {
                    var metadata = await legalDocumentService.GetMetadataAsync();
                    var replacements = metadata is null
                        ? placeholderService.BuildAnonymousReplacements(string.Empty)
                        : placeholderService.BuildAnonymousReplacements(metadata.Version);
                    pdf = await legalPdfService.GenerateDocumentPdfAsync(documentKey, replacements);
                }
            }
            else
            {
                pdf = await legalPdfService.GenerateDocumentPdfAsync(documentKey);
            }

            if (pdf is null)
            {
                return Results.Problem(
                    title: "PDF konnte nicht erstellt werden",
                    detail: "Das PDF-Dokument konnte nicht generiert werden. Bitte versuchen Sie es später erneut.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            return Results.File(pdf.Content, pdf.ContentType, pdf.FileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Legal-PDF-Download fehlgeschlagen für Route {Route}", route);
            return Results.Problem(
                title: "PDF konnte nicht erstellt werden",
                detail: "Das PDF-Dokument konnte nicht generiert werden. Bitte versuchen Sie es später erneut.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
