using System.Text.RegularExpressions;

namespace Dsms.Web.Services.Training;

/// <summary>
/// Erkennt und ersetzt Asset-Platzhalter im Markdown: <c>{{asset:asset-key}}</c>.
/// </summary>
public static partial class TrainingMarkdownAssetResolver
{
    [GeneratedRegex(@"\{\{asset:([a-z0-9]+(?:[-_][a-z0-9]+)*)\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex AssetPlaceholderRegex();

    /// <summary>Extrahiert alle AssetKeys aus dem Markdown.</summary>
    public static IReadOnlyList<string> ExtractAssetKeys(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return [];

        return AssetPlaceholderRegex()
            .Matches(markdown)
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Ersetzt Platzhalter durch Bild-URLs oder Hinweistext bei fehlenden Assets.
    /// </summary>
    public static string ResolveAssetPlaceholders(
        string? markdown,
        int trainingTemplateId,
        IReadOnlyDictionary<string, string> assetKeyToAltText)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        return AssetPlaceholderRegex().Replace(markdown, match =>
        {
            var assetKey = match.Groups[1].Value.ToLowerInvariant();
            if (!TrainingAssetUploadValidation.IsValidAssetKey(assetKey))
                return $"*[Ungültiger Asset-Platzhalter: {assetKey}]*";

            var url = BuildAssetUrl(trainingTemplateId, assetKey);
            var alt = assetKeyToAltText.TryGetValue(assetKey, out var altText) && !string.IsNullOrWhiteSpace(altText)
                ? altText
                : assetKey;

            return $"![{alt}]({url})";
        });
    }

    /// <summary>Ersetzt fehlende Assets durch sichtbaren Hinweis statt Exception.</summary>
    public static string ResolveAssetPlaceholdersWithAvailability(
        string? markdown,
        int trainingTemplateId,
        IReadOnlySet<string> availableAssetKeys,
        IReadOnlyDictionary<string, string> assetKeyToAltText)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        return AssetPlaceholderRegex().Replace(markdown, match =>
        {
            var assetKey = match.Groups[1].Value.ToLowerInvariant();
            if (!availableAssetKeys.Contains(assetKey))
                return $"*[Bild nicht gefunden: {assetKey}]*";

            var url = BuildAssetUrl(trainingTemplateId, assetKey);
            var alt = assetKeyToAltText.TryGetValue(assetKey, out var altText) && !string.IsNullOrWhiteSpace(altText)
                ? altText
                : assetKey;

            return $"![{alt}]({url})";
        });
    }

    public static string BuildAssetUrl(int trainingTemplateId, string assetKey) =>
        $"/training-assets/{trainingTemplateId}/{assetKey}";

    public static string BuildPortalAssetUrl(string assetKey) =>
        $"/training-portal-assets/{assetKey}";

    public static string ResolvePortalAssetPlaceholders(
        string? markdown,
        IReadOnlySet<string> availableAssetKeys,
        IReadOnlyDictionary<string, string> assetKeyToAltText)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        return AssetPlaceholderRegex().Replace(markdown, match =>
        {
            var assetKey = match.Groups[1].Value.ToLowerInvariant();
            if (!availableAssetKeys.Contains(assetKey))
                return $"*[Bild nicht gefunden: {assetKey}]*";

            var url = BuildPortalAssetUrl(assetKey);
            var alt = assetKeyToAltText.TryGetValue(assetKey, out var altText) && !string.IsNullOrWhiteSpace(altText)
                ? altText
                : assetKey;

            return $"![{alt}]({url})";
        });
    }
}
