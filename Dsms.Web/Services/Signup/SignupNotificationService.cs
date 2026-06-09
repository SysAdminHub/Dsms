using System.Net;
using System.Text;
using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Email;
using Dsms.Web.Services.Logging;
using Dsms.Web.Services.PendingSignups;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Signup;

public sealed class SignupNotificationService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IEmailSettingsService emailSettingsService,
    IEmailService emailService,
    ILogService logService,
    ILogger<SignupNotificationService> logger) : ISignupNotificationService
{
    public async Task TrySendPublicSignupNotificationAsync(Guid pendingSignupId, bool passwordSetupEmailSent)
    {
        var emailSettings = await emailSettingsService.GetSettingsForSendingAsync();
        if (emailSettings is null || !emailSettings.SystemNotificationsEnabled)
        {
            logger.LogInformation("Systembenachrichtigungen sind deaktiviert.");
            return;
        }

        var recipient = emailSettings.SystemNotificationRecipientEmail?.Trim();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            logger.LogWarning("Keine Empfängeradresse für Systembenachrichtigungen konfiguriert.");
            await TryLogSystemAsync(
                "SignupNotificationSkipped",
                "Keine Empfängeradresse für Systembenachrichtigungen konfiguriert.",
                pendingSignupId);
            return;
        }

        logger.LogInformation(
            "Interne Signup-Benachrichtigung wird vorbereitet für PendingSignup {PendingSignupId}.",
            pendingSignupId);

        await using var db = await dbFactory.CreateDbContextAsync();
        var signup = await db.PendingSignups.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pendingSignupId);
        if (signup is null)
        {
            logger.LogWarning(
                "PendingSignup {PendingSignupId} nicht gefunden – Benachrichtigung übersprungen.",
                pendingSignupId);
            return;
        }

        License? license = null;
        if (signup.ProvisionedLicenseId is Guid licenseId)
        {
            license = await db.Licenses.AsNoTracking().FirstOrDefaultAsync(l => l.Id == licenseId);
        }

        var isFree = IsFreeSignup(signup);
        var subject = BuildSubject(signup, isFree);
        var htmlBody = BuildHtmlBody(signup, license, isFree, passwordSetupEmailSent);
        var textBody = BuildTextBody(signup, license, isFree, passwordSetupEmailSent);

        var result = await emailService.SendEmailAsync(recipient, subject, htmlBody, textBody);
        if (result.Succeeded)
        {
            logger.LogInformation(
                "Interne Signup-Benachrichtigung erfolgreich versendet für PendingSignup {PendingSignupId}.",
                pendingSignupId);
            await TryLogSystemAsync(
                "SignupNotificationSent",
                "Interne Signup-Benachrichtigung erfolgreich versendet.",
                pendingSignupId,
                new { Recipient = recipient, signup.CustomerName, signup.PlanDisplayNameSnapshot });
            return;
        }

        logger.LogError(
            "Fehler beim Versand der internen Signup-Benachrichtigung für PendingSignup {PendingSignupId}: {Message}",
            pendingSignupId,
            result.Message);
        await TryLogSystemAsync(
            "SignupNotificationFailed",
            "Fehler beim Versand der internen Signup-Benachrichtigung.",
            pendingSignupId,
            new { Recipient = recipient, signup.CustomerName, Detail = result.Message },
            severity: "Error");
    }

    private static string BuildSubject(PendingSignup signup, bool isFree)
    {
        var customer = signup.CustomerName.Trim();
        var plan = signup.PlanDisplayNameSnapshot ?? signup.PlanNameSnapshot ?? "Tarif";
        return isFree
            ? $"Neue DSMS-Registrierung: {customer} ({plan})"
            : $"Neue kostenpflichtige DSMS-Registrierung: {customer} ({plan})";
    }

    private static string BuildHtmlBody(
        PendingSignup signup,
        License? license,
        bool isFree,
        bool passwordSetupEmailSent)
    {
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family: sans-serif; line-height: 1.5;\">");
        sb.Append("<h2>Neue DSMS-Registrierung</h2>");

        AppendSectionHtml(sb, "Registrierung", [
            ("Zeitpunkt", FormatDateTime(signup.CreatedAt)),
            ("Quelle", signup.Source ?? "—"),
            ("Status", signup.Status),
            ("Provisioniert am", FormatDateTime(signup.ProvisionedAt)),
            ("Passwortmail versendet", passwordSetupEmailSent ? "Ja" : "Nein")
        ]);

        AppendSectionHtml(sb, "Kunde", [
            ("Unternehmensname", signup.CustomerName),
            ("Unternehmens-E-Mail", signup.CustomerEmail ?? "—"),
            ("Mandantenname", signup.TenantName),
            ("Rechtlicher Name", signup.TenantLegalName ?? "—")
        ]);

        AppendSectionHtml(sb, "Administrator", [
            ("Name", signup.AdminDisplayName),
            ("E-Mail", signup.AdminEmail)
        ]);

        var billingCycle = PendingSignupDisplayHelper.GetBillingCycle(signup.MetadataJson);
        AppendSectionHtml(sb, "Tarif / Lizenz", [
            ("Planname", signup.PlanDisplayNameSnapshot ?? signup.PlanNameSnapshot ?? "—"),
            ("Kostenlos", isFree ? "Ja" : "Nein"),
            ("Betrag", PendingSignupDisplayHelper.FormatAmount(signup.Amount, signup.Currency)),
            ("Abrechnung", FormatBillingCycle(billingCycle)),
            ("Lizenznummer", signup.ProvisionedLicenseNumber ?? license?.LicenseNumber ?? "—"),
            ("Lizenzstatus", license?.Status ?? "—"),
            ("Lizenz gültig bis", FormatDate(license?.ValidUntil))
        ]);

        if (isFree)
        {
            sb.Append("<h3>Rechnungsdaten</h3><p>Keine Rechnungsdaten erforderlich.</p>");
            sb.Append("<p><strong>Hinweis:</strong> Keine Rechnung erforderlich.</p>");
        }
        else
        {
            AppendSectionHtml(sb, "Rechnungsdaten", [
                ("Rechnungsempfänger / Firma", signup.BillingCompanyName ?? "—"),
                ("Rechnungs-E-Mail", signup.BillingEmail ?? "—"),
                ("Straße und Hausnummer", signup.BillingStreet ?? "—"),
                ("PLZ", signup.BillingPostalCode ?? "—"),
                ("Ort", signup.BillingCity ?? "—"),
                ("Land", signup.BillingCountry ?? "—"),
                ("Umsatzsteuer-ID", signup.BillingVatId ?? "—"),
                ("Bestellnummer / Referenz", signup.BillingReference ?? "—")
            ]);
            sb.Append("<p><strong>Hinweis:</strong> Bitte manuell Rechnung erstellen und Lizenzlaufzeit nach Zahlung/Absprache im Backend anpassen.</p>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildTextBody(
        PendingSignup signup,
        License? license,
        bool isFree,
        bool passwordSetupEmailSent)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Neue DSMS-Registrierung");
        sb.AppendLine();
        sb.AppendLine("=== Registrierung ===");
        sb.AppendLine($"Zeitpunkt: {FormatDateTime(signup.CreatedAt)}");
        sb.AppendLine($"Quelle: {signup.Source ?? "—"}");
        sb.AppendLine($"Status: {signup.Status}");
        sb.AppendLine($"Provisioniert am: {FormatDateTime(signup.ProvisionedAt)}");
        sb.AppendLine($"Passwortmail versendet: {(passwordSetupEmailSent ? "Ja" : "Nein")}");
        sb.AppendLine();
        sb.AppendLine("=== Kunde ===");
        sb.AppendLine($"Unternehmensname: {signup.CustomerName}");
        sb.AppendLine($"Unternehmens-E-Mail: {signup.CustomerEmail ?? "—"}");
        sb.AppendLine($"Mandantenname: {signup.TenantName}");
        sb.AppendLine($"Rechtlicher Name: {signup.TenantLegalName ?? "—"}");
        sb.AppendLine();
        sb.AppendLine("=== Administrator ===");
        sb.AppendLine($"Name: {signup.AdminDisplayName}");
        sb.AppendLine($"E-Mail: {signup.AdminEmail}");
        sb.AppendLine();
        sb.AppendLine("=== Tarif / Lizenz ===");
        sb.AppendLine($"Planname: {signup.PlanDisplayNameSnapshot ?? signup.PlanNameSnapshot ?? "—"}");
        sb.AppendLine($"Kostenlos: {(isFree ? "Ja" : "Nein")}");
        sb.AppendLine($"Betrag: {PendingSignupDisplayHelper.FormatAmount(signup.Amount, signup.Currency)}");
        sb.AppendLine($"Abrechnung: {FormatBillingCycle(PendingSignupDisplayHelper.GetBillingCycle(signup.MetadataJson))}");
        sb.AppendLine($"Lizenznummer: {signup.ProvisionedLicenseNumber ?? license?.LicenseNumber ?? "—"}");
        sb.AppendLine($"Lizenzstatus: {license?.Status ?? "—"}");
        sb.AppendLine($"Lizenz gültig bis: {FormatDate(license?.ValidUntil)}");
        sb.AppendLine();

        if (isFree)
        {
            sb.AppendLine("=== Rechnungsdaten ===");
            sb.AppendLine("Keine Rechnungsdaten erforderlich.");
            sb.AppendLine();
            sb.AppendLine("Hinweis: Keine Rechnung erforderlich.");
        }
        else
        {
            sb.AppendLine("=== Rechnungsdaten ===");
            sb.AppendLine($"Rechnungsempfänger / Firma: {signup.BillingCompanyName ?? "—"}");
            sb.AppendLine($"Rechnungs-E-Mail: {signup.BillingEmail ?? "—"}");
            sb.AppendLine($"Straße: {signup.BillingStreet ?? "—"}");
            sb.AppendLine($"PLZ: {signup.BillingPostalCode ?? "—"}");
            sb.AppendLine($"Ort: {signup.BillingCity ?? "—"}");
            sb.AppendLine($"Land: {signup.BillingCountry ?? "—"}");
            sb.AppendLine($"USt-IdNr.: {signup.BillingVatId ?? "—"}");
            sb.AppendLine($"Referenz: {signup.BillingReference ?? "—"}");
            sb.AppendLine();
            sb.AppendLine("Hinweis: Bitte manuell Rechnung erstellen und Lizenzlaufzeit nach Zahlung/Absprache im Backend anpassen.");
        }

        return sb.ToString();
    }

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

    private static bool IsFreeSignup(PendingSignup signup) =>
        string.Equals(signup.PaymentProvider, "None", StringComparison.OrdinalIgnoreCase)
        || signup.Amount == 0;

    private static string FormatDateTime(DateTime? value) =>
        value?.ToLocalTime().ToString("g") ?? "—";

    private static string FormatDate(DateTime? value) =>
        value?.ToLocalTime().ToString("d") ?? "—";

    private static string FormatBillingCycle(string? cycle) => cycle switch
    {
        "Yearly" => "Jährlich",
        "Monthly" => "Monatlich",
        "None" => "—",
        _ => cycle ?? "—"
    };

    private async Task TryLogSystemAsync(
        string action,
        string description,
        Guid pendingSignupId,
        object? metadata = null,
        string severity = "Information")
    {
        try
        {
            await logService.LogSystemAsync(
                action: action,
                description: description,
                severity: severity,
                entityType: "PendingSignup",
                entityId: pendingSignupId.ToString(),
                metadata: metadata);
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }
}
