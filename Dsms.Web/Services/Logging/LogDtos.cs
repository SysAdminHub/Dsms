namespace Dsms.Web.Services.Logging;

public sealed class LogEntryListItemDto
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public string LogCategory { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? UserDisplayName { get; init; }
    public string? UserEmail { get; init; }
    public string? LicenseNumber { get; init; }
    public string? TenantName { get; init; }
    public string? EntityType { get; init; }
    public string? EntityName { get; init; }
}

public sealed class LogEntryDetailsDto
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public string LogCategory { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public string? EntityName { get; init; }

    public string? UserId { get; init; }
    public string? UserEmail { get; init; }
    public string? UserDisplayName { get; init; }

    public Guid? LicenseId { get; init; }
    public string? LicenseNumber { get; init; }

    public int? TenantId { get; init; }
    public string? TenantName { get; init; }

    public string? IpAddressAnonymized { get; init; }
    public string? UserAgent { get; init; }
    public string? RequestPath { get; init; }
    public string? CorrelationId { get; init; }
    public string? Source { get; init; }

    public string? OldValuesJson { get; init; }
    public string? NewValuesJson { get; init; }
    public string? MetadataJson { get; init; }

    public string? ExceptionType { get; init; }
    public string? ExceptionMessage { get; init; }
    public string? ExceptionDetails { get; init; }

    public bool IsVisibleToAdmin { get; init; }
}

public sealed class LogQueryFilter
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Category { get; set; }
    public string? Severity { get; set; }
    public string? UserEmail { get; set; }
    public Guid? LicenseId { get; set; }
    public int? TenantId { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    public string? SearchText { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed class LogQueryResult
{
    public IReadOnlyList<LogEntryListItemDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public sealed class AdminAuditLogQueryResult
{
    public AdminAuditLogStatus Status { get; init; }
    public string? Message { get; init; }
    public LogQueryResult? Data { get; init; }
    public IReadOnlyList<TenantFilterOption> Tenants { get; init; } = [];
}

public enum AdminAuditLogStatus
{
    Success,
    NoLicenseAssigned,
    LicenseNotFound,
    Unauthorized
}

public sealed class TenantFilterOption
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
