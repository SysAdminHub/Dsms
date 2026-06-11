namespace Dsms.Web.Services.Provisioning;

public sealed class ProvisionCustomerRequestDto
{
    public Guid PlanId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? LicenseStatus { get; set; }
    public DateTime? LicenseValidFrom { get; set; }
    public DateTime? LicenseValidUntil { get; set; }
    public string? LicenseInternalNote { get; set; }

    public string TenantName { get; set; } = string.Empty;
    public string? TenantLegalName { get; set; }

    public string AdminEmail { get; set; } = string.Empty;
    public string AdminDisplayName { get; set; } = string.Empty;

    public bool SendWelcomeEmail { get; set; } = true;
    public string? Source { get; set; }

    /// <summary>Verknüpfter PendingSignup für Public Signup (Rabattcode-Einlösung).</summary>
    public Guid? PendingSignupId { get; set; }
}

public sealed class ProvisionCustomerResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Guid? LicenseId { get; init; }
    public string? LicenseNumber { get; init; }
    public int? TenantId { get; init; }
    public string? TenantName { get; init; }
    public string? AdminUserId { get; init; }
    public string? AdminEmail { get; init; }
    public bool PasswordSetupEmailSent { get; init; }
    public bool CreatedLicense { get; init; }
    public bool CreatedTenant { get; init; }
    public bool CreatedAdmin { get; init; }
    public List<string> Warnings { get; init; } = [];
    public List<string> Errors { get; init; } = [];

    public static ProvisionCustomerResultDto Ok(
        string message,
        Guid licenseId,
        string licenseNumber,
        int tenantId,
        string tenantName,
        string adminUserId,
        string adminEmail,
        bool passwordSetupEmailSent,
        IReadOnlyList<string>? warnings = null) => new()
    {
        Success = true,
        Message = message,
        LicenseId = licenseId,
        LicenseNumber = licenseNumber,
        TenantId = tenantId,
        TenantName = tenantName,
        AdminUserId = adminUserId,
        AdminEmail = adminEmail,
        PasswordSetupEmailSent = passwordSetupEmailSent,
        CreatedLicense = true,
        CreatedTenant = true,
        CreatedAdmin = true,
        Warnings = warnings?.ToList() ?? []
    };

    public static ProvisionCustomerResultDto Fail(string message, IReadOnlyList<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        Errors = errors?.ToList() ?? [message]
    };
}
