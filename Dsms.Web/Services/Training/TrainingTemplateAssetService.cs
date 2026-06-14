using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Training;

/// <summary>Verwaltung von Schulungsassets (Upload, Validierung, Platzhalter-Auflösung).</summary>
public class TrainingTemplateAssetService(
    ApplicationDbContext db,
    ICurrentUserContext currentUser,
    TrainingAssetStorageService storage,
    TrainingTemplateAccessService templateAccess,
    IComplianceAuditLogService complianceAuditLog)
{
    public async Task<IReadOnlyList<TrainingTemplateAsset>> GetAssetsAsync(
        int templateId, int? tenantId, CancellationToken ct = default)
    {
        var template = await templateAccess.GetTemplateByIdAsync(templateId, tenantId, ct: ct);
        if (template is null)
            return [];

        return await db.TrainingTemplateAssets
            .Where(a => a.TrainingTemplateId == templateId && a.IsActive)
            .OrderBy(a => a.AssetKey)
            .ToListAsync(ct);
    }

    public async Task<TrainingTemplateAsset?> GetAssetByKeyAsync(
        int templateId, string assetKey, int? tenantId, CancellationToken ct = default)
    {
        if (!TrainingAssetUploadValidation.IsValidAssetKey(assetKey))
            return null;

        var normalizedKey = TrainingAssetUploadValidation.NormalizeAssetKey(assetKey);
        var template = await templateAccess.GetTemplateByIdAsync(templateId, tenantId, ct: ct);
        if (template is null)
            return null;

        return await db.TrainingTemplateAssets
            .FirstOrDefaultAsync(a =>
                a.TrainingTemplateId == templateId
                && a.AssetKey == normalizedKey
                && a.IsActive, ct);
    }

    public TrainingAssetUploadValidationResult ValidateAssetUpload(string fileName, string contentType, long fileSize) =>
        TrainingAssetUploadValidation.Validate(fileName, contentType, fileSize);

    public string GenerateAssetKey(string fileName, IEnumerable<string> existingKeys)
    {
        var baseKey = TrainingAssetUploadValidation.GenerateAssetKeyFromFileName(fileName);
        var keys = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!keys.Contains(baseKey))
            return baseKey;

        for (var i = 2; i < 1000; i++)
        {
            var candidate = $"{baseKey}-{i}";
            if (!keys.Contains(candidate))
                return candidate;
        }

        return $"{baseKey}-{Guid.NewGuid():N}"[..80];
    }

    public async Task<TrainingAssetOperationResult> SaveAssetAsync(
        int templateId,
        string assetKey,
        string fileName,
        string contentType,
        long fileSize,
        Stream content,
        string? altText = null,
        CancellationToken ct = default)
    {
        var template = await db.TrainingTemplates.FirstOrDefaultAsync(t => t.Id == templateId, ct);
        if (template is null || !await templateAccess.CanEditAsync(template, ct))
            return TrainingAssetOperationResult.Fail(TrainingLabels.AccessDenied);

        var validation = ValidateAssetUpload(fileName, contentType, fileSize);
        if (!validation.IsValid)
            return TrainingAssetOperationResult.Fail(validation.ErrorMessage!);

        var normalizedKey = TrainingAssetUploadValidation.NormalizeAssetKey(assetKey);
        if (!TrainingAssetUploadValidation.IsValidAssetKey(normalizedKey))
            return TrainingAssetOperationResult.Fail(TrainingLabels.InvalidAssetKey);

        var keyExists = await db.TrainingTemplateAssets.AnyAsync(
            a => a.TrainingTemplateId == templateId && a.AssetKey == normalizedKey && a.IsActive, ct);
        if (keyExists)
            return TrainingAssetOperationResult.Fail($"Asset-Schlüssel „{normalizedKey}“ ist bereits vergeben.");

        var (storagePath, storedFileName) = await storage.SaveAsync(
            template.TenantId, templateId, fileName, content, ct);

        var userId = await currentUser.GetUserIdAsync();
        var asset = new TrainingTemplateAsset
        {
            TenantId = template.TenantId,
            TrainingTemplateId = templateId,
            AssetKey = normalizedKey,
            OriginalFileName = Path.GetFileName(fileName),
            StoredFileName = storedFileName,
            ContentType = contentType,
            FileSizeBytes = fileSize,
            StoragePath = storagePath,
            AltText = altText?.Trim(),
            IsActive = true,
            CreatedByUserId = userId
        };

        db.TrainingTemplateAssets.Add(asset);
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = userId;
        await db.SaveChangesAsync(ct);

        await complianceAuditLog.LogTrainingTemplateAssetUploadedAsync(
            templateId, template.Title, template.TenantId, asset.Id, normalizedKey);
        return TrainingAssetOperationResult.Ok(asset.Id);
    }

    public async Task<TrainingAssetOperationResult> ArchiveAssetAsync(int assetId, CancellationToken ct = default)
    {
        var asset = await db.TrainingTemplateAssets
            .Include(a => a.TrainingTemplate)
            .FirstOrDefaultAsync(a => a.Id == assetId && a.IsActive, ct);

        if (asset is null || !await templateAccess.CanEditAsync(asset.TrainingTemplate, ct))
            return TrainingAssetOperationResult.Fail(TrainingLabels.AccessDenied);

        var userId = await currentUser.GetUserIdAsync();
        asset.IsActive = false;
        asset.UpdatedAt = DateTime.UtcNow;
        asset.UpdatedByUserId = userId;

        asset.TrainingTemplate.UpdatedAt = DateTime.UtcNow;
        asset.TrainingTemplate.UpdatedByUserId = userId;
        await db.SaveChangesAsync(ct);

        await complianceAuditLog.LogTrainingTemplateAssetArchivedAsync(
            asset.TrainingTemplateId, asset.TrainingTemplate.Title, asset.TrainingTemplate.TenantId,
            asset.Id, asset.AssetKey);
        return TrainingAssetOperationResult.Ok(asset.Id);
    }

    public async Task<string> ResolveAssetPlaceholderAsync(
        string markdown,
        int templateId,
        int? tenantId,
        CancellationToken ct = default)
    {
        if (await templateAccess.GetTemplateByIdAsync(templateId, tenantId, ct: ct) is null)
            return markdown;

        var assets = await db.TrainingTemplateAssets
            .Where(a => a.TrainingTemplateId == templateId && a.IsActive)
            .ToListAsync(ct);

        var availableKeys = assets.Select(a => a.AssetKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var altTexts = assets.ToDictionary(a => a.AssetKey, a => a.AltText ?? a.AssetKey, StringComparer.OrdinalIgnoreCase);

        return TrainingMarkdownAssetResolver.ResolveAssetPlaceholdersWithAvailability(
            markdown, templateId, availableKeys, altTexts);
    }

    public async Task<bool> IsAssetKeyUsedInSectionsAsync(
        int templateId, string assetKey, CancellationToken ct = default)
    {
        var sections = await db.TrainingTemplateSections
            .Where(s => s.TrainingTemplateId == templateId && s.IsActive)
            .Select(s => s.ContentMarkdown)
            .ToListAsync(ct);

        return sections.Any(md =>
            TrainingMarkdownAssetResolver.ExtractAssetKeys(md)
                .Any(k => k.Equals(assetKey, StringComparison.OrdinalIgnoreCase)));
    }

    public async Task<TrainingAssetOperationResult> UpdateAltTextAsync(
        int assetId, string? altText, CancellationToken ct = default)
    {
        var asset = await db.TrainingTemplateAssets
            .Include(a => a.TrainingTemplate)
            .FirstOrDefaultAsync(a => a.Id == assetId && a.IsActive, ct);

        if (asset is null || !await templateAccess.CanEditAsync(asset.TrainingTemplate, ct))
            return TrainingAssetOperationResult.Fail(TrainingLabels.AccessDenied);

        asset.AltText = altText?.Trim();
        asset.UpdatedAt = DateTime.UtcNow;
        asset.UpdatedByUserId = await currentUser.GetUserIdAsync();
        await db.SaveChangesAsync(ct);
        return TrainingAssetOperationResult.Ok(asset.Id);
    }

    /// <summary>Kopiert alle aktiven Assets einer Quellvorlage in eine Zielvorlage (Dateien + DB).</summary>
    public async Task CopyAssetsFromTemplateAsync(
        int sourceTemplateId,
        int targetTemplateId,
        int? targetTenantId,
        string? userId,
        CancellationToken ct = default)
    {
        var sourceAssets = await db.TrainingTemplateAssets
            .IgnoreQueryFilters()
            .Where(a => a.TrainingTemplateId == sourceTemplateId && a.IsActive)
            .ToListAsync(ct);

        foreach (var sourceAsset in sourceAssets)
        {
            var copyResult = await storage.CopyAsync(
                sourceAsset.StoragePath,
                targetTenantId,
                targetTemplateId,
                sourceAsset.OriginalFileName,
                ct);

            if (copyResult is null)
                continue;

            var (storagePath, storedFileName) = copyResult.Value;
            db.TrainingTemplateAssets.Add(new TrainingTemplateAsset
            {
                TenantId = targetTenantId,
                TrainingTemplateId = targetTemplateId,
                AssetKey = sourceAsset.AssetKey,
                OriginalFileName = sourceAsset.OriginalFileName,
                StoredFileName = storedFileName,
                ContentType = sourceAsset.ContentType,
                FileSizeBytes = sourceAsset.FileSizeBytes,
                StoragePath = storagePath,
                AltText = sourceAsset.AltText,
                IsActive = true,
                CreatedByUserId = userId
            });
        }

        await db.SaveChangesAsync(ct);
    }
}

public readonly record struct TrainingAssetOperationResult(bool Success, string? Message = null, int? AssetId = null)
{
    public static TrainingAssetOperationResult Ok(int assetId) => new(true, null, assetId);
    public static TrainingAssetOperationResult Fail(string message) => new(false, message);
}
