using Dsms.Web.Data;
using Dsms.Web.Data.Seed;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

public enum TomCategorySaveResult
{
    Success,
    NotFound,
    PermissionDenied,
    DuplicateName,
    InvalidTenant,
    Failed
}

public sealed class TomCategoryEditModel
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Verwaltung der mandantenbezogenen TOM-Kategorien. Kategorien werden nie gelöscht,
/// sondern nur aktiviert/deaktiviert. Alle Abfragen filtern strikt nach Mandant.
/// </summary>
public class TomCategoryService(
    ApplicationDbContext db,
    IUserAccessService userAccess,
    ICurrentUserContext currentUser,
    IComplianceAuditLogService complianceAuditLog)
{
    public const int MaxNameLength = 150;

    public Task<bool> CanManageCategoriesAsync() => userAccess.CanManageTomCategoriesAsync();

    public async Task EnsureDefaultCategoriesAsync(int tenantId, string? userId = null, CancellationToken ct = default) =>
        await TomCategorySeeder.EnsureDefaultCategoriesAsync(db, tenantId, userId, ct);

    public async Task<IReadOnlyList<TomCategory>> GetAllForTenantAsync(
        int tenantId,
        CancellationToken ct = default) =>
        await db.TomCategories
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TomCategory>> GetActiveForSelectionAsync(
        int tenantId,
        CancellationToken ct = default) =>
        await db.TomCategories
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

    /// <summary>
    /// Liefert die im TOM-Formular auswählbaren Kategorien: alle aktiven Kategorien,
    /// zusätzlich die aktuell zugewiesene Kategorie (auch wenn diese inzwischen deaktiviert wurde).
    /// </summary>
    public async Task<IReadOnlyList<TomCategory>> GetOptionsForTomEditAsync(
        int tenantId,
        int? currentCategoryId,
        CancellationToken ct = default)
    {
        var active = await GetActiveForSelectionAsync(tenantId, ct);
        if (currentCategoryId is null || active.Any(c => c.Id == currentCategoryId))
        {
            return active;
        }

        var current = await db.TomCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == currentCategoryId && c.TenantId == tenantId, ct);

        return current is null
            ? active
            : active.Concat([current]).OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList();
    }

    /// <summary>
    /// Prüft, ob eine Kategorie einer TOM zugewiesen werden darf. Inaktive Kategorien sind nur
    /// zulässig, wenn sie der TOM bereits zugeordnet sind.
    /// </summary>
    public async Task<(bool IsValid, string? Error)> ValidateCategoryForTomAsync(
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

        var category = await db.TomCategories
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

    public async Task<(TomCategorySaveResult Result, string? Error, TomCategory? Category)> CreateAsync(
        TomCategoryEditModel model,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await CanManageCategoriesAsync())
        {
            return (TomCategorySaveResult.PermissionDenied, "Keine Berechtigung.", null);
        }

        var name = model.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return (TomCategorySaveResult.Failed, "Name ist erforderlich.", null);
        }

        if (name.Length > MaxNameLength)
        {
            return (TomCategorySaveResult.Failed, $"Name darf höchstens {MaxNameLength} Zeichen lang sein.", null);
        }

        if (await NameExistsAsync(tenantId, name, null, ct))
        {
            return (TomCategorySaveResult.DuplicateName, "Es existiert bereits eine Kategorie mit diesem Namen.", null);
        }

        var userId = await currentUser.GetUserIdAsync();
        var category = new TomCategory
        {
            TenantId = tenantId,
            Name = name,
            Description = NormalizeOptional(model.Description),
            SortOrder = model.SortOrder,
            IsActive = model.IsActive,
            IsSystemDefault = false,
            CreatedByUserId = userId
        };

        db.TomCategories.Add(category);
        await db.SaveChangesAsync(ct);
        await complianceAuditLog.LogTomCategoryCreatedAsync(category.Id, category.Name, tenantId);

        return (TomCategorySaveResult.Success, null, category);
    }

    public async Task<(TomCategorySaveResult Result, string? Error)> UpdateAsync(
        TomCategoryEditModel model,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await CanManageCategoriesAsync())
        {
            return (TomCategorySaveResult.PermissionDenied, "Keine Berechtigung.");
        }

        if (model.Id is null)
        {
            return (TomCategorySaveResult.NotFound, "Kategorie nicht gefunden.");
        }

        var category = await db.TomCategories
            .FirstOrDefaultAsync(c => c.Id == model.Id && c.TenantId == tenantId, ct);

        if (category is null)
        {
            return (TomCategorySaveResult.NotFound, "Kategorie nicht gefunden.");
        }

        var name = model.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return (TomCategorySaveResult.Failed, "Name ist erforderlich.");
        }

        if (name.Length > MaxNameLength)
        {
            return (TomCategorySaveResult.Failed, $"Name darf höchstens {MaxNameLength} Zeichen lang sein.");
        }

        if (await NameExistsAsync(tenantId, name, category.Id, ct))
        {
            return (TomCategorySaveResult.DuplicateName, "Es existiert bereits eine Kategorie mit diesem Namen.");
        }

        var changes = ComplianceAuditDiffBuilder.ForTomCategory(
            category.Name,
            category.Description,
            category.SortOrder,
            category.IsActive,
            new TomCategoryEditModel
            {
                Name = name,
                Description = NormalizeOptional(model.Description),
                SortOrder = model.SortOrder,
                IsActive = model.IsActive
            });

        category.Name = name;
        category.Description = NormalizeOptional(model.Description);
        category.SortOrder = model.SortOrder;
        category.IsActive = model.IsActive;
        category.UpdatedAt = DateTime.UtcNow;
        category.UpdatedByUserId = await currentUser.GetUserIdAsync();

        await db.SaveChangesAsync(ct);

        if (changes.Count > 0)
        {
            await complianceAuditLog.LogTomCategoryUpdatedAsync(category.Id, category.Name, tenantId, changes);
        }

        return (TomCategorySaveResult.Success, null);
    }

    public async Task<(TomCategorySaveResult Result, string? Error)> SetActiveAsync(
        int categoryId,
        int tenantId,
        bool isActive,
        CancellationToken ct = default)
    {
        if (!await CanManageCategoriesAsync())
        {
            return (TomCategorySaveResult.PermissionDenied, "Keine Berechtigung.");
        }

        var category = await db.TomCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.TenantId == tenantId, ct);

        if (category is null)
        {
            return (TomCategorySaveResult.NotFound, "Kategorie nicht gefunden.");
        }

        if (category.IsActive == isActive)
        {
            return (TomCategorySaveResult.Success, null);
        }

        category.IsActive = isActive;
        category.UpdatedAt = DateTime.UtcNow;
        category.UpdatedByUserId = await currentUser.GetUserIdAsync();
        await db.SaveChangesAsync(ct);

        if (isActive)
        {
            await complianceAuditLog.LogTomCategoryReactivatedAsync(category.Id, category.Name, tenantId);
        }
        else
        {
            await complianceAuditLog.LogTomCategoryDeactivatedAsync(category.Id, category.Name, tenantId);
        }

        return (TomCategorySaveResult.Success, null);
    }

    private async Task<bool> NameExistsAsync(
        int tenantId,
        string name,
        int? excludeId,
        CancellationToken ct) =>
        await db.TomCategories
            .IgnoreQueryFilters()
            .AnyAsync(c => c.TenantId == tenantId
                && c.Name == name
                && (excludeId == null || c.Id != excludeId), ct);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
