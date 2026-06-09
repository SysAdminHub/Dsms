namespace Dsms.Web.Services.PendingSignups;

public sealed class PendingSignupListDto
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public string AdminEmail { get; init; } = string.Empty;
    public string AdminDisplayName { get; init; } = string.Empty;
    public string? PlanNameSnapshot { get; init; }
    public string? PlanDisplayNameSnapshot { get; init; }
    public string? Source { get; init; }
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public string? PaymentProvider { get; init; }
    public string? MetadataJson { get; init; }
    public string? BillingEmail { get; init; }
    public string? BillingCompanyName { get; init; }
    public string? ExternalPaymentId { get; init; }
    public Guid? ProvisionedLicenseId { get; init; }
    public string? ProvisionedLicenseNumber { get; init; }
    public DateTime? ProvisionedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class PendingSignupDetailsDto
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Source { get; init; }

    public Guid PlanId { get; init; }
    public string? PlanNameSnapshot { get; init; }
    public string? PlanDisplayNameSnapshot { get; init; }
    public decimal? PlanPriceMonthlySnapshot { get; init; }
    public decimal? PlanPriceYearlySnapshot { get; init; }
    public string? CurrencySnapshot { get; init; }

    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }

    public string TenantName { get; init; } = string.Empty;
    public string? TenantLegalName { get; init; }
    public string? TenantEmail { get; init; }
    public string? TenantPhone { get; init; }
    public string? TenantAddress { get; init; }

    public string AdminEmail { get; init; } = string.Empty;
    public string AdminDisplayName { get; init; } = string.Empty;
    public string? AdminFirstName { get; init; }
    public string? AdminLastName { get; init; }

    public string? PaymentProvider { get; init; }
    public string? ExternalPaymentId { get; init; }
    public string? ExternalCheckoutUrl { get; init; }
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }

    public DateTime? PaidAt { get; init; }
    public DateTime? ProvisionedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public DateTime? FailedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }

    public Guid? ProvisionedLicenseId { get; init; }
    public string? ProvisionedLicenseNumber { get; init; }
    public int? ProvisionedTenantId { get; init; }
    public string? ProvisionedTenantName { get; init; }
    public string? ProvisionedAdminUserId { get; init; }

    public string? ErrorMessage { get; init; }
    public string? InternalNote { get; init; }
    public string? MetadataJson { get; init; }

    public string? BillingCompanyName { get; init; }
    public string? BillingEmail { get; init; }
    public string? BillingStreet { get; init; }
    public string? BillingPostalCode { get; init; }
    public string? BillingCity { get; init; }
    public string? BillingCountry { get; init; }
    public string? BillingVatId { get; init; }
    public string? BillingReference { get; init; }
}

public class CreatePendingSignupDto
{
    public Guid PlanId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string? TenantLegalName { get; set; }
    public string? TenantEmail { get; set; }
    public string? TenantPhone { get; set; }
    public string? TenantAddress { get; set; }
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminDisplayName { get; set; } = string.Empty;
    public string? AdminFirstName { get; set; }
    public string? AdminLastName { get; set; }
    public string? Source { get; set; }
    public string? InternalNote { get; set; }

    public string? BillingCompanyName { get; set; }
    public string? BillingEmail { get; set; }
    public string? BillingStreet { get; set; }
    public string? BillingPostalCode { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingCountry { get; set; }
    public string? BillingVatId { get; set; }
    public string? BillingReference { get; set; }
}

/// <summary>Öffentlicher Signup – Free und Paid, inkl. Billing-Metadaten.</summary>
public sealed class CreatePublicPendingSignupDto : CreatePendingSignupDto
{
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? PaymentProvider { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class PendingSignupStatusUpdateDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public sealed class PendingSignupInternalNoteDto
{
    public Guid Id { get; set; }
    public string? InternalNote { get; set; }
}
