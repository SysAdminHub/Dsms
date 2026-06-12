using System.Globalization;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Services.DiscountCodes;

public static class DiscountCodeDisplayHelper
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public static string FormatDiscountValue(
        DiscountCodeType type,
        decimal? percentageValue,
        decimal? fixedAmountValue,
        int? freeMonths,
        string currency = "EUR")
    {
        return type switch
        {
            DiscountCodeType.Percentage when percentageValue.HasValue =>
                $"{percentageValue.Value.ToString("0.##", GermanCulture)} %",
            DiscountCodeType.FixedAmount when fixedAmountValue.HasValue =>
                $"{fixedAmountValue.Value.ToString("N2", GermanCulture)} {currency}",
            DiscountCodeType.FreeMonths when freeMonths.HasValue =>
                $"{freeMonths.Value} Monat{(freeMonths.Value == 1 ? "" : "e")} kostenlos",
            _ => "—"
        };
    }

    public static string FormatPlanBinding(string? planDisplayName) =>
        string.IsNullOrWhiteSpace(planDisplayName) ? "Alle Pläne" : planDisplayName;

    public static string FormatBillingCycleScope(string? cycle) =>
        DiscountCodeLabels.GetBillingCycleScopeLabel(cycle);

    public static string FormatValidity(DateTime? validFrom, DateTime? validUntil)
    {
        if (!validFrom.HasValue && !validUntil.HasValue)
        {
            return "Immer";
        }

        if (validFrom.HasValue && validUntil.HasValue)
        {
            return $"{validFrom.Value.ToLocalTime():d} – {validUntil.Value.ToLocalTime():d}";
        }

        if (validFrom.HasValue)
        {
            return $"ab {validFrom.Value.ToLocalTime():d}";
        }

        return $"bis {validUntil!.Value.ToLocalTime():d}";
    }

    public static string FormatUsage(int currentRedemptions, int? maxRedemptions) =>
        maxRedemptions.HasValue
            ? $"{currentRedemptions} / {maxRedemptions.Value}"
            : $"{currentRedemptions} / unbegrenzt";
}
