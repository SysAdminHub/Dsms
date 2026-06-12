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
            "{{AppName}} Testmail",
            """
            <p>Hallo,</p>
            <p>dies ist eine Testmail aus {{AppName}}.</p>
            <p>Wenn diese Email angekommen ist, funktioniert der zentrale Emailversand.</p>
            <p>Viele Grüße<br/>{{AppName}}</p>
            """);

        await SeedTemplateIfMissingAsync(db, EmailTemplateKeys.FeedbackMessageToSupport, "Feedback an Support",
            "[{{ProductName}} Feedback] {{Category}}: {{Subject}}",
            """
            <div style="font-family: sans-serif; line-height: 1.5;">
            <h2>Feedback-Nachricht</h2>
            <h3>Nachricht</h3>
            <table style="border-collapse: collapse;">
            <tr><td style="padding: 2px 12px 2px 0; vertical-align: top;"><strong>Kategorie:</strong></td><td>{{Category}}</td></tr>
            <tr><td style="padding: 2px 12px 2px 0; vertical-align: top;"><strong>Betreff:</strong></td><td>{{Subject}}</td></tr>
            </table>
            <p style="margin-top: 1rem;"><strong>Nachricht:</strong></p>
            <p>{{Message}}</p>
            <h3>Absender</h3>
            <table style="border-collapse: collapse;">
            <tr><td style="padding: 2px 12px 2px 0; vertical-align: top;"><strong>E-Mail:</strong></td><td>{{UserEmail}}</td></tr>
            <tr><td style="padding: 2px 12px 2px 0; vertical-align: top;"><strong>Name:</strong></td><td>{{UserName}}</td></tr>
            <tr><td style="padding: 2px 12px 2px 0; vertical-align: top;"><strong>Rolle:</strong></td><td>{{UserRole}}</td></tr>
            </table>
            <h3>Kontext</h3>
            <table style="border-collapse: collapse;">
            <tr><td style="padding: 2px 12px 2px 0; vertical-align: top;"><strong>Mandant:</strong></td><td>{{TenantName}}</td></tr>
            <tr><td style="padding: 2px 12px 2px 0; vertical-align: top;"><strong>Tenant-ID:</strong></td><td>{{TenantId}}</td></tr>
            <tr><td style="padding: 2px 12px 2px 0; vertical-align: top;"><strong>Seite:</strong></td><td>{{CurrentUrl}}</td></tr>
            <tr><td style="padding: 2px 12px 2px 0; vertical-align: top;"><strong>Version:</strong></td><td>{{AppVersion}}</td></tr>
            <tr><td style="padding: 2px 12px 2px 0; vertical-align: top;"><strong>Zeitpunkt:</strong></td><td>{{CreatedAt}}</td></tr>
            </table>
            </div>
            """);

        await SeedTemplateIfMissingAsync(db, EmailTemplateKeys.SignupLegalConfirmation, "Registrierung – Vertragsunterlagen",
            "Ihre Registrierung bei {{AppName}}",
            """
            <p>Hallo {{CustomerContactName}},</p>
            <p>vielen Dank für Ihre Registrierung bei {{AppName}}.</p>
            <p>Ihr Zugang wurde erfolgreich erstellt.</p>
            <p>Die folgenden Vertragsunterlagen erhalten Sie als PDF-Anhang zu dieser E-Mail:</p>
            <ul>
            <li>AGB / SaaS-Nutzungsbedingungen</li>
            <li>Datenschutzerklärung</li>
            <li>Auftragsverarbeitungsvertrag einschließlich TOM-Anlage und Unterauftragnehmerliste</li>
            </ul>
            <p><strong>Dokumentversion:</strong> {{LegalVersion}}<br/>
            <strong>Registrierungsdatum:</strong> {{RegistrationDate}}</p>
            <p>Sie können die Dokumente außerdem jederzeit über die Fußzeile der Anwendung erneut abrufen.</p>
            <p>Freundliche Grüße<br/>{{ProviderName}}</p>
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
