using System.Net;
using System.Text;
using Dsms.Web.Configuration;
using Dsms.Web.Data;
using Dsms.Web.Services.Email;
using Dsms.Web.Services.Licenses;
using Dsms.Web.Services.Logging;
using Dsms.Web.Services.PendingSignups;
using Dsms.Web.Services.SubscriptionPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dsms.Web.Services.UpgradeRequests;

public sealed class UpgradeRequestService(
    ISubscriptionPlanService planService,
    IEmailSettingsService emailSettingsService,
    IEmailService emailService,
    ICurrentUserContext currentUser,
    ILogService logService,
    ILogger<UpgradeRequestService> logger,
    IOptions<AppBrandingOptions> brandingOptions) : IUpgradeRequestService
{
    private readonly AppBrandingOptions _branding = brandingOptions.Value;
    private const string ValidationMessage = "Bitte wählen Sie ein gewünschtes Upgrade aus.";
    private const string SystemDisabledMessage =
        "Die Upgrade-Anfrage konnte nicht versendet werden, da Systembenachrichtigungen deaktiviert sind.";
    private const string NoRecipientMessage =
        "Die Upgrade-Anfrage konnte nicht versendet werden, da keine Empfängeradresse für Systembenachrichtigungen konfiguriert ist.";
    private const string SendFailedMessage =
        "Die Upgrade-Anfrage konnte nicht versendet werden. Bitte versuchen Sie es später erneut oder wenden Sie sich an den Support.";
    private const string SuccessMessage =
        "Die Upgrade-Anfrage wurde gesendet. Wir melden uns zeitnah bei Ihnen.";
    private const string IndividualSuccessMessage =
        "Die Anfrage für ein individuelles Upgrade wurde gesendet. Wir melden uns zur Terminabstimmung.";

    public async Task<IReadOnlyList<UpgradeTargetPlanDto>> GetUpgradeTargetPlansAsync(string currentPlanName)
    {
        var paidPlans = await planService.GetActivePaidPlansAsync();
        var current = currentPlanName.Trim();

        return paidPlans
            .Where(p => !string.Equals(p.Name, current, StringComparison.OrdinalIgnoreCase))
            .Select(p => new UpgradeTargetPlanDto
            {
                Id = p.Id,
                Name = p.Name,
                DisplayName = p.DisplayName,
                Description = p.Description,
                DisplayLabel = string.IsNullOrWhiteSpace(p.DisplayName) ? p.Name : p.DisplayName,
                PriceMonthly = p.PriceMonthly,
                PriceYearly = p.PriceYearly,
                Currency = string.IsNullOrWhiteSpace(p.Currency) ? "EUR" : p.Currency,
                IsFree = p.IsFree,
                SortOrder = p.SortOrder,
                MaxTenants = p.MaxTenants,
                MaxAdmins = p.MaxAdmins,
                MaxUsersPerTenant = p.MaxUsersPerTenant,
                MaxDpiaPerTenant = p.MaxDpiaPerTenant,
                MaxTomsPerTenant = p.MaxTomsPerTenant,
                MaxStorageMb = p.MaxStorageMb
            })
            .ToList();
    }

    public async Task<UpgradeRequestResult> SubmitUpgradeRequestAsync(
        LicenseDetailsDto license,
        UpgradeRequestInput input)
    {
        logger.LogInformation(
            "Upgrade-Anfrage gestartet für Lizenz {LicenseNumber} ({LicenseId}).",
            license.LicenseNumber,
            license.Id);

        var selected = input.SelectedOption?.Trim();
        if (string.IsNullOrWhiteSpace(selected))
        {
            return UpgradeRequestResult.Fail(ValidationMessage);
        }

        var isIndividual = string.Equals(
            selected,
            UpgradeRequestConstants.IndividualUpgradeOption,
            StringComparison.Ordinal);

        UpgradeTargetPlanDto? targetPlan = null;
        if (!isIndividual)
        {
            if (!Guid.TryParse(selected, out var targetPlanId))
            {
                return UpgradeRequestResult.Fail(ValidationMessage);
            }

            var availablePlans = await GetUpgradeTargetPlansAsync(license.PlanName);
            targetPlan = availablePlans.FirstOrDefault(p => p.Id == targetPlanId);
            if (targetPlan is null)
            {
                return UpgradeRequestResult.Fail(ValidationMessage);
            }

            logger.LogInformation(
                "Upgrade-Anfrage Zielplan {TargetPlanName} für Lizenz {LicenseNumber}.",
                targetPlan.Name,
                license.LicenseNumber);
        }
        else
        {
            logger.LogInformation(
                "Upgrade-Anfrage individuelles Upgrade für Lizenz {LicenseNumber}.",
                license.LicenseNumber);
        }

        var emailSettings = await emailSettingsService.GetSettingsForSendingAsync();
        if (emailSettings is null || !emailSettings.SystemNotificationsEnabled)
        {
            logger.LogWarning(
                "Upgrade-Anfrage für Lizenz {LicenseNumber} abgebrochen: Systembenachrichtigungen deaktiviert.",
                license.LicenseNumber);
            await TryLogSystemAsync(
                "UpgradeRequestFailed",
                SystemDisabledMessage,
                license,
                severity: "Warning");
            return UpgradeRequestResult.Fail(SystemDisabledMessage);
        }

        var recipient = emailSettings.SystemNotificationRecipientEmail?.Trim();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            logger.LogWarning(
                "Upgrade-Anfrage für Lizenz {LicenseNumber} abgebrochen: keine Empfängeradresse.",
                license.LicenseNumber);
            await TryLogSystemAsync(
                "UpgradeRequestFailed",
                NoRecipientMessage,
                license,
                severity: "Warning");
            return UpgradeRequestResult.Fail(NoRecipientMessage);
        }

        var user = await currentUser.GetUserAsync();
        var userId = await currentUser.GetUserIdAsync();
        var tenantId = await currentUser.GetTenantIdAsync();

        var subject = BuildSubject(license, isIndividual);
        var htmlBody = BuildHtmlBody(license, input.Message, isIndividual, targetPlan, user, userId);
        var textBody = BuildTextBody(license, input.Message, isIndividual, targetPlan, user, userId);

        var sendResult = await emailService.SendEmailAsync(recipient, subject, htmlBody, textBody);
        if (!sendResult.Succeeded)
        {
            logger.LogError(
                "Upgrade-Anfrage für Lizenz {LicenseNumber} fehlgeschlagen: {Detail}",
                license.LicenseNumber,
                sendResult.Message);
            await TryLogSystemAsync(
                "UpgradeRequestFailed",
                SendFailedMessage,
                license,
                metadata: new
                {
                    Recipient = recipient,
                    Detail = sendResult.Message,
                    TargetPlan = targetPlan?.Name,
                    IsIndividual = isIndividual
                },
                severity: "Error");
            return UpgradeRequestResult.Fail(SendFailedMessage);
        }

        logger.LogInformation(
            "Upgrade-Anfrage für Lizenz {LicenseNumber} erfolgreich versendet an {Recipient}.",
            license.LicenseNumber,
            recipient);

        await TryLogAuditAsync(license, tenantId, userId, isIndividual, targetPlan);
        await TryLogSystemAsync(
            "UpgradeRequestSent",
            isIndividual ? IndividualSuccessMessage : SuccessMessage,
            license,
            metadata: new
            {
                Recipient = recipient,
                TargetPlan = targetPlan?.Name,
                IsIndividual = isIndividual
            });

        return UpgradeRequestResult.Ok(
            isIndividual ? IndividualSuccessMessage : SuccessMessage,
            isIndividual);
    }

    private string BuildSubject(LicenseDetailsDto license, bool isIndividual)
    {
        var customer = license.CustomerName.Trim();
        var productName = _branding.ProductName;
        return isIndividual
            ? $"{productName} individuelles Upgrade angefragt: {customer}"
            : $"{productName} Upgrade-Anfrage: {customer}";
    }

    private static string BuildHtmlBody(
        LicenseDetailsDto license,
        string? message,
        bool isIndividual,
        UpgradeTargetPlanDto? targetPlan,
        ApplicationUser? user,
        string? userId)
    {
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family: sans-serif; line-height: 1.5;\">");
        sb.Append("<h2>Upgrade-Anfrage</h2>");

        AppendSectionHtml(sb, "Anfrage", BuildRequestRows(message, isIndividual, targetPlan));

        AppendSectionHtml(sb, "Aktuelle Lizenz", BuildLicenseRows(license));

        AppendSectionHtml(sb, "Kunde", [
            ("Kundenname", license.CustomerName),
            ("Kunden-E-Mail", license.CustomerEmail ?? "—")
        ]);

        AppendSectionHtml(sb, "Anfragender Admin", [
            ("Anzeigename", string.IsNullOrWhiteSpace(user?.DisplayName) ? "—" : user.DisplayName),
            ("E-Mail-Adresse", user?.Email ?? "—"),
            ("Benutzer-ID", userId ?? "—")
        ]);

        AppendSectionHtml(sb, "Aktuelle Nutzung", BuildUsageRows(license));

        sb.Append("<p><strong>Hinweis:</strong> ")
            .Append(WebUtility.HtmlEncode(isIndividual
                ? "Bitte Online-Termin zur Bedarfsklärung vereinbaren."
                : UpgradeTargetPlanDisplayHelper.EmailRequestHint))
            .Append("</p>");

        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildTextBody(
        LicenseDetailsDto license,
        string? message,
        bool isIndividual,
        UpgradeTargetPlanDto? targetPlan,
        ApplicationUser? user,
        string? userId)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Upgrade-Anfrage");
        sb.AppendLine();
        sb.AppendLine("=== Anfrage ===");
        foreach (var (label, value) in BuildRequestRows(message, isIndividual, targetPlan))
        {
            sb.AppendLine($"{label}: {value}");
        }

        sb.AppendLine();
        sb.AppendLine("=== Aktuelle Lizenz ===");
        foreach (var (label, value) in BuildLicenseRows(license))
        {
            sb.AppendLine($"{label}: {value}");
        }

        sb.AppendLine();
        sb.AppendLine("=== Kunde ===");
        sb.AppendLine($"Kundenname: {license.CustomerName}");
        sb.AppendLine($"Kunden-E-Mail: {license.CustomerEmail ?? "—"}");
        sb.AppendLine();
        sb.AppendLine("=== Anfragender Admin ===");
        sb.AppendLine($"Anzeigename: {(string.IsNullOrWhiteSpace(user?.DisplayName) ? "—" : user.DisplayName)}");
        sb.AppendLine($"E-Mail-Adresse: {user?.Email ?? "—"}");
        sb.AppendLine($"Benutzer-ID: {userId ?? "—"}");
        sb.AppendLine();
        sb.AppendLine("=== Aktuelle Nutzung ===");
        foreach (var (label, value) in BuildUsageRows(license))
        {
            sb.AppendLine($"{label}: {value}");
        }

        sb.AppendLine();
        sb.AppendLine(isIndividual
            ? "Hinweis: Bitte Online-Termin zur Bedarfsklärung vereinbaren."
            : $"Hinweis: {UpgradeTargetPlanDisplayHelper.EmailRequestHint}");

        return sb.ToString();
    }

    private static (string Label, string Value)[] BuildRequestRows(
        string? message,
        bool isIndividual,
        UpgradeTargetPlanDto? targetPlan)
    {
        if (isIndividual)
        {
            return
            [
                ("Zeitpunkt", FormatDateTime(DateTime.UtcNow)),
                ("Art der Anfrage", "Individuelles Upgrade / Beratungstermin"),
                ("Gewünschtes Upgrade", "Individuelles Upgrade / Beratungstermin"),
                ("Preis", UpgradeTargetPlanDisplayHelper.IndividualPriceMessage),
                ("Nachricht des Admins", string.IsNullOrWhiteSpace(message) ? "—" : message.Trim())
            ];
        }

        var rows = new List<(string Label, string Value)>
        {
            ("Zeitpunkt", FormatDateTime(DateTime.UtcNow)),
            ("Art der Anfrage", "Tarif-Upgrade"),
            ("Gewünschter Tarif", FormatTargetPlan(targetPlan)),
            ("Preis", targetPlan is null
                ? "—"
                : UpgradeTargetPlanDisplayHelper.FormatPriceSummary(targetPlan)),
            ("Nachricht des Admins", string.IsNullOrWhiteSpace(message) ? "—" : message.Trim())
        };

        return rows.ToArray();
    }

    private static (string Label, string Value)[] BuildLicenseRows(LicenseDetailsDto license)
    {
        var rows = new List<(string Label, string Value)>
        {
            ("Lizenznummer", license.LicenseNumber),
            ("Aktueller Tarif", FormatCurrentPlan(license)),
            ("Lizenzstatus", FormatStatusLabel(license.Status)),
            ("Gültig von", FormatDate(license.ValidFrom)),
            ("Gültig bis", FormatValidUntil(license.ValidUntil))
        };

        if (!string.IsNullOrWhiteSpace(license.BillingCycleDisplay))
        {
            rows.Add(("Abrechnung", license.BillingCycleDisplay));
        }

        if (!string.IsNullOrWhiteSpace(license.AmountDisplay))
        {
            rows.Add(("Betrag", license.AmountDisplay));
        }

        if (!string.IsNullOrWhiteSpace(license.NextInvoiceDateDisplay))
        {
            rows.Add(("Nächste Rechnung am", license.NextInvoiceDateDisplay));
        }

        return rows.ToArray();
    }

    private static (string Label, string Value)[] BuildUsageRows(LicenseDetailsDto license)
    {
        var usage = license.Usage;
        return
        [
            ("Mandanten", FormatLimit(usage.CurrentTenants, license.MaxTenants)),
            ("Admins", FormatLimit(usage.CurrentAdmins, license.MaxAdmins)),
            ("Speicher (MB)", FormatLimit(usage.CurrentStorageMb, license.MaxStorageMb)),
            ("Benutzer gesamt", usage.CurrentUsersTotal.ToString()),
            ("Auditoren gesamt", usage.CurrentAuditorsTotal.ToString()),
            ("DSFA gesamt", usage.CurrentDpiaTotal.ToString()),
            ("TOMs gesamt", usage.CurrentTomsTotal.ToString()),
            ("Dienstleister gesamt", usage.CurrentProcessorsTotal.ToString()),
            ("Laufende Maßnahmen gesamt", usage.CurrentActiveMeasuresTotal.ToString())
        ];
    }

    private static string FormatCurrentPlan(LicenseDetailsDto license)
    {
        if (!string.IsNullOrWhiteSpace(license.PlanDisplayName)
            && !string.Equals(license.PlanDisplayName, license.PlanName, StringComparison.Ordinal))
        {
            return $"{license.PlanDisplayName} ({license.PlanName})";
        }

        return license.PlanName;
    }

    private static string FormatTargetPlan(UpgradeTargetPlanDto? targetPlan)
    {
        if (targetPlan is null)
        {
            return "—";
        }

        if (!string.IsNullOrWhiteSpace(targetPlan.DisplayName)
            && !string.Equals(targetPlan.DisplayName, targetPlan.Name, StringComparison.Ordinal))
        {
            return $"{targetPlan.DisplayName} ({targetPlan.Name})";
        }

        return targetPlan.Name;
    }

    private static string FormatLimit(int current, int? limit) =>
        limit is null ? $"{current} / unbegrenzt" : $"{current} / {limit}";

    private static string FormatStatusLabel(string status) => status switch
    {
        "Active" => "Aktiv",
        "Suspended" => "Gesperrt",
        "Inactive" => "Inaktiv",
        _ => status
    };

    private static void AppendSectionHtml(StringBuilder sb, string title, (string Label, string Value)[] rows)
    {
        sb.Append("<h3>").Append(WebUtility.HtmlEncode(title)).Append("</h3><table style=\"border-collapse:collapse;\">");
        foreach (var (label, value) in rows)
        {
            sb.Append("<tr><td style=\"padding:2px 12px 2px 0;vertical-align:top;\"><strong>")
                .Append(WebUtility.HtmlEncode(label))
                .Append(":</strong></td><td>")
                .Append(WebUtility.HtmlEncode(value))
                .Append("</td></tr>");
        }

        sb.Append("</table>");
    }

    private static string FormatDateTime(DateTime value) =>
        value.ToLocalTime().ToString("g");

    private static string FormatDate(DateTime? value) =>
        value?.ToLocalTime().ToString("d") ?? "—";

    private static string FormatValidUntil(DateTime? validUntil) =>
        validUntil?.ToLocalTime().ToString("d") ?? "Keine Laufzeitbegrenzung hinterlegt";

    private async Task TryLogAuditAsync(
        LicenseDetailsDto license,
        int? tenantId,
        string? userId,
        bool isIndividual,
        UpgradeTargetPlanDto? targetPlan)
    {
        try
        {
            await logService.LogAuditAsync(
                action: "UpgradeRequestSent",
                description: "Upgrade-Anfrage gesendet.",
                entityType: "License",
                entityId: license.Id.ToString(),
                entityName: license.LicenseNumber,
                tenantId: tenantId,
                licenseId: license.Id,
                metadata: new
                {
                    TargetPlan = targetPlan?.Name,
                    IsIndividual = isIndividual,
                    RequestedByUserId = userId
                });
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
        object? metadata = null,
        string severity = "Information")
    {
        try
        {
            await logService.LogSystemAsync(
                action: action,
                description: description,
                severity: severity,
                entityType: "License",
                entityId: license.Id.ToString(),
                metadata: metadata);
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }
}
