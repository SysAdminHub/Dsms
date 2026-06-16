using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.Licenses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Logging;

public sealed class LogService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IHttpContextAccessor httpContextAccessor,
    ICurrentUserContext currentUser,
    UserManager<ApplicationUser> userManager,
    ISupportContextService supportContext,
    ITenantContextService tenantContext,
    ILogger<LogService> logger) : ILogService
{
    private const int MaxExceptionDetailsLength = 4000;
    private const int MaxUserAgentLength = 500;
    private const int MaxRequestPathLength = 500;

    public Task LogAuditAsync(
        string action,
        string description,
        string? entityType = null,
        string? entityId = null,
        string? entityName = null,
        int? tenantId = null,
        Guid? licenseId = null,
        object? oldValues = null,
        object? newValues = null,
        object? metadata = null,
        bool isVisibleToAdmin = true) =>
        WriteEntryAsync(new LogWriteRequest
        {
            LogCategory = LogCategory.Audit,
            Severity = LogSeverity.Info,
            Action = action,
            Description = description,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            TenantId = tenantId,
            LicenseId = licenseId,
            OldValues = oldValues,
            NewValues = newValues,
            Metadata = metadata,
            IsVisibleToAdmin = isVisibleToAdmin
        });

    public Task LogSystemAsync(
        string action,
        string description,
        string severity = "Info",
        string? entityType = null,
        string? entityId = null,
        int? tenantId = null,
        Guid? licenseId = null,
        object? metadata = null) =>
        WriteEntryAsync(new LogWriteRequest
        {
            LogCategory = LogCategory.System,
            Severity = ParseSeverity(severity, LogSeverity.Info),
            Action = action,
            Description = description,
            EntityType = entityType,
            EntityId = entityId,
            TenantId = tenantId,
            LicenseId = licenseId,
            Metadata = metadata,
            IsVisibleToAdmin = false
        });

    public Task LogSystemErrorAsync(
        string action,
        string description,
        Exception exception,
        string? entityType = null,
        string? entityId = null,
        int? tenantId = null,
        Guid? licenseId = null,
        object? metadata = null) =>
        WriteEntryAsync(new LogWriteRequest
        {
            LogCategory = LogCategory.System,
            Severity = LogSeverity.Error,
            Action = action,
            Description = description,
            EntityType = entityType,
            EntityId = entityId,
            TenantId = tenantId,
            LicenseId = licenseId,
            Metadata = metadata,
            IsVisibleToAdmin = false,
            Exception = exception
        });

    public Task LogSecurityAsync(
        string action,
        string description,
        string severity = "Warning",
        string? userEmail = null,
        int? tenantId = null,
        Guid? licenseId = null,
        object? metadata = null) =>
        WriteEntryAsync(new LogWriteRequest
        {
            LogCategory = LogCategory.Security,
            Severity = ParseSeverity(severity, LogSeverity.Warning),
            Action = action,
            Description = description,
            TenantId = tenantId,
            LicenseId = licenseId,
            Metadata = metadata,
            IsVisibleToAdmin = false,
            OverrideUserEmail = userEmail
        });

    public Task LogBlockedCreationAsync(LicenseLimitCheckResult check, string? entityType = null)
    {
        if (check.IsAllowed)
        {
            return Task.CompletedTask;
        }

        var action = MapBlockReasonToAction(check.BlockReason);
        return LogAuditAsync(
            action: action,
            description: "Erstellung wurde durch die Lizenzprüfung blockiert.",
            entityType: entityType ?? check.LimitName,
            tenantId: check.TenantId,
            licenseId: check.LicenseId,
            metadata: new
            {
                Reason = check.BlockReason.ToString(),
                check.LimitName,
                check.CurrentValue,
                check.LimitValue,
                EntityType = entityType,
                check.TenantId,
                check.LicenseId,
                check.LicenseStatus,
                check.ValidUntil,
                check.Message
            },
            isVisibleToAdmin: true);
    }

    public async Task LogLoginSuccessfulAsync(ApplicationUser user, string loginType = "Password")
    {
        var roles = await userManager.GetRolesAsync(user);
        var context = await ResolveLoginContextAsync(user, roles);

        await WriteEntryAsync(new LogWriteRequest
        {
            LogCategory = LogCategory.Audit,
            Severity = LogSeverity.Info,
            Action = "UserLoginSuccessful",
            Description = "Benutzer hat sich angemeldet.",
            EntityType = "ApplicationUser",
            EntityId = user.Id,
            EntityName = user.Email,
            TenantId = context.TenantId,
            LicenseId = context.LicenseId,
            Metadata = new
            {
                LoginType = loginType,
                TenantCount = context.TenantCount,
                MultipleTenants = context.TenantCount > 1 ? "Mehrere Mandanten" : null
            },
            IsVisibleToAdmin = false,
            OverrideUserId = user.Id,
            OverrideUserEmail = user.Email,
            OverrideUserDisplayName = user.DisplayName,
            OverrideLicenseNumber = context.LicenseNumber,
            OverrideTenantName = context.TenantName
        });
    }

    public Task LogLoginFailedAsync(string? email, string reason) =>
        LogSecurityAsync(
            action: "UserLoginFailed",
            description: "Anmeldung fehlgeschlagen.",
            severity: "Warning",
            userEmail: email,
            metadata: new { Reason = reason });

    private async Task WriteEntryAsync(LogWriteRequest request)
    {
        try
        {
            request = await EnrichSupportModeAuditAsync(request);

            var httpContext = httpContextAccessor.HttpContext;
            var userId = request.OverrideUserId ?? await currentUser.GetUserIdAsync();
            ApplicationUser? user = null;

            if (request.OverrideUserId is not null)
            {
                user = await userManager.FindByIdAsync(request.OverrideUserId);
            }
            else if (userId is not null)
            {
                user = await userManager.FindByIdAsync(userId);
            }

            var tenantId = request.TenantId;
            var licenseId = request.LicenseId;

            if (!tenantId.HasValue)
            {
                tenantId = await currentUser.GetTenantIdAsync();
            }

            await using var db = await dbFactory.CreateDbContextAsync();

            string? tenantName = request.OverrideTenantName;
            if (tenantId.HasValue && tenantName is null)
            {
                tenantName = await db.Tenants
                    .IgnoreQueryFilters()
                    .Where(t => t.Id == tenantId.Value)
                    .Select(t => t.Name)
                    .FirstOrDefaultAsync();
            }

            if (!licenseId.HasValue && tenantId.HasValue)
            {
                licenseId = await db.Tenants
                    .IgnoreQueryFilters()
                    .Where(t => t.Id == tenantId.Value)
                    .Select(t => t.LicenseId)
                    .FirstOrDefaultAsync();
            }

            if (!licenseId.HasValue && user?.LicenseId is Guid userLicenseId)
            {
                licenseId = userLicenseId;
            }

            string? licenseNumber = request.OverrideLicenseNumber;
            if (licenseId.HasValue && licenseNumber is null)
            {
                licenseNumber = await db.Licenses
                    .AsNoTracking()
                    .Where(l => l.Id == licenseId.Value)
                    .Select(l => l.LicenseNumber)
                    .FirstOrDefaultAsync();
            }

            var entry = new LogEntry
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                LogCategory = request.LogCategory.ToString(),
                Severity = request.Severity.ToString(),
                Action = Truncate(request.Action, 200) ?? string.Empty,
                Description = Truncate(request.Description, 2000) ?? string.Empty,
                EntityType = Truncate(request.EntityType, 100),
                EntityId = Truncate(request.EntityId, 100),
                EntityName = Truncate(request.EntityName, 300),
                UserId = request.OverrideUserId ?? userId,
                UserEmail = Truncate(request.OverrideUserEmail ?? user?.Email, 255),
                UserDisplayName = Truncate(request.OverrideUserDisplayName ?? user?.DisplayName, 200),
                LicenseId = licenseId,
                LicenseNumber = Truncate(licenseNumber, 50),
                TenantId = tenantId,
                TenantName = Truncate(tenantName, 200),
                IpAddressAnonymized = LogIpAnonymizer.AnonymizeIpAddress(
                    LogIpAnonymizer.GetClientIpAddress(httpContext)),
                UserAgent = Truncate(httpContext?.Request.Headers.UserAgent.ToString(), MaxUserAgentLength),
                RequestPath = Truncate(httpContext?.Request.Path.Value, MaxRequestPathLength),
                CorrelationId = Truncate(httpContext?.TraceIdentifier, 100),
                Source = Truncate(request.Source, 200),
                OldValuesJson = LogJsonHelper.SerializeSafe(request.OldValues),
                NewValuesJson = LogJsonHelper.SerializeSafe(request.NewValues),
                MetadataJson = LogJsonHelper.SerializeSafe(request.Metadata),
                IsVisibleToAdmin = request.IsVisibleToAdmin
            };

            if (request.Exception is not null)
            {
                entry.ExceptionType = Truncate(request.Exception.GetType().FullName, 200);
                entry.ExceptionMessage = Truncate(request.Exception.Message, 2000);
                entry.ExceptionDetails = Truncate(request.Exception.ToString(), MaxExceptionDetailsLength);
            }

            db.LogEntries.Add(entry);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Logeintrag konnte nicht geschrieben werden (Action: {Action})", request.Action);
        }
    }

    private async Task<LogWriteRequest> EnrichSupportModeAuditAsync(LogWriteRequest request)
    {
        if (request.LogCategory != LogCategory.Audit)
        {
            return request;
        }

        if (!await currentUser.IsInRoleAsync(DsmsRoles.Superuser))
        {
            return request;
        }

        var grantId = await supportContext.GetActiveGrantIdAsync();
        var tenantId = await tenantContext.GetCurrentTenantIdAsync();
        if (!grantId.HasValue || !tenantId.HasValue)
        {
            return request;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var grant = await db.SupportAccessGrants
            .AsNoTracking()
            .Include(g => g.Tenant)
            .FirstOrDefaultAsync(g => g.Id == grantId.Value);

        if (grant is null || !grant.IsActive(DateTime.UtcNow) || grant.TenantId != tenantId.Value)
        {
            return request;
        }

        var tenantName = grant.Tenant?.Name ?? $"Mandant #{grant.TenantId}";

        var description = request.Description.StartsWith("[Supportmodus]", StringComparison.Ordinal)
            ? request.Description
            : $"[Supportmodus] {request.Description}";

        var supportMetadata = new
        {
            SupportMode = true,
            UserRole = DsmsRoles.Superuser,
            SupportAccessGrantId = grant.Id,
            TenantId = grant.TenantId,
            TenantName = tenantName,
            Result = "Success"
        };

        request.Description = description;
        request.Metadata = LogJsonHelper.MergeMetadata(request.Metadata, supportMetadata);
        request.TenantId ??= grant.TenantId;

        return request;
    }

    private async Task<LoginContextInfo> ResolveLoginContextAsync(ApplicationUser user, IList<string> roles)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        if (roles.Contains(DsmsRoles.Superuser))
        {
            return new LoginContextInfo(null, null, null, 0);
        }

        if (roles.Contains(DsmsRoles.Admin) && user.LicenseId is Guid adminLicenseId)
        {
            var licenseNumber = await db.Licenses
                .AsNoTracking()
                .Where(l => l.Id == adminLicenseId)
                .Select(l => l.LicenseNumber)
                .FirstOrDefaultAsync();

            return new LoginContextInfo(null, adminLicenseId, licenseNumber, 0);
        }

        var tenantIds = await db.UserTenants
            .IgnoreQueryFilters()
            .Where(ut => ut.UserId == user.Id)
            .Select(ut => ut.TenantId)
            .ToListAsync();

        if (tenantIds.Count == 0 && user.TenantId is int legacyTenantId)
        {
            tenantIds.Add(legacyTenantId);
        }

        if (tenantIds.Count == 0)
        {
            return new LoginContextInfo(null, user.LicenseId, null, 0);
        }

        if (tenantIds.Count > 1)
        {
            var licenseIds = await db.Tenants
                .IgnoreQueryFilters()
                .Where(t => tenantIds.Contains(t.Id))
                .Select(t => t.LicenseId)
                .Distinct()
                .ToListAsync();

            var singleLicenseId = licenseIds.Count == 1 ? licenseIds[0] : null;
            string? licenseNumber = null;
            if (singleLicenseId is Guid lid)
            {
                licenseNumber = await db.Licenses
                    .AsNoTracking()
                    .Where(l => l.Id == lid)
                    .Select(l => l.LicenseNumber)
                    .FirstOrDefaultAsync();
            }

            return new LoginContextInfo(null, singleLicenseId, licenseNumber, tenantIds.Count);
        }

        var tenantId = tenantIds[0];
        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Id == tenantId)
            .Select(t => new { t.Name, t.LicenseId })
            .FirstOrDefaultAsync();

        string? licNum = null;
        if (tenant?.LicenseId is Guid tid)
        {
            licNum = await db.Licenses
                .AsNoTracking()
                .Where(l => l.Id == tid)
                .Select(l => l.LicenseNumber)
                .FirstOrDefaultAsync();
        }

        return new LoginContextInfo(tenantId, tenant?.LicenseId, licNum, 1)
        {
            TenantName = tenant?.Name
        };
    }

    private static string MapBlockReasonToAction(LicenseBlockReason reason) => reason switch
    {
        LicenseBlockReason.LicenseExpired => "CreateBlockedByLicenseExpired",
        LicenseBlockReason.LicenseInactive => "CreateBlockedByLicenseInactive",
        LicenseBlockReason.LicenseSuspended => "CreateBlockedByLicenseSuspended",
        LicenseBlockReason.LimitReached => "CreateBlockedByLicenseLimit",
        _ => "CreateBlockedByLicenseLimit"
    };

    private static LogSeverity ParseSeverity(string severity, LogSeverity fallback) =>
        Enum.TryParse<LogSeverity>(severity, ignoreCase: true, out var parsed)
            ? parsed
            : fallback;

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private sealed class LogWriteRequest
    {
        public LogCategory LogCategory { get; init; }
        public LogSeverity Severity { get; init; }
        public string Action { get; init; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? EntityType { get; init; }
        public string? EntityId { get; init; }
        public string? EntityName { get; init; }
        public int? TenantId { get; set; }
        public Guid? LicenseId { get; init; }
        public object? OldValues { get; init; }
        public object? NewValues { get; init; }
        public object? Metadata { get; set; }
        public bool IsVisibleToAdmin { get; init; }
        public Exception? Exception { get; init; }
        public string? OverrideUserId { get; init; }
        public string? OverrideUserEmail { get; init; }
        public string? OverrideUserDisplayName { get; init; }
        public string? OverrideLicenseNumber { get; init; }
        public string? OverrideTenantName { get; init; }
        public string? Source { get; init; }
    }

    private sealed class LoginContextInfo(int? tenantId, Guid? licenseId, string? licenseNumber, int tenantCount)
    {
        public int? TenantId { get; } = tenantId;
        public Guid? LicenseId { get; } = licenseId;
        public string? LicenseNumber { get; } = licenseNumber;
        public int TenantCount { get; } = tenantCount;
        public string? TenantName { get; init; }
    }
}
