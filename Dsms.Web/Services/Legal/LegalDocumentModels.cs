namespace Dsms.Web.Services.Legal;

public sealed class LegalDocumentsMetadata
{
    public string Version { get; init; } = string.Empty;
    public string EffectiveDate { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Documents { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class LegalDocumentContent
{
    public string DocumentKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Markdown { get; init; } = string.Empty;
    public string Html { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string EffectiveDate { get; init; } = string.Empty;
    public string RelativeFilePath { get; init; } = string.Empty;
    public bool IsFound { get; init; }
    public string? ErrorMessage { get; init; }

    public static LegalDocumentContent NotFound(string documentKey, string message) => new()
    {
        DocumentKey = documentKey,
        Title = LegalDocumentKeys.GetTitle(documentKey),
        IsFound = false,
        ErrorMessage = message
    };
}
