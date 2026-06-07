using System.Net.Mail;
using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace Dsms.Web.Services.Email;

public sealed class EmailService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IEmailSecretProtector secretProtector,
    IEmailTemplateRenderer templateRenderer,
    ILogger<EmailService> logger) : IEmailService
{
    private const string DefaultTestSubject = "DSMS Testmail";
    private const string DefaultTestHtml =
        "Dies ist eine Testmail aus dem DSMS. Wenn diese Email angekommen ist, funktioniert der zentrale Emailversand.";

    public async Task<EmailOperationResult> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody = null,
        bool bypassEnabledCheck = false)
    {
        if (!IsValidEmail(toEmail))
        {
            return EmailOperationResult.Fail("Empfänger-Email ist ungültig.");
        }

        var settings = await LoadSettingsAsync();
        var validation = ValidateSettings(settings, bypassEnabledCheck);
        if (!validation.Succeeded)
        {
            return validation;
        }

        try
        {
            var message = BuildMimeMessage(settings!, toEmail.Trim(), subject, htmlBody, textBody);
            await SendViaSmtpAsync(settings!, message);
            return EmailOperationResult.Ok("Email wurde versendet.");
        }
        catch (SmtpCommandException ex)
        {
            logger.LogWarning(ex, "SMTP-Befehl fehlgeschlagen beim Versand an {Recipient}", toEmail);
            return EmailOperationResult.Fail(
                "Email konnte nicht gesendet werden. Bitte SMTP-Einstellungen prüfen.",
                MapSmtpError(ex.Message));
        }
        catch (SmtpProtocolException ex)
        {
            logger.LogWarning(ex, "SMTP-Protokollfehler beim Versand an {Recipient}", toEmail);
            return EmailOperationResult.Fail(
                "Email konnte nicht gesendet werden. Bitte SMTP-Einstellungen prüfen.",
                MapSmtpError(ex.Message));
        }
        catch (AuthenticationException ex)
        {
            logger.LogWarning(ex, "SMTP-Authentifizierung fehlgeschlagen");
            return EmailOperationResult.Fail(
                "Email konnte nicht gesendet werden. Bitte SMTP-Einstellungen prüfen.",
                "Fehlerdetails: Authentifizierung fehlgeschlagen.");
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or OperationCanceledException)
        {
            logger.LogWarning(ex, "SMTP-Server nicht erreichbar");
            return EmailOperationResult.Fail(
                "Email konnte nicht gesendet werden. Bitte SMTP-Einstellungen prüfen.",
                "Fehlerdetails: SMTP-Server nicht erreichbar.");
        }
    }

    public async Task<EmailOperationResult> SendTemplateEmailAsync(
        string toEmail,
        string templateKey,
        IReadOnlyDictionary<string, string> variables,
        bool bypassEnabledCheck = false)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var template = await db.EmailTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateKey == templateKey);

        if (template is null)
        {
            return EmailOperationResult.Fail($"Email-Vorlage '{templateKey}' wurde nicht gefunden.");
        }

        if (!template.IsEnabled)
        {
            return EmailOperationResult.Fail($"Email-Vorlage '{templateKey}' ist deaktiviert.");
        }

        var rendered = templateRenderer.Render(template.Subject, template.HtmlContent, template.TextContent, variables);
        return await SendEmailAsync(toEmail, rendered.Subject, rendered.HtmlBody, rendered.TextBody, bypassEnabledCheck);
    }

    public Task<EmailOperationResult> SendTestEmailAsync(string toEmail, bool bypassEnabledCheck = false) =>
        SendTestEmailInternalAsync(toEmail, bypassEnabledCheck);

    public Task<EmailOperationResult> SendPasswordResetEmailAsync(
        string toEmail,
        string userName,
        string resetLink,
        int expiresInMinutes,
        int? tenantId = null,
        string? supportEmail = null)
    {
        var variables = BuildCommonVariables(userName, toEmail);
        variables["ResetLink"] = resetLink;
        variables["ExpiresInMinutes"] = expiresInMinutes.ToString();
        if (!string.IsNullOrWhiteSpace(supportEmail))
        {
            variables["SupportEmail"] = supportEmail;
        }
        return SendTemplateEmailAsync(toEmail, EmailTemplateKeys.PasswordReset, variables);
    }

    public Task<EmailOperationResult> SendWelcomeSetPasswordEmailAsync(
        string toEmail,
        string userName,
        string tenantName,
        string inviteLink,
        int expiresInMinutes,
        int? tenantId = null)
    {
        var variables = BuildCommonVariables(userName, toEmail);
        variables["TenantName"] = tenantName;
        variables["InviteLink"] = inviteLink;
        variables["ExpiresInMinutes"] = expiresInMinutes.ToString();
        return SendTemplateEmailAsync(toEmail, EmailTemplateKeys.WelcomeSetPassword, variables);
    }

    public Task<EmailOperationResult> SendReminderEmailAsync(
        string toEmail,
        string userName,
        string reminderTitle,
        string reminderText,
        string dueDate,
        string actionLink,
        int? tenantId = null)
    {
        var variables = BuildCommonVariables(userName, toEmail);
        variables["ReminderTitle"] = reminderTitle;
        variables["ReminderText"] = reminderText;
        variables["DueDate"] = dueDate;
        variables["ActionLink"] = actionLink;
        return SendTemplateEmailAsync(toEmail, EmailTemplateKeys.Reminder, variables);
    }

    private async Task<EmailOperationResult> SendTestEmailInternalAsync(string toEmail, bool bypassEnabledCheck)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var template = await db.EmailTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateKey == EmailTemplateKeys.TestEmail);

        if (template is not null)
        {
            var variables = EmailTemplateSampleData.AsDictionary();
            var rendered = templateRenderer.Render(template.Subject, template.HtmlContent, template.TextContent, variables);
            return await SendEmailAsync(toEmail, rendered.Subject, rendered.HtmlBody, rendered.TextBody, bypassEnabledCheck);
        }

        return await SendEmailAsync(toEmail, DefaultTestSubject, $"<p>{DefaultTestHtml}</p>", DefaultTestHtml, bypassEnabledCheck);
    }

    private static Dictionary<string, string> BuildCommonVariables(string userName, string userEmail)
    {
        var variables = new Dictionary<string, string>(EmailTemplateSampleData.AsDictionary())
        {
            ["UserName"] = userName,
            ["UserEmail"] = userEmail,
            ["AppName"] = EmailTemplateSampleData.AppName
        };
        return variables;
    }

    private async Task<EmailSettings?> LoadSettingsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.EmailSettings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync();
    }

    private static EmailOperationResult ValidateSettings(EmailSettings? settings, bool bypassEnabledCheck)
    {
        if (settings is null)
        {
            return EmailOperationResult.Fail("Emailversand ist nicht konfiguriert.");
        }

        if (!settings.IsEnabled && !bypassEnabledCheck)
        {
            return EmailOperationResult.Fail("Emailversand ist deaktiviert.");
        }

        if (string.IsNullOrWhiteSpace(settings.SmtpHost) || settings.SmtpPort is null)
        {
            return EmailOperationResult.Fail("SMTP-Einstellungen sind unvollständig.");
        }

        if (string.IsNullOrWhiteSpace(settings.SenderEmail) || !IsValidEmail(settings.SenderEmail))
        {
            return EmailOperationResult.Fail("Absender-Email ist ungültig oder nicht konfiguriert.");
        }

        return EmailOperationResult.Ok();
    }

    private MimeMessage BuildMimeMessage(
        EmailSettings settings,
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody)
    {
        var from = new MailboxAddress(settings.SenderName ?? settings.SenderEmail, settings.SenderEmail!);
        var message = new MimeMessage
        {
            Subject = subject
        };
        message.From.Add(from);
        message.To.Add(MailboxAddress.Parse(toEmail));

        var builder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = textBody ?? StripHtml(htmlBody)
        };
        message.Body = builder.ToMessageBody();
        return message;
    }

    private async Task SendViaSmtpAsync(EmailSettings settings, MimeMessage message)
    {
        using var client = new MailKit.Net.Smtp.SmtpClient();
        var secureSocketOptions = MapEncryption(settings.Encryption);

        await client.ConnectAsync(settings.SmtpHost!, settings.SmtpPort!.Value, secureSocketOptions);

        if (!string.IsNullOrWhiteSpace(settings.SmtpUsername))
        {
            var password = secretProtector.HasProtectedValue(settings.EncryptedSmtpPassword)
                ? secretProtector.Unprotect(settings.EncryptedSmtpPassword!)
                : string.Empty;
            await client.AuthenticateAsync(settings.SmtpUsername, password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private static SecureSocketOptions MapEncryption(SmtpEncryption encryption) => encryption switch
    {
        SmtpEncryption.None => SecureSocketOptions.None,
        SmtpEncryption.StartTls => SecureSocketOptions.StartTls,
        SmtpEncryption.SslTls => SecureSocketOptions.SslOnConnect,
        _ => SecureSocketOptions.Auto
    };

    private static string MapSmtpError(string message)
    {
        if (message.Contains("auth", StringComparison.OrdinalIgnoreCase)
            || message.Contains("authentication", StringComparison.OrdinalIgnoreCase))
        {
            return "Fehlerdetails: Authentifizierung fehlgeschlagen.";
        }

        return $"Fehlerdetails: {message}";
    }

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", string.Empty).Trim();

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            _ = new MailAddress(email.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }
}
