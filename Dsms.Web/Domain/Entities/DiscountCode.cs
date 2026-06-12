using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Plattformweiter Rabattcode für spätere Nutzung im Signup-Flow.
/// Mandantenunabhängig; nur Superuser verwalten Rabattcodes.
/// </summary>
public class DiscountCode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public DiscountCodeType DiscountType { get; set; }
    public decimal? PercentageValue { get; set; }
    public decimal? FixedAmountValue { get; set; }
    public int? FreeMonths { get; set; }

    public Guid? AppliesToPlanId { get; set; }
    public SubscriptionPlan? AppliesToPlan { get; set; }

    /// <summary>
    /// <c>null</c> = gilt für monatliche und jährliche Abrechnung.
    /// Sonst <see cref="BillingCycles.Monthly"/> oder <see cref="BillingCycles.Yearly"/>.
    /// </summary>
    public string? AppliesToBillingCycle { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }

    public int? MaxRedemptions { get; set; }
    public int CurrentRedemptions { get; set; }

    public string? InternalNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }
}
