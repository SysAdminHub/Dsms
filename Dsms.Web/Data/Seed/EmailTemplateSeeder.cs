using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Data.Seed;

/// <summary>
/// Legt Standard-Email-Vorlagen an, ohne bestehende angepasste Vorlagen zu überschreiben.
/// </summary>
public static class EmailTemplateSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        await SeedTemplateIfMissingAsync(db, EmailTemplateKeys.PasswordReset, "Passwort zurücksetzen",
            "Passwort zurücksetzen für {{AppName}}",
            """
            <p>Hallo {{UserName}},</p>
            <p>für dein Benutzerkonto wurde das Zurücksetzen des Passworts angefordert.</p>
            <p>Bitte klicke auf den folgenden Link, um ein neues Passwort festzulegen:</p>
            <p><a href="{{ResetLink}}">{{ResetLink}}</a></p>
            <p>Der Link ist für {{ExpiresInMinutes}} Minuten gültig.</p>
            <p>Falls du diese Anfrage nicht gestellt hast, kannst du diese Email ignorieren.</p>
            <p>Viele Grüße<br/>{{AppName}}</p>
            """);

        await SeedTemplateIfMissingAsync(db, EmailTemplateKeys.WelcomeSetPassword, "Willkommen / Passwort anlegen",
            "Willkommen bei {{AppName}}",
            """
            <p>Hallo {{UserName}},</p>
            <p>für dich wurde ein Benutzerkonto in {{AppName}} angelegt.</p>
            <p><strong>Mandant:</strong> {{TenantName}}</p>
            <p>Bitte lege über den folgenden Link dein Passwort fest:</p>
            <p><a href="{{InviteLink}}">{{InviteLink}}</a></p>
            <p>Der Link ist für {{ExpiresInMinutes}} Minuten gültig.</p>
            <p>Viele Grüße<br/>{{AppName}}</p>
            """);

        await SeedTemplateIfMissingAsync(db, EmailTemplateKeys.Reminder, "Erinnerung",
            "Erinnerung: {{ReminderTitle}}",
            """
            <p>Hallo {{UserName}},</p>
            <p>dies ist eine Erinnerung aus {{AppName}}.</p>
            <p>{{ReminderText}}</p>
            <p><strong>Fällig am:</strong> {{DueDate}}</p>
            <p><a href="{{ActionLink}}">Direkt öffnen</a></p>
            <p>Viele Grüße<br/>{{AppName}}</p>
            """);

        await SeedTemplateIfMissingAsync(db, EmailTemplateKeys.TestEmail, "Testmail",
            "DSMS Testmail",
            """
            <p>Hallo,</p>
            <p>dies ist eine Testmail aus {{AppName}}.</p>
            <p>Wenn diese Email angekommen ist, funktioniert der zentrale Emailversand.</p>
            <p>Viele Grüße<br/>{{AppName}}</p>
            """);
    }

    private static async Task SeedTemplateIfMissingAsync(
        ApplicationDbContext db,
        string templateKey,
        string displayName,
        string subject,
        string htmlContent)
    {
        if (await db.EmailTemplates.AnyAsync(t => t.TemplateKey == templateKey))
        {
            return;
        }

        db.EmailTemplates.Add(new EmailTemplate
        {
            TemplateKey = templateKey,
            DisplayName = displayName,
            Subject = subject,
            HtmlContent = htmlContent,
            IsEnabled = true
        });
        await db.SaveChangesAsync();
    }
}
