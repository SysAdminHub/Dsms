using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Services.DiscountCodes;

public sealed class DiscountCodeListItemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DiscountCodeType DiscountType { get; init; }
    public string DiscountDisplayText { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string AppliesToPlanDisplayName { get; init; } = string.Empty;
    public string AppliesToBillingCycleDisplayText { get; init; } = string.Empty;
    public string ValidityDisplayText { get; init; } = string.Empty;
    public string UsageDisplayText { get; init; } = string.Empty;
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidUntil { get; init; }
    public int? MaxRedemptions { get; init; }
    public int CurrentRedemptions { get; init; }
}

public sealed class DiscountCodeDetailDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DiscountCodeType DiscountType { get; init; }
    public string DiscountTypeDisplayText { get; init; } = string.Empty;
    public string DiscountDisplayText { get; init; } = string.Empty;
    public decimal? PercentageValue { get; init; }
    public decimal? FixedAmountValue { get; init; }
    public int? FreeMonths { get; init; }
    public Guid? AppliesToPlanId { get; init; }
    public string AppliesToPlanDisplayName { get; init; } = string.Empty;
    public string? AppliesToBillingCycle { get; init; }
    public string AppliesToBillingCycleDisplayText { get; init; } = string.Empty;
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidUntil { get; init; }
    public string ValidityDisplayText { get; init; } = string.Empty;
    public int? MaxRedemptions { get; init; }
    public int CurrentRedemptions { get; init; }
    public string UsageDisplayText { get; init; } = string.Empty;
    public string? InternalNote { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? CreatedByUserId { get; init; }
    public string? CreatedByDisplayName { get; init; }
    public string? UpdatedByUserId { get; init; }
    public string? UpdatedByDisplayName { get; init; }
}

public sealed class DiscountCodeEditDto
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DiscountCodeType DiscountType { get; set; }
    public decimal? PercentageValue { get; set; }
    public decimal? FixedAmountValue { get; set; }
    public int? FreeMonths { get; set; }
    public Guid? AppliesToPlanId { get; set; }
    public string? AppliesToBillingCycle { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public int? MaxRedemptions { get; set; }
    public int CurrentRedemptions { get; set; }
    public string? InternalNote { get; set; }
}

public sealed class SaveDiscountCodeResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? Id { get; init; }

    public static SaveDiscountCodeResult Succeeded(Guid id) => new()
    {
        Success = true,
        Id = id
    };

    public static SaveDiscountCodeResult Failed(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}

public sealed class DiscountCodePlanOptionDto
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}
