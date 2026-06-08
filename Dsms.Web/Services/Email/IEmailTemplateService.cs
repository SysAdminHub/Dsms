using Dsms.Web.Domain.Entities;

namespace Dsms.Web.Services.Email;

/// <summary>Verwaltung globaler Email-Vorlagen (nur Superuser).</summary>
public interface IEmailTemplateService
{
    Task<IReadOnlyList<EmailTemplate>> ListAsync();
    Task<EmailTemplate?> GetByIdAsync(int id);
    Task<EmailTemplateEditModel?> GetForEditAsync(int id);
    Task<EmailOperationResult> SaveAsync(int id, EmailTemplateEditModel model);
    Task<EmailOperationResult> SendTestFromTemplateAsync(int id, string recipientEmail);
    RenderedEmail GetPreview(EmailTemplateEditModel model);
}

public sealed class EmailTemplateEditModel
{
    public int Id { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedByDisplay { get; set; }
}
