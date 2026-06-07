namespace Dsms.Web.Services.PasswordReset;

/// <summary>
/// Passwortreset über ASP.NET Identity (GeneratePasswordResetTokenAsync / ResetPasswordAsync)
/// und den zentralen Emailservice.
/// </summary>
public interface IPasswordResetService
{
    /// <summary>Neutrale Meldung für Self-Service – verrät keine Kontenexistenz.</summary>
    string NeutralSelfServiceMessage { get; }

    /// <summary>Self-Service: immer neutrale Meldung; sendet nur bei existierendem aktivem Benutzer.</summary>
    Task<PasswordResetSelfServiceResult> RequestSelfServiceResetAsync(string email);

    /// <summary>Admin/Superuser: konkrete Erfolgs- oder Fehlermeldung.</summary>
    Task<PasswordResetAdminResult> SendAdminResetAsync(string userId);

    /// <summary>Neues Passwort setzen via Identity ResetPasswordAsync.</summary>
    Task<PasswordResetChangeResult> ChangePasswordAsync(
        string email,
        string encodedToken,
        string newPassword,
        string confirmPassword);
}

public sealed class PasswordResetSelfServiceResult
{
    public bool Succeeded { get; init; } = true;
    public string Message { get; init; } = "";
    public string? ValidationError { get; init; }
}

public sealed class PasswordResetAdminResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = "";
}

public sealed class PasswordResetChangeResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = "";
    public IEnumerable<string> FieldErrors { get; init; } = [];
}
