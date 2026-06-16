using System.Net;
using System.Text;
using Dsms.Web.Configuration;
using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Email;
using Dsms.Web.Services.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Dsms.Web.Services.TenantDeletion;

public interface ITenantDeletionNotificationService
{
    /// <summary>
    /// Versendet eine Systembenachrichtigung. Gibt <c>false</c> zurück, wenn der Versand fehlgeschlagen ist.
    /// </summary>
    Task<bool> TrySendDeletionRequestedNotificationAsync(
        Tenant tenant,
        string requestedByUserId,
        CancellationToken cancellationToken = default);
}

public sealed class TenantDeletionNotificationService(
    IEmailSettingsService emailSettingsService,
    IEmailService emailService,
    UserManager<ApplicationUser> userManager,
    ILogService logService,
    ILogger<TenantDeletionNotificationService> logger,
    IOptions<AppBrandingOptions> brandingOptions) : ITenantDeletionNotificationService
{
    private readonly AppBrandingOptions _branding = brandingOptions.Value;

    public async Task<bool> TrySendDeletionRequestedNotificationAsync(
        Tenant tenant,
        string requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        var emailSettings = await emailSettingsService.GetSettingsForSendingAsync();
        if (emailSettings is null || !emailSettings.SystemNotificationsEnabled)
        {
            logger.LogInformation(
                "Systembenachrichtigung für Mandantenlöschung übersprungen (deaktiviert). TenantId={TenantId}",
                tenant.Id);
            return true;
        }

        var recipient = emailSettings.SystemNotificationRecipientEmail?.Trim();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            logger.LogWarning(
                "Keine Empfängeradresse für Systembenachrichtigungen konfiguriert. TenantId={TenantId}",
                tenant.Id);
            await TryLogSystemAsync(
                "TenantDeletionNotificationSkipped",
                "Keine Empfängeradresse für Systembenachrichtigungen konfiguriert.",
                tenant,
                requestedByUserId);
            return true;
        }

        var requester = await userManager.FindByIdAsync(requestedByUserId);
        var requesterEmail = requester?.Email ?? requestedByUserId;
        var requesterName = requester?.DisplayName ?? requester?.UserName ?? requesterEmail;
        var requestedAt = tenant.DeletionRequestedAt ?? DateTime.UtcNow;

        const string subject = "Mandantenlöschung angefordert";
        var htmlBody = BuildHtmlBody(tenant, requesterName, requesterEmail, requestedAt);
        var textBody = BuildTextBody(tenant, requesterName, requesterEmail, requestedAt);

        var result = await emailService.SendEmailAsync(recipient, subject, htmlBody, textBody);
        if (result.Succeeded)
        {
            logger.LogInformation(
                "Systembenachrichtigung für Mandantenlöschung versendet. TenantId={TenantId}, Recipient={Recipient}",
                tenant.Id,
                recipient);
            await TryLogSystemAsync(
                "TenantDeletionNotificationSent",
                "Systembenachrichtigung für Mandantenlöschung versendet.",
                tenant,
                requestedByUserId,
                new { Recipient = recipient, RequesterEmail = requesterEmail });
            return true;
        }

        logger.LogError(
            "Systembenachrichtigung für Mandantenlöschung fehlgeschlagen. TenantId={TenantId}: {Message}",
            tenant.Id,
            result.Message);
        await TryLogSystemAsync(
            "TenantDeletionNotificationFailed",
            "Systembenachrichtigung für Mandantenlöschung fehlgeschlagen.",
            tenant,
            requestedByUserId,
            new { Recipient = recipient, Detail = result.Message },
            severity: "Error");
        return false;
    }

    private string BuildHtmlBody(Tenant tenant, string requesterName, string requesterEmail, DateTime requestedAtUtc)
    {
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family: sans-serif; line-height: 1.5;\">");
        sb.Append("<h2>Mandantenlöschung angefordert</h2>");
        sb.Append("<p>Für den Mandanten <strong>")
            .Append(WebUtility.HtmlEncode(tenant.Name))
            .Append("</strong> wurde eine Löschung angefordert.</p>");
        sb.Append("<table style=\"border-collapse:collapse;\">");
        AppendRow(sb, "Mandanten-ID", tenant.Id.ToString());
        AppendRow(sb, "Angefordert von", $"{requesterName} ({requesterEmail})");
        AppendRow(sb, "Zeitpunkt", FormatDateTime(requestedAtUtc));
        sb.Append("</table>");
        sb.Append("<p>Der Mandant wurde nur zur Löschung markiert. Es wurde keine automatische Löschung durchgeführt.</p>");
        sb.Append("<p>Bitte prüfen Sie die Anfrage in der Mandantenverwaltung.</p>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private string BuildTextBody(Tenant tenant, string requesterName, string requesterEmail, DateTime requestedAtUtc)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Mandantenlöschung angefordert");
        sb.AppendLine();
        sb.AppendLine($"Für den Mandanten \"{tenant.Name}\" wurde eine Löschung angefordert.");
        sb.AppendLine();
        sb.AppendLine($"Mandanten-ID: {tenant.Id}");
        sb.AppendLine($"Angefordert von: {requesterName} ({requesterEmail})");
        sb.AppendLine($"Zeitpunkt: {FormatDateTime(requestedAtUtc)}");
        sb.AppendLine();
        sb.AppendLine("Der Mandant wurde nur zur Löschung markiert. Es wurde keine automatische Löschung durchgeführt.");
        sb.AppendLine();
        sb.AppendLine("Bitte prüfen Sie die Anfrage in der Mandantenverwaltung.");
        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, string label, string value)
    {
        sb.Append("<tr><td style=\"padding:2px 12px 2px 0;vertical-align:top;\"><strong>")
            .Append(WebUtility.HtmlEncode(label))
            .Append(":</strong></td><td>")
            .Append(WebUtility.HtmlEncode(value))
            .Append("</td></tr>");
    }

    private static string FormatDateTime(DateTime utc) =>
        utc.ToLocalTime().ToString("dd.MM.yyyy, HH:mm 'Uhr'");

    private async Task TryLogSystemAsync(
        string action,
        string description,
        Tenant tenant,
        string requestedByUserId,
        object? metadata = null,
        string severity = "Information")
    {
        try
        {
            await logService.LogSystemAsync(
                action: action,
                description: description,
                severity: severity,
                entityType: "Tenant",
                entityId: tenant.Id.ToString(),
                tenantId: tenant.Id,
                licenseId: tenant.LicenseId,
                metadata: metadata ?? new { RequestedByUserId = requestedByUserId, tenant.Name });
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }
}
