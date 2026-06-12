using System.Globalization;
using Dsms.Web.Configuration;
using Dsms.Web.Domain;
using Dsms.Web.Services.Email;
using Dsms.Web.Services.Legal;
using Dsms.Web.Services.Logging;
using Microsoft.Extensions.Options;

namespace Dsms.Web.Services.Signup;

public interface ISignupLegalEmailService
{
    Task TrySendSignupLegalConfirmationAsync(
        int tenantId,
        string recipientEmail,
        string contactName,
        DateTime registrationDateUtc,
        CancellationToken cancellationToken = default);
}

public sealed class SignupLegalEmailService(
    ILegalPdfService legalPdfService,
    ILegalDocumentService legalDocumentService,
    IEmailService emailService,
    ILogService logService,
    IOptions<AppBrandingOptions> brandingOptions,
    ILogger<SignupLegalEmailService> logger) : ISignupLegalEmailService
{
    public async Task TrySendSignupLegalConfirmationAsync(
        int tenantId,
        string recipientEmail,
        string contactName,
        DateTime registrationDateUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            return;
        }

        try
        {
            var metadata = await legalDocumentService.GetMetadataAsync(cancellationToken);
            if (metadata is null)
            {
                logger.LogWarning("Legal-Bestätigungsmail: legal-documents.json nicht gefunden.");
                return;
            }

            var agbPdf = await legalPdfService.GenerateDocumentPdfForTenantAsync(LegalDocumentKeys.Agb, tenantId, cancellationToken);
            var privacyPdf = await legalPdfService.GenerateDocumentPdfForTenantAsync(
                LegalDocumentKeys.Datenschutzerklaerung,
                tenantId,
                cancellationToken);
            var avvPackagePdf = await legalPdfService.GenerateAvvPackagePdfForTenantAsync(tenantId, cancellationToken);

            if (agbPdf is null || privacyPdf is null || avvPackagePdf is null)
            {
                logger.LogWarning(
                    "Legal-Bestätigungsmail: PDF-Generierung unvollständig für Tenant {TenantId}.",
                    tenantId);
                await TryLogEmailFailedAsync(tenantId, recipientEmail, "PDF-Generierung unvollständig.");
                return;
            }

            var branding = brandingOptions.Value;
            var variables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["AppName"] = branding.ProductName,
                ["ProductName"] = branding.ProductName,
                ["CustomerContactName"] = string.IsNullOrWhiteSpace(contactName) ? "Kunde" : contactName.Trim(),
                ["LegalVersion"] = metadata.Version,
                ["RegistrationDate"] = registrationDateUtc.ToLocalTime().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                ["ProviderName"] = "Stefan Keller – The SysAdminHub"
            };

            var attachments = new List<EmailAttachment>
            {
                ToAttachment(agbPdf),
                ToAttachment(privacyPdf),
                ToAttachment(avvPackagePdf)
            };

            var result = await emailService.SendTemplateEmailAsync(
                recipientEmail,
                EmailTemplateKeys.SignupLegalConfirmation,
                variables,
                attachments: attachments);

            if (!result.Succeeded)
            {
                logger.LogWarning(
                    "Legal-Bestätigungsmail konnte nicht gesendet werden an {Recipient}: {Message}",
                    recipientEmail,
                    result.Message);
                await TryLogEmailFailedAsync(tenantId, recipientEmail, result.Message ?? "Unbekannter Fehler");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Legal-Bestätigungsmail fehlgeschlagen für Tenant {TenantId}", tenantId);
            await TryLogEmailFailedAsync(tenantId, recipientEmail, ex.Message);
        }
    }

    private static EmailAttachment ToAttachment(LegalPdfResult pdf) => new()
    {
        FileName = pdf.FileName,
        ContentType = pdf.ContentType,
        ContentBytes = pdf.Content
    };

    private async Task TryLogEmailFailedAsync(int tenantId, string recipientEmail, string detail)
    {
        try
        {
            await logService.LogSystemAsync(
                action: "SignupLegalEmailFailed",
                description: $"Legal-Bestätigungsmail konnte nicht gesendet werden ({recipientEmail}).",
                severity: "Warning",
                tenantId: tenantId,
                metadata: new { Detail = detail });
        }
        catch
        {
            // Logging darf den Signup-Flow nicht beeinträchtigen.
        }
    }
}
