namespace Dsms.Web.Domain.Entities;

using Dsms.Web.Domain;

/// <summary>
/// Zwischenspeicher für Registrierungsdaten vor Provisionierung (später bezahlte Signups mit Mollie).
/// Erstellt noch keine License, keinen Tenant und keinen Admin.
/// </summary>
public class PendingSignup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Guid PlanId { get; set; }
    public string? PlanNameSnapshot { get; set; }
    public string? PlanDisplayNameSnapshot { get; set; }
    public decimal? PlanPriceMonthlySnapshot { get; set; }
    public decimal? PlanPriceYearlySnapshot { get; set; }
    public string? CurrencySnapshot { get; set; }

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

    public string Status { get; set; } = PendingSignupStatuses.Draft;

    public string? PaymentProvider { get; set; }
    public string? ExternalPaymentId { get; set; }
    public string? ExternalCheckoutUrl { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? BillingCycle { get; set; }

    public Guid? DiscountCodeId { get; set; }
    public string? DiscountCodeSnapshot { get; set; }
    public string? DiscountNameSnapshot { get; set; }
    public string? DiscountTypeSnapshot { get; set; }
    public decimal? DiscountValueSnapshot { get; set; }
    public int? DiscountFreeMonthsSnapshot { get; set; }
    public decimal? OriginalAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? FinalAmount { get; set; }
    /// <summary>Zeitpunkt der finalen Rabattcode-Einlösung nach erfolgreicher Provisionierung.</summary>
    public DateTime? DiscountRedeemedAt { get; set; }

    public DateTime? PaidAt { get; set; }
    public DateTime? ProvisionedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public Guid? ProvisionedLicenseId { get; set; }
    public string? ProvisionedLicenseNumber { get; set; }
    /// <summary>Mandanten-ID nach Provisionierung (<see cref="Tenant.Id"/> ist int).</summary>
    public int? ProvisionedTenantId { get; set; }
    public string? ProvisionedTenantName { get; set; }
    public string? ProvisionedAdminUserId { get; set; }

    public string? ErrorMessage { get; set; }
    public string? InternalNote { get; set; }
    public string? Source { get; set; }
    public string? MetadataJson { get; set; }

    public string? BillingCompanyName { get; set; }
    public string? BillingEmail { get; set; }
    public string? BillingStreet { get; set; }
    public string? BillingPostalCode { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingCountry { get; set; }
    public string? BillingVatId { get; set; }
    public string? BillingReference { get; set; }

    public string? BillingStatus { get; set; }
    public DateTime? InvoiceSentAt { get; set; }
    public DateTime? InvoicePaidAt { get; set; }
    public DateOnly? NextInvoiceDate { get; set; }
    public string? BillingNote { get; set; }
}
