using Dsms.Web.Domain.Enums;

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
    public decimal? EffectiveMonthlyPrice { get; init; }
    public decimal? EffectiveYearlyPrice { get; init; }
    public bool IsPromotionalPriceEnabled { get; init; }
    public decimal? PromotionalMonthlyPrice { get; init; }
    public decimal? PromotionalYearlyPrice { get; init; }
    public string? PromotionalBadgeText { get; init; }
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
    public string TenantStreet { get; set; } = string.Empty;
    public string TenantPostalCode { get; set; } = string.Empty;
    public string TenantCity { get; set; } = string.Empty;
    public string TenantCountry { get; set; } = "Deutschland";
    public string? TenantPhone { get; set; }
    public string? TenantVatId { get; set; }
    public string AdminDisplayName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public bool AcceptAgb { get; set; }
    public bool AcceptPrivacyPolicy { get; set; }
    public bool AcceptDataProcessingAgreement { get; set; }

    /// <summary>
    /// Wenn false, wird die Rechnungsadresse aus den Unternehmensdaten übernommen.
    /// </summary>
    public bool HasDifferentBillingAddress { get; set; }

    public string BillingCompanyName { get; set; } = string.Empty;
    public string BillingEmail { get; set; } = string.Empty;
    public string BillingStreet { get; set; } = string.Empty;
    public string BillingPostalCode { get; set; } = string.Empty;
    public string BillingCity { get; set; } = string.Empty;
    public string BillingCountry { get; set; } = string.Empty;
    public string? BillingVatId { get; set; }
    public string? BillingReference { get; set; }

    /// <summary>Honeypot-Feld – muss leer bleiben.</summary>
    public string? Website { get; set; }

    /// <summary>Abrechnungszeitraum bei kostenpflichtigen Plänen (<see cref="BillingCycles"/>).</summary>
    public string? BillingCycle { get; set; }

    public string? DiscountCodeInput { get; set; }
    public Guid? AppliedDiscountCodeId { get; set; }
    public string? AppliedDiscountCode { get; set; }
    public string? AppliedDiscountName { get; set; }
    public DiscountCodeType? AppliedDiscountType { get; set; }
    public string? AppliedDiscountDisplayText { get; set; }
    public decimal? OriginalAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? FinalAmount { get; set; }
}

public sealed class PublicSignupSubmitResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsPaidPlan { get; init; }
    public bool PasswordSetupEmailSent { get; init; }
    public string? PlanDisplayName { get; init; }

    public static PublicSignupSubmitResult Succeeded(
        bool isPaidPlan,
        bool passwordSetupEmailSent,
        string? planDisplayName) => new()
    {
        Success = true,
        IsPaidPlan = isPaidPlan,
        PasswordSetupEmailSent = passwordSetupEmailSent,
        PlanDisplayName = planDisplayName
    };

    public static PublicSignupSubmitResult Failed(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}
