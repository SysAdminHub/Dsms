namespace Dsms.Web.Services.Training;

/// <summary>
/// Validierung für Schulungsasset-Uploads (Bildtypen, Größe, MIME).
/// Schulungsassets sind getrennt vom Dokumentenmodul.
/// </summary>
public static class TrainingAssetUploadValidation
{
    public const long MaxTrainingAssetSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif"
    };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif"
    };

    /// <summary>AssetKey: Kleinbuchstaben, Ziffern, Bindestrich oder Unterstrich.</summary>
    private static readonly System.Text.RegularExpressions.Regex AssetKeyPattern =
        new(@"^[a-z0-9]+(?:[-_][a-z0-9]+)*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static TrainingAssetUploadValidationResult Validate(string fileName, string contentType, long fileSize)
    {
        if (fileSize <= 0)
            return TrainingAssetUploadValidationResult.Fail("Keine Datei ausgewählt.");

        if (fileSize > MaxTrainingAssetSizeBytes)
            return TrainingAssetUploadValidationResult.Fail($"Datei ist zu groß (max. {MaxTrainingAssetSizeBytes / (1024 * 1024)} MB).");

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            return TrainingAssetUploadValidationResult.Fail("Dateityp nicht erlaubt. Erlaubt: PNG, JPG, WEBP, GIF.");

        if (!IsMimeTypeAllowed(contentType, extension))
            return TrainingAssetUploadValidationResult.Fail("Dateityp (MIME) nicht erlaubt.");

        return TrainingAssetUploadValidationResult.Ok();
    }

    public static bool IsValidAssetKey(string? assetKey) =>
        !string.IsNullOrWhiteSpace(assetKey) && AssetKeyPattern.IsMatch(assetKey);

    public static string NormalizeAssetKey(string assetKey) =>
        assetKey.Trim().ToLowerInvariant();

    public static string GenerateAssetKeyFromFileName(string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        var normalized = new string(baseName
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray());

        while (normalized.Contains("--", StringComparison.Ordinal))
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);

        normalized = normalized.Trim('-');
        if (string.IsNullOrEmpty(normalized))
            normalized = "asset";

        return normalized.Length > 80 ? normalized[..80].TrimEnd('-') : normalized;
    }

    private static bool IsMimeTypeAllowed(string contentType, string extension)
    {
        if (string.IsNullOrWhiteSpace(contentType)
            || contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return AllowedExtensions.Contains(extension);
        }

        return AllowedMimeTypes.Contains(contentType);
    }
}

public readonly record struct TrainingAssetUploadValidationResult(bool IsValid, string? ErrorMessage)
{
    public static TrainingAssetUploadValidationResult Ok() => new(true, null);
    public static TrainingAssetUploadValidationResult Fail(string message) => new(false, message);
}
