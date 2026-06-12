using Dsms.Web.Domain.Entities;
using Dsms.Web.Services.PendingSignups;

namespace Dsms.Web.Services.Signup;

/// <summary>Einheitliche Rabatt-Anzeige für interne Signup-Benachrichtigungen.</summary>
internal static class SignupNotificationDiscountHelper
{
    public static bool HasDiscount(PendingSignup signup) =>
        PendingSignupDisplayHelper.HasDiscountCode(signup.DiscountCodeSnapshot);

    public static IReadOnlyList<(string Label, string Value)> BuildDiscountRows(PendingSignup signup, License? license)
    {
        if (!HasDiscount(signup))
        {
            return [("Rabattcode", "keiner")];
        }

        var rows = new List<(string, string)>
        {
            ("Rabattcode", signup.DiscountCodeSnapshot!),
            ("Rabatt", PendingSignupDisplayHelper.GetDiscountDisplayText(
                signup.DiscountTypeSnapshot,
                signup.DiscountValueSnapshot,
                signup.DiscountFreeMonthsSnapshot,
                signup.Currency))
        };

        if (!PendingSignupDisplayHelper.IsFreeMonthsDiscount(signup.DiscountTypeSnapshot))
        {
            rows.Add(("Regulärer Betrag", PendingSignupDisplayHelper.FormatAmount(signup.OriginalAmount, signup.Currency)));
            rows.Add(("Rabattbetrag", PendingSignupDisplayHelper.FormatAmount(signup.DiscountAmount, signup.Currency)));
        }

        rows.Add(("Heute zu zahlen", PendingSignupDisplayHelper.FormatTodayAmountDisplay(
            signup.PaymentProvider,
            signup.FinalAmount,
            signup.Amount,
            signup.Currency,
            signup.DiscountTypeSnapshot)));

        if (signup.NextInvoiceDate.HasValue)
        {
            rows.Add(("Nächste Rechnung", signup.NextInvoiceDate.Value.ToString("d")));
        }

        var followUp = PendingSignupDisplayHelper.GetFollowUpBillingDisplay(
            signup.BillingCycle,
            signup.MetadataJson,
            signup.DiscountTypeSnapshot,
            signup.DiscountFreeMonthsSnapshot,
            signup.OriginalAmount,
            signup.Currency);

        if (!string.IsNullOrEmpty(followUp))
        {
            rows.Add(("Folgeabrechnung", followUp));
        }

        if (signup.DiscountRedeemedAt.HasValue)
        {
            rows.Add(("Eingelöst am", signup.DiscountRedeemedAt.Value.ToLocalTime().ToString("g")));
        }

        if (PendingSignupDisplayHelper.IsFreeMonthsDiscount(signup.DiscountTypeSnapshot)
            && signup.DiscountFreeMonthsSnapshot is > 0)
        {
            var months = signup.DiscountFreeMonthsSnapshot.Value;
            rows.Add((
                "Hinweis",
                $"Die Lizenzlaufzeit wurde durch den Rabattcode auf {months} Monat{(months == 1 ? "" : "e")} gesetzt."));
        }

        return rows;
    }

    public static Dictionary<string, string> BuildPlaceholderVariables(PendingSignup signup, License? license)
    {
        var hasDiscount = HasDiscount(signup);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["HasDiscountCode"] = hasDiscount ? "Ja" : "Nein",
            ["DiscountCode"] = signup.DiscountCodeSnapshot ?? "—",
            ["DiscountName"] = signup.DiscountNameSnapshot ?? "—",
            ["DiscountType"] = signup.DiscountTypeSnapshot ?? "—",
            ["DiscountDisplayText"] = hasDiscount
                ? PendingSignupDisplayHelper.GetDiscountDisplayText(
                    signup.DiscountTypeSnapshot,
                    signup.DiscountValueSnapshot,
                    signup.DiscountFreeMonthsSnapshot,
                    signup.Currency)
                : "—",
            ["OriginalAmount"] = PendingSignupDisplayHelper.FormatAmount(signup.OriginalAmount, signup.Currency),
            ["DiscountAmount"] = PendingSignupDisplayHelper.FormatAmount(signup.DiscountAmount, signup.Currency),
            ["FinalAmount"] = PendingSignupDisplayHelper.FormatAmount(signup.FinalAmount ?? signup.Amount, signup.Currency),
            ["FreeMonths"] = signup.DiscountFreeMonthsSnapshot?.ToString() ?? "—",
            ["DiscountRedeemedAt"] = signup.DiscountRedeemedAt?.ToLocalTime().ToString("g") ?? "—",
            ["NextInvoiceDate"] = signup.NextInvoiceDate?.ToString("d") ?? "—",
            ["FollowUpBilling"] = PendingSignupDisplayHelper.GetFollowUpBillingDisplay(
                signup.BillingCycle,
                signup.MetadataJson,
                signup.DiscountTypeSnapshot,
                signup.DiscountFreeMonthsSnapshot,
                signup.OriginalAmount,
                signup.Currency),
            ["CurrentBillingAmount"] = PendingSignupDisplayHelper.FormatAmount(
                PendingSignupDisplayHelper.ResolveEffectiveCurrentBillingAmount(
                    signup.CurrentBillingAmount,
                    signup.FinalAmount,
                    signup.Amount,
                    signup.DiscountTypeSnapshot,
                    signup.OriginalAmount),
                signup.CurrentBillingCurrency ?? signup.Currency),
            ["CurrentBillingCurrency"] = signup.CurrentBillingCurrency ?? signup.Currency ?? "—",
            ["CurrentBillingCycle"] = signup.CurrentBillingCycle ?? signup.BillingCycle ?? "—",
            ["CurrentBillingDisplayText"] = PendingSignupDisplayHelper.FormatCurrentBillingDisplay(
                signup.CurrentBillingAmount,
                signup.CurrentBillingCurrency,
                signup.CurrentBillingCycle,
                signup.FinalAmount,
                signup.Amount,
                signup.Currency,
                signup.BillingCycle,
                signup.MetadataJson,
                signup.DiscountTypeSnapshot,
                signup.OriginalAmount)
        };
    }
}
