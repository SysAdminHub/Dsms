namespace Dsms.Web.Services.Email;

/// <summary>
/// Kapselt Schutz und Entschlüsselung von SMTP-Secrets.
/// Produktiv müssen Secrets sicher geschützt werden (z. B. ASP.NET Data Protection).
/// </summary>
public interface IEmailSecretProtector
{
    string Protect(string plainText);
    string Unprotect(string protectedText);
    bool HasProtectedValue(string? protectedText);
}
