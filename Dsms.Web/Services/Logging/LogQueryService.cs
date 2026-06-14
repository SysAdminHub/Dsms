using Dsms.Web.Data;
using Dsms.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Logging;

public sealed class LogQueryService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IUserAccessService access,
    ICurrentUserContext currentUser) : ILogQueryService
{
    public async Task<LogQueryResult> GetPlatformLogsAsync(LogQueryFilter filter)
    {
        if (!await access.IsSuperuserAsync())
        {
            throw new UnauthorizedAccessException("Keine Berechtigung für Plattform-Protokolle.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        // LogEntries haben keinen Mandanten-Query-Filter – Superuser sieht alle Audit-Metadaten mandantenübergreifend.
        var query = db.LogEntries.AsNoTracking();
        query = ApplyCommonFilters(query, filter);
        query = ApplyPlatformFilters(query, filter);

        return await ExecutePlatformQueryAsync(query, filter);
    }

    public async Task<LogEntryDetailsDto?> GetPlatformLogDetailsAsync(Guid logId)
    {
        if (!await access.IsSuperuserAsync())
        {
            return null;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var entry = await db.LogEntries.AsNoTracking().FirstOrDefaultAsync(l => l.Id == logId);
        return entry is null ? null : MapToPlatformDetails(entry);
    }

    public async Task<AdminAuditLogQueryResult> GetAdminAuditLogsAsync(LogQueryFilter filter)
    {
        if (!await access.IsTenantAdminAsync())
        {
            return new AdminAuditLogQueryResult
            {
                Status = AdminAuditLogStatus.Unauthorized,
                Message = "Keine Berechtigung für das Auditlog."
            };
        }

        var user = await currentUser.GetUserAsync();
        if (user?.LicenseId is not Guid licenseId)
        {
            return new AdminAuditLogQueryResult
            {
                Status = AdminAuditLogStatus.NoLicenseAssigned,
                Message = "Für Ihr Benutzerkonto ist keine Lizenz zugeordnet. Bitte wenden Sie sich an den Support."
            };
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.Licenses.AnyAsync(l => l.Id == licenseId))
        {
            return new AdminAuditLogQueryResult
            {
                Status = AdminAuditLogStatus.LicenseNotFound,
                Message = "Die zugeordnete Lizenz konnte nicht gefunden werden. Bitte wenden Sie sich an den Support."
            };
        }

        var tenantIds = await db.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.LicenseId == licenseId)
            .Select(t => t.Id)
            .ToListAsync();

        var tenants = await db.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.LicenseId == licenseId)
            .OrderBy(t => t.Name)
            .Select(t => new TenantFilterOption { Id = t.Id, Name = t.Name })
            .ToListAsync();

        var query = db.LogEntries.AsNoTracking()
            .Where(l => l.LogCategory == LogCategory.Audit.ToString())
            .Where(l => l.IsVisibleToAdmin)
            .Where(l => l.LicenseId == licenseId
                || (l.TenantId != null && tenantIds.Contains(l.TenantId.Value)));

        if (filter.TenantId is int tid)
        {
            if (!tenantIds.Contains(tid))
            {
                return new AdminAuditLogQueryResult
                {
                    Status = AdminAuditLogStatus.Unauthorized,
                    Message = "Der ausgewählte Mandant gehört nicht zu Ihrer Lizenz."
                };
            }

            query = query.Where(l => l.TenantId == tid);
        }

        query = ApplyAdminFilters(query, filter);
        var data = await ExecuteAdminQueryAsync(query, filter);

        return new AdminAuditLogQueryResult
        {
            Status = AdminAuditLogStatus.Success,
            Data = data,
            Tenants = tenants
        };
    }

    public async Task<LogEntryDetailsDto?> GetAdminAuditLogDetailsAsync(Guid logId)
    {
        if (!await access.IsTenantAdminAsync())
        {
            return null;
        }

        var user = await currentUser.GetUserAsync();
        if (user?.LicenseId is not Guid licenseId)
        {
            return null;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var tenantIds = await db.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.LicenseId == licenseId)
            .Select(t => t.Id)
            .ToListAsync();

        var entry = await db.LogEntries.AsNoTracking()
            .Where(l => l.Id == logId)
            .Where(l => l.LogCategory == LogCategory.Audit.ToString())
            .Where(l => l.IsVisibleToAdmin)
            .Where(l => l.LicenseId == licenseId
                || (l.TenantId != null && tenantIds.Contains(l.TenantId.Value)))
            .FirstOrDefaultAsync();

        return entry is null ? null : MapToAdminDetails(entry);
    }

    private static IQueryable<Domain.Entities.LogEntry> ApplyCommonFilters(
        IQueryable<Domain.Entities.LogEntry> query,
        LogQueryFilter filter)
    {
        if (filter.From is DateTime from)
        {
            query = query.Where(l => l.CreatedAt >= from.ToUniversalTime());
        }

        if (filter.To is DateTime to)
        {
            var toUtc = to.Date.AddDays(1).ToUniversalTime();
            query = query.Where(l => l.CreatedAt < toUtc);
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            query = query.Where(l => l.LogCategory == filter.Category);
        }

        if (!string.IsNullOrWhiteSpace(filter.Severity))
        {
            query = query.Where(l => l.Severity == filter.Severity);
        }

        if (!string.IsNullOrWhiteSpace(filter.UserEmail))
        {
            var email = filter.UserEmail.Trim();
            query = query.Where(l => l.UserEmail != null && l.UserEmail.Contains(email));
        }

        if (filter.LicenseId is Guid licenseId)
        {
            query = query.Where(l => l.LicenseId == licenseId);
        }

        if (filter.TenantId is int tenantId)
        {
            query = query.Where(l => l.TenantId == tenantId);
        }

        if (!string.IsNullOrWhiteSpace(filter.TenantName))
        {
            var tenantName = filter.TenantName.Trim();
            query = query.Where(l => l.TenantName != null && l.TenantName.Contains(tenantName));
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            query = query.Where(l => l.EntityType == filter.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(l => l.Action == filter.Action);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(l => l.Description.Contains(term));
        }

        return query;
    }

    private static IQueryable<Domain.Entities.LogEntry> ApplyPlatformFilters(
        IQueryable<Domain.Entities.LogEntry> query,
        LogQueryFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Module))
        {
            var entityTypes = AuditLogPresentationHelper.GetEntityTypesForModule(filter.Module);
            if (entityTypes.Count > 0)
            {
                query = query.Where(l => l.EntityType != null && entityTypes.Contains(l.EntityType));
            }
            else
            {
                var module = filter.Module.Trim();
                query = query.Where(l => l.MetadataJson != null && l.MetadataJson.Contains($"\"Module\":\"{module}\""));
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Result))
        {
            var result = filter.Result.Trim();
            query = query.Where(l => l.MetadataJson != null && l.MetadataJson.Contains($"\"Result\":\"{result}\""));
        }

        if (filter.IsSupportMode is bool supportMode)
        {
            query = supportMode
                ? query.Where(l => l.Description.StartsWith("[Supportmodus]")
                    || (l.MetadataJson != null && l.MetadataJson.Contains("\"SupportMode\":true")))
                : query.Where(l => !l.Description.StartsWith("[Supportmodus]")
                    && (l.MetadataJson == null || !l.MetadataJson.Contains("\"SupportMode\":true")));
        }

        return query;
    }

    private static IQueryable<Domain.Entities.LogEntry> ApplyAdminFilters(
        IQueryable<Domain.Entities.LogEntry> query,
        LogQueryFilter filter)
    {
        if (filter.From is DateTime from)
        {
            query = query.Where(l => l.CreatedAt >= from.ToUniversalTime());
        }

        if (filter.To is DateTime to)
        {
            var toUtc = to.Date.AddDays(1).ToUniversalTime();
            query = query.Where(l => l.CreatedAt < toUtc);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            query = query.Where(l => l.EntityType == filter.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(l => l.Action == filter.Action);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(l => l.Description.Contains(term));
        }

        return query;
    }

    private static async Task<LogQueryResult> ExecutePlatformQueryAsync(
        IQueryable<Domain.Entities.LogEntry> query,
        LogQueryFilter filter)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 10, 200);

        query = query.OrderByDescending(l => l.CreatedAt);

        var totalCount = await query.CountAsync();
        var entries = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entries.Select(MapToPlatformListItem).ToList();

        return new LogQueryResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static async Task<LogQueryResult> ExecuteAdminQueryAsync(
        IQueryable<Domain.Entities.LogEntry> query,
        LogQueryFilter filter)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 10, 200);

        query = query.OrderByDescending(l => l.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LogEntryListItemDto
            {
                Id = l.Id,
                CreatedAt = l.CreatedAt,
                LogCategory = l.LogCategory,
                Severity = l.Severity,
                Action = l.Action,
                Description = l.Description,
                UserDisplayName = l.UserDisplayName,
                UserEmail = l.UserEmail,
                LicenseNumber = l.LicenseNumber,
                TenantName = l.TenantName,
                EntityType = l.EntityType,
                EntityName = l.EntityName
            })
            .ToListAsync();

        return new LogQueryResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static LogEntryListItemDto MapToPlatformListItem(Domain.Entities.LogEntry entry) => new()
    {
        Id = entry.Id,
        CreatedAt = entry.CreatedAt,
        LogCategory = entry.LogCategory,
        Severity = entry.Severity,
        Action = entry.Action,
        Description = entry.Description,
        UserDisplayName = entry.UserDisplayName,
        UserEmail = entry.UserEmail,
        LicenseNumber = entry.LicenseNumber,
        TenantName = entry.TenantName,
        TenantId = entry.TenantId,
        EntityType = entry.EntityType,
        EntityName = AuditLogPresentationHelper.RedactEntityNameForPlatform(entry.EntityType, entry.EntityName),
        Module = AuditLogPresentationHelper.ResolveModule(entry.EntityType, entry.MetadataJson)
    };

    private static LogEntryDetailsDto MapToPlatformDetails(Domain.Entities.LogEntry entry) => new()
    {
        Id = entry.Id,
        CreatedAt = entry.CreatedAt,
        LogCategory = entry.LogCategory,
        Severity = entry.Severity,
        Action = entry.Action,
        Description = entry.Description,
        EntityType = entry.EntityType,
        EntityId = entry.EntityId,
        EntityName = AuditLogPresentationHelper.RedactEntityNameForPlatform(entry.EntityType, entry.EntityName),
        UserId = entry.UserId,
        UserEmail = entry.UserEmail,
        UserDisplayName = entry.UserDisplayName,
        LicenseId = entry.LicenseId,
        LicenseNumber = entry.LicenseNumber,
        TenantId = entry.TenantId,
        TenantName = entry.TenantName,
        IpAddressAnonymized = entry.IpAddressAnonymized,
        UserAgent = entry.UserAgent,
        CorrelationId = entry.CorrelationId,
        Module = AuditLogPresentationHelper.ResolveModule(entry.EntityType, entry.MetadataJson),
        Result = AuditLogPresentationHelper.ResolveResult(entry.MetadataJson, entry.Description),
        IsSupportMode = AuditLogPresentationHelper.ResolveIsSupportMode(entry.MetadataJson, entry.Description),
        SupportAccessGrantId = AuditLogPresentationHelper.ResolveSupportAccessGrantId(entry.MetadataJson),
        MetadataJson = SanitizePlatformMetadata(entry.MetadataJson),
        IsVisibleToAdmin = entry.IsVisibleToAdmin,
        IncludeFieldChanges = false
    };

    private static LogEntryDetailsDto MapToAdminDetails(Domain.Entities.LogEntry entry) => new()
    {
        Id = entry.Id,
        CreatedAt = entry.CreatedAt,
        LogCategory = entry.LogCategory,
        Severity = entry.Severity,
        Action = entry.Action,
        Description = entry.Description,
        EntityType = entry.EntityType,
        EntityId = entry.EntityId,
        EntityName = entry.EntityName,
        UserId = entry.UserId,
        UserEmail = entry.UserEmail,
        UserDisplayName = entry.UserDisplayName,
        LicenseId = entry.LicenseId,
        LicenseNumber = entry.LicenseNumber,
        TenantId = entry.TenantId,
        TenantName = entry.TenantName,
        OldValuesJson = entry.OldValuesJson,
        NewValuesJson = entry.NewValuesJson,
        MetadataJson = entry.MetadataJson,
        IsVisibleToAdmin = entry.IsVisibleToAdmin,
        IncludeFieldChanges = true
    };

    private static string? SanitizePlatformMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        if (!AuditLogChangeParser.HasDisplayableMetadata(metadataJson))
        {
            return null;
        }

        return metadataJson;
    }
}
