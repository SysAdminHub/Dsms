namespace Dsms.Web.Services.Signup;

public sealed class PublicSignupPlanDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsFree { get; init; }
    public decimal? PriceMonthly { get; init; }
    public decimal? PriceYearly { get; init; }
    public string Currency { get; init; } = "EUR";
    public int SortOrder { get; init; }

    public int? MaxTenants { get; init; }
    public int? MaxAdmins { get; init; }
    public int? MaxUsersPerTenant { get; init; }
    public int? MaxAuditorsPerTenant { get; init; }
    public int? MaxDpiaPerTenant { get; init; }
    public int? MaxTomsPerTenant { get; init; }
    public int? MaxProcessorsPerTenant { get; init; }
    public int? MaxActiveMeasuresPerTenant { get; init; }
    public int? MaxStorageMb { get; init; }
}

public sealed class PublicSignupFormDto
{
    public Guid SelectedPlanId { get; set; }
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

public sealed class PublicSignupSubmitResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsFreeProvisioning { get; init; }
    public bool PasswordSetupEmailSent { get; init; }

    public static PublicSignupSubmitResult FreeSucceeded(bool passwordSetupEmailSent) => new()
    {
        Success = true,
        IsFreeProvisioning = true,
        PasswordSetupEmailSent = passwordSetupEmailSent
    };

    public static PublicSignupSubmitResult PaidPendingSucceeded() => new()
    {
        Success = true,
        IsFreeProvisioning = false
    };

    public static PublicSignupSubmitResult Failed(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}
