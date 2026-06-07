using System.Net.Mail;
using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Email;

public sealed class EmailSettingsService(
    ApplicationDbContext db,
    IUserAccessService access,
    ICurrentUserContext currentUser,
    UserManager<ApplicationUser> userManager,
    IEmailSecretProtector secretProtector,
    IEmailService emailService) : IEmailSettingsService
{
    public async Task<EmailSettingsEditModel> GetForEditAsync()
    {
        await EnsureSuperuserAsync();

        var settings = await GetOrCreateSettingsAsync();
        var updatedByDisplay = await ResolveUpdatedByDisplayAsync(settings.UpdatedByUserId);

        return new EmailSettingsEditModel
        {
            SmtpHost = settings.SmtpHost,
            SmtpPort = settings.SmtpPort,
            SmtpUsername = settings.SmtpUsername,
            HasStoredPassword = secretProtector.HasProtectedValue(settings.EncryptedSmtpPassword),
            SenderEmail = settings.SenderEmail,
            SenderName = settings.SenderName,
            Encryption = settings.Encryption,
            IsEnabled = settings.IsEnabled,
            UpdatedAt = settings.UpdatedAt,
            UpdatedByDisplay = updatedByDisplay
        };
    }

    public async Task<EmailOperationResult> SaveAsync(EmailSettingsEditModel model)
    {
        await EnsureSuperuserAsync();

        var validation = Validate(model);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var settings = await GetOrCreateSettingsAsync();
        settings.SmtpHost = model.SmtpHost?.Trim();
        settings.SmtpPort = model.SmtpPort;
        settings.SmtpUsername = string.IsNullOrWhiteSpace(model.SmtpUsername) ? null : model.SmtpUsername.Trim();
        settings.SenderEmail = model.SenderEmail?.Trim();
        settings.SenderName = string.IsNullOrWhiteSpace(model.SenderName) ? null : model.SenderName.Trim();
        settings.Encryption = model.Encryption;
        settings.IsEnabled = model.IsEnabled;

        if (!string.IsNullOrWhiteSpace(model.NewSmtpPassword))
        {
            settings.EncryptedSmtpPassword = secretProtector.Protect(model.NewSmtpPassword);
        }

        settings.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedByUserId = await currentUser.GetUserIdAsync();

        await db.SaveChangesAsync();
        return EmailOperationResult.Ok("SMTP-Einstellungen wurden gespeichert.");
    }

    public async Task<EmailOperationResult> SendTestEmailAsync(string recipientEmail)
    {
        await EnsureSuperuserAsync();

        if (!IsValidEmail(recipientEmail))
        {
            return EmailOperationResult.Fail("Bitte eine gültige Testempfänger-Email eingeben.");
        }

        return await emailService.SendTestEmailAsync(recipientEmail.Trim(), bypassEnabledCheck: false);
    }

    public async Task<EmailSettings?> GetSettingsForSendingAsync()
    {
        return await db.EmailSettings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync();
    }

    private async Task<EmailSettings> GetOrCreateSettingsAsync()
    {
        var settings = await db.EmailSettings.OrderBy(s => s.Id).FirstOrDefaultAsync();
        if (settings is not null)
        {
            return settings;
        }

        settings = new EmailSettings();
        db.EmailSettings.Add(settings);
        await db.SaveChangesAsync();
        return settings;
    }

    private static EmailOperationResult Validate(EmailSettingsEditModel model)
    {
        if (!Enum.IsDefined(typeof(SmtpEncryption), model.Encryption))
        {
            return EmailOperationResult.Fail("Ungültige Verschlüsselungsart.");
        }

        if (model.SmtpPort is < 1 or > 65535)
        {
            return EmailOperationResult.Fail("SMTP-Port muss zwischen 1 und 65535 liegen.");
        }

        if (!model.IsEnabled)
        {
            return EmailOperationResult.Ok();
        }

        if (string.IsNullOrWhiteSpace(model.SmtpHost))
        {
            return EmailOperationResult.Fail("SMTP-Host ist erforderlich, wenn der Emailversand aktiviert ist.");
        }

        if (model.SmtpPort is null)
        {
            return EmailOperationResult.Fail("SMTP-Port ist erforderlich, wenn der Emailversand aktiviert ist.");
        }

        if (string.IsNullOrWhiteSpace(model.SenderEmail))
        {
            return EmailOperationResult.Fail("Absender-Email ist erforderlich, wenn der Emailversand aktiviert ist.");
        }

        if (!IsValidEmail(model.SenderEmail))
        {
            return EmailOperationResult.Fail("Absender-Email muss eine gültige Emailadresse sein.");
        }

        return EmailOperationResult.Ok();
    }

    private async Task EnsureSuperuserAsync()
    {
        if (!await access.IsSuperuserAsync())
        {
            throw new UnauthorizedAccessException("Nur Superuser dürfen Email-Einstellungen verwalten.");
        }
    }

    private async Task<string?> ResolveUpdatedByDisplayAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var user = await userManager.FindByIdAsync(userId);
        return user?.DisplayName ?? user?.Email;
    }

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
