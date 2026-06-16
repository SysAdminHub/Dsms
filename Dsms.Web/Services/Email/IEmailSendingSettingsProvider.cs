using Dsms.Web.Domain.Entities;

namespace Dsms.Web.Services.Email;

/// <summary>
/// Liest SMTP-/Systembenachrichtigungs-Einstellungen ohne Superuser-Prüfung.
/// Für Versand und Systembenachrichtigungen – bricht DI-Zyklen zu <see cref="IEmailSettingsService"/> auf.
/// </summary>
public interface IEmailSendingSettingsProvider
{
    Task<EmailSettings?> GetSettingsForSendingAsync(CancellationToken cancellationToken = default);
}
