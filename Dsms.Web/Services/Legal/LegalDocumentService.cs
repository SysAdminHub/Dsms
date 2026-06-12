using System.Text.Json;

namespace Dsms.Web.Services.Legal;

public sealed class LegalDocumentService(
    IWebHostEnvironment environment,
    ILegalPlaceholderService placeholderService) : ILegalDocumentService
{
    private const string MetadataFileName = "legal-documents.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<LegalDocumentsMetadata?> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        var metadataPath = Path.Combine(GetLegalRootPath(), MetadataFileName);
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(metadataPath);
        var metadata = await JsonSerializer.DeserializeAsync<LegalDocumentsMetadataDto>(stream, JsonOptions, cancellationToken);
        if (metadata is null)
        {
            return null;
        }

        return new LegalDocumentsMetadata
        {
            Version = metadata.Version ?? string.Empty,
            EffectiveDate = metadata.EffectiveDate ?? metadata.Version ?? string.Empty,
            Documents = metadata.Documents ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
    }

    public async Task<LegalDocumentContent> GetDocumentAsync(
        string documentKey,
        CancellationToken cancellationToken = default)
    {
        var metadata = await GetMetadataAsync(cancellationToken);
        if (metadata is null)
        {
            return LegalDocumentContent.NotFound(documentKey, "Die Legal-Konfiguration wurde nicht gefunden.");
        }

        var replacements = await placeholderService.BuildReplacementsAsync(metadata.Version, cancellationToken);
        return await GetDocumentWithReplacementsAsync(documentKey, replacements, cancellationToken);
    }

    public async Task<LegalDocumentContent> GetDocumentWithReplacementsAsync(
        string documentKey,
        IReadOnlyDictionary<string, string> replacements,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentKey))
        {
            return LegalDocumentContent.NotFound(string.Empty, "Kein Dokument angegeben.");
        }

        var normalizedKey = documentKey.Trim();
        var metadata = await GetMetadataAsync(cancellationToken);
        if (metadata is null)
        {
            return LegalDocumentContent.NotFound(normalizedKey, "Die Legal-Konfiguration wurde nicht gefunden.");
        }

        if (!metadata.Documents.TryGetValue(normalizedKey, out var relativePath) || string.IsNullOrWhiteSpace(relativePath))
        {
            return LegalDocumentContent.NotFound(normalizedKey, "Das angeforderte Dokument ist nicht konfiguriert.");
        }

        if (!TryResolveDocumentPath(relativePath, out var absolutePath, out var safeRelativePath))
        {
            return LegalDocumentContent.NotFound(normalizedKey, "Der Dokumentpfad ist ungültig.");
        }

        if (!File.Exists(absolutePath))
        {
            return LegalDocumentContent.NotFound(normalizedKey, "Die Dokumentdatei wurde nicht gefunden.");
        }

        var markdown = await File.ReadAllTextAsync(absolutePath, cancellationToken);
        var version = string.IsNullOrWhiteSpace(metadata.EffectiveDate)
            ? metadata.Version
            : metadata.EffectiveDate;

        markdown = placeholderService.ApplyPlaceholders(markdown, replacements);
        var html = LegalMarkdownRenderer.ToHtml(markdown);

        return new LegalDocumentContent
        {
            DocumentKey = normalizedKey,
            Title = LegalDocumentKeys.GetTitle(normalizedKey),
            Markdown = markdown,
            Html = html,
            Version = metadata.Version,
            EffectiveDate = version,
            RelativeFilePath = safeRelativePath,
            IsFound = true
        };
    }

    private string GetLegalRootPath() =>
        Path.Combine(environment.ContentRootPath, "Legal");

    private bool TryResolveDocumentPath(string relativePath, out string absolutePath, out string safeRelativePath)
    {
        absolutePath = string.Empty;
        safeRelativePath = string.Empty;

        var legalRoot = Path.GetFullPath(GetLegalRootPath());
        var normalizedRelative = relativePath
            .Replace('\\', '/')
            .TrimStart('/');

        if (normalizedRelative.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        var combined = Path.GetFullPath(Path.Combine(legalRoot, normalizedRelative.Replace('/', Path.DirectorySeparatorChar)));
        if (!combined.StartsWith(legalRoot, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        absolutePath = combined;
        safeRelativePath = normalizedRelative;
        return true;
    }

    private sealed class LegalDocumentsMetadataDto
    {
        public string? Version { get; set; }
        public string? EffectiveDate { get; set; }
        public Dictionary<string, string>? Documents { get; set; }
    }
}
