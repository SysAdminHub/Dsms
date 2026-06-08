namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Zentraler Protokolleintrag für Audit- und Systemlogs.
/// Kategorie über <see cref="LogCategory"/>; Sichtbarkeit für Admins über <see cref="IsVisibleToAdmin"/>.
/// </summary>
public class LogEntry
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string LogCategory { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? EntityName { get; set; }

    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserDisplayName { get; set; }

    public Guid? LicenseId { get; set; }
    public string? LicenseNumber { get; set; }

    public int? TenantId { get; set; }
    public string? TenantName { get; set; }

    public string? IpAddressAnonymized { get; set; }
    public string? UserAgent { get; set; }

    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? MetadataJson { get; set; }

    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? ExceptionDetails { get; set; }

    public bool IsVisibleToAdmin { get; set; }

    public string? CorrelationId { get; set; }
    public string? RequestPath { get; set; }
    public string? Source { get; set; }
}
