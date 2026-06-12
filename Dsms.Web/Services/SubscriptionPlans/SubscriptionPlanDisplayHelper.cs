using System.Globalization;

namespace Dsms.Web.Services.SubscriptionPlans;

public static class SubscriptionPlanDisplayHelper
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public static string FormatLimit(int? value) =>
        value.HasValue ? value.Value.ToString(GermanCulture) : "unbegrenzt";

    public static string FormatMonthlyPrice(decimal? price, string currency, bool isFree)
    {
        if (isFree)
        {
            return "Kostenlos";
        }

        if (!price.HasValue)
        {
            return "nicht festgelegt";
        }

        return $"{price.Value.ToString("N2", GermanCulture)} {currency} / Monat";
    }

    public static string FormatYearlyPrice(decimal? price, string currency, bool isFree)
    {
        if (isFree)
        {
            return "Kostenlos";
        }

        if (!price.HasValue)
        {
            return "nicht festgelegt";
        }

        return $"{price.Value.ToString("N2", GermanCulture)} {currency} / Jahr";
    }

    public static string FormatCompactLimits(int? maxTenants, int? maxAdmins, int? maxUsersPerTenant)
    {
        return $"Mandanten: {FormatLimit(maxTenants)}; Admins: {FormatLimit(maxAdmins)}; Benutzer/Mandant: {FormatLimit(maxUsersPerTenant)}";
    }

    public static string FormatPlanOptionLabel(SubscriptionPlanOptionDto plan)
    {
        var price = FormatMonthlyPrice(plan.PriceMonthly, plan.Currency, plan.IsFree);
        return $"{plan.DisplayName} - {price}";
    }

    public static string FormatPriceAmount(decimal? price, string currency)
    {
        if (!price.HasValue)
        {
            return "nicht festgelegt";
        }

        return $"{price.Value.ToString("N2", GermanCulture)} {currency}";
    }

    public static string FormatPromotionalMonthlyPrice(decimal? price, string currency) =>
        $"{FormatPriceAmount(price, currency)} / Monat";

    public static string FormatPromotionalYearlyPrice(decimal? price, string currency) =>
        $"{FormatPriceAmount(price, currency)} / Jahr";

    public static string GetPromotionalBadgeText(string? badgeText) =>
        string.IsNullOrWhiteSpace(badgeText) ? "Limitiertes Angebot" : badgeText.Trim();

    public static bool ShowsPromotionalMonthly(
        bool isFree,
        bool isPromotionalPriceEnabled,
        decimal? promotionalMonthlyPrice) =>
        !isFree && isPromotionalPriceEnabled && promotionalMonthlyPrice.HasValue;

    public static bool ShowsPromotionalYearly(
        bool isFree,
        bool isPromotionalPriceEnabled,
        decimal? promotionalYearlyPrice) =>
        !isFree && isPromotionalPriceEnabled && promotionalYearlyPrice.HasValue;

    public static bool ShowsPromotionalBadge(
        bool isFree,
        bool isPromotionalPriceEnabled,
        decimal? promotionalMonthlyPrice,
        decimal? promotionalYearlyPrice) =>
        !isFree && isPromotionalPriceEnabled
        && (promotionalMonthlyPrice.HasValue || promotionalYearlyPrice.HasValue);

    public static string FormatPromotionalPrice(decimal? price, string currency) =>
        FormatPriceAmount(price, currency);

    public static string FormatPromotionalMonthlyPriceForDetails(decimal? price, string currency) =>
        price.HasValue ? $"{FormatPriceAmount(price, currency)} / Monat" : "—";

    public static string FormatPromotionalYearlyPriceForDetails(decimal? price, string currency) =>
        price.HasValue ? $"{FormatPriceAmount(price, currency)} / Jahr" : "—";
}

