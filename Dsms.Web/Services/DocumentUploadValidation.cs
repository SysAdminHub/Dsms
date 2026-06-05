namespace Dsms.Web.Services;

/// <summary>
/// Client- und serverseitige Validierung für Nachweis-Uploads (Dateityp, Größe, MIME).
/// </summary>
public static class DocumentUploadValidation
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;
    public const string AcceptAttribute = ".pdf,.docx,.xlsx,.jpg,.jpeg,.png";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".xlsx", ".jpg", ".jpeg", ".png"
    };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "image/jpeg",
        "image/png"
    };

    public static bool IsViewableInBrowser(string contentType, string fileName) =>
        contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    public static DocumentUploadValidationResult Validate(string fileName, string contentType, long fileSize)
    {
        if (fileSize <= 0)
            return DocumentUploadValidationResult.Fail("Keine Datei ausgewählt.");

        if (fileSize > MaxFileSizeBytes)
            return DocumentUploadValidationResult.Fail($"Datei ist zu groß (max. {MaxFileSizeBytes / (1024 * 1024)} MB).");

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            return DocumentUploadValidationResult.Fail("Dateityp nicht erlaubt. Erlaubt: PDF, DOCX, XLSX, JPG, PNG.");

        if (!IsMimeTypeAllowed(contentType, extension))
            return DocumentUploadValidationResult.Fail("Dateityp (MIME) nicht erlaubt.");

        return DocumentUploadValidationResult.Ok();
    }

    private static bool IsMimeTypeAllowed(string contentType, string extension)
    {
        if (string.IsNullOrWhiteSpace(contentType) || contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
            return AllowedExtensions.Contains(extension);

        return AllowedMimeTypes.Contains(contentType);
    }
}

public readonly record struct DocumentUploadValidationResult(bool IsValid, string? ErrorMessage)
{
    public static DocumentUploadValidationResult Ok() => new(true, null);
    public static DocumentUploadValidationResult Fail(string message) => new(false, message);
}
