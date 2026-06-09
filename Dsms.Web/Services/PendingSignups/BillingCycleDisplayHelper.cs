using Dsms.Web.Domain;

namespace Dsms.Web.Services.PendingSignups;

public static class BillingCycleDisplayHelper
{
    public static string GetDisplayName(string? billingCycle, string? metadataJson, bool isFree)
    {
        if (isFree)
        {
            return "Nicht erforderlich";
        }

        var cycle = PendingSignupDisplayHelper.ResolveBillingCycle(billingCycle, metadataJson);
        return GetDisplayName(cycle);
    }

    public static string GetDisplayName(string? cycle) => cycle switch
    {
        BillingCycles.Monthly => "Monatlich",
        BillingCycles.Yearly => "Jährlich",
        null or "" or "None" => "—",
        _ => cycle
    };

    public static string FormatAmountWithCycle(
        string? paymentProvider,
        decimal? amount,
        string? currency,
        string? billingCycle,
        string? metadataJson)
    {
        if (PendingSignupDisplayHelper.IsFreeSignup(paymentProvider, amount))
        {
            return "Kostenlos";
        }

        var cycle = PendingSignupDisplayHelper.ResolveBillingCycle(billingCycle, metadataJson);
        var amountStr = PendingSignupDisplayHelper.FormatAmount(amount, currency);
        if (amountStr == "—")
        {
            return amountStr;
        }

        var suffix = cycle switch
        {
            BillingCycles.Monthly => "/ Monat",
            BillingCycles.Yearly => "/ Jahr",
            _ => string.Empty
        };

        return string.IsNullOrEmpty(suffix) ? amountStr : $"{amountStr} {suffix}";
    }

    public static string FormatBillingSummaryLine(
        string? paymentProvider,
        decimal? amount,
        string? currency,
        string? billingCycle,
        string? metadataJson)
    {
        if (PendingSignupDisplayHelper.IsFreeSignup(paymentProvider, amount))
        {
            return "Nicht erforderlich, Kostenlos";
        }

        var cycleName = GetDisplayName(billingCycle, metadataJson, isFree: false);
        var amountLine = FormatAmountWithCycle(paymentProvider, amount, currency, billingCycle, metadataJson);
        return $"{cycleName}, {amountLine}";
    }
}
