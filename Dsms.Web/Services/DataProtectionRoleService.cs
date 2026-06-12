using System.ComponentModel.DataAnnotations;
using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

public enum DataProtectionRoleSaveResult
{
    Success,
    NotFound,
    PermissionDenied,
    InvalidTenant,
    ValidationFailed,
    Failed
}

public sealed class DataProtectionRoleListFilter
{
    /// <summary>True = nur aktive, False = nur inaktive, null = alle.</summary>
    public bool? IsActive { get; set; } = true;
    public string? SearchText { get; set; }
    public string? RoleTitle { get; set; }
    public string? Department { get; set; }
}

public sealed class DataProtectionRoleEditModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Rollenbezeichnung ist erforderlich.")]
    [MaxLength(200)]
    public string RoleTitle { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? PersonName { get; set; }

    [MaxLength(256)]
    [EmailAddress(ErrorMessage = "Bitte geben Sie eine gültige E-Mail-Adresse ein.")]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Department { get; set; }

    [MaxLength(2000)]
    public string? AreaOfResponsibility { get; set; }

    [MaxLength(500)]
    public string? ReportsTo { get; set; }

    [MaxLength(500)]
    public string? Deputy { get; set; }

    public int? ReportsToRoleId { get; set; }
    public int? DeputyRoleId { get; set; }
    public string? LinkedUserId { get; set; }
    public bool IsActive { get; set; } = true;

    [MaxLength(4000)]
    public string? Remarks { get; set; }
}

public sealed class DataProtectionRoleListItem
{
    public required DataProtectionRole Role { get; init; }
    public string? LinkedUserLabel { get; init; }
    public string? ReportsToRoleLabel { get; init; }
    public string? DeputyRoleLabel { get; init; }
}

public sealed class DataProtectionRoleDetailModel
{
    public required DataProtectionRole Role { get; init; }
    public string? LinkedUserLabel { get; init; }
    public string? ReportsToRoleDisplay { get; init; }
    public string? DeputyRoleDisplay { get; init; }
    public string? CreatedByLabel { get; init; }
    public string? UpdatedByLabel { get; init; }
}

public sealed class DataProtectionRoleExportDto
{
    public string RoleTitle { get; init; } = string.Empty;
    public string? Person { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Department { get; init; }
    public string? AreaOfResponsibility { get; init; }
    public string? ReportsTo { get; init; }
    public string? Deputy { get; init; }
    public bool IsActive { get; init; }
    public string? Remarks { get; init; }
}

public class DataProtectionRoleService(
    ApplicationDbContext db,
    IUserAccessService userAccess,
    ICurrentUserContext currentUser,
    IComplianceAuditLogService complianceAuditLog)
{
    public Task<bool> CanManageRolesAsync() => userAccess.CanManageDataProtectionRolesAsync();

    public async Task<bool> CanAccessTenantRolesAsync(int tenantId) =>
        await userAccess.CanAccessTenantAsync(tenantId);

    public async Task<IReadOnlyList<DataProtectionRoleListItem>> GetListAsync(
        int tenantId,
        DataProtectionRoleListFilter? filter = null,
        CancellationToken ct = default)
    {
        if (!await CanAccessTenantRolesAsync(tenantId))
        {
            return [];
        }

        filter ??= new DataProtectionRoleListFilter();
        var query = db.DataProtectionRoles
            .AsNoTracking()
            .Include(r => r.LinkedUser)
            .Include(r => r.ReportsToRole)
            .Include(r => r.DeputyRole)
            .Where(r => r.TenantId == tenantId);

        if (filter.IsActive.HasValue)
        {
            query = query.Where(r => r.IsActive == filter.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.RoleTitle))
        {
            var title = filter.RoleTitle.Trim();
            query = query.Where(r => r.RoleTitle.Contains(title));
        }

        if (!string.IsNullOrWhiteSpace(filter.Department))
        {
            var department = filter.Department.Trim();
            query = query.Where(r => r.Department != null && r.Department.Contains(department));
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(r =>
                r.RoleTitle.Contains(term)
                || (r.PersonName != null && r.PersonName.Contains(term))
                || (r.Email != null && r.Email.Contains(term))
                || (r.Department != null && r.Department.Contains(term))
                || (r.AreaOfResponsibility != null && r.AreaOfResponsibility.Contains(term))
                || (r.ReportsTo != null && r.ReportsTo.Contains(term))
                || (r.Deputy != null && r.Deputy.Contains(term))
                || (r.Remarks != null && r.Remarks.Contains(term)));
        }

        var roles = await query
            .OrderBy(r => r.RoleTitle)
            .ThenBy(r => r.PersonName)
            .ToListAsync(ct);

        return roles.Select(MapListItem).ToList();
    }

    public async Task<DataProtectionRoleDetailModel?> GetByIdAsync(
        int id,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await CanAccessTenantRolesAsync(tenantId))
        {
            return null;
        }

        var role = await db.DataProtectionRoles
            .AsNoTracking()
            .Include(r => r.LinkedUser)
            .Include(r => r.ReportsToRole)
            .Include(r => r.DeputyRole)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);

