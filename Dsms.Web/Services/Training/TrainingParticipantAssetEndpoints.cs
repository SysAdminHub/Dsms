using Dsms.Web.Services.Training;

namespace Dsms.Web.Services;

/// <summary>
/// Geschützte Asset-Auslieferung für das öffentliche Teilnehmerportal.
/// Route: /training-portal-assets/{assetKey}
/// </summary>
public static class TrainingParticipantAssetEndpoints
{
    public static void MapTrainingParticipantAssetEndpoints(this WebApplication app)
    {
        app.MapGet("/training-portal-assets/{assetKey}", ServeAssetAsync);
    }

    private static async Task<IResult> ServeAssetAsync(
        string assetKey,
        TrainingParticipantPortalService portalService,
        TrainingAssetStorageService storage,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("TrainingParticipantAssetEndpoints");

        try
        {
            if (!TrainingAssetUploadValidation.IsValidAssetKey(assetKey))
                return Results.NotFound();

            var asset = await portalService.GetPortalAssetAsync(assetKey, ct);
            if (asset is null)
                return Results.NotFound();

            var fullPath = storage.GetFullPath(asset.StoragePath);
            if (!File.Exists(fullPath))
            {
                logger.LogWarning("Teilnehmerportal-Asset fehlt auf dem Datensystem (Key {AssetKey})", assetKey);
                return Results.NotFound();
            }

            return Results.File(fullPath, asset.ContentType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Bereitstellen von Teilnehmerportal-Asset {AssetKey}", assetKey);
            return Results.Problem("Datei konnte nicht geladen werden.");
        }
    }
}
