namespace Dsms.Web.Services.Legal;

public sealed class LegalPdfResult
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
    public string ContentType { get; init; } = "application/pdf";
}

public sealed class LegalPdfDocumentRequest
{
    public required string ProductName { get; init; }
    public required string ProviderName { get; init; }
    public required string Title { get; init; }
    public required string Version { get; init; }
    public required string EffectiveDate { get; init; }
    public required string Markdown { get; init; }
}

public static class LegalPdfFileNames
{
    private const string Prefix = "Datenschutz-Cloud";

    public static string ForDocument(string documentKey, string version) =>
        Sanitize($"{Prefix}_{GetDocumentSuffix(documentKey)}_{version}.pdf");

    public static string ForAvvPackage(string version) =>
        Sanitize($"{Prefix}_AVV_TOM_Unterauftragnehmerliste_{version}.pdf");

    private static string GetDocumentSuffix(string documentKey) => documentKey.ToLowerInvariant() switch
    {
        LegalDocumentKeys.Impressum => "Impressum",
        LegalDocumentKeys.Datenschutzerklaerung => "Datenschutzerklaerung",
        LegalDocumentKeys.Agb => "AGB",
        LegalDocumentKeys.Avv => "AVV",
        LegalDocumentKeys.Tom => "TOM",
        LegalDocumentKeys.Unterauftragnehmerliste => "Unterauftragnehmerliste",
        _ => documentKey
    };

    private static string Sanitize(string fileName) =>
        string.Concat(fileName.Select(ch => char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-' ? ch : '_'));
}