        return role is null ? null : await MapDetailAsync(role, ct);
    }

    public async Task<IReadOnlyList<DataProtectionRole>> GetActiveRolesForTenantAsync(
        int tenantId,
        CancellationToken ct = default) =>
        await db.DataProtectionRoles
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.IsActive)
            .OrderBy(r => r.RoleTitle)
            .ThenBy(r => r.PersonName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DataProtectionRole>> GetRoleOptionsAsync(
        int tenantId,
        int? excludeRoleId = null,
        CancellationToken ct = default)
    {
        var query = db.DataProtectionRoles
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.IsActive);

        if (excludeRoleId.HasValue)
        {
            query = query.Where(r => r.Id != excludeRoleId.Value);
        }

        return await query
            .OrderBy(r => r.RoleTitle)
            .ThenBy(r => r.PersonName)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DataProtectionRole>> GetHighlightedActiveRolesAsync(
        int tenantId,
        int maxCount = 4,
        CancellationToken ct = default)
    {
        var active = await GetActiveRolesForTenantAsync(tenantId, ct);
        var highlighted = new List<DataProtectionRole>();

        foreach (var title in DataProtectionRoleTitleSuggestions.HighlightedTitles)
        {
            var match = active.FirstOrDefault(r =>
                string.Equals(r.RoleTitle, title, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                highlighted.Add(match);
            }
        }

        if (highlighted.Count < maxCount)
        {
            foreach (var role in active)
            {
                if (highlighted.Any(h => h.Id == role.Id))
                {
                    continue;
                }

                highlighted.Add(role);
                if (highlighted.Count >= maxCount)
                {
                    break;
                }
            }
        }

        return highlighted.Take(maxCount).ToList();
    }

    public async Task<IReadOnlyList<TenantUserOption>> GetTenantUserOptionsAsync(
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await CanAccessTenantRolesAsync(tenantId))
        {
            return [];
        }

        var userIds = await db.UserTenants
            .AsNoTracking()
            .Where(ut => ut.TenantId == tenantId)
            .Select(ut => ut.UserId)
            .ToListAsync(ct);

        return await db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .OrderBy(u => u.DisplayName)
            .ThenBy(u => u.Email)
            .Select(u => new TenantUserOption
            {
                Id = u.Id,
                DisplayName = u.DisplayName,
                Email = u.Email,
                IsActive = u.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<(DataProtectionRoleSaveResult Result, string? Error, DataProtectionRole? Role)> CreateAsync(
        DataProtectionRoleEditModel model,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await CanManageRolesAsync())
        {
            return (DataProtectionRoleSaveResult.PermissionDenied, "Keine Berechtigung.", null);
        }

        if (!await CanAccessTenantRolesAsync(tenantId))
        {
            return (DataProtectionRoleSaveResult.InvalidTenant, "Ungültiger Mandant.", null);
        }

        var validation = await ValidateModelAsync(model, tenantId, null, ct);
        if (validation is not null)
        {
            return (DataProtectionRoleSaveResult.ValidationFailed, validation, null);
        }

        var userId = await currentUser.GetUserIdAsync();
        var role = new DataProtectionRole
        {
            TenantId = tenantId,
            RoleTitle = model.RoleTitle.Trim(),
            PersonName = NormalizeOptional(model.PersonName),
            Email = NormalizeOptional(model.Email),
            Phone = NormalizeOptional(model.Phone),
            Department = NormalizeOptional(model.Department),
            AreaOfResponsibility = NormalizeOptional(model.AreaOfResponsibility),
            ReportsTo = NormalizeOptional(model.ReportsTo),
            Deputy = NormalizeOptional(model.Deputy),
            ReportsToRoleId = model.ReportsToRoleId,
            DeputyRoleId = model.DeputyRoleId,
            LinkedUserId = NormalizeOptional(model.LinkedUserId),
            IsActive = model.IsActive,
            Remarks = NormalizeOptional(model.Remarks),
            CreatedByUserId = userId
        };

        db.DataProtectionRoles.Add(role);
        await db.SaveChangesAsync(ct);
        await complianceAuditLog.LogDataProtectionRoleCreatedAsync(role.Id, role.RoleTitle, tenantId);

        return (DataProtectionRoleSaveResult.Success, null, role);
    }

    public async Task<(DataProtectionRoleSaveResult Result, string? Error)> UpdateAsync(
        int id,
        DataProtectionRoleEditModel model,
        int tenantId,
        CancellationToken ct = default)
    {
        if (!await CanManageRolesAsync())
        {
            return (DataProtectionRoleSaveResult.PermissionDenied, "Keine Berechtigung.");
        }

        if (!await CanAccessTenantRolesAsync(tenantId))
        {
            return (DataProtectionRoleSaveResult.InvalidTenant, "Ungültiger Mandant.");
        }

        var role = await db.DataProtectionRoles
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);

        if (role is null)
        {
            return (DataProtectionRoleSaveResult.NotFound, "Datenschutzrolle nicht gefunden.");
        }

        model.Id = id;
        var validation = await ValidateModelAsync(model, tenantId, id, ct);
        if (validation is not null)
        {
            return (DataProtectionRoleSaveResult.ValidationFailed, validation);
        }

        var previousTitle = role.RoleTitle;
        var previousActive = role.IsActive;
        var changes = ComplianceAuditDiffBuilder.ForDataProtectionRole(
            previousTitle,
            previousActive,
            role.Department,
            new DataProtectionRoleEditModel
            {
                RoleTitle = model.RoleTitle.Trim(),
                IsActive = model.IsActive,
                Department = NormalizeOptional(model.Department)
            });

        role.RoleTitle = model.RoleTitle.Trim();
        role.PersonName = NormalizeOptional(model.PersonName);
        role.Email = NormalizeOptional(model.Email);
        role.Phone = NormalizeOptional(model.Phone);
        role.Department = NormalizeOptional(model.Department);
        role.AreaOfResponsibility = NormalizeOptional(model.AreaOfResponsibility);
        role.ReportsTo = NormalizeOptional(model.ReportsTo);
        role.Deputy = NormalizeOptional(model.Deputy);
        role.ReportsToRoleId = model.ReportsToRoleId;
        role.DeputyRoleId = model.DeputyRoleId;
        role.LinkedUserId = NormalizeOptional(model.LinkedUserId);
        role.IsActive = model.IsActive;
        role.Remarks = NormalizeOptional(model.Remarks);
        role.UpdatedAt = DateTime.UtcNow;
        role.UpdatedByUserId = await currentUser.GetUserIdAsync();

        if (role.IsActive)
        {
            role.ArchivedAt = null;
            role.ArchivedByUserId = null;
        }

        await db.SaveChangesAsync(ct);

        if (changes.Count > 0)
        {
            await complianceAuditLog.LogDataProtectionRoleUpdatedAsync(role.Id, role.RoleTitle, tenantId, changes);
        }

        return (DataProtectionRoleSaveResult.Success, null);
    }

    public async Task<(DataProtectionRoleSaveResult Result, string? Error)> ArchiveAsync(
        int id,
        int tenantId,
        CancellationToken ct = default) =>
        await SetActiveInternalAsync(id, tenantId, false, ct);

    public async Task<(DataProtectionRoleSaveResult Result, string? Error)> ReactivateAsync(
        int id,
        int tenantId,
        CancellationToken ct = default) =>
        await SetActiveInternalAsync(id, tenantId, true, ct);

    public async Task<IReadOnlyList<DataProtectionRoleExportDto>> GetExportDataAsync(
        int tenantId,
        CancellationToken ct = default)
    {
        var roles = await db.DataProtectionRoles
            .AsNoTracking()
            .Include(r => r.ReportsToRole)
            .Include(r => r.DeputyRole)
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.RoleTitle)
            .ThenBy(r => r.PersonName)
            .ToListAsync(ct);

        return roles.Select(r => new DataProtectionRoleExportDto
        {
            RoleTitle = r.RoleTitle,
            Person = r.PersonName,
            Email = r.Email,
            Phone = r.Phone,
            Department = r.Department,
            AreaOfResponsibility = r.AreaOfResponsibility,
            ReportsTo = BuildHierarchyExportText(r.ReportsTo, r.ReportsToRole),
            Deputy = BuildHierarchyExportText(r.Deputy, r.DeputyRole),
            IsActive = r.IsActive,
            Remarks = r.Remarks
        }).ToList();
    }

    private async Task<(DataProtectionRoleSaveResult Result, string? Error)> SetActiveInternalAsync(
        int id,
        int tenantId,
        bool isActive,
        CancellationToken ct)
    {
        if (!await CanManageRolesAsync())
        {
            return (DataProtectionRoleSaveResult.PermissionDenied, "Keine Berechtigung.");
        }

        var role = await db.DataProtectionRoles
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);

        if (role is null)
        {
            return (DataProtectionRoleSaveResult.NotFound, "Datenschutzrolle nicht gefunden.");
        }

        if (role.IsActive == isActive)
        {
            return (DataProtectionRoleSaveResult.Success, null);
        }

        var userId = await currentUser.GetUserIdAsync();
        role.IsActive = isActive;
        role.UpdatedAt = DateTime.UtcNow;
        role.UpdatedByUserId = userId;

        if (isActive)
        {
            role.ArchivedAt = null;
            role.ArchivedByUserId = null;
            await db.SaveChangesAsync(ct);
            await complianceAuditLog.LogDataProtectionRoleReactivatedAsync(role.Id, role.RoleTitle, tenantId);
        }
        else
        {
            role.ArchivedAt = DateTime.UtcNow;
            role.ArchivedByUserId = userId;
            await db.SaveChangesAsync(ct);
            await complianceAuditLog.LogDataProtectionRoleDeactivatedAsync(role.Id, role.RoleTitle, tenantId);
        }

        return (DataProtectionRoleSaveResult.Success, null);
    }

    private async Task<string?> ValidateModelAsync(
        DataProtectionRoleEditModel model,
        int tenantId,
        int? currentRoleId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.RoleTitle))
        {
            return "Rollenbezeichnung ist erforderlich.";
        }

        var hasPerson = !string.IsNullOrWhiteSpace(model.PersonName);
        var hasLinkedUser = !string.IsNullOrWhiteSpace(model.LinkedUserId);
        if (!hasPerson && !hasLinkedUser)
        {
            return "Bitte geben Sie eine Person an oder verknüpfen Sie einen Benutzer.";
        }

        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var email = model.Email.Trim();
            if (!new EmailAddressAttribute().IsValid(email))
            {
                return "Bitte geben Sie eine gültige E-Mail-Adresse ein.";
            }
        }

        if (model.ReportsToRoleId == currentRoleId)
        {
            return "Eine Rolle kann nicht an sich selbst berichten.";
        }

        if (model.DeputyRoleId == currentRoleId)
        {
            return "Eine Rolle kann nicht ihre eigene Vertretung sein.";
        }

        if (!string.IsNullOrWhiteSpace(model.LinkedUserId)
            && !await IsUserInTenantAsync(model.LinkedUserId, tenantId, ct))
        {
            return "Der verknüpfte Benutzer gehört nicht zum aktuellen Mandanten.";
        }

        if (model.ReportsToRoleId.HasValue
            && !await IsRoleInTenantAsync(model.ReportsToRoleId.Value, tenantId, ct))
        {
            return "Die gewählte Berichtslinie ist ungültig.";
        }

        if (model.DeputyRoleId.HasValue
            && !await IsRoleInTenantAsync(model.DeputyRoleId.Value, tenantId, ct))
        {
            return "Die gewählte Vertretung ist ungültig.";
        }

        return null;
    }

    private async Task<bool> IsUserInTenantAsync(string userId, int tenantId, CancellationToken ct) =>
        await db.UserTenants
            .AsNoTracking()
            .AnyAsync(ut => ut.UserId == userId && ut.TenantId == tenantId, ct);

    private async Task<bool> IsRoleInTenantAsync(int roleId, int tenantId, CancellationToken ct) =>
        await db.DataProtectionRoles
            .AsNoTracking()
            .AnyAsync(r => r.Id == roleId && r.TenantId == tenantId, ct);

    private async Task<DataProtectionRoleDetailModel> MapDetailAsync(DataProtectionRole role, CancellationToken ct)
    {
        var userLabels = await LoadUserLabelsAsync(role.TenantId, ct);
        return new DataProtectionRoleDetailModel
        {
            Role = role,
            LinkedUserLabel = GetUserLabel(role.LinkedUser, role.LinkedUserId, userLabels),
            ReportsToRoleDisplay = BuildHierarchyDisplay(role.ReportsTo, role.ReportsToRole),
            DeputyRoleDisplay = BuildHierarchyDisplay(role.Deputy, role.DeputyRole),
            CreatedByLabel = GetUserLabelById(role.CreatedByUserId, userLabels),
            UpdatedByLabel = GetUserLabelById(role.UpdatedByUserId, userLabels)
        };
    }

    private DataProtectionRoleListItem MapListItem(DataProtectionRole role) => new()
    {
        Role = role,
        LinkedUserLabel = GetUserLabel(role.LinkedUser, role.LinkedUserId, null),
        ReportsToRoleLabel = BuildHierarchyDisplay(role.ReportsTo, role.ReportsToRole),
        DeputyRoleLabel = BuildHierarchyDisplay(role.Deputy, role.DeputyRole)
    };

    private async Task<Dictionary<string, string>> LoadUserLabelsAsync(int tenantId, CancellationToken ct)
    {
        var userIds = await db.UserTenants
            .AsNoTracking()
            .Where(ut => ut.TenantId == tenantId)
            .Select(ut => ut.UserId)
            .ToListAsync(ct);

        return await db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(
                u => u.Id,
                u => string.IsNullOrWhiteSpace(u.DisplayName) ? u.Email ?? u.Id : u.DisplayName,
                ct);
    }

    private static string? GetUserLabelById(string? userId, IReadOnlyDictionary<string, string> labels) =>
        userId is not null && labels.TryGetValue(userId, out var label) ? label : null;

    private static string? GetUserLabel(ApplicationUser? user, string? userId, IReadOnlyDictionary<string, string>? labels)
    {
        if (user is not null)
        {
            return string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email ?? user.Id : user.DisplayName;
        }

        if (userId is not null && labels is not null && labels.TryGetValue(userId, out var label))
        {
            return label;
        }

        return null;
    }

    public static string? BuildHierarchyDisplay(string? freeText, DataProtectionRole? linkedRole)
    {
        if (linkedRole is not null)
        {
            var person = string.IsNullOrWhiteSpace(linkedRole.PersonName) ? null : linkedRole.PersonName.Trim();
            return person is null
                ? linkedRole.RoleTitle
                : $"{linkedRole.RoleTitle} – {person}";
        }

        return string.IsNullOrWhiteSpace(freeText) ? null : freeText.Trim();
    }

    private static string? BuildHierarchyExportText(string? freeText, DataProtectionRole? linkedRole) =>
        BuildHierarchyDisplay(freeText, linkedRole);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class TenantUserOption
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsActive { get; init; }
}
