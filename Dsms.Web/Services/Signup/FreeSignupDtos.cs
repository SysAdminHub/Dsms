namespace Dsms.Web.Services.Signup;

public sealed class FreeSignupFormDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string? TenantLegalName { get; set; }
    public string AdminDisplayName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public bool AcceptTerms { get; set; }

    /// <summary>Honeypot-Feld – muss leer bleiben.</summary>
    public string? Website { get; set; }
}

public sealed class FreeSignupSubmitResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public bool PasswordSetupEmailSent { get; init; }

    public static FreeSignupSubmitResult Succeeded(bool passwordSetupEmailSent) => new()
    {
        Success = true,
        PasswordSetupEmailSent = passwordSetupEmailSent
    };

    public static FreeSignupSubmitResult Failed(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}
