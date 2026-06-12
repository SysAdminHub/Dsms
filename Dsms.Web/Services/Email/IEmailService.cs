namespace Dsms.Web.Services.Email;

/// <summary>Zentraler Emailversand der SaaS-Plattform.</summary>
public interface IEmailService
{
    Task<EmailOperationResult> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody = null,
        bool bypassEnabledCheck = false,
        IReadOnlyList<EmailAttachment>? attachments = null);

    Task<EmailOperationResult> SendTemplateEmailAsync(
        string toEmail,
        string templateKey,
        IReadOnlyDictionary<string, string> variables,
        bool bypassEnabledCheck = false,
        IReadOnlyList<EmailAttachment>? attachments = null);

    Task<EmailOperationResult> SendTestEmailAsync(string toEmail, bool bypassEnabledCheck = false);

    Task<EmailOperationResult> SendPasswordResetEmailAsync(
        string toEmail,
        string userName,
        string resetLink,
        int expiresInMinutes,
        int? tenantId = null,
        string? supportEmail = null);

    Task<EmailOperationResult> SendWelcomeSetPasswordEmailAsync(
        string toEmail,
        string userName,
        string tenantName,
        string inviteLink,
        int expiresInMinutes,
        int? tenantId = null,
        string? supportEmail = null);

    Task<EmailOperationResult> SendReminderEmailAsync(
        string toEmail,
        string userName,
        string reminderTitle,
        string reminderText,
        string dueDate,
        string actionLink,
        int? tenantId = null,
        string? supportEmail = null);
}
