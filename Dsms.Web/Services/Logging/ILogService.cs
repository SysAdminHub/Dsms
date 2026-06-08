using Dsms.Web.Data;
using Dsms.Web.Services.Licenses;

namespace Dsms.Web.Services.Logging;

/// <summary>
/// Zentraler Logging-Service für Audit- und Systemprotokolle.
///
/// Beispiele für manuelle Verwendung im Code:
///
/// // Auditlog beim Erstellen einer DSFA:
/// await logService.LogAuditAsync(
///     action: "DpiaCreated",
///     description: "DSFA wurde erstellt.",
///     entityType: "Dpia",
///     entityId: dpia.Id.ToString(),
///     entityName: dpia.Title,
///     tenantId: tenantId,
///     licenseId: licenseId);
///
/// // Systemfehler bei E-Mail-Versand:
/// await logService.LogSystemErrorAsync(
///     action: "EmailSendFailed",
///     description: "E-Mail konnte nicht versendet werden.",
///     exception: ex,
///     tenantId: tenantId,
///     licenseId: licenseId,
///     metadata: new { Recipient = recipientEmail });
/// </summary>
public interface ILogService
{
    Task LogAuditAsync(
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
        bool isVisibleToAdmin = true);

    Task LogSystemAsync(
        string action,
        string description,
        string severity = "Info",
        string? entityType = null,
        string? entityId = null,
        int? tenantId = null,
        Guid? licenseId = null,
        object? metadata = null);

    Task LogSystemErrorAsync(
        string action,
        string description,
        Exception exception,
        string? entityType = null,
        string? entityId = null,
        int? tenantId = null,
        Guid? licenseId = null,
        object? metadata = null);

    Task LogSecurityAsync(
        string action,
        string description,
        string severity = "Warning",
        string? userEmail = null,
        int? tenantId = null,
        Guid? licenseId = null,
        object? metadata = null);

    Task LogBlockedCreationAsync(LicenseLimitCheckResult check, string? entityType = null);

    Task LogLoginSuccessfulAsync(ApplicationUser user, string loginType = "Password");

    Task LogLoginFailedAsync(string? email, string reason);
}
