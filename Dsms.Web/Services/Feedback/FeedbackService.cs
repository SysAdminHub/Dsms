using System.Net;
using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Feedback;

public sealed class FeedbackService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IEmailTemplateRenderer templateRenderer,
    IEmailService emailService,
    ICurrentUserContext currentUser,
    ITenantService tenantService,
    IApplicationInfoService applicationInfo,
    ILogger<FeedbackService> logger) : IFeedbackService
{
    private const int MaxSubjectLength = 200;
    private const int MaxMessageLength = 5000;

    private const string SuccessMessage = "Vielen Dank. Ihre Nachricht wurde gesendet.";
    private const string SendFailedMessage =
        "Die Nachricht konnte nicht gesendet werden. Bitte versuchen Sie es später erneut.";

    public async Task<FeedbackResult> SendFeedbackAsync(FeedbackMessageModel model)
    {
        var validation = Validate(model);
        if (validation is not null)
        {
            return FeedbackResult.Fail(validation);
        }

        var userId = await currentUser.GetUserIdAsync();
        if (userId is null)
        {
            return FeedbackResult.Fail("Sie müssen angemeldet sein, um Feedback zu senden.");
        }

        var supportEmail = applicationInfo.SupportEmail?.Trim();
        if (string.IsNullOrWhiteSpace(supportEmail))
        {
            logger.LogWarning("Feedback-Versand abgebrochen: keine Support-E-Mail konfiguriert.");
            return FeedbackResult.Fail(SendFailedMessage);
        }

        var user = await currentUser.GetUserAsync();
        var tenant = await tenantService.GetCurrentTenantAsync();
        var tenantId = await currentUser.GetTenantIdAsync();
        var userRole = await GetUserRoleDisplayAsync();
        var createdAt = DateTime.UtcNow;

        await using var db = await dbFactory.CreateDbContextAsync();
        var template = await db.EmailTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateKey == EmailTemplateKeys.FeedbackMessageToSupport);

        if (template is null || !template.IsEnabled)
        {
            logger.LogWarning("Feedback-Versand abgebrochen: Vorlage {TemplateKey} fehlt oder ist deaktiviert.",
                EmailTemplateKeys.FeedbackMessageToSupport);
            return FeedbackResult.Fail(SendFailedMessage);
        }

        var variables = new Dictionary<string, string>
        {
            ["ProductName"] = Encode(applicationInfo.ProductName),
            ["Category"] = Encode(model.Category.Trim()),
            ["Subject"] = Encode(model.Subject.Trim()),
            ["Message"] = FormatMultilineHtml(model.Message.Trim()),
            ["UserEmail"] = Encode(user?.Email ?? "—"),
            ["UserName"] = Encode(ResolveUserName(user)),
            ["UserRole"] = Encode(userRole),
            ["TenantName"] = Encode(tenant?.Name ?? "—"),
            ["TenantId"] = Encode(tenantId?.ToString() ?? "—"),
            ["CurrentUrl"] = Encode(string.IsNullOrWhiteSpace(model.CurrentUrl) ? "—" : model.CurrentUrl.Trim()),
            ["AppVersion"] = Encode(applicationInfo.Version),
            ["CreatedAt"] = Encode(createdAt.ToLocalTime().ToString("g"))
        };

        var rendered = templateRenderer.Render(
            template.Subject,
            template.HtmlContent,
            template.TextContent,
            variables);

        var emailSubject = BuildEmailSubject(model.Category.Trim(), model.Subject.Trim());
        var sendResult = await emailService.SendEmailAsync(
            supportEmail,
            emailSubject,
            rendered.HtmlBody,
            rendered.TextBody);

        if (!sendResult.Succeeded)
        {
            logger.LogError(
                "Feedback-Versand fehlgeschlagen für Benutzer {UserId}: {Detail}",
                userId,
                sendResult.Message);
            return FeedbackResult.Fail(SendFailedMessage);
        }

        logger.LogInformation(
            "Feedback gesendet von Benutzer {UserId} (Kategorie: {Category}).",
            userId,
            model.Category);

        return FeedbackResult.Ok(SuccessMessage);
    }

    private static string? Validate(FeedbackMessageModel model)
    {
        var category = model.Category?.Trim();
        if (string.IsNullOrWhiteSpace(category) || !FeedbackCategory.All.Contains(category))
        {
            return "Bitte wählen Sie eine Kategorie aus.";
        }

        var subject = model.Subject?.Trim();
        if (string.IsNullOrWhiteSpace(subject))
        {
            return "Bitte geben Sie einen Betreff ein.";
        }

        if (subject.Length > MaxSubjectLength)
        {
            return $"Der Betreff darf maximal {MaxSubjectLength} Zeichen lang sein.";
        }

        var message = model.Message?.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Bitte geben Sie eine Nachricht ein.";
        }

        if (message.Length > MaxMessageLength)
        {
            return $"Die Nachricht darf maximal {MaxMessageLength} Zeichen lang sein.";
        }

        return null;
    }

    private async Task<string> GetUserRoleDisplayAsync()
    {
        var roles = new List<string>();
        foreach (var role in DsmsRoles.All)
        {
            if (await currentUser.IsInRoleAsync(role))
            {
                roles.Add(role);
            }
        }

        return roles.Count > 0 ? string.Join(", ", roles) : "—";
    }

    private static string ResolveUserName(Data.ApplicationUser? user)
    {
        if (user is null)
        {
            return "—";
        }

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            return user.DisplayName.Trim();
        }

        return user.Email ?? "—";
    }

    private string BuildEmailSubject(string category, string subject) =>
        $"[{applicationInfo.ProductName} Feedback] {category}: {subject}";

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string FormatMultilineHtml(string value)
    {
        var encoded = Encode(value)
            .Replace("\r\n", "\n")
            .Replace("\n", "<br>");
        return encoded;
    }
}
