using Dsms.Web.Data;
using Dsms.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

/// <summary>
/// HTTP-Endpunkte für Download und Inline-Anzeige von Nachweisdokumenten (mandantengebunden via EF-Filter).
/// </summary>
public static class DocumentFileEndpoints
{
    public static void MapDocumentFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/documents").RequireAuthorization();

        group.MapGet("/{id:int}/download", (int id, ApplicationDbContext db, DocumentStorageService storage, ILoggerFactory loggerFactory) =>
            ServeFileAsync(id, db, storage, inline: false, loggerFactory));

        group.MapGet("/{id:int}/view", (int id, ApplicationDbContext db, DocumentStorageService storage, ILoggerFactory loggerFactory) =>
            ServeFileAsync(id, db, storage, inline: true, loggerFactory));
    }

    private static async Task<IResult> ServeFileAsync(
        int id,
        ApplicationDbContext db,
        DocumentStorageService storage,
        bool inline,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("DocumentFileEndpoints");

        try
        {
            var doc = await db.EvidenceDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doc is null)
                return Results.NotFound();

            if (inline && !DocumentUploadValidation.IsViewableInBrowser(doc.ContentType, doc.FileName))
                return Results.NotFound();

            var fullPath = storage.GetFullPath(doc.StoragePath);
            if (!File.Exists(fullPath))
            {
                logger.LogWarning("Datei fehlt auf dem Datensystem: {Path} (Dokument {DocumentId})", fullPath, id);
                return Results.NotFound();
            }

            var contentType = ResolveContentType(doc.ContentType, doc.FileName);

            return inline
                ? Results.File(fullPath, contentType)
                : Results.File(fullPath, contentType, doc.FileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Bereitstellen von Dokument {DocumentId}", id);
            return Results.Problem("Datei konnte nicht geladen werden.");
        }
    }

    private static string ResolveContentType(string contentType, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(contentType) && !contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
            return contentType;

        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }
}
