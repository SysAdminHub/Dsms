using System.Net;
using System.Text;
using Dsms.Web.Configuration;
using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.Email;
using Dsms.Web.Services.Logging;
using Dsms.Web.Services.PendingSignups;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dsms.Web.Services.Signup;

public sealed class SignupNotificationService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IEmailSettingsService emailSettingsService,
    IEmailService emailService,
    ILogService logService,
    ILogger<SignupNotificationService> logger,
    IOptions<AppBrandingOptions> brandingOptions) : ISignupNotificationService
{
    private readonly AppBrandingOptions _branding = brandingOptions.Value;
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

        var isFree = PendingSignupDisplayHelper.IsFreeSignup(signup.PaymentProvider, signup.Amount);
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

    private string BuildSubject(PendingSignup signup, bool isFree)
    {
        var customer = signup.CustomerName.Trim();
        var plan = signup.PlanDisplayNameSnapshot ?? signup.PlanNameSnapshot ?? "Tarif";
        var productName = _branding.ProductName;
        return isFree
            ? $"Neue {productName}-Registrierung: {customer} ({plan})"
            : $"Neue kostenpflichtige {productName}-Registrierung: {customer} ({plan})";
    }

    private string BuildHtmlBody(
        PendingSignup signup,
        License? license,
        bool isFree,
        bool passwordSetupEmailSent)
    {
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family: sans-serif; line-height: 1.5;\">");
        sb.Append($"<h2>Neue {_branding.ProductName}-Registrierung</h2>");

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

        var billingCycle = PendingSignupDisplayHelper.ResolveBillingCycle(signup.BillingCycle, signup.MetadataJson);
        var amountDisplay = PendingSignupDisplayHelper.FormatSignupAmountDisplay(
            signup.PaymentProvider,
            signup.Amount,
            signup.FinalAmount,
            signup.Currency,
            signup.BillingCycle,
            signup.MetadataJson,
            signup.DiscountTypeSnapshot,
            signup.DiscountCodeSnapshot);
        var followUpBilling = PendingSignupDisplayHelper.GetFollowUpBillingDisplay(
            signup.BillingCycle,
            signup.MetadataJson,
            signup.DiscountTypeSnapshot,
            signup.DiscountFreeMonthsSnapshot,
            signup.OriginalAmount,
            signup.Currency);

        var tariffRows = new List<(string Label, string Value)>
        {
            ("Planname", signup.PlanDisplayNameSnapshot ?? signup.PlanNameSnapshot ?? "—"),
            ("Kostenlos", isFree ? "Ja" : "Nein"),
            ("Abrechnung", isFree ? "Nicht erforderlich" : BillingCycleDisplayHelper.GetDisplayName(billingCycle)),
            ("Betrag", isFree ? "Kostenlos" : amountDisplay),
            ("Nächste Rechnung", BillingStatusDisplayHelper.FormatNextInvoiceDate(signup.NextInvoiceDate, isFree)),
            ("Zahlungsart", isFree ? "—" : "Manuelle Rechnung / Rechnung folgt separat"),
            ("Lizenznummer", signup.ProvisionedLicenseNumber ?? license?.LicenseNumber ?? "—"),
            ("Lizenzstatus", license?.Status ?? "—"),
            ("Lizenz gültig bis", FormatDate(license?.ValidUntil))
        };

        if (!isFree)
        {
            tariffRows.Insert(4, ("Aktuell gültiger Betrag", PendingSignupDisplayHelper.FormatCurrentBillingDisplay(
                signup.CurrentBillingAmount,
                signup.CurrentBillingCurrency,
                signup.CurrentBillingCycle,
                signup.FinalAmount,
                signup.Amount,
                signup.Currency,
                signup.BillingCycle,
                signup.MetadataJson,
                signup.DiscountTypeSnapshot,
                signup.OriginalAmount)));
        }

        AppendSectionHtml(sb, "Tarif / Lizenz", tariffRows.ToArray());
        AppendSectionHtml(sb, "Rabattcode", SignupNotificationDiscountHelper.BuildDiscountRows(signup, license).ToArray());

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

    private string BuildTextBody(
        PendingSignup signup,
        License? license,
        bool isFree,
        bool passwordSetupEmailSent)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Neue {_branding.ProductName}-Registrierung");
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
        if (isFree)
        {
            sb.AppendLine("Abrechnung: Nicht erforderlich");
            sb.AppendLine("Betrag: Kostenlos");
            sb.AppendLine("Nächste Rechnung: Nicht erforderlich");
        }
        else
        {
            var billingCycle = PendingSignupDisplayHelper.ResolveBillingCycle(signup.BillingCycle, signup.MetadataJson);
            sb.AppendLine($"Abrechnung: {BillingCycleDisplayHelper.GetDisplayName(billingCycle)}");
            sb.AppendLine($"Betrag: {PendingSignupDisplayHelper.FormatSignupAmountDisplay(signup.PaymentProvider, signup.Amount, signup.FinalAmount, signup.Currency, signup.BillingCycle, signup.MetadataJson, signup.DiscountTypeSnapshot, signup.DiscountCodeSnapshot)}");
            sb.AppendLine($"Aktuell gültiger Betrag: {PendingSignupDisplayHelper.FormatCurrentBillingDisplay(signup.CurrentBillingAmount, signup.CurrentBillingCurrency, signup.CurrentBillingCycle, signup.FinalAmount, signup.Amount, signup.Currency, signup.BillingCycle, signup.MetadataJson, signup.DiscountTypeSnapshot, signup.OriginalAmount)}");
            sb.AppendLine($"Nächste Rechnung: {BillingStatusDisplayHelper.FormatNextInvoiceDate(signup.NextInvoiceDate, isFree: false)}");
            sb.AppendLine("Zahlungsart: Manuelle Rechnung / Rechnung folgt separat");
        }
        sb.AppendLine($"Lizenznummer: {signup.ProvisionedLicenseNumber ?? license?.LicenseNumber ?? "—"}");
        sb.AppendLine($"Lizenzstatus: {license?.Status ?? "—"}");
        sb.AppendLine($"Lizenz gültig bis: {FormatDate(license?.ValidUntil)}");
        sb.AppendLine();
        sb.AppendLine("=== Rabattcode ===");
        foreach (var (label, value) in SignupNotificationDiscountHelper.BuildDiscountRows(signup, license))
        {
            sb.AppendLine($"{label}: {value}");
        }
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

    private static string FormatDateTime(DateTime? value) =>
        value?.ToLocalTime().ToString("g") ?? "—";

    private static string FormatDate(DateTime? value) =>
        value?.ToLocalTime().ToString("d") ?? "—";

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
