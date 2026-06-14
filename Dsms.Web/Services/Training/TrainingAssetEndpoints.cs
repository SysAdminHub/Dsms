using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Services.Training;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

/// <summary>
/// HTTP-Endpunkte für mandantensichere Auslieferung von Schulungsassets.
/// Route: /training-assets/{trainingTemplateId}/{assetKey}
/// </summary>
public static class TrainingAssetEndpoints
{
    public static void MapTrainingAssetEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/training-assets").RequireAuthorization();

        group.MapGet("/{trainingTemplateId:int}/{assetKey}", ServeAssetAsync);
    }

    private static async Task<IResult> ServeAssetAsync(
        int trainingTemplateId,
        string assetKey,
        ApplicationDbContext db,
        TrainingAssetStorageService storage,
        TrainingTemplateService templateService,
        IUserAccessService access,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("TrainingAssetEndpoints");

        try
        {
            if (!TrainingAssetUploadValidation.IsValidAssetKey(assetKey))
                return Results.NotFound();

            var tenantId = await access.GetCurrentTenantIdAsync();
            var asset = await db.TrainingTemplateAssets
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.TrainingTemplateId == trainingTemplateId
                    && a.AssetKey == TrainingAssetUploadValidation.NormalizeAssetKey(assetKey)
                    && a.IsActive, ct);

            if (asset is null)
                return Results.NotFound();

            var template = await db.TrainingTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == trainingTemplateId, ct);

            if (template is null || !await templateService.CanViewAsync(template, ct))
                return Results.NotFound();

            var fullPath = storage.GetFullPath(asset.StoragePath);
            if (!File.Exists(fullPath))
            {
                logger.LogWarning("Schulungsasset fehlt auf dem Datensystem (Template {TemplateId}, Key {AssetKey})",
                    trainingTemplateId, assetKey);
                return Results.NotFound();
            }

            return Results.File(fullPath, asset.ContentType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Bereitstellen von Schulungsasset {AssetKey} (Template {TemplateId})",
                assetKey, trainingTemplateId);
            return Results.Problem("Datei konnte nicht geladen werden.");
        }
    }
}
