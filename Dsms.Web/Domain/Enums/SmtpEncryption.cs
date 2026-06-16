namespace Dsms.Web.Domain.Enums;

/// <summary>SMTP-Verschlüsselungsmodus für den zentralen Plattform-Mailversand.</summary>
public enum SmtpEncryption
{
    None = 0,
    StartTls = 1,
    SslTls = 2
}
