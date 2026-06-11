using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Services.DiscountCodes;

public sealed class DiscountCodeValidationResult
{
    public bool IsValid { get; init; }
    public bool IsApplied { get; init; }
    public string? ErrorMessage { get; init; }

    public Guid? DiscountCodeId { get; init; }
    public string? Code { get; init; }
    public string? Name { get; init; }
    public DiscountCodeType? DiscountType { get; init; }
    public decimal? PercentageValue { get; init; }
    public decimal? FixedAmountValue { get; init; }
    public int? FreeMonths { get; init; }
    public decimal OriginalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public string Currency { get; init; } = "EUR";
    public string? DisplayText { get; init; }
    public string? SuccessMessage { get; init; }

    public static DiscountCodeValidationResult NoDiscount() => new()
    {
        IsValid = true,
        IsApplied = false
    };

    public static DiscountCodeValidationResult Invalid(string message) => new()
    {
        IsValid = false,
        IsApplied = false,
        ErrorMessage = message
    };
}
