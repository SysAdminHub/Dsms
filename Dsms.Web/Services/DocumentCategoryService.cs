using Dsms.Web.Data;
using Dsms.Web.Data.Seed;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

public enum DocumentCategorySaveResult
{
    Success,
    NotFound,
    PermissionDenied,
    DuplicateName,
    InvalidTenant,
    Failed
}

public sealed class DocumentCategoryEditModel
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = DocumentCategoryColors.Gray;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class DocumentCategoryService(
    ApplicationDbContext db,
    IUserAccessService userAccess,
    ICurrentUserContext currentUser,
    IComplianceAuditLogService complianceAuditLog)
{
    public Task<bool> CanManageCategoriesAsync() => userAccess.CanManageDocumentCategoriesAsync();

    public async Task EnsureDefaultCategoriesAsync(int tenantId, string? userId = null, CancellationToken ct = default) =>
        await DocumentCategorySeeder.EnsureDefaultCategoriesAsync(db, tenantId, userId, ct);

    public async Task<IReadOnlyList<DocumentCategory>> GetAllForTenantAsync(
        int tenantId,
        CancellationToken ct = default) =>
        await db.DocumentCategories
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DocumentCategory>> GetActiveForSelectionAsync(
        int tenantId,
        CancellationToken ct = default) =>
        await db.DocumentCategories
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DocumentCategory>> GetFilterOptionsAsync(
        int tenantId,
        CancellationToken ct = default)
    {
        var categories = await GetAllForTenantAsync(tenantId, ct);
        var usedInactiveIds = await db.EvidenceDocuments
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.DocumentCategoryId != null)
            .Select(d => d.DocumentCategoryId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var usedInactiveSet = usedInactiveIds.ToHashSet();
        return categories
            .Where(c => c.IsActive || usedInactiveSet.Contains(c.Id))
            .ToList();
    }

    public async Task<IReadOnlyList<DocumentCategory>> GetOptionsForDocumentEditAsync(
        int tenantId,
        int? currentCategoryId,
        CancellationToken ct = default)
    {
        var active = await GetActiveForSelectionAsync(tenantId, ct);
        if (currentCategoryId is null || active.Any(c => c.Id == currentCategoryId))
        {
            return active;
        }

        var current = await db.DocumentCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == currentCategoryId && c.TenantId == tenantId, ct);

        return current is null ? active : active.Concat([current]).OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList();
    }

    public async Task<(bool IsValid, string? Error)> ValidateCategoryForDocumentAsync(
        int tenantId,
        int? categoryId,
        int? currentCategoryId = null,
        CancellationToken ct = default)
    {
        if (categoryId is null)
        {
            return (true, null);
        }

        if (currentCategoryId == categoryId)
        {
            return (true, null);
        }

        var category = await db.DocumentCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.TenantId == tenantId, ct);

        if (category is null)
        {
            return (false, "Die gewählte Kategorie ist ungültig.");
        }

        if (!category.IsActive)
        {
            return (false, "Die gewählte Kategorie ist inaktiv.");
        }

        return (true, null);
    }

    public async Task<(DocumentCategorySaveResult Result, string? Error, DocumentCategory? Category)> CreateAsync(
        DocumentCategoryEditModel model,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await CanManageCategoriesAsync())
        {
            return (DocumentCategorySaveResult.PermissionDenied, "Keine Berechtigung.", null);
        }

        var name = model.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return (DocumentCategorySaveResult.Failed, "Name ist erforderlich.", null);
        }

        if (await NameExistsAsync(tenantId, name, null, ct))
        {
            return (DocumentCategorySaveResult.DuplicateName, "Es existiert bereits eine Kategorie mit diesem Namen.", null);
        }

        var userId = await currentUser.GetUserIdAsync();
        var category = new DocumentCategory
        {
            TenantId = tenantId,
            Name = name,
            Description = NormalizeOptional(model.Description),
            Color = DocumentCategoryColors.Normalize(model.Color),
            SortOrder = model.SortOrder,
            IsActive = model.IsActive,
            IsSystemDefault = false,
            CreatedByUserId = userId
        };

        db.DocumentCategories.Add(category);
        await db.SaveChangesAsync(ct);
        await complianceAuditLog.LogDocumentCategoryCreatedAsync(category.Id, category.Name, tenantId);

        return (DocumentCategorySaveResult.Success, null, category);
    }

    public async Task<(DocumentCategorySaveResult Result, string? Error)> UpdateAsync(
        DocumentCategoryEditModel model,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await CanManageCategoriesAsync())
        {
            return (DocumentCategorySaveResult.PermissionDenied, "Keine Berechtigung.");
        }

        if (model.Id is null)
        {
            return (DocumentCategorySaveResult.NotFound, "Kategorie nicht gefunden.");
        }

        var category = await db.DocumentCategories
            .FirstOrDefaultAsync(c => c.Id == model.Id && c.TenantId == tenantId, ct);

        if (category is null)
        {
            return (DocumentCategorySaveResult.NotFound, "Kategorie nicht gefunden.");
        }

        var name = model.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return (DocumentCategorySaveResult.Failed, "Name ist erforderlich.");
        }

        if (await NameExistsAsync(tenantId, name, category.Id, ct))
        {
            return (DocumentCategorySaveResult.DuplicateName, "Es existiert bereits eine Kategorie mit diesem Namen.");
        }

        var previousName = category.Name;
        var previousActive = category.IsActive;
        var changes = ComplianceAuditDiffBuilder.ForDocumentCategory(
            previousName,
            category.Description,
            category.Color,
            category.SortOrder,
            previousActive,
            new DocumentCategoryEditModel
            {
                Name = name,
                Description = NormalizeOptional(model.Description),
                Color = DocumentCategoryColors.Normalize(model.Color),
                SortOrder = model.SortOrder,
                IsActive = model.IsActive
            });

        category.Name = name;
        category.Description = NormalizeOptional(model.Description);
        category.Color = DocumentCategoryColors.Normalize(model.Color);
        category.SortOrder = model.SortOrder;
        category.IsActive = model.IsActive;
        category.UpdatedAt = DateTime.UtcNow;
        category.UpdatedByUserId = await currentUser.GetUserIdAsync();

        await db.SaveChangesAsync(ct);

        if (changes.Count > 0)
        {
            await complianceAuditLog.LogDocumentCategoryUpdatedAsync(category.Id, category.Name, tenantId, changes);
        }

        return (DocumentCategorySaveResult.Success, null);
    }

    public async Task<(DocumentCategorySaveResult Result, string? Error)> SetActiveAsync(
        int categoryId,
        int tenantId,
        bool isActive,
        CancellationToken ct = default)
    {
        if (!await CanManageCategoriesAsync())
        {
            return (DocumentCategorySaveResult.PermissionDenied, "Keine Berechtigung.");
        }

        var category = await db.DocumentCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.TenantId == tenantId, ct);

        if (category is null)
        {
            return (DocumentCategorySaveResult.NotFound, "Kategorie nicht gefunden.");
        }

        if (category.IsActive == isActive)
        {
            return (DocumentCategorySaveResult.Success, null);
        }

        category.IsActive = isActive;
        category.UpdatedAt = DateTime.UtcNow;
        category.UpdatedByUserId = await currentUser.GetUserIdAsync();
        await db.SaveChangesAsync(ct);

        if (isActive)
        {
            await complianceAuditLog.LogDocumentCategoryReactivatedAsync(category.Id, category.Name, tenantId);
        }
        else
        {
            await complianceAuditLog.LogDocumentCategoryDeactivatedAsync(category.Id, category.Name, tenantId);
        }

        return (DocumentCategorySaveResult.Success, null);
    }

    private async Task<bool> NameExistsAsync(
        int tenantId,
        string name,
        int? excludeId,
        CancellationToken ct) =>
        await db.DocumentCategories
            .IgnoreQueryFilters()
            .AnyAsync(c => c.TenantId == tenantId
                && c.Name == name
                && (excludeId == null || c.Id != excludeId), ct);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
