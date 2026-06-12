using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Legal;

public interface ILegalPdfService
{
    Task<LegalPdfResult?> GenerateDocumentPdfAsync(
        string documentKey,
        IReadOnlyDictionary<string, string>? replacements = null,
        CancellationToken cancellationToken = default);

    Task<LegalPdfResult?> GenerateDocumentPdfForTenantAsync(
        string documentKey,
        int tenantId,
        CancellationToken cancellationToken = default);

    Task<LegalPdfResult?> GenerateAvvPackagePdfForTenantAsync(
        int tenantId,
        CancellationToken cancellationToken = default);
}

public sealed class LegalPdfService(
    ILegalDocumentService documentService,
    ILegalPlaceholderService placeholderService,
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ILogger<LegalPdfService> logger) : ILegalPdfService
{
    private const string ProductName = "Datenschutz-Cloud";
    private const string ProviderName = "Stefan Keller – The SysAdminHub";

    public async Task<LegalPdfResult?> GenerateDocumentPdfAsync(
        string documentKey,
        IReadOnlyDictionary<string, string>? replacements = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await documentService.GetMetadataAsync(cancellationToken);
            if (metadata is null)
            {
                return null;
            }

            replacements ??= placeholderService.BuildAnonymousReplacements(metadata.Version);
            var content = await documentService.GetDocumentWithReplacementsAsync(
                documentKey,
                replacements,
                cancellationToken);

            if (!content.IsFound)
            {
                return null;
            }

            var pdfBytes = LegalPdfMarkdownComposer.Compose(CreatePdfRequest(content));
            return new LegalPdfResult
            {
                Content = pdfBytes,
                FileName = LegalPdfFileNames.ForDocument(documentKey, metadata.Version)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PDF-Generierung fehlgeschlagen für Dokument {DocumentKey}", documentKey);
            return null;
        }
    }

    public async Task<LegalPdfResult?> GenerateDocumentPdfForTenantAsync(
        string documentKey,
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await LoadTenantAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        var metadata = await documentService.GetMetadataAsync(cancellationToken);
        if (metadata is null)
        {
            return null;
        }

        var replacements = placeholderService.BuildReplacementsForTenant(tenant, metadata.Version);
        return await GenerateDocumentPdfAsync(documentKey, replacements, cancellationToken);
    }

    public async Task<LegalPdfResult?> GenerateAvvPackagePdfForTenantAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenant = await LoadTenantAsync(tenantId, cancellationToken);
            if (tenant is null)
            {
                return null;
            }

            var metadata = await documentService.GetMetadataAsync(cancellationToken);
            if (metadata is null)
            {
                return null;
            }

            var replacements = placeholderService.BuildReplacementsForTenant(tenant, metadata.Version);
            var sections = new List<LegalPdfDocumentRequest>();

            foreach (var documentKey in new[]
                     {
                         LegalDocumentKeys.Avv,
                         LegalDocumentKeys.Tom,
                         LegalDocumentKeys.Unterauftragnehmerliste
                     })
            {
                var content = await documentService.GetDocumentWithReplacementsAsync(
                    documentKey,
                    replacements,
                    cancellationToken);

                if (!content.IsFound)
                {
                    logger.LogWarning("AVV-Paket: Dokument {DocumentKey} nicht gefunden.", documentKey);
                    return null;
                }

                sections.Add(CreatePdfRequest(content));
            }

            var pdfBytes = LegalPdfMarkdownComposer.Compose(sections);
            return new LegalPdfResult
            {
                Content = pdfBytes,
                FileName = LegalPdfFileNames.ForAvvPackage(metadata.Version)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AVV-Paket-PDF fehlgeschlagen für Tenant {TenantId}", tenantId);
            return null;
        }
    }

    private static LegalPdfDocumentRequest CreatePdfRequest(LegalDocumentContent content) => new()
    {
        ProductName = ProductName,
        ProviderName = ProviderName,
        Title = content.Title,
        Version = content.Version,
        EffectiveDate = content.EffectiveDate,
        Markdown = content.Markdown
    };

    private async Task<Tenant?> LoadTenantAsync(int tenantId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
    }
}
