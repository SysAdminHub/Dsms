using Microsoft.AspNetCore.DataProtection;

namespace Dsms.Web.Services.Email;

/// <summary>
/// Schützt SMTP-Passwörter und API-Keys mit ASP.NET Data Protection.
/// Hinweis: In Produktion Data-Protection-Keys persistent speichern (z. B. gemeinsames Key-Ring-Verzeichnis),
/// damit Secrets nach Neustarts/Deployments weiter entschlüsselbar bleiben.
/// </summary>
public sealed class EmailSecretProtector(IDataProtectionProvider dataProtectionProvider) : IEmailSecretProtector
{
    private const string ProtectorPurpose = "Dsms.Email.SmtpPassword.v1";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);

    public string Protect(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        return _protector.Protect(plainText);
    }

    public string Unprotect(string protectedText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedText);
        return _protector.Unprotect(protectedText);
    }

    public bool HasProtectedValue(string? protectedText) =>
        !string.IsNullOrWhiteSpace(protectedText);
}
