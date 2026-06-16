using Dsms.Web.Services.Training;
using Microsoft.Extensions.Options;

namespace Dsms.Web.Services;

/// <summary>
/// Download der Teilnahmebescheinigung für eingeloggte Schulungsteilnehmer (Cookie-Session).
/// Route: /schulung/teilnahme/bescheinigung/download
/// </summary>
public static class TrainingParticipantCertificateEndpoints
{
    public static void MapTrainingParticipantCertificateEndpoints(this WebApplication app)
    {
        app.MapGet("/schulung/teilnahme/bescheinigung/download", DownloadAsync);
    }

    private static async Task<IResult> DownloadAsync(
        TrainingParticipantSessionService sessionService,
        TrainingParticipantPortalService portalService,
        TrainingCertificateService certificateService,
        DocumentStorageService storage,
        IOptions<TrainingAccessOptions> options,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("TrainingParticipantCertificateEndpoints");

        try
        {
            var session = sessionService.GetSession();
            if (session is null)
                return Results.Redirect(options.Value.AccessPath);

            var context = await portalService.GetContextAsync(ct);
            if (context is null || !context.IsCompleted)
                return Results.NotFound();

            var certificate = await certificateService.EnsureCertificateForAssignmentAsync(
                session.AssignmentId,
                session.TenantId,
                ct);

            if (certificate.DocumentId is not int documentId)
                return Results.NotFound();

            var document = await certificateService.GetCertificateDocumentForParticipantAsync(
                session.AssignmentId,
                session.TenantId,
                ct);

            if (document is null || document.Id != documentId)
                return Results.NotFound();

            var fullPath = storage.GetFullPath(document.StoragePath);
            if (!File.Exists(fullPath))
            {
                logger.LogWarning(
                    "Teilnahmebescheinigung fehlt auf dem Datensystem (Document {DocumentId}, Assignment {AssignmentId})",
                    document.Id,
                    session.AssignmentId);
                return Results.NotFound();
            }

            return Results.File(fullPath, document.ContentType, document.FileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Download der Teilnahmebescheinigung");
            return Results.Problem("Die Teilnahmebescheinigung konnte nicht geladen werden.");
        }
    }
}
