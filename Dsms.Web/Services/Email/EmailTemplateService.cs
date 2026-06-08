using System.Net.Mail;
using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Email;

public sealed class EmailTemplateService(
    ApplicationDbContext db,
    IUserAccessService access,
    ICurrentUserContext currentUser,
    UserManager<ApplicationUser> userManager,
    IEmailTemplateRenderer templateRenderer,
    IEmailService emailService) : IEmailTemplateService
{
    public async Task<IReadOnlyList<EmailTemplate>> ListAsync()
    {
        await EnsureSuperuserAsync();
        return await db.EmailTemplates.AsNoTracking()
            .OrderBy(t => t.DisplayName)
            .ToListAsync();
    }

    public async Task<EmailTemplate?> GetByIdAsync(int id)
    {
        await EnsureSuperuserAsync();
        return await db.EmailTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<EmailTemplateEditModel?> GetForEditAsync(int id)
    {
        await EnsureSuperuserAsync();

        var template = await db.EmailTemplates.FirstOrDefaultAsync(t => t.Id == id);
        if (template is null)
        {
            return null;
        }

        return await MapToEditModelAsync(template);
    }

    public async Task<EmailOperationResult> SaveAsync(int id, EmailTemplateEditModel model)
    {
        await EnsureSuperuserAsync();

        var template = await db.EmailTemplates.FirstOrDefaultAsync(t => t.Id == id);
        if (template is null)
        {
            return EmailOperationResult.Fail("Vorlage wurde nicht gefunden.");
        }

        if (!string.Equals(template.TemplateKey, model.TemplateKey, StringComparison.Ordinal))
        {
            return EmailOperationResult.Fail("TemplateKey kann nach Anlage nicht geändert werden.");
        }

        if (string.IsNullOrWhiteSpace(model.DisplayName))
        {
            return EmailOperationResult.Fail("Anzeigename ist erforderlich.");
        }

        if (string.IsNullOrWhiteSpace(model.Subject))
        {
            return EmailOperationResult.Fail("Betreff ist erforderlich.");
        }

        if (string.IsNullOrWhiteSpace(model.HtmlContent))
        {
            return EmailOperationResult.Fail("HTML-Inhalt ist erforderlich.");
        }

        template.DisplayName = model.DisplayName.Trim();
        template.Subject = model.Subject.Trim();
        template.HtmlContent = model.HtmlContent;
        template.TextContent = string.IsNullOrWhiteSpace(model.TextContent) ? null : model.TextContent;
        template.IsEnabled = model.IsEnabled;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = await currentUser.GetUserIdAsync();

        await db.SaveChangesAsync();
        return EmailOperationResult.Ok("Vorlage wurde gespeichert.");
    }

    public async Task<EmailOperationResult> SendTestFromTemplateAsync(int id, string recipientEmail)
    {
        await EnsureSuperuserAsync();

        if (!IsValidEmail(recipientEmail))
        {
            return EmailOperationResult.Fail("Bitte eine gültige Testempfänger-Email eingeben.");
        }

        var template = await db.EmailTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (template is null)
        {
            return EmailOperationResult.Fail("Vorlage wurde nicht gefunden.");
        }

        var variables = EmailTemplateSampleData.AsDictionary();
        var rendered = templateRenderer.Render(template.Subject, template.HtmlContent, template.TextContent, variables);

        var result = await emailService.SendEmailAsync(
            recipientEmail.Trim(),
            rendered.Subject,
            rendered.HtmlBody,
            rendered.TextBody,
            bypassEnabledCheck: false);

        if (!result.Succeeded)
        {
            return result;
        }

        if (!template.IsEnabled)
        {
            return EmailOperationResult.Warn(
                "Diese Vorlage ist deaktiviert. Die Testmail wurde trotzdem manuell versendet.",
                result.Message);
        }

        return EmailOperationResult.Ok(result.Message ?? "Testmail wurde versendet.");
    }

    public RenderedEmail GetPreview(EmailTemplateEditModel model) =>
        templateRenderer.Render(model.Subject, model.HtmlContent, model.TextContent, EmailTemplateSampleData.AsDictionary());

    private async Task<EmailTemplateEditModel> MapToEditModelAsync(EmailTemplate template)
    {
        var updatedByDisplay = await ResolveUpdatedByDisplayAsync(template.UpdatedByUserId);
        return new EmailTemplateEditModel
        {
            Id = template.Id,
            TemplateKey = template.TemplateKey,
            DisplayName = template.DisplayName,
            Subject = template.Subject,
            HtmlContent = template.HtmlContent,
            TextContent = template.TextContent,
            IsEnabled = template.IsEnabled,
            UpdatedAt = template.UpdatedAt,
            UpdatedByDisplay = updatedByDisplay
        };
    }

    private async Task EnsureSuperuserAsync()
    {
        if (!await access.IsSuperuserAsync())
        {
            throw new UnauthorizedAccessException("Nur Superuser dürfen Email-Vorlagen verwalten.");
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
