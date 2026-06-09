namespace Dsms.Web.Services.Signup;

public sealed class PaidSignupFormDto
{
    public Guid PlanId { get; set; }
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

public sealed class PaidSignupSubmitResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static PaidSignupSubmitResult Succeeded() => new() { Success = true };

    public static PaidSignupSubmitResult Failed(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}
