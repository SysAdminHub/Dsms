using System.Net;
using System.Text;
using Dsms.Web.Data;
using Dsms.Web.Services.Email;
using Dsms.Web.Services.Logging;
using Microsoft.AspNetCore.Identity;

namespace Dsms.Web.Services.CommunityTemplates;

public sealed class CommunityTemplateNotificationService(
    IEmailSettingsService emailSettingsService,
    IEmailService emailService,
    IApplicationInfoService appInfo,
    UserManager<ApplicationUser> userManager,
    ILogService logService,
    ILogger<CommunityTemplateNotificationService> logger) : ICommunityTemplateNotificationService
{
    public async Task TryNotifyCommunityTemplateSubmittedAsync(
        CommunityTemplateNotificationModel model,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var emailSettings = await emailSettingsService.GetSettingsForSendingAsync();
            if (emailSettings is null || !emailSettings.SystemNotificationsEnabled)
            {
                logger.LogInformation(
                    "Community-Systembenachrichtigung übersprungen (deaktiviert). TemplateType={TemplateType}, TemplateId={TemplateId}, TenantId={TenantId}",
                    model.TemplateType,
                    model.TemplateId,
                    model.TenantId);
                return;
            }

            var recipient = emailSettings.SystemNotificationRecipientEmail?.Trim();
            if (string.IsNullOrWhiteSpace(recipient))
            {
                logger.LogWarning(
                    "Keine Empfängeradresse für Systembenachrichtigungen konfiguriert. TemplateType={TemplateType}, TemplateId={TemplateId}, TenantId={TenantId}",
                    model.TemplateType,
                    model.TemplateId,
                    model.TenantId);
                await TryLogSystemAsync(
                    "CommunityTemplateNotificationSkipped",
                    "Keine Empfängeradresse für Systembenachrichtigungen konfiguriert.",
                    model);
                return;
            }

            var subject = BuildSubject(model.TemplateType);
            var reviewUrl = BuildReviewUrl(model.TemplateType);
            var htmlBody = BuildHtmlBody(model, reviewUrl);
            var textBody = BuildTextBody(model, reviewUrl);

            var result = await emailService.SendEmailAsync(recipient, subject, htmlBody, textBody);
            if (result.Succeeded)
            {
                logger.LogInformation(
                    "Community-Systembenachrichtigung versendet. TemplateType={TemplateType}, TemplateId={TemplateId}, TenantId={TenantId}, Recipient={Recipient}",
                    model.TemplateType,
                    model.TemplateId,
                    model.TenantId,
                    recipient);
                await TryLogSystemAsync(
                    "CommunityTemplateNotificationSent",
                    "Community-Systembenachrichtigung erfolgreich versendet.",
                    model,
                    new { Recipient = recipient });
                return;
            }

            logger.LogError(
                "Community-Systembenachrichtigung fehlgeschlagen: {Detail}. TemplateType={TemplateType}, TemplateId={TemplateId}, TemplateTitle={TemplateTitle}, TenantId={TenantId}, SubmittedByUserId={SubmittedByUserId}",
                result.Message,
                model.TemplateType,
                model.TemplateId,
                model.TemplateTitle,
                model.TenantId,
                model.SubmittedByUserId);
            await TryLogSystemAsync(
                "CommunityTemplateNotificationFailed",
                "Community-Systembenachrichtigung fehlgeschlagen.",
                model,
                new { Recipient = recipient, Detail = result.Message },
                severity: "Error");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Community-Systembenachrichtigung unerwarteter Fehler. TemplateType={TemplateType}, TemplateId={TemplateId}, TemplateTitle={TemplateTitle}, TenantId={TenantId}, SubmittedByUserId={SubmittedByUserId}",
                model.TemplateType,
                model.TemplateId,
                model.TemplateTitle,
                model.TenantId,
                model.SubmittedByUserId);
        }
    }

    public async Task TryNotifyCommunityTemplateReviewedAsync(
        CommunityTemplateReviewNotificationModel model,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var recipient = await ResolveRecipientEmailAsync(model);
            if (string.IsNullOrWhiteSpace(recipient))
            {
                logger.LogWarning(
                    "Community-Prüfungsbenachrichtigung übersprungen (keine E-Mail). TemplateType={TemplateType}, TemplateId={TemplateId}, SubmittedByUserId={SubmittedByUserId}",
                    model.TemplateType,
                    model.TemplateId,
                    model.SubmittedByUserId);
                return;
            }

            var displayName = await ResolveDisplayNameAsync(model);
            var subject = BuildReviewSubject(model.TemplateType, model.IsApproved);
            var templateUrl = BuildTemplateEditUrl(model.TemplateType, model.TemplateId);
            var htmlBody = BuildReviewHtmlBody(model, displayName, templateUrl);
            var textBody = BuildReviewTextBody(model, displayName, templateUrl);

            var result = await emailService.SendEmailAsync(recipient, subject, htmlBody, textBody);
            if (result.Succeeded)
            {
                logger.LogInformation(
                    "Community-Prüfungsbenachrichtigung versendet. TemplateType={TemplateType}, TemplateId={TemplateId}, IsApproved={IsApproved}, Recipient={Recipient}",
                    model.TemplateType,
                    model.TemplateId,
                    model.IsApproved,
                    recipient);
                return;
            }

            logger.LogError(
                "Community-Prüfungsbenachrichtigung fehlgeschlagen: {Detail}. TemplateType={TemplateType}, TemplateId={TemplateId}, TemplateTitle={TemplateTitle}, IsApproved={IsApproved}, SubmittedByUserId={SubmittedByUserId}",
                result.Message,
                model.TemplateType,
                model.TemplateId,
                model.TemplateTitle,
                model.IsApproved,
                model.SubmittedByUserId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Community-Prüfungsbenachrichtigung unerwarteter Fehler. TemplateType={TemplateType}, TemplateId={TemplateId}, TemplateTitle={TemplateTitle}, IsApproved={IsApproved}, SubmittedByUserId={SubmittedByUserId}",
                model.TemplateType,
                model.TemplateId,
                model.TemplateTitle,
                model.IsApproved,
                model.SubmittedByUserId);
        }
    }

    private async Task<string?> ResolveRecipientEmailAsync(CommunityTemplateReviewNotificationModel model)
    {
        var email = model.SubmittedByEmail?.Trim();
        if (!string.IsNullOrWhiteSpace(email))
            return email;

        if (string.IsNullOrEmpty(model.SubmittedByUserId))
            return null;

        var user = await userManager.FindByIdAsync(model.SubmittedByUserId);
        return user?.Email?.Trim();
    }

    private async Task<string> ResolveDisplayNameAsync(CommunityTemplateReviewNotificationModel model)
    {
        var name = model.SubmittedByName?.Trim();
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        if (!string.IsNullOrEmpty(model.SubmittedByUserId))
        {
            var user = await userManager.FindByIdAsync(model.SubmittedByUserId);
            name = user?.DisplayName?.Trim() ?? user?.UserName?.Trim();
            if (!string.IsNullOrWhiteSpace(name))
                return name;
        }

        return "Benutzer";
    }

    private static string BuildReviewSubject(CommunityTemplateNotificationType templateType, bool isApproved) =>
        (templateType, isApproved) switch
        {
            (CommunityTemplateNotificationType.Audit, true) => "Ihre Community-Auditvorlage wurde freigegeben",
            (CommunityTemplateNotificationType.Audit, false) => "Ihre Community-Auditvorlage wurde abgelehnt",
            (CommunityTemplateNotificationType.Training, true) => "Ihre Community-Schulungsvorlage wurde freigegeben",
            (CommunityTemplateNotificationType.Training, false) => "Ihre Community-Schulungsvorlage wurde abgelehnt",
            _ => isApproved ? "Ihre Community-Vorlage wurde freigegeben" : "Ihre Community-Vorlage wurde abgelehnt"
        };

    private string? BuildTemplateEditUrl(CommunityTemplateNotificationType templateType, int templateId)
    {
        var baseUrl = appInfo.AppUrl?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
            return null;

        baseUrl = baseUrl.TrimEnd('/');
        var path = templateType switch
        {
            CommunityTemplateNotificationType.Audit => $"/audit-templates/edit/{templateId}",
            CommunityTemplateNotificationType.Training => $"/training-templates/edit/{templateId}",
            _ => null
        };

        return path is null ? null : $"{baseUrl}{path}";
    }

    private string BuildReviewHtmlBody(
        CommunityTemplateReviewNotificationModel model,
        string displayName,
        string? templateUrl)
    {
        var typeLabel = GetTemplateTypeLabel(model.TemplateType);
        var resultLabel = model.IsApproved ? "freigegeben" : "abgelehnt";
        var intro = model.IsApproved
            ? $"Ihre {typeLabel} wurde von der Community-Prüfung freigegeben und steht nun als Community-Vorlage zur Verfügung."
            : $"Ihre {typeLabel} wurde von der Community-Prüfung abgelehnt. Sie können die Vorlage bearbeiten und erneut einreichen.";

        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family: sans-serif; line-height: 1.5;\">");
        sb.Append("<p>Hallo ").Append(WebUtility.HtmlEncode(displayName)).Append(",</p>");
        sb.Append("<p>").Append(WebUtility.HtmlEncode(intro)).Append("</p>");
        sb.Append("<table style=\"border-collapse:collapse;\">");
        AppendRow(sb, "Vorlagentyp", typeLabel);
        AppendRow(sb, "Titel", model.TemplateTitle);
        AppendRow(sb, "Ergebnis", model.IsApproved ? "Freigegeben" : "Abgelehnt");
        AppendRow(sb, "Zeitpunkt", FormatDateTime(model.ReviewedAt));

        if (!string.IsNullOrWhiteSpace(model.ReviewComment))
            AppendRow(sb, "Kommentar", model.ReviewComment.Trim());

        sb.Append("</table>");

        if (!string.IsNullOrWhiteSpace(templateUrl))
        {
            sb.Append("<p>Vorlage öffnen:<br><a href=\"")
                .Append(WebUtility.HtmlEncode(templateUrl))
                .Append("\">")
                .Append(WebUtility.HtmlEncode(templateUrl))
                .Append("</a></p>");
        }

        sb.Append("<p>Mit freundlichen Grüßen<br>")
            .Append(WebUtility.HtmlEncode(appInfo.ProductName))
            .Append("</p>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private string BuildReviewTextBody(
        CommunityTemplateReviewNotificationModel model,
        string displayName,
        string? templateUrl)
    {
        var typeLabel = GetTemplateTypeLabel(model.TemplateType);
        var intro = model.IsApproved
            ? $"Ihre {typeLabel} wurde von der Community-Prüfung freigegeben und steht nun als Community-Vorlage zur Verfügung."
            : $"Ihre {typeLabel} wurde von der Community-Prüfung abgelehnt. Sie können die Vorlage bearbeiten und erneut einreichen.";

        var sb = new StringBuilder();
        sb.AppendLine($"Hallo {displayName},");
        sb.AppendLine();
        sb.AppendLine(intro);
        sb.AppendLine();
        sb.AppendLine($"Vorlagentyp: {typeLabel}");
        sb.AppendLine($"Titel: {model.TemplateTitle}");
        sb.AppendLine($"Ergebnis: {(model.IsApproved ? "Freigegeben" : "Abgelehnt")}");
        sb.AppendLine($"Zeitpunkt: {FormatDateTime(model.ReviewedAt)}");

        if (!string.IsNullOrWhiteSpace(model.ReviewComment))
            sb.AppendLine($"Kommentar: {model.ReviewComment.Trim()}");

        if (!string.IsNullOrWhiteSpace(templateUrl))
        {
            sb.AppendLine();
            sb.AppendLine("Vorlage öffnen:");
            sb.AppendLine(templateUrl);
        }

        sb.AppendLine();
        sb.AppendLine("Mit freundlichen Grüßen");
        sb.AppendLine(appInfo.ProductName);
        return sb.ToString();
    }

    private static string BuildSubject(CommunityTemplateNotificationType templateType) =>
        templateType switch
        {
            CommunityTemplateNotificationType.Audit => "Neue Community-Auditvorlage eingereicht",
            CommunityTemplateNotificationType.Training => "Neue Community-Schulungsvorlage eingereicht",
            _ => "Neue Community-Vorlage eingereicht"
        };

    private string? BuildReviewUrl(CommunityTemplateNotificationType templateType)
    {
        var baseUrl = appInfo.AppUrl?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
            return null;

        baseUrl = baseUrl.TrimEnd('/');
        var path = templateType switch
        {
            CommunityTemplateNotificationType.Audit => "/platform/audit-templates/community",
            CommunityTemplateNotificationType.Training => "/platform/training-templates/community",
            _ => null
        };

        return path is null ? null : $"{baseUrl}{path}";
    }

    private static string GetTemplateTypeLabel(CommunityTemplateNotificationType templateType) =>
        templateType switch
        {
            CommunityTemplateNotificationType.Audit => "Auditvorlage",
            CommunityTemplateNotificationType.Training => "Schulungsvorlage",
            _ => "Vorlage"
        };

    private static string FormatSubmittedBy(CommunityTemplateNotificationModel model)
    {
        var name = model.SubmittedByName?.Trim();
        var email = model.SubmittedByEmail?.Trim();

        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(email))
            return $"{name} ({email})";

        if (!string.IsNullOrWhiteSpace(name))
            return name;

        if (!string.IsNullOrWhiteSpace(email))
            return email;

        return model.SubmittedByUserId;
    }

    private static string FormatDateTime(DateTime utc) =>
        utc.ToLocalTime().ToString("dd.MM.yyyy, HH:mm 'Uhr'");

    private static string BuildHtmlBody(CommunityTemplateNotificationModel model, string? reviewUrl)
    {
        var typeLabel = GetTemplateTypeLabel(model.TemplateType);
        var intro = model.TemplateType switch
        {
            CommunityTemplateNotificationType.Audit =>
                "Eine neue Auditvorlage wurde zur Community-Prüfung eingereicht.",
            CommunityTemplateNotificationType.Training =>
                "Eine neue Schulungsvorlage wurde zur Community-Prüfung eingereicht.",
            _ => "Eine neue Vorlage wurde zur Community-Prüfung eingereicht."
        };

        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family: sans-serif; line-height: 1.5;\">");
        sb.Append("<h2>").Append(WebUtility.HtmlEncode(intro)).Append("</h2>");
        sb.Append("<table style=\"border-collapse:collapse;\">");
        AppendRow(sb, "Vorlagentyp", typeLabel);
        AppendRow(sb, "Titel", model.TemplateTitle);
        AppendRow(sb, "Mandant", model.TenantName);
        AppendRow(sb, "Mandanten-ID", model.TenantId.ToString());
        AppendRow(sb, "Eingereicht von", FormatSubmittedBy(model));
        AppendRow(sb, "Zeitpunkt", FormatDateTime(model.SubmittedAt));
        sb.Append("</table>");
        sb.Append("<p>Bitte prüfen Sie die Vorlage im Superuser-Bereich.</p>");

        if (!string.IsNullOrWhiteSpace(reviewUrl))
        {
            sb.Append("<p>Prüfung öffnen:<br><a href=\"")
                .Append(WebUtility.HtmlEncode(reviewUrl))
                .Append("\">")
                .Append(WebUtility.HtmlEncode(reviewUrl))
                .Append("</a></p>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildTextBody(CommunityTemplateNotificationModel model, string? reviewUrl)
    {
        var typeLabel = GetTemplateTypeLabel(model.TemplateType);
        var intro = model.TemplateType switch
        {
            CommunityTemplateNotificationType.Audit =>
                "Eine neue Auditvorlage wurde zur Community-Prüfung eingereicht.",
            CommunityTemplateNotificationType.Training =>
                "Eine neue Schulungsvorlage wurde zur Community-Prüfung eingereicht.",
            _ => "Eine neue Vorlage wurde zur Community-Prüfung eingereicht."
        };

        var sb = new StringBuilder();
        sb.AppendLine(intro);
        sb.AppendLine();
        sb.AppendLine($"Vorlagentyp: {typeLabel}");
        sb.AppendLine($"Titel: {model.TemplateTitle}");
        sb.AppendLine($"Mandant: {model.TenantName}");
        sb.AppendLine($"Mandanten-ID: {model.TenantId}");
        sb.AppendLine($"Eingereicht von: {FormatSubmittedBy(model)}");
        sb.AppendLine($"Zeitpunkt: {FormatDateTime(model.SubmittedAt)}");
        sb.AppendLine();
        sb.AppendLine("Bitte prüfen Sie die Vorlage im Superuser-Bereich.");

        if (!string.IsNullOrWhiteSpace(reviewUrl))
        {
            sb.AppendLine();
            sb.AppendLine("Prüfung öffnen:");
            sb.AppendLine(reviewUrl);
        }

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
        CommunityTemplateNotificationModel model,
        object? metadata = null,
        string severity = "Information")
    {
        try
        {
            await logService.LogSystemAsync(
                action: action,
                description: description,
                severity: severity,
                entityType: model.TemplateType == CommunityTemplateNotificationType.Audit
                    ? "AuditTemplate"
                    : "TrainingTemplate",
                entityId: model.TemplateId.ToString(),
                tenantId: model.TenantId,
                metadata: metadata ?? new
                {
                    model.TemplateType,
                    model.TemplateTitle,
                    model.SubmittedByUserId
                });
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }
}
