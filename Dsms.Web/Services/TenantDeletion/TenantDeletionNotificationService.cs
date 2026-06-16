using System.Net;
using System.Text;
using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Email;
using Dsms.Web.Services.Logging;
using Microsoft.AspNetCore.Identity;

namespace Dsms.Web.Services.TenantDeletion;

public interface ITenantDeletionNotificationService
{
    Task<bool> TrySendDeletionRequestedNotificationAsync(
        Tenant tenant,
        string requestedByUserId,
        CancellationToken cancellationToken = default);

    Task<bool> TrySendMarkedForDeletionNotificationAsync(
        Tenant tenant,
        string markedByUserId,
        CancellationToken cancellationToken = default);

    Task<bool> TrySendPermanentDeletionNotificationAsync(
        string tenantName,
        int tenantId,
        Guid? licenseId,
        string deletedByUserId,
        CancellationToken cancellationToken = default);

    Task<bool> TrySendPermanentDeletionFailedNotificationAsync(
        string tenantName,
        int tenantId,
        Guid? licenseId,
        string triggeredByUserId,
        CancellationToken cancellationToken = default);
}

public sealed class TenantDeletionNotificationService(
    IEmailSendingSettingsProvider emailSettingsProvider,
    IEmailService emailService,
    UserManager<ApplicationUser> userManager,
    ILogService logService,
    ILogger<TenantDeletionNotificationService> logger) : ITenantDeletionNotificationService
{
    public async Task<bool> TrySendDeletionRequestedNotificationAsync(
        Tenant tenant,
        string requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        var requester = await userManager.FindByIdAsync(requestedByUserId);
        var requesterEmail = requester?.Email ?? requestedByUserId;
        var requesterName = requester?.DisplayName ?? requester?.UserName ?? requesterEmail;
        var requestedAt = tenant.DeletionRequestedAt ?? DateTime.UtcNow;

        return await SendSystemNotificationAsync(
            tenant,
            requestedByUserId,
            "Mandantenlöschung angefordert",
            BuildDeletionRequestedHtml(tenant, requesterName, requesterEmail, requestedAt),
            BuildDeletionRequestedText(tenant, requesterName, requesterEmail, requestedAt),
            "TenantDeletionNotificationSent",
            "TenantDeletionNotificationFailed",
            cancellationToken);
    }

    public async Task<bool> TrySendMarkedForDeletionNotificationAsync(
        Tenant tenant,
        string markedByUserId,
        CancellationToken cancellationToken = default)
    {
        var admin = await userManager.FindByIdAsync(markedByUserId);
        var adminLabel = admin?.DisplayName ?? admin?.UserName ?? markedByUserId;
        var markedAt = tenant.DeletionRequestedAt ?? DateTime.UtcNow;

        var html = new StringBuilder()
            .Append("<div style=\"font-family: sans-serif; line-height: 1.5;\">")
            .Append("<h2>Mandant zur Löschung vorgemerkt</h2>")
            .Append("<p>Der Mandant <strong>")
            .Append(WebUtility.HtmlEncode(tenant.Name))
            .Append("</strong> wurde durch einen Plattform-Administrator zur Löschung vorgemerkt.</p>")
            .Append("<table style=\"border-collapse:collapse;\">")
            .AppendRow("Mandanten-ID", tenant.Id.ToString())
            .AppendRow("Vorgemerkt von", adminLabel)
            .AppendRow("Zeitpunkt", FormatDateTime(markedAt))
            .AppendRow("Geplant", tenant.DeletionScheduledAt.HasValue
                ? FormatDateTime(tenant.DeletionScheduledAt.Value)
                : "—")
            .Append("</table>")
            .Append("<p>Es wurde noch keine endgültige Löschung durchgeführt.</p>")
            .Append("</div>")
            .ToString();

        var text = new StringBuilder()
            .AppendLine("Mandant zur Löschung vorgemerkt")
            .AppendLine()
            .AppendLine($"Mandant: {tenant.Name} (ID {tenant.Id})")
            .AppendLine($"Vorgemerkt von: {adminLabel}")
            .AppendLine($"Zeitpunkt: {FormatDateTime(markedAt)}")
            .AppendLine()
            .AppendLine("Es wurde noch keine endgültige Löschung durchgeführt.")
            .ToString();

        return await SendSystemNotificationAsync(
            tenant,
            markedByUserId,
            "Mandant zur Löschung vorgemerkt",
            html,
            text,
            "TenantMarkedForDeletionNotificationSent",
            "TenantMarkedForDeletionNotificationFailed",
            cancellationToken);
    }

    public Task<bool> TrySendPermanentDeletionNotificationAsync(
        string tenantName,
        int tenantId,
        Guid? licenseId,
        string deletedByUserId,
        CancellationToken cancellationToken = default)
    {
        var html = new StringBuilder()
            .Append("<div style=\"font-family: sans-serif; line-height: 1.5;\">")
            .Append("<h2>Mandant endgültig gelöscht</h2>")
            .Append("<p>Der Mandant <strong>")
            .Append(WebUtility.HtmlEncode(tenantName))
            .Append("</strong> wurde endgültig gelöscht.</p>")
            .Append("<table style=\"border-collapse:collapse;\">")
            .AppendRow("Mandanten-ID", tenantId.ToString())
            .AppendRow("Zeitpunkt", FormatDateTime(DateTime.UtcNow))
            .Append("</table>")
            .Append("</div>")
            .ToString();

        var text = new StringBuilder()
            .AppendLine("Mandant endgültig gelöscht")
            .AppendLine()
            .AppendLine($"Mandant: {tenantName} (ID {tenantId})")
            .AppendLine($"Zeitpunkt: {FormatDateTime(DateTime.UtcNow)}")
            .ToString();

        return SendSystemNotificationWithoutTenantAsync(
            tenantName,
            tenantId,
            licenseId,
            deletedByUserId,
            "Mandant endgültig gelöscht",
            html,
            text,
            "TenantPermanentDeletionNotificationSent",
            "TenantPermanentDeletionNotificationFailed",
            cancellationToken);
    }

    public Task<bool> TrySendPermanentDeletionFailedNotificationAsync(
        string tenantName,
        int tenantId,
        Guid? licenseId,
        string triggeredByUserId,
        CancellationToken cancellationToken = default)
    {
        var html = new StringBuilder()
            .Append("<div style=\"font-family: sans-serif; line-height: 1.5;\">")
            .Append("<h2>Endgültige Mandantenlöschung fehlgeschlagen</h2>")
            .Append("<p>Die endgültige Löschung für den Mandanten <strong>")
            .Append(WebUtility.HtmlEncode(tenantName))
            .Append("</strong> ist fehlgeschlagen.</p>")
            .Append("<p>Bitte prüfen Sie die Systemprotokolle.</p>")
            .Append("</div>")
            .ToString();

        var text = new StringBuilder()
            .AppendLine("Endgültige Mandantenlöschung fehlgeschlagen")
            .AppendLine()
            .AppendLine($"Mandant: {tenantName} (ID {tenantId})")
            .AppendLine("Bitte prüfen Sie die Systemprotokolle.")
            .ToString();

        return SendSystemNotificationWithoutTenantAsync(
            tenantName,
            tenantId,
            licenseId,
            triggeredByUserId,
            "Endgültige Mandantenlöschung fehlgeschlagen",
            html,
            text,
            "TenantPermanentDeletionFailedNotificationSent",
            "TenantPermanentDeletionFailedNotificationFailed",
            cancellationToken,
            severityOnFailure: "Error");
    }

    private async Task<bool> SendSystemNotificationAsync(
        Tenant tenant,
        string actorUserId,
        string subject,
        string htmlBody,
        string textBody,
        string successAction,
        string failureAction,
        CancellationToken cancellationToken,
        string severityOnFailure = "Error")
    {
        var emailSettings = await emailSettingsProvider.GetSettingsForSendingAsync();
        if (emailSettings is null || !emailSettings.SystemNotificationsEnabled)
        {
            logger.LogInformation(
                "Systembenachrichtigung übersprungen (deaktiviert). TenantId={TenantId}, Subject={Subject}",
                tenant.Id,
                subject);
            return true;
        }

        var recipient = emailSettings.SystemNotificationRecipientEmail?.Trim();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            logger.LogWarning(
                "Keine Empfängeradresse für Systembenachrichtigungen konfiguriert. TenantId={TenantId}",
                tenant.Id);
            return true;
        }

        try
        {
            var result = await emailService.SendEmailAsync(recipient, subject, htmlBody, textBody);
            if (result.Succeeded)
            {
                await TryLogSystemAsync(successAction, $"Systembenachrichtigung versendet: {subject}", tenant, actorUserId,
                    new { Recipient = recipient });
                return true;
            }

            logger.LogError(
                "Systembenachrichtigung fehlgeschlagen. TenantId={TenantId}, Subject={Subject}: {Message}",
                tenant.Id,
                subject,
                result.Message);
            await TryLogSystemAsync(
                failureAction,
                $"Systembenachrichtigung fehlgeschlagen: {subject}",
                tenant,
                actorUserId,
                new { Recipient = recipient, Detail = result.Message },
                severityOnFailure);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Systembenachrichtigung unerwarteter Fehler. TenantId={TenantId}, Subject={Subject}",
                tenant.Id,
                subject);
            await TryLogSystemAsync(
                failureAction,
                $"Systembenachrichtigung fehlgeschlagen: {subject}",
                tenant,
                actorUserId,
                new { Detail = ex.GetType().Name },
                severityOnFailure);
            return false;
        }
    }

    private async Task<bool> SendSystemNotificationWithoutTenantAsync(
        string tenantName,
        int tenantId,
        Guid? licenseId,
        string actorUserId,
        string subject,
        string htmlBody,
        string textBody,
        string successAction,
        string failureAction,
        CancellationToken cancellationToken,
        string severityOnFailure = "Error")
    {
        var emailSettings = await emailSettingsProvider.GetSettingsForSendingAsync();
        if (emailSettings is null || !emailSettings.SystemNotificationsEnabled)
        {
            return true;
        }

        var recipient = emailSettings.SystemNotificationRecipientEmail?.Trim();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            return true;
        }

        try
        {
            var result = await emailService.SendEmailAsync(recipient, subject, htmlBody, textBody);
            if (result.Succeeded)
            {
                await TryLogSystemAsync(
                    successAction,
                    $"Systembenachrichtigung versendet: {subject}",
                    tenantName,
                    tenantId,
                    licenseId,
                    actorUserId,
                    new { Recipient = recipient });
                return true;
            }

            logger.LogError(
                "Systembenachrichtigung fehlgeschlagen. TenantId={TenantId}, Subject={Subject}: {Message}",
                tenantId,
                subject,
                result.Message);
            await TryLogSystemAsync(
                failureAction,
                $"Systembenachrichtigung fehlgeschlagen: {subject}",
                tenantName,
                tenantId,
                licenseId,
                actorUserId,
                new { Recipient = recipient, Detail = result.Message },
                severityOnFailure);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Systembenachrichtigung unerwarteter Fehler. TenantId={TenantId}, Subject={Subject}",
                tenantId,
                subject);
            await TryLogSystemAsync(
                failureAction,
                $"Systembenachrichtigung fehlgeschlagen: {subject}",
                tenantName,
                tenantId,
                licenseId,
                actorUserId,
                new { Detail = ex.GetType().Name },
                severityOnFailure);
            return false;
        }
    }

    private static string BuildDeletionRequestedHtml(
        Tenant tenant,
        string requesterName,
        string requesterEmail,
        DateTime requestedAtUtc)
    {
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family: sans-serif; line-height: 1.5;\">");
        sb.Append("<h2>Mandantenlöschung angefordert</h2>");
        sb.Append("<p>Für den Mandanten <strong>")
            .Append(WebUtility.HtmlEncode(tenant.Name))
            .Append("</strong> wurde eine Löschung angefordert.</p>");
        sb.Append("<table style=\"border-collapse:collapse;\">");
        sb.AppendRow("Mandanten-ID", tenant.Id.ToString());
        sb.AppendRow("Angefordert von", $"{requesterName} ({requesterEmail})");
        sb.AppendRow("Zeitpunkt", FormatDateTime(requestedAtUtc));
        sb.Append("</table>");
        sb.Append("<p>Der Mandant wurde zur Löschung vorgemerkt. Es wurde keine endgültige Löschung durchgeführt.</p>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildDeletionRequestedText(
        Tenant tenant,
        string requesterName,
        string requesterEmail,
        DateTime requestedAtUtc)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Mandantenlöschung angefordert");
        sb.AppendLine();
        sb.AppendLine($"Mandant: {tenant.Name} (ID {tenant.Id})");
        sb.AppendLine($"Angefordert von: {requesterName} ({requesterEmail})");
        sb.AppendLine($"Zeitpunkt: {FormatDateTime(requestedAtUtc)}");
        sb.AppendLine();
        sb.AppendLine("Der Mandant wurde zur Löschung vorgemerkt. Es wurde keine endgültige Löschung durchgeführt.");
        return sb.ToString();
    }

    private static string FormatDateTime(DateTime utc) =>
        utc.ToLocalTime().ToString("dd.MM.yyyy, HH:mm 'Uhr'");

    private async Task TryLogSystemAsync(
        string action,
        string description,
        Tenant tenant,
        string actorUserId,
        object? metadata = null,
        string severity = "Information")
    {
        await TryLogSystemAsync(action, description, tenant.Name, tenant.Id, tenant.LicenseId, actorUserId, metadata,
            severity);
    }

    private async Task TryLogSystemAsync(
        string action,
        string description,
        string tenantName,
        int tenantId,
        Guid? licenseId,
        string actorUserId,
        object? metadata = null,
        string severity = "Information")
    {
        try
        {
            await logService.LogSystemAsync(
                action: action,
                description: description,
                severity: severity,
                entityType: "Mandant",
                entityId: tenantId.ToString(),
                tenantId: tenantId,
                licenseId: licenseId,
                metadata: metadata ?? new { ActorUserId = actorUserId, TenantName = tenantName });
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }
}

file static class TenantDeletionNotificationStringBuilderExtensions
{
    public static StringBuilder AppendRow(this StringBuilder sb, string label, string value)
    {
        sb.Append("<tr><td style=\"padding:2px 12px 2px 0;vertical-align:top;\"><strong>")
            .Append(WebUtility.HtmlEncode(label))
            .Append(":</strong></td><td>")
            .Append(WebUtility.HtmlEncode(value))
            .Append("</td></tr>");
        return sb;
    }
}
