using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Zentrale SMTP-Konfiguration der SaaS-Plattform (ein Datensatz, nicht mandantenbezogen).
/// </summary>
public class EmailSettings : EntityBase
{
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }

    /// <summary>
    /// Geschütztes SMTP-Passwort oder API-Key – niemals im Klartext an die UI zurückgeben.
    /// </summary>
    public string? EncryptedSmtpPassword { get; set; }

    public string? SenderEmail { get; set; }
    public string? SenderName { get; set; }
    public SmtpEncryption Encryption { get; set; } = SmtpEncryption.StartTls;
    public bool IsEnabled { get; set; }

    /// <summary>Interne Systembenachrichtigungen (z. B. neue Registrierungen) versenden.</summary>
    public bool SystemNotificationsEnabled { get; set; }

    /// <summary>Empfängeradresse für interne Systembenachrichtigungen.</summary>
    public string? SystemNotificationRecipientEmail { get; set; }

    public string? UpdatedByUserId { get; set; }
}
