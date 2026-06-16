using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Services.Email;

/// <summary>Verwaltung der zentralen SMTP-Einstellungen (nur Superuser).</summary>
public interface IEmailSettingsService
{
    Task<EmailSettingsEditModel> GetForEditAsync();
    Task<EmailOperationResult> SaveAsync(EmailSettingsEditModel model);
    Task<EmailOperationResult> SendTestEmailAsync(string recipientEmail);
}

/// <summary>Formularmodell ohne Klartext-Passwort.</summary>
public sealed class EmailSettingsEditModel
{
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }

    /// <summary>Neues Passwort/API-Key – leer lassen, um das bestehende beizubehalten.</summary>
    public string? NewSmtpPassword { get; set; }

    public bool HasStoredPassword { get; set; }
    public string? SenderEmail { get; set; }
    public string? SenderName { get; set; }
    public SmtpEncryption Encryption { get; set; } = SmtpEncryption.StartTls;
    public bool IsEnabled { get; set; }
    public bool SystemNotificationsEnabled { get; set; }
    public string? SystemNotificationRecipientEmail { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedByDisplay { get; set; }
}
