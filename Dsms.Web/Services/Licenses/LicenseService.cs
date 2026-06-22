using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceProviderEntity = Dsms.Web.Domain.Entities.ServiceProvider;

namespace Dsms.Web.Services.Licenses;

public sealed partial class LicenseService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IUserAccessService access,
    ICurrentUserContext currentUser,
    ILogService logService) : ILicenseService
{
    public async Task<IReadOnlyList<LicenseListItemDto>> GetAllLicensesWithUsageAsync(
        string? search = null,
        string? statusFilter = null,
        string sortBy = "CreatedAt",
        bool sortDescending = true)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var query = db.Licenses.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(l =>
                l.LicenseNumber.Contains(term)
                || l.CustomerName.Contains(term)
                || (l.CustomerEmail != null && l.CustomerEmail.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(l => l.Status == statusFilter);
        }

        query = sortBy switch
        {
            "CustomerName" => sortDescending
                ? query.OrderByDescending(l => l.CustomerName)
                : query.OrderBy(l => l.CustomerName),
            "LicenseNumber" => sortDescending
                ? query.OrderByDescending(l => l.LicenseNumber)
                : query.OrderBy(l => l.LicenseNumber),
            _ => sortDescending
                ? query.OrderByDescending(l => l.CreatedAt)
                : query.OrderBy(l => l.CreatedAt)
        };

        var licenses = await query.ToListAsync();
        var result = new List<LicenseListItemDto>();

        foreach (var license in licenses)
        {
            var usage = await BuildUsageAsync(db, license.Id);
            result.Add(new LicenseListItemDto
            {
                Id = license.Id,
                LicenseNumber = license.LicenseNumber,
                CustomerName = license.CustomerName,
                CustomerEmail = license.CustomerEmail,
                PlanName = license.PlanName,
                Status = license.Status,
                ValidUntil = license.ValidUntil,
                CreatedAt = license.CreatedAt,
                MaxTenants = license.MaxTenants,
                MaxAdmins = license.MaxAdmins,
                MaxUsersPerTenant = license.MaxUsersPerTenant,
                MaxAuditorsPerTenant = license.MaxAuditorsPerTenant,
                MaxActiveAuditsPerTenant = license.MaxActiveAuditsPerTenant,
                MaxActiveMeasuresPerTenant = license.MaxActiveMeasuresPerTenant,
                Usage = usage,
                Usability = LicenseLimitHelper.EvaluateUsability(license.Status, license.ValidUntil)
            });
        }

        return result;
    }

    public async Task<LicenseDetailsDto?> GetLicenseDetailsAsync(Guid licenseId)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var license = await db.Licenses.AsNoTracking().FirstOrDefaultAsync(l => l.Id == licenseId);
        if (license is null)
        {
            return null;
        }

        var usage = await BuildUsageAsync(db, licenseId);
        return MapToDetails(license, usage);
    }

    public async Task<LicenseUsageDto> GetUsageAsync(Guid licenseId)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        return await BuildUsageAsync(db, licenseId);
    }

    public async Task<Guid> CreateLicenseAsync(LicenseEditDto dto)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var license = new License
        {
            Id = Guid.NewGuid(),
            LicenseNumber = await LicenseNumberGenerator.GenerateAsync(db),
            CustomerName = dto.CustomerName.Trim(),
            CustomerEmail = NormalizeOptional(dto.CustomerEmail),
            PlanName = string.IsNullOrWhiteSpace(dto.PlanName) ? "Manual" : dto.PlanName.Trim(),
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status.Trim(),
            ValidFrom = dto.ValidFrom,
            ValidUntil = dto.ValidUntil,
            InternalNote = NormalizeOptional(dto.InternalNote),
            CreatedAt = DateTime.UtcNow,
            MaxTenants = dto.MaxTenants,
            MaxAdmins = dto.MaxAdmins,
            MaxUsersPerTenant = dto.MaxUsersPerTenant,
            MaxAuditorsPerTenant = dto.MaxAuditorsPerTenant,
            MaxCustomAuditTemplatesPerTenant = dto.MaxCustomAuditTemplatesPerTenant,
            MaxActiveAuditsPerTenant = dto.MaxActiveAuditsPerTenant,
            MaxProcessingActivitiesPerTenant = dto.MaxProcessingActivitiesPerTenant,
            MaxDpiaPerTenant = dto.MaxDpiaPerTenant,
            MaxTomsPerTenant = dto.MaxTomsPerTenant,
            MaxProcessorsPerTenant = dto.MaxProcessorsPerTenant,
            MaxActiveMeasuresPerTenant = dto.MaxActiveMeasuresPerTenant,
            MaxStorageMb = dto.MaxStorageMb,
            MaxEmailRemindersPerMonth = dto.MaxEmailRemindersPerMonth,
            HasTrainingModule = dto.HasTrainingModule,
            TrainingModuleStatus = dto.HasTrainingModule ? TrainingModuleStatus.Unlimited : TrainingModuleStatus.Disabled,
            TrainingModuleUnlimited = dto.HasTrainingModule
        };

        db.Licenses.Add(license);
        await db.SaveChangesAsync();

        await logService.LogAuditAsync(
            action: "LicenseCreated",
            description: "Lizenz wurde erstellt.",
            entityType: "License",
            entityId: license.Id.ToString(),
            entityName: license.LicenseNumber,
            licenseId: license.Id,
            newValues: MapLicenseSnapshot(license));

        return license.Id;
    }

    public async Task<bool> UpdateLicenseAsync(Guid licenseId, LicenseEditDto dto)
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        var license = await db.Licenses.FirstOrDefaultAsync(l => l.Id == licenseId);
        if (license is null)
        {
            return false;
        }

        var oldHasTrainingModule = license.HasTrainingModule;
        var oldSnapshot = MapLicenseSnapshot(license);
        var oldLimits = ExtractLimits(license);
        var oldStatus = license.Status;

        license.CustomerName = dto.CustomerName.Trim();
        license.CustomerEmail = NormalizeOptional(dto.CustomerEmail);
        license.PlanName = string.IsNullOrWhiteSpace(dto.PlanName) ? "Manual" : dto.PlanName.Trim();
        license.Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status.Trim();
        license.ValidFrom = dto.ValidFrom;
        license.ValidUntil = dto.ValidUntil;
        license.InternalNote = NormalizeOptional(dto.InternalNote);
        license.UpdatedAt = DateTime.UtcNow;
        license.MaxTenants = dto.MaxTenants;
        license.MaxAdmins = dto.MaxAdmins;
        license.MaxUsersPerTenant = dto.MaxUsersPerTenant;
        license.MaxAuditorsPerTenant = dto.MaxAuditorsPerTenant;
        license.MaxCustomAuditTemplatesPerTenant = dto.MaxCustomAuditTemplatesPerTenant;
        license.MaxActiveAuditsPerTenant = dto.MaxActiveAuditsPerTenant;
        license.MaxProcessingActivitiesPerTenant = dto.MaxProcessingActivitiesPerTenant;
        license.MaxDpiaPerTenant = dto.MaxDpiaPerTenant;
        license.MaxTomsPerTenant = dto.MaxTomsPerTenant;
        license.MaxProcessorsPerTenant = dto.MaxProcessorsPerTenant;
        license.MaxActiveMeasuresPerTenant = dto.MaxActiveMeasuresPerTenant;
        license.MaxStorageMb = dto.MaxStorageMb;
        license.MaxEmailRemindersPerMonth = dto.MaxEmailRemindersPerMonth;
        license.HasTrainingModule = dto.HasTrainingModule;
        license.TrainingModuleStatus = dto.HasTrainingModule
            ? TrainingModuleStatus.Unlimited
            : TrainingModuleStatus.Disabled;
        license.TrainingModuleUnlimited = dto.HasTrainingModule;

        await db.SaveChangesAsync();

        var newSnapshot = MapLicenseSnapshot(license);
        await logService.LogAuditAsync(
            action: "LicenseUpdated",
            description: "Lizenz wurde geändert.",
            entityType: "License",
            entityId: license.Id.ToString(),
            entityName: license.LicenseNumber,
            licenseId: license.Id,
            oldValues: oldSnapshot,
            newValues: newSnapshot);

        if (!string.Equals(oldStatus, license.Status, StringComparison.OrdinalIgnoreCase))
        {
            await logService.LogAuditAsync(
                action: "LicenseStatusChanged",
                description: "Lizenzstatus wurde geändert.",
                entityType: "License",
                entityId: license.Id.ToString(),
                entityName: license.LicenseNumber,
                licenseId: license.Id,
                oldValues: new { Status = oldStatus },
                newValues: new { Status = license.Status });
        }

        var newLimits = ExtractLimits(license);
        if (!LimitsEqual(oldLimits, newLimits))
        {
            await logService.LogAuditAsync(
                action: "LicenseLimitChanged",
                description: "Lizenzlimits wurden geändert.",
                entityType: "License",
                entityId: license.Id.ToString(),
                entityName: license.LicenseNumber,
                licenseId: license.Id,
                oldValues: oldLimits,
                newValues: newLimits);
        }

        if (oldHasTrainingModule != license.HasTrainingModule)
        {
            var actionText = license.HasTrainingModule ? "aktiviert" : "deaktiviert";
            await logService.LogAuditAsync(
                action: license.HasTrainingModule ? "LicenseTrainingModuleEnabled" : "LicenseTrainingModuleDisabled",
                description: $"Schulungsmodul für Mandant „{license.CustomerName}“ {actionText}.",
                entityType: "License",
                entityId: license.Id.ToString(),
                entityName: license.LicenseNumber,
                licenseId: license.Id,
                oldValues: new { HasTrainingModule = oldHasTrainingModule },
                newValues: new { HasTrainingModule = license.HasTrainingModule });
        }

        return true;
    }

    private static object MapLicenseSnapshot(License license) => new
    {
        license.CustomerName,
        license.CustomerEmail,
        license.PlanName,
        license.Status,
        license.ValidFrom,
        license.ValidUntil,
        license.MaxTenants,
        license.MaxAdmins,
        license.MaxUsersPerTenant,
        license.MaxAuditorsPerTenant,
        license.MaxCustomAuditTemplatesPerTenant,
        license.MaxActiveAuditsPerTenant,
        license.MaxProcessingActivitiesPerTenant,
        license.MaxDpiaPerTenant,
        license.MaxTomsPerTenant,
        license.MaxProcessorsPerTenant,
        license.MaxActiveMeasuresPerTenant,
        license.MaxStorageMb,
        license.MaxEmailRemindersPerMonth,
        license.HasTrainingModule
    };

    private static object ExtractLimits(License license) => new
    {
        license.MaxTenants,
        license.MaxAdmins,
        license.MaxUsersPerTenant,
        license.MaxAuditorsPerTenant,
        license.MaxCustomAuditTemplatesPerTenant,
        license.MaxActiveAuditsPerTenant,
        license.MaxProcessingActivitiesPerTenant,
        license.MaxDpiaPerTenant,
        license.MaxTomsPerTenant,
        license.MaxProcessorsPerTenant,
        license.MaxActiveMeasuresPerTenant,
        license.MaxStorageMb,
        license.MaxEmailRemindersPerMonth
    };

    private static bool LimitsEqual(object oldLimits, object newLimits) =>
        string.Equals(
            LogJsonHelper.SerializeSafe(oldLimits),
            LogJsonHelper.SerializeSafe(newLimits),
            StringComparison.Ordinal);

    private async Task<LicenseUsageDto> BuildUsageAsync(ApplicationDbContext db, Guid licenseId)
    {
        var tenants = await db.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.LicenseId == licenseId && t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync();

        var tenantIds = tenants.Select(t => t.Id).ToList();

        var roleIds = await db.Roles
            .Where(r => r.Name == DsmsRoles.Admin || r.Name == DsmsRoles.Superuser
                || r.Name == DsmsRoles.User || r.Name == DsmsRoles.Auditor)
            .ToDictionaryAsync(r => r.Name!, r => r.Id);

        var currentAdmins = await CountLicenseAdminsAsync(db, licenseId, roleIds);
        var usersByTenant = await CountUsersByRolePerTenantAsync(db, tenantIds, roleIds[DsmsRoles.User], roleIds[DsmsRoles.Superuser]);
        var auditorsByTenant = await CountUsersByRolePerTenantAsync(db, tenantIds, roleIds[DsmsRoles.Auditor], roleIds[DsmsRoles.Superuser]);

        var templatesByTenant = tenantIds.Count == 0
            ? new Dictionary<int, int>()
            : await db.AuditTemplates
                .IgnoreQueryFilters()
                .Where(t => t.TenantId != null
                    && tenantIds.Contains(t.TenantId.Value)
                    && !t.IsArchived
                    && t.TemplateType == AuditTemplateType.Tenant)
                .GroupBy(t => t.TenantId!.Value)
                .Select(g => new { TenantId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TenantId, x => x.Count);

        var activeAuditsByTenant = tenantIds.Count == 0
            ? new Dictionary<int, int>()
            : await db.AuditRuns
                .IgnoreQueryFilters()
                .Where(r => tenantIds.Contains(r.TenantId)
                    && !r.IsArchived
                    && (r.Status == AuditRunStatus.Draft || r.Status == AuditRunStatus.InProgress))
                .GroupBy(r => r.TenantId)
                .Select(g => new { TenantId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TenantId, x => x.Count);

        var processingByTenant = await CountArchivablePerTenantAsync<ProcessingActivity>(db, tenantIds);
        var dpiaByTenant = await CountArchivablePerTenantAsync<DataProtectionImpactAssessment>(db, tenantIds);
        var tomsByTenant = await CountArchivablePerTenantAsync<Tom>(db, tenantIds);
        var processorsByTenant = await CountArchivablePerTenantAsync<ServiceProviderEntity>(db, tenantIds);

        var activeMeasuresByTenant = tenantIds.Count == 0
            ? new Dictionary<int, int>()
            : await db.Measures
                .IgnoreQueryFilters()
                .Where(m => tenantIds.Contains(m.TenantId)
                    && !m.IsArchived
                    && m.Status != MeasureStatus.Done
                    && m.Status != MeasureStatus.Cancelled)
                .GroupBy(m => m.TenantId)
                .Select(g => new { TenantId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TenantId, x => x.Count);

        var storageBytes = tenantIds.Count == 0
            ? 0L
            : await db.EvidenceDocuments
                .IgnoreQueryFilters()
                .Where(d => tenantIds.Contains(d.TenantId) && !d.IsArchived)
                .SumAsync(d => (long?)d.FileSizeBytes) ?? 0L;

        // TODO: E-Mail-Erinnerungen pro Monat zählen, sobald Reminder-Logs/Entität vorhanden sind.
        var emailRemindersThisMonth = 0;

        var tenantUsages = tenants.Select(t => new TenantUsageDto
        {
            TenantId = t.Id,
            TenantName = t.Name,
            CurrentUsers = usersByTenant.GetValueOrDefault(t.Id),
            CurrentAuditors = auditorsByTenant.GetValueOrDefault(t.Id),
            CurrentCustomAuditTemplates = templatesByTenant.GetValueOrDefault(t.Id),
            CurrentActiveAudits = activeAuditsByTenant.GetValueOrDefault(t.Id),
            CurrentProcessingActivities = processingByTenant.GetValueOrDefault(t.Id),
            CurrentDpia = dpiaByTenant.GetValueOrDefault(t.Id),
            CurrentToms = tomsByTenant.GetValueOrDefault(t.Id),
            CurrentProcessors = processorsByTenant.GetValueOrDefault(t.Id),
            CurrentActiveMeasures = activeMeasuresByTenant.GetValueOrDefault(t.Id)
        }).ToList();

        return new LicenseUsageDto
        {
            LicenseId = licenseId,
            CurrentTenants = tenants.Count,
            CurrentAdmins = currentAdmins,
            CurrentUsersTotal = tenantUsages.Sum(t => t.CurrentUsers),
            CurrentAuditorsTotal = tenantUsages.Sum(t => t.CurrentAuditors),
            CurrentCustomAuditTemplatesTotal = tenantUsages.Sum(t => t.CurrentCustomAuditTemplates),
            CurrentActiveAuditsTotal = tenantUsages.Sum(t => t.CurrentActiveAudits),
            CurrentProcessingActivitiesTotal = tenantUsages.Sum(t => t.CurrentProcessingActivities),
            CurrentDpiaTotal = tenantUsages.Sum(t => t.CurrentDpia),
            CurrentTomsTotal = tenantUsages.Sum(t => t.CurrentToms),
            CurrentProcessorsTotal = tenantUsages.Sum(t => t.CurrentProcessors),
            CurrentActiveMeasuresTotal = tenantUsages.Sum(t => t.CurrentActiveMeasures),
            CurrentStorageMb = (int)Math.Ceiling(storageBytes / (1024.0 * 1024.0)),
            CurrentEmailRemindersThisMonth = emailRemindersThisMonth,
            Tenants = tenantUsages
        };
    }

    private static async Task<int> CountLicenseAdminsAsync(
        ApplicationDbContext db,
        Guid licenseId,
        Dictionary<string, string> roleIds)
    {
        var adminRoleId = roleIds[DsmsRoles.Admin];
        var superuserRoleId = roleIds[DsmsRoles.Superuser];

        return await db.Users
            .Where(u => u.LicenseId == licenseId && u.IsActive)
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == adminRoleId))
            .Where(u => !db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == superuserRoleId))
            .CountAsync();
    }

    private static async Task<Dictionary<int, int>> CountUsersByRolePerTenantAsync(
        ApplicationDbContext db,
        IReadOnlyList<int> tenantIds,
        string roleId,
        string superuserRoleId)
    {
        if (tenantIds.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        var fromUserTenants = await (
            from ut in db.UserTenants.IgnoreQueryFilters()
            join u in db.Users on ut.UserId equals u.Id
            where tenantIds.Contains(ut.TenantId)
                && u.IsActive
                && db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId)
                && !db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == superuserRoleId)
            select new { ut.TenantId, u.Id }
        ).ToListAsync();

        var fromLegacy = await (
            from u in db.Users
            where u.TenantId != null
                && tenantIds.Contains(u.TenantId.Value)
                && u.IsActive
                && db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId)
                && !db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == superuserRoleId)
            select new { TenantId = u.TenantId!.Value, u.Id }
        ).ToListAsync();

        return fromUserTenants
            .Concat(fromLegacy)
            .GroupBy(x => x.TenantId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Id).Distinct().Count());
    }

    private static async Task<Dictionary<int, int>> CountArchivablePerTenantAsync<TEntity>(
        ApplicationDbContext db,
        IReadOnlyList<int> tenantIds)
        where TEntity : ArchivableEntityBase, ITenantEntity
    {
        if (tenantIds.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        return await db.Set<TEntity>()
            .IgnoreQueryFilters()
            .Where(e => tenantIds.Contains(e.TenantId) && !e.IsArchived)
            .GroupBy(e => e.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count);
    }

    private static LicenseDetailsDto MapToDetails(License license, LicenseUsageDto usage) => new()
    {
        Id = license.Id,
        LicenseNumber = license.LicenseNumber,
        CustomerName = license.CustomerName,
        CustomerEmail = license.CustomerEmail,
        PlanName = license.PlanName,
        Status = license.Status,
        ValidFrom = license.ValidFrom,
        ValidUntil = license.ValidUntil,
        InternalNote = license.InternalNote,
        CreatedAt = license.CreatedAt,
        UpdatedAt = license.UpdatedAt,
        MaxTenants = license.MaxTenants,
        MaxAdmins = license.MaxAdmins,
        MaxUsersPerTenant = license.MaxUsersPerTenant,
        MaxAuditorsPerTenant = license.MaxAuditorsPerTenant,
        MaxCustomAuditTemplatesPerTenant = license.MaxCustomAuditTemplatesPerTenant,
        MaxActiveAuditsPerTenant = license.MaxActiveAuditsPerTenant,
        MaxProcessingActivitiesPerTenant = license.MaxProcessingActivitiesPerTenant,
        MaxDpiaPerTenant = license.MaxDpiaPerTenant,
        MaxTomsPerTenant = license.MaxTomsPerTenant,
        MaxProcessorsPerTenant = license.MaxProcessorsPerTenant,
        MaxActiveMeasuresPerTenant = license.MaxActiveMeasuresPerTenant,
        MaxStorageMb = license.MaxStorageMb,
        MaxEmailRemindersPerMonth = license.MaxEmailRemindersPerMonth,
        HasTrainingModule = license.HasTrainingModule,
        Usage = usage,
        Usability = LicenseLimitHelper.EvaluateUsability(license.Status, license.ValidUntil)
    };

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async Task<IReadOnlyList<LicenseOptionDto>> GetActiveLicenseOptionsAsync()
    {
        await EnsureSuperuserAsync();
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.Licenses
            .AsNoTracking()
            .Where(l => l.Status == "Active")
            .OrderBy(l => l.CustomerName)
            .ThenBy(l => l.LicenseNumber)
            .Select(l => new LicenseOptionDto
            {
                Id = l.Id,
                LicenseNumber = l.LicenseNumber,
                CustomerName = l.CustomerName,
                PlanName = l.PlanName,
                Status = l.Status,
                DisplayName = l.LicenseNumber + " - " + l.CustomerName + " - " + l.PlanName
            })
            .ToListAsync();
    }

    private async Task EnsureSuperuserAsync()
    {
        if (!await access.IsSuperuserAsync())
        {
            throw new UnauthorizedAccessException("Keine Berechtigung für die Lizenzverwaltung.");
        }
    }
}
