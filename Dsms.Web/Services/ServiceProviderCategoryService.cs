using Dsms.Web.Data;
using Dsms.Web.Data.Seed;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

public enum ServiceProviderCategorySaveResult
{
    Success,
    NotFound,
    PermissionDenied,
    DuplicateName,
    InvalidTenant,
    Failed
}

public sealed class ServiceProviderCategoryEditModel
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Verwaltung der mandantenbezogenen Dienstleister-Arten ("Art des Dienstleisters").
/// Werte werden nie gelöscht, sondern nur aktiviert/deaktiviert. Alle Abfragen filtern strikt nach Mandant.
/// </summary>
public class ServiceProviderCategoryService(
    ApplicationDbContext db,
    IUserAccessService userAccess,
    ICurrentUserContext currentUser,
    IComplianceAuditLogService complianceAuditLog)
{
    public const int MaxNameLength = 150;

    public Task<bool> CanManageCategoriesAsync() => userAccess.CanManageServiceProviderCategoriesAsync();

    public async Task EnsureDefaultCategoriesAsync(int tenantId, string? userId = null, CancellationToken ct = default) =>
        await ServiceProviderCategorySeeder.EnsureDefaultCategoriesAsync(db, tenantId, userId, ct);

    public async Task<IReadOnlyList<ServiceProviderCategory>> GetAllForTenantAsync(
        int tenantId,
        CancellationToken ct = default) =>
        await db.ServiceProviderCategories
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceProviderCategory>> GetActiveForSelectionAsync(
        int tenantId,
        CancellationToken ct = default) =>
        await db.ServiceProviderCategories
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

    /// <summary>
    /// Liefert die im Dienstleister-Formular auswählbaren Arten: alle aktiven Arten,
    /// zusätzlich die aktuell zugewiesene Art (auch wenn diese inzwischen deaktiviert wurde).
    /// </summary>
    public async Task<IReadOnlyList<ServiceProviderCategory>> GetOptionsForServiceProviderEditAsync(
        int tenantId,
        int? currentCategoryId,
        CancellationToken ct = default)
    {
        var active = await GetActiveForSelectionAsync(tenantId, ct);
        if (currentCategoryId is null || active.Any(c => c.Id == currentCategoryId))
        {
            return active;
        }

        var current = await db.ServiceProviderCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == currentCategoryId && c.TenantId == tenantId, ct);

        return current is null
            ? active
            : active.Concat([current]).OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList();
    }

    /// <summary>
    /// Prüft, ob eine Art einem Dienstleister zugewiesen werden darf. Inaktive Arten sind nur
    /// zulässig, wenn sie dem Dienstleister bereits zugeordnet sind.
    /// </summary>
    public async Task<(bool IsValid, string? Error)> ValidateCategoryForServiceProviderAsync(
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

        var category = await db.ServiceProviderCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.TenantId == tenantId, ct);

        if (category is null)
        {
            return (false, "Die gewählte Art ist ungültig.");
        }

        if (!category.IsActive)
        {
            return (false, "Die gewählte Art ist inaktiv.");
        }

        return (true, null);
    }

    public async Task<(ServiceProviderCategorySaveResult Result, string? Error, ServiceProviderCategory? Category)> CreateAsync(
        ServiceProviderCategoryEditModel model,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await CanManageCategoriesAsync())
        {
            return (ServiceProviderCategorySaveResult.PermissionDenied, "Keine Berechtigung.", null);
        }

        var name = model.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return (ServiceProviderCategorySaveResult.Failed, "Name ist erforderlich.", null);
        }

        if (name.Length > MaxNameLength)
        {
            return (ServiceProviderCategorySaveResult.Failed, $"Name darf höchstens {MaxNameLength} Zeichen lang sein.", null);
        }

        if (await NameExistsAsync(tenantId, name, null, ct))
        {
            return (ServiceProviderCategorySaveResult.DuplicateName, "Es existiert bereits eine Art mit diesem Namen.", null);
        }

        var userId = await currentUser.GetUserIdAsync();
        var category = new ServiceProviderCategory
        {
            TenantId = tenantId,
            Name = name,
            Description = NormalizeOptional(model.Description),
            SortOrder = model.SortOrder,
            IsActive = model.IsActive,
            IsSystemDefault = false,
            CreatedByUserId = userId
        };

        db.ServiceProviderCategories.Add(category);
        await db.SaveChangesAsync(ct);
        await complianceAuditLog.LogServiceProviderCategoryCreatedAsync(category.Id, category.Name, tenantId);

        return (ServiceProviderCategorySaveResult.Success, null, category);
    }

    public async Task<(ServiceProviderCategorySaveResult Result, string? Error)> UpdateAsync(
        ServiceProviderCategoryEditModel model,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await CanManageCategoriesAsync())
        {
            return (ServiceProviderCategorySaveResult.PermissionDenied, "Keine Berechtigung.");
        }

        if (model.Id is null)
        {
            return (ServiceProviderCategorySaveResult.NotFound, "Art nicht gefunden.");
        }

        var category = await db.ServiceProviderCategories
            .FirstOrDefaultAsync(c => c.Id == model.Id && c.TenantId == tenantId, ct);

        if (category is null)
        {
            return (ServiceProviderCategorySaveResult.NotFound, "Art nicht gefunden.");
        }

        var name = model.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return (ServiceProviderCategorySaveResult.Failed, "Name ist erforderlich.");
        }

        if (name.Length > MaxNameLength)
        {
            return (ServiceProviderCategorySaveResult.Failed, $"Name darf höchstens {MaxNameLength} Zeichen lang sein.");
        }

        if (await NameExistsAsync(tenantId, name, category.Id, ct))
        {
            return (ServiceProviderCategorySaveResult.DuplicateName, "Es existiert bereits eine Art mit diesem Namen.");
        }

        var changes = ComplianceAuditDiffBuilder.ForServiceProviderCategory(
            category.Name,
            category.Description,
            category.SortOrder,
            category.IsActive,
            new ServiceProviderCategoryEditModel
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
            await complianceAuditLog.LogServiceProviderCategoryUpdatedAsync(category.Id, category.Name, tenantId, changes);
        }

        return (ServiceProviderCategorySaveResult.Success, null);
    }

    public async Task<(ServiceProviderCategorySaveResult Result, string? Error)> SetActiveAsync(
        int categoryId,
        int tenantId,
        bool isActive,
        CancellationToken ct = default)
    {
        if (!await CanManageCategoriesAsync())
        {
            return (ServiceProviderCategorySaveResult.PermissionDenied, "Keine Berechtigung.");
        }

        var category = await db.ServiceProviderCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.TenantId == tenantId, ct);

        if (category is null)
        {
            return (ServiceProviderCategorySaveResult.NotFound, "Art nicht gefunden.");
        }

        if (category.IsActive == isActive)
        {
            return (ServiceProviderCategorySaveResult.Success, null);
        }

        category.IsActive = isActive;
        category.UpdatedAt = DateTime.UtcNow;
        category.UpdatedByUserId = await currentUser.GetUserIdAsync();
        await db.SaveChangesAsync(ct);

        if (isActive)
        {
            await complianceAuditLog.LogServiceProviderCategoryReactivatedAsync(category.Id, category.Name, tenantId);
        }
        else
        {
            await complianceAuditLog.LogServiceProviderCategoryDeactivatedAsync(category.Id, category.Name, tenantId);
        }

        return (ServiceProviderCategorySaveResult.Success, null);
    }

    private async Task<bool> NameExistsAsync(
        int tenantId,
        string name,
        int? excludeId,
        CancellationToken ct) =>
        await db.ServiceProviderCategories
            .IgnoreQueryFilters()
            .AnyAsync(c => c.TenantId == tenantId
                && c.Name == name
                && (excludeId == null || c.Id != excludeId), ct);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
