using System.Net;
using System.Text;
using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Email;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Support;

public sealed class SupportAccessNotificationService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IEmailSendingSettingsProvider emailSettingsProvider,
    IEmailService emailService,
    IApplicationInfoService appInfo,
    ILogService logService,
    ILogger<SupportAccessNotificationService> logger) : ISupportAccessNotificationService
{
    public async Task TrySendSupportAccessRequestedNotificationAsync(
        int grantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var grant = await db.SupportAccessGrants
                .AsNoTracking()
                .Include(g => g.Tenant)
                .Include(g => g.GrantedByUser)
                .FirstOrDefaultAsync(g => g.Id == grantId, cancellationToken);

            if (grant is null)
            {
                logger.LogWarning(
                    "Supportzugriffs-Systembenachrichtigung übersprungen: Grant {GrantId} nicht gefunden.",
                    grantId);
                return;
            }

            var emailSettings = await emailSettingsProvider.GetSettingsForSendingAsync(cancellationToken);
            if (emailSettings is null || !emailSettings.SystemNotificationsEnabled)
            {
                logger.LogInformation(
                    "Supportzugriffs-Systembenachrichtigung übersprungen (deaktiviert). GrantId={GrantId}, TenantId={TenantId}",
                    grant.Id,
                    grant.TenantId);
                return;
            }

            var recipient = emailSettings.SystemNotificationRecipientEmail?.Trim();
            if (string.IsNullOrWhiteSpace(recipient))
            {
                logger.LogWarning(
                    "Keine Empfängeradresse für Systembenachrichtigungen konfiguriert. GrantId={GrantId}, TenantId={TenantId}",
                    grant.Id,
                    grant.TenantId);
                await TryLogSystemAsync(
                    "SupportAccessRequestedNotificationSkipped",
                    "Keine Empfängeradresse für Systembenachrichtigungen konfiguriert.",
                    grant);
                return;
            }

            var userName = grant.GrantedByUser.DisplayName
                ?? grant.GrantedByUser.UserName
                ?? grant.GrantedByUserId;
            var userEmail = grant.GrantedByUser.Email ?? "—";
            var status = grant.IsActive(DateTime.UtcNow) ? "Aktiv" : "Inaktiv";
            var durationLabel = FormatDuration(grant.GrantedAt, grant.ValidUntil);
            var adminUrl = BuildAdminUrl();

            var subject = $"Neue Supportzugriffs-Anfrage in {appInfo.ProductName}";
            var htmlBody = BuildHtmlBody(grant, userName, userEmail, status, durationLabel, adminUrl);
            var textBody = BuildTextBody(grant, userName, userEmail, status, durationLabel, adminUrl);

            var result = await emailService.SendEmailAsync(recipient, subject, htmlBody, textBody);
            if (result.Succeeded)
            {
                logger.LogInformation(
                    "Supportzugriffs-Systembenachrichtigung versendet. GrantId={GrantId}, TenantId={TenantId}, Recipient={Recipient}",
                    grant.Id,
                    grant.TenantId,
                    recipient);
                await TryLogSystemAsync(
                    "SupportAccessRequestedNotificationSent",
                    "Supportzugriffs-Systembenachrichtigung erfolgreich versendet.",
                    grant,
                    new { Recipient = recipient, grant.GrantedByUserId });
                return;
            }

            logger.LogError(
                "Supportzugriffs-Systembenachrichtigung fehlgeschlagen: {Detail}. GrantId={GrantId}, TenantId={TenantId}, GrantedByUserId={GrantedByUserId}",
                result.Message,
                grant.Id,
                grant.TenantId,
                grant.GrantedByUserId);
            await TryLogSystemAsync(
                "SupportAccessRequestedNotificationFailed",
                "Supportzugriffs-Systembenachrichtigung fehlgeschlagen.",
                grant,
                new { Recipient = recipient, Detail = result.Message },
                severity: "Error");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Supportzugriffs-Systembenachrichtigung unerwarteter Fehler. GrantId={GrantId}",
                grantId);
        }
    }

    private string? BuildAdminUrl()
    {
        var baseUrl = appInfo.AppUrl?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        return $"{baseUrl.TrimEnd('/')}/platform/support-access";
    }

    private static string FormatDuration(DateTime grantedAt, DateTime validUntil)
    {
        var duration = validUntil - grantedAt;
        foreach (var option in SupportAccessDurations.Options)
        {
            if (Math.Abs((option.Duration - duration).TotalMinutes) < 1)
            {
                return option.Label;
            }
        }

        if (duration.TotalDays >= 1)
        {
            return $"{(int)Math.Round(duration.TotalDays)} Tag(e)";
        }

        return $"{(int)Math.Round(duration.TotalHours)} Stunde(n)";
    }

    private static string FormatDateTime(DateTime utc) =>
        utc.ToLocalTime().ToString("dd.MM.yyyy, HH:mm 'Uhr'");

    private static string BuildHtmlBody(
        SupportAccessGrant grant,
        string userName,
        string userEmail,
        string status,
        string durationLabel,
        string? adminUrl)
    {
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family: sans-serif; line-height: 1.5;\">");
        sb.Append("<p>Hallo,</p>");
        sb.Append("<p>ein Benutzer hat einen Supportzugriff angefragt.</p>");
        sb.Append("<table style=\"border-collapse:collapse;\">");
        AppendRow(sb, "Mandant", grant.Tenant.Name);
        AppendRow(sb, "Mandanten-ID", grant.TenantId.ToString());
        AppendRow(sb, "Benutzer", userName);
        AppendRow(sb, "E-Mail", userEmail);
        AppendRow(sb, "Zeitpunkt", FormatDateTime(grant.GrantedAt));
        AppendRow(sb, "Status", status);
        AppendRow(sb, "Gewünschte Dauer", durationLabel);
        AppendRow(sb, "Gültig bis", FormatDateTime(grant.ValidUntil));

        if (!string.IsNullOrWhiteSpace(grant.Reason))
        {
            AppendRow(sb, "Grund/Nachricht", grant.Reason.Trim());
        }

        sb.Append("</table>");
        sb.Append("<p>Bitte prüfe die Anfrage im Administrationsbereich.</p>");

        if (!string.IsNullOrWhiteSpace(adminUrl))
        {
            sb.Append("<p>Supportzugriffe öffnen:<br><a href=\"")
                .Append(WebUtility.HtmlEncode(adminUrl))
                .Append("\">")
                .Append(WebUtility.HtmlEncode(adminUrl))
                .Append("</a></p>");
        }

        sb.Append("<p><em>Dies ist eine automatische Systembenachrichtigung.</em></p>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildTextBody(
        SupportAccessGrant grant,
        string userName,
        string userEmail,
        string status,
        string durationLabel,
        string? adminUrl)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Hallo,");
        sb.AppendLine();
        sb.AppendLine("ein Benutzer hat einen Supportzugriff angefragt.");
        sb.AppendLine();
        sb.AppendLine($"Mandant: {grant.Tenant.Name}");
        sb.AppendLine($"Mandanten-ID: {grant.TenantId}");
        sb.AppendLine($"Benutzer: {userName}");
        sb.AppendLine($"E-Mail: {userEmail}");
        sb.AppendLine($"Zeitpunkt: {FormatDateTime(grant.GrantedAt)}");
        sb.AppendLine($"Status: {status}");
        sb.AppendLine($"Gewünschte Dauer: {durationLabel}");
        sb.AppendLine($"Gültig bis: {FormatDateTime(grant.ValidUntil)}");

        if (!string.IsNullOrWhiteSpace(grant.Reason))
        {
            sb.AppendLine($"Grund/Nachricht: {grant.Reason.Trim()}");
        }

        sb.AppendLine();
        sb.AppendLine("Bitte prüfe die Anfrage im Administrationsbereich.");

        if (!string.IsNullOrWhiteSpace(adminUrl))
        {
            sb.AppendLine();
            sb.AppendLine("Supportzugriffe öffnen:");
            sb.AppendLine(adminUrl);
        }

        sb.AppendLine();
        sb.AppendLine("Dies ist eine automatische Systembenachrichtigung.");
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

    private async Task TryLogSystemAsync(
        string action,
        string description,
        SupportAccessGrant grant,
        object? metadata = null,
        string severity = "Information")
    {
        try
        {
            await logService.LogSystemAsync(
                action: action,
                description: description,
                severity: severity,
                entityType: "SupportAccessGrant",
                entityId: grant.Id.ToString(),
                tenantId: grant.TenantId,
                metadata: metadata ?? new
                {
                    grant.GrantedByUserId,
                    grant.Tenant.Name,
                    grant.GrantedAt,
                    grant.ValidUntil
                });
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }
}
