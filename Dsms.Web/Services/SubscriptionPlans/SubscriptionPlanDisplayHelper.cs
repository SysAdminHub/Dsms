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
}

