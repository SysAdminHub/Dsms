using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using Dsms.Web.Data;
using Dsms.Web.Services.Email;
using Dsms.Web.Services.Licenses;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.UpgradeRequests;

public sealed class UpgradeRequestService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IEmailSendingSettingsProvider emailSettingsProvider,
    IEmailService emailService,
    ICurrentUserContext currentUser,
    ILogService logService,
    ILogger<UpgradeRequestService> logger) : IUpgradeRequestService
{
    private const string SystemDisabledMessage =
        "Die Anfrage konnte nicht versendet werden, da Systembenachrichtigungen deaktiviert sind.";
    private const string NoRecipientMessage =
        "Die Anfrage konnte nicht versendet werden, da keine Empfängeradresse für Systembenachrichtigungen konfiguriert ist.";
    private const string SendFailedMessage =
        "Die Anfrage konnte nicht versendet werden. Bitte versuchen Sie es später erneut oder wenden Sie sich an den Support.";

    private const string TrainingSuccessMessage =
        "Ihre Anfrage zum Schulungsmodul wurde gesendet. Wir melden uns anschließend bei Ihnen.";
    private const string AdditionalTenantSuccessMessage =
        "Ihre Anfrage für einen weiteren Mandanten wurde gesendet. Wir melden uns zeitnah bei Ihnen.";
    private const string TenantExpansionSuccessMessage =
        "Ihre Anfrage zur Mandantenerweiterung wurde gesendet. Wir melden uns zeitnah bei Ihnen.";

    public async Task<UpgradeRequestResult> SubmitTrainingModuleRequestAsync(
        LicenseDetailsDto license,
        TrainingModuleRequestInput input)
    {
        var ctx = await PrepareAsync(license);
        if (ctx.Failure is not null)
        {
            return ctx.Failure;
        }

        var rows = new (string Label, string Value)[]
        {
            ("Mandant / TenantName", ctx.TenantName),
            ("TenantId", ctx.TenantId?.ToString() ?? "—"),
            ("Anfragender Benutzer", ctx.UserDisplayName),
            ("Benutzer-E-Mail", ctx.UserEmail),
            ("Aktueller Lizenzstatus", FormatStatusLabel(license.Status)),
            ("Schulungsmodul-Status", license.HasTrainingModule ? "Aktiv" : "Nicht aktiv"),
            ("Nachricht des Benutzers", Clean(input.Message)),
            ("Datum/Uhrzeit", FormatDateTime(DateTime.UtcNow))
        };

        var subject = $"Anfrage Schulungsmodul – {ctx.TenantName}";
        return await SendAsync(
            license,
            ctx,
            "TrainingModuleRequest",
            subject,
            "Anfrage Schulungsmodul",
            rows,
            TrainingSuccessMessage);
    }

    public async Task<UpgradeRequestResult> SubmitAdditionalTenantRequestAsync(
        LicenseDetailsDto license,
        AdditionalTenantRequestInput input)
    {
        var tenantName = input.TenantName?.Trim();
        var contactName = input.ContactName?.Trim();
        var contactEmail = input.ContactEmail?.Trim();

        if (string.IsNullOrWhiteSpace(tenantName))
        {
            return UpgradeRequestResult.Fail("Bitte geben Sie den Namen des gewünschten Mandanten an.");
        }

        if (string.IsNullOrWhiteSpace(contactName))
        {
            return UpgradeRequestResult.Fail("Bitte geben Sie einen Ansprechpartner an.");
        }

        if (string.IsNullOrWhiteSpace(contactEmail) || !IsValidEmail(contactEmail))
        {
            return UpgradeRequestResult.Fail("Bitte geben Sie eine gültige E-Mail-Adresse des Ansprechpartners an.");
        }

        var ctx = await PrepareAsync(license);
        if (ctx.Failure is not null)
        {
            return ctx.Failure;
        }

        var (current, max, free) = GetTenantCounts(license);

        var rows = new (string Label, string Value)[]
        {
            ("Aktueller Mandant / TenantName", ctx.TenantName),
            ("TenantId", ctx.TenantId?.ToString() ?? "—"),
            ("Lizenznummer", license.LicenseNumber),
            ("Aktueller Tarif", license.PlanName),
            ("Lizenzstatus", FormatStatusLabel(license.Status)),
            ("Aktuelle Mandantenanzahl", current.ToString()),
            ("Max. Mandanten", FormatMax(max)),
            ("Freie Mandanten", free?.ToString() ?? "unbegrenzt"),
            ("Gewünschter neuer Mandant", tenantName),
            ("Ansprechpartner", contactName),
            ("Ansprechpartner E-Mail", contactEmail),
            ("Telefonnummer", Clean(input.Phone)),
            ("Nachricht", Clean(input.Message)),
            ("Anfragender Benutzer", ctx.UserDisplayName),
            ("Benutzer-E-Mail", ctx.UserEmail),
            ("Datum/Uhrzeit", FormatDateTime(DateTime.UtcNow))
        };

        var subject = $"Anfrage weiterer Mandant – {ctx.TenantName}";
        return await SendAsync(
            license,
            ctx,
            "AdditionalTenantRequest",
            subject,
            "Anfrage weiterer Mandant",
            rows,
            AdditionalTenantSuccessMessage);
    }

    public async Task<UpgradeRequestResult> SubmitTenantExpansionRequestAsync(
        LicenseDetailsDto license,
        TenantExpansionRequestInput input)
    {
        var ctx = await PrepareAsync(license);
        if (ctx.Failure is not null)
        {
            return ctx.Failure;
        }

        var (current, max, _) = GetTenantCounts(license);

        var rows = new (string Label, string Value)[]
        {
            ("Mandant / TenantName", ctx.TenantName),
            ("TenantId", ctx.TenantId?.ToString() ?? "—"),
            ("Lizenznummer", license.LicenseNumber),
            ("Aktueller Tarif", license.PlanName),
            ("Lizenzstatus", FormatStatusLabel(license.Status)),
            ("Aktuelle Mandantenanzahl", current.ToString()),
            ("Max. Mandanten", FormatMax(max)),
            ("Anfragender Benutzer", ctx.UserDisplayName),
            ("Benutzer-E-Mail", ctx.UserEmail),
            ("Nachricht", Clean(input.Message)),
            ("Datum/Uhrzeit", FormatDateTime(DateTime.UtcNow))
        };

        var subject = $"Anfrage Mandantenerweiterung – {ctx.TenantName}";
        return await SendAsync(
            license,
            ctx,
            "TenantExpansionRequest",
            subject,
            "Anfrage Mandantenerweiterung",
            rows,
            TenantExpansionSuccessMessage);
    }

    private async Task<RequestContext> PrepareAsync(LicenseDetailsDto license)
    {
        var emailSettings = await emailSettingsProvider.GetSettingsForSendingAsync();
        if (emailSettings is null || !emailSettings.SystemNotificationsEnabled)
        {
            logger.LogWarning(
                "Lizenz-Anfrage für {LicenseNumber} abgebrochen: Systembenachrichtigungen deaktiviert.",
                license.LicenseNumber);
            return RequestContext.Failed(SystemDisabledMessage);
        }

        var recipient = emailSettings.SystemNotificationRecipientEmail?.Trim();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            logger.LogWarning(
                "Lizenz-Anfrage für {LicenseNumber} abgebrochen: keine Empfängeradresse.",
                license.LicenseNumber);
            return RequestContext.Failed(NoRecipientMessage);
        }

        var user = await currentUser.GetUserAsync();
        var userId = await currentUser.GetUserIdAsync();
        var tenantId = await currentUser.GetTenantIdAsync();
        var tenantName = await ResolveTenantNameAsync(tenantId, license);

        return new RequestContext
        {
            Recipient = recipient,
            TenantId = tenantId,
            TenantName = tenantName,
            UserDisplayName = string.IsNullOrWhiteSpace(user?.DisplayName) ? "—" : user.DisplayName,
            UserEmail = string.IsNullOrWhiteSpace(user?.Email) ? "—" : user.Email,
            UserId = userId
        };
    }

    private async Task<string> ResolveTenantNameAsync(int? tenantId, LicenseDetailsDto license)
    {
        if (tenantId.HasValue)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var name = await db.Tenants
                .AsNoTracking()
                .Where(t => t.Id == tenantId.Value)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return string.IsNullOrWhiteSpace(license.CustomerName) ? "—" : license.CustomerName;
    }

    private async Task<UpgradeRequestResult> SendAsync(
        LicenseDetailsDto license,
        RequestContext ctx,
        string action,
        string subject,
        string heading,
        (string Label, string Value)[] rows,
        string successMessage)
    {
        var htmlBody = BuildHtmlBody(heading, rows);
        var textBody = BuildTextBody(heading, rows);

        var sendResult = await emailService.SendEmailAsync(ctx.Recipient!, subject, htmlBody, textBody);
        if (!sendResult.Succeeded)
        {
            logger.LogError(
                "Lizenz-Anfrage ({Action}) für {LicenseNumber} fehlgeschlagen: {Detail}",
                action,
                license.LicenseNumber,
                sendResult.Message);
            await TryLogSystemAsync($"{action}Failed", SendFailedMessage, license, severity: "Error");
            return UpgradeRequestResult.Fail(SendFailedMessage);
        }

        logger.LogInformation(
            "Lizenz-Anfrage ({Action}) für {LicenseNumber} erfolgreich versendet an {Recipient}.",
            action,
            license.LicenseNumber,
            ctx.Recipient);

        await TryLogAuditAsync(license, ctx, action);
        await TryLogSystemAsync($"{action}Sent", successMessage, license);

        return UpgradeRequestResult.Ok(successMessage);
    }

    private static (int Current, int? Max, int? Free) GetTenantCounts(LicenseDetailsDto license)
    {
        var current = license.Usage.CurrentTenants;
        var max = license.MaxTenants;
        int? free = max.HasValue ? Math.Max(0, max.Value - current) : null;
        return (current, max, free);
    }

    private static string FormatMax(int? max) => max?.ToString() ?? "unbegrenzt";

    private static string Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    private static bool IsValidEmail(string email) =>
        new EmailAddressAttribute().IsValid(email);

    private static string BuildHtmlBody(string heading, (string Label, string Value)[] rows)
    {
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family: sans-serif; line-height: 1.5;\">");
        sb.Append("<h2>").Append(WebUtility.HtmlEncode(heading)).Append("</h2>");
        sb.Append("<table style=\"border-collapse:collapse;\">");
        foreach (var (label, value) in rows)
        {
            sb.Append("<tr><td style=\"padding:2px 12px 2px 0;vertical-align:top;\"><strong>")
                .Append(WebUtility.HtmlEncode(label))
                .Append(":</strong></td><td>")
                .Append(WebUtility.HtmlEncode(value))
                .Append("</td></tr>");
        }

        sb.Append("</table>");
        sb.Append("<p><strong>Hinweis:</strong> Es handelt sich um eine Anfrage. ")
            .Append("Es wurde keine automatische Lizenzänderung und keine automatische Mandantenanlage durchgeführt.</p>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildTextBody(string heading, (string Label, string Value)[] rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(heading);
        sb.AppendLine();
        foreach (var (label, value) in rows)
        {
            sb.AppendLine($"{label}: {value}");
        }

        sb.AppendLine();
        sb.AppendLine("Hinweis: Es handelt sich um eine Anfrage. Es wurde keine automatische Lizenzänderung "
            + "und keine automatische Mandantenanlage durchgeführt.");
        return sb.ToString();
    }

    private static string FormatDateTime(DateTime value) => value.ToLocalTime().ToString("g");

    private static string FormatStatusLabel(string status) => status switch
    {
        "Active" => "Aktiv",
        "Suspended" => "Gesperrt",
        "Inactive" => "Inaktiv",
        _ => status
    };

    private async Task TryLogAuditAsync(LicenseDetailsDto license, RequestContext ctx, string action)
    {
        try
        {
            await logService.LogAuditAsync(
                action: action,
                description: "Lizenz-Anfrage gesendet.",
                entityType: "License",
                entityId: license.Id.ToString(),
                entityName: license.LicenseNumber,
                tenantId: ctx.TenantId,
                licenseId: license.Id,
                metadata: new { RequestedByUserId = ctx.UserId, Action = action });
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }

    private async Task TryLogSystemAsync(
        string action,
        string description,
        LicenseDetailsDto license,
        string severity = "Information")
    {
        try
        {
            await logService.LogSystemAsync(
                action: action,
                description: description,
                severity: severity,
                entityType: "License",
                entityId: license.Id.ToString());
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }

    private sealed class RequestContext
    {
        public UpgradeRequestResult? Failure { get; init; }
        public string? Recipient { get; init; }
        public int? TenantId { get; init; }
        public string TenantName { get; init; } = "—";
        public string UserDisplayName { get; init; } = "—";
        public string UserEmail { get; init; } = "—";
        public string? UserId { get; init; }

        public static RequestContext Failed(string message) =>
            new() { Failure = UpgradeRequestResult.Fail(message) };
    }
}
