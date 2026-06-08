namespace Dsms.Web.Services.Logging;

public interface ILogQueryService
{
    Task<LogQueryResult> GetPlatformLogsAsync(LogQueryFilter filter);

    Task<LogEntryDetailsDto?> GetPlatformLogDetailsAsync(Guid logId);

    Task<AdminAuditLogQueryResult> GetAdminAuditLogsAsync(LogQueryFilter filter);

    Task<LogEntryDetailsDto?> GetAdminAuditLogDetailsAsync(Guid logId);
}
