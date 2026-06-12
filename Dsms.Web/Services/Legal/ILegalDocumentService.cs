namespace Dsms.Web.Services.Legal;

public interface ILegalDocumentService
{
    Task<LegalDocumentsMetadata?> GetMetadataAsync(CancellationToken cancellationToken = default);

    Task<LegalDocumentContent> GetDocumentAsync(
        string documentKey,
        CancellationToken cancellationToken = default);

    Task<LegalDocumentContent> GetDocumentWithReplacementsAsync(
        string documentKey,
        IReadOnlyDictionary<string, string> replacements,
        CancellationToken cancellationToken = default);
}
