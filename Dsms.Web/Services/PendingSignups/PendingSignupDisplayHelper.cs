using System.Globalization;
using System.Text.Json;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Services.PendingSignups;

public static class PendingSignupDisplayHelper
{
    public static string GetStatusDisplayName(string status) =>
        PendingSignupStatuses.GetDisplayName(status);

    public static string StatusVariant(string status) => status switch
    {
        PendingSignupStatuses.Provisioned => "success",
        PendingSignupStatuses.Provisioning => "warning",
        PendingSignupStatuses.Paid => "success",
        PendingSignupStatuses.PendingPayment => "warning",
        PendingSignupStatuses.Draft => "default",
        PendingSignupStatuses.Failed => "danger",
        PendingSignupStatuses.Cancelled => "default",
        PendingSignupStatuses.Expired => "default",
        _ => "default"
    };

    public static string FormatAmount(decimal? amount, string? currency)
    {
        if (!amount.HasValue)
        {
            return "—";
        }

        return string.IsNullOrWhiteSpace(currency)
            ? amount.Value.ToString("N2")
            : $"{amount.Value:N2} {currency}";
    }

    /// <summary>Echter kostenloser Tarif (nicht kostenpflichtiger Plan mit Rabatt).</summary>
    public static bool IsFreeSignup(string? paymentProvider, decimal? amount) =>
        string.Equals(paymentProvider, "None", StringComparison.OrdinalIgnoreCase);

    public static bool HasDiscountCode(string? discountCodeSnapshot) =>
        !string.IsNullOrWhiteSpace(discountCodeSnapshot);

    public static bool IsFreeMonthsDiscount(string? discountTypeSnapshot) =>
        string.Equals(discountTypeSnapshot, nameof(DiscountCodeType.FreeMonths), StringComparison.Ordinal);

    public static string GetDiscountDisplayText(
        string? discountTypeSnapshot,
        decimal? discountValueSnapshot,
        int? discountFreeMonthsSnapshot,
        string? currency)
    {
        if (string.IsNullOrWhiteSpace(discountTypeSnapshot))
        {
            return "—";
        }

        if (IsFreeMonthsDiscount(discountTypeSnapshot) && discountFreeMonthsSnapshot is > 0)
        {
            var months = discountFreeMonthsSnapshot.Value;
            return $"{months} Monat{(months == 1 ? "" : "e")} kostenlos";
        }

        if (string.Equals(discountTypeSnapshot, nameof(DiscountCodeType.Percentage), StringComparison.Ordinal)
            && discountValueSnapshot is > 0)
        {
            return $"{discountValueSnapshot.Value.ToString("0.##", CultureInfo.GetCultureInfo("de-DE"))} % Rabatt";
        }

        if (discountValueSnapshot is > 0)
        {
            return $"{FormatAmount(discountValueSnapshot, currency)} Rabatt";
        }

        return "—";
    }

    public static string FormatTodayAmountDisplay(
        string? paymentProvider,
        decimal? finalAmount,
        decimal? amount,
        string? currency,
        string? discountTypeSnapshot)
    {
        if (IsFreeSignup(paymentProvider, amount))
        {
            return "Kostenlos";
        }

        var today = finalAmount ?? amount ?? 0m;
        var formatted = FormatAmount(today, currency);

        if (IsFreeMonthsDiscount(discountTypeSnapshot))
        {
            return $"{formatted} für kostenlose Startlaufzeit";
        }

        if (today == 0m && !string.IsNullOrWhiteSpace(discountTypeSnapshot))
        {
            return $"Heute zu zahlen: {formatted}";
        }

        return formatted;
    }

    public static string GetFollowUpBillingDisplay(
        string? billingCycle,
        string? metadataJson,
        string? discountTypeSnapshot,
        int? discountFreeMonthsSnapshot,
        decimal? originalAmount,
        string? currency)
    {
        if (!IsFreeMonthsDiscount(discountTypeSnapshot)
            || discountFreeMonthsSnapshot is not > 0
            || !originalAmount.HasValue)
        {
            return string.Empty;
        }

        var cycle = ResolveBillingCycle(billingCycle, metadataJson);
        var startMonth = discountFreeMonthsSnapshot.Value + 1;
        var amount = FormatAmount(originalAmount, currency);

        return cycle switch
        {
            BillingCycles.Yearly => $"Jahresrechnung ab Monat {startMonth}: {amount} / Jahr",
            BillingCycles.Monthly => $"Monatsrechnung ab Monat {startMonth}: {amount} / Monat",
            _ => $"Reguläre Rechnung ab Monat {startMonth}: {amount}"
        };
    }

    public static string FormatSignupAmountDisplay(
        string? paymentProvider,
        decimal? amount,
        decimal? finalAmount,
        string? currency,
        string? billingCycle,
        string? metadataJson,
        string? discountTypeSnapshot,
        string? discountCodeSnapshot)
    {
        if (IsFreeSignup(paymentProvider, amount))
        {
            return "Kostenlos";
        }

        if (IsFreeMonthsDiscount(discountTypeSnapshot) || (HasDiscountCode(discountCodeSnapshot) && (finalAmount ?? amount) == 0m))
        {
            return FormatTodayAmountDisplay(paymentProvider, finalAmount, amount, currency, discountTypeSnapshot);
        }

        return BillingCycleDisplayHelper.FormatAmountWithCycle(
            paymentProvider, amount, currency, billingCycle, metadataJson);
    }

    public static (decimal? Amount, string? Currency, string? Cycle) ResolveInitialCurrentBilling(
        string? paymentProvider,
        decimal? amount,
        decimal? finalAmount,
        string? currency,
        string? billingCycle,
        string? discountTypeSnapshot,
        decimal? originalAmount)
    {
        if (IsFreeSignup(paymentProvider, amount))
        {
            return (0m, currency, null);
        }

        var normalizedCycle = string.IsNullOrWhiteSpace(billingCycle) ? null : billingCycle.Trim();

        if (IsFreeMonthsDiscount(discountTypeSnapshot))
        {
            return (originalAmount, currency, normalizedCycle);
        }

        return (finalAmount ?? amount, currency, normalizedCycle);
    }

    public static decimal? ResolveEffectiveCurrentBillingAmount(
        decimal? currentBillingAmount,
        decimal? finalAmount,
        decimal? amount,
        string? discountTypeSnapshot,
        decimal? originalAmount)
    {
        if (currentBillingAmount.HasValue)
        {
            return currentBillingAmount;
        }

        if (IsFreeMonthsDiscount(discountTypeSnapshot))
        {
            return originalAmount;
        }

        return finalAmount ?? amount;
    }

    public static string FormatCurrentBillingDisplay(
        decimal? currentBillingAmount,
        string? currentBillingCurrency,
        string? currentBillingCycle,
        decimal? finalAmount,
        decimal? amount,
        string? currency,
        string? billingCycle,
        string? metadataJson,
        string? discountTypeSnapshot,
        decimal? originalAmount)
    {
        var effectiveAmount = ResolveEffectiveCurrentBillingAmount(
            currentBillingAmount,
            finalAmount,
            amount,
            discountTypeSnapshot,
            originalAmount);

        if (!effectiveAmount.HasValue)
        {
            return "—";
        }

        var effectiveCurrency = currentBillingCurrency ?? currency;
        var effectiveCycle = !string.IsNullOrWhiteSpace(currentBillingCycle)
            ? currentBillingCycle
            : ResolveBillingCycle(billingCycle, metadataJson);

        var amountStr = FormatAmount(effectiveAmount, effectiveCurrency);
        var suffix = effectiveCycle switch
        {
            BillingCycles.Monthly => "/ Monat",
            BillingCycles.Yearly => "/ Jahr",
            _ => string.Empty
        };

        return string.IsNullOrEmpty(suffix) ? amountStr : $"{amountStr} {suffix}";
    }

    public static string FormatListPriceSummary(PendingSignupListDto item)
    {
        if (IsFreeSignup(item.PaymentProvider, item.Amount))
        {
            return "Kostenlos";
        }

        var currentDisplay = FormatCurrentBillingDisplay(
            item.CurrentBillingAmount,
            item.CurrentBillingCurrency,
            item.CurrentBillingCycle,
            item.FinalAmount,
            item.Amount,
            item.Currency,
            item.BillingCycle,
            item.MetadataJson,
            item.DiscountTypeSnapshot,
            item.OriginalAmount);

        if (IsFreeMonthsDiscount(item.DiscountTypeSnapshot) && item.DiscountFreeMonthsSnapshot is > 0)
        {
            var today = FormatAmount(item.FinalAmount ?? item.Amount ?? 0m, item.Currency);
            return $"{today} heute · danach {currentDisplay}";
        }

        if (item.NextInvoiceDate.HasValue)
        {
            return $"{currentDisplay}\nNächste Rechnung: {item.NextInvoiceDate.Value:d}";
        }

        return currentDisplay;
    }

    public static bool HasBillingData(string? billingEmail, string? billingCompanyName) =>
        !string.IsNullOrWhiteSpace(billingEmail)
        || !string.IsNullOrWhiteSpace(billingCompanyName);

    public static bool HasCompleteBillingAddress(
        string? billingEmail,
        string? billingCompanyName,
        string? billingStreet,
        string? billingPostalCode,
        string? billingCity,
        string? billingCountry) =>
        HasBillingData(billingEmail, billingCompanyName)
        && !string.IsNullOrWhiteSpace(billingStreet)
        && !string.IsNullOrWhiteSpace(billingPostalCode)
        && !string.IsNullOrWhiteSpace(billingCity)
        && !string.IsNullOrWhiteSpace(billingCountry);

    /// <summary>
    /// Liest aus MetadataJson, ob eine abweichende Rechnungsadresse verwendet wurde.
    /// Fehlt der Wert (ältere Registrierungen), wird null zurückgegeben.
    /// </summary>
    public static bool? GetHasDifferentBillingAddress(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            if (doc.RootElement.TryGetProperty("HasDifferentBillingAddress", out var flag))
            {
                return flag.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => null
                };
            }
        }
        catch (JsonException)
        {
            // Ungültiges JSON ignorieren.
        }

        return null;
    }

    public static bool UsesCompanyBillingAddress(string? metadataJson) =>
        GetHasDifferentBillingAddress(metadataJson) == false;

    public static IReadOnlyList<(string Label, string Value)> BuildBillingNotificationRows(PendingSignup signup)
    {
        var rows = new List<(string Label, string Value)>
        {
            ("Rechnungs-E-Mail", signup.BillingEmail ?? "—")
        };

        if (UsesCompanyBillingAddress(signup.MetadataJson))
        {
            rows.Add(("Rechnungsadresse", "Entspricht der Unternehmensadresse"));
        }
        else
        {
            rows.Add(("Rechnungsempfänger / Firma", signup.BillingCompanyName ?? "—"));
            rows.Add(("Straße und Hausnummer", signup.BillingStreet ?? "—"));
            rows.Add(("PLZ", signup.BillingPostalCode ?? "—"));
            rows.Add(("Ort", signup.BillingCity ?? "—"));
            rows.Add(("Land", signup.BillingCountry ?? "—"));
        }

        rows.Add(("Umsatzsteuer-ID", signup.BillingVatId ?? "—"));
        rows.Add(("Bestellnummer / Referenz", signup.BillingReference ?? "—"));
        return rows;
    }

    public static IReadOnlyList<(string Label, string Value)> BuildBillingNotificationTextRows(PendingSignup signup)
    {
        var rows = new List<(string Label, string Value)>
        {
            ("Rechnungs-E-Mail", signup.BillingEmail ?? "—")
        };

        if (UsesCompanyBillingAddress(signup.MetadataJson))
        {
            rows.Add(("Rechnungsadresse", "Entspricht der Unternehmensadresse"));
        }
        else
        {
            rows.Add(("Rechnungsempfänger / Firma", signup.BillingCompanyName ?? "—"));
            rows.Add(("Straße", signup.BillingStreet ?? "—"));
            rows.Add(("PLZ", signup.BillingPostalCode ?? "—"));
            rows.Add(("Ort", signup.BillingCity ?? "—"));
            rows.Add(("Land", signup.BillingCountry ?? "—"));
        }

        rows.Add(("USt-IdNr.", signup.BillingVatId ?? "—"));
        rows.Add(("Referenz", signup.BillingReference ?? "—"));
        return rows;
    }

    public static string GetBillingStatus(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            if (doc.RootElement.TryGetProperty("BillingStatus", out var status))
            {
                return status.GetString() ?? string.Empty;
            }
        }
        catch (JsonException)
        {
            // Ungültiges JSON ignorieren.
        }

        return string.Empty;
    }

    public static string GetBillingCycle(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            if (doc.RootElement.TryGetProperty("BillingCycle", out var cycle))
            {
                return cycle.GetString() ?? string.Empty;
            }
        }
        catch (JsonException)
        {
            // Ungültiges JSON ignorieren.
        }

        return string.Empty;
    }

    public static string ResolveBillingCycle(string? billingCycle, string? metadataJson)
    {
        if (!string.IsNullOrWhiteSpace(billingCycle))
        {
            return billingCycle.Trim();
        }

        return GetBillingCycle(metadataJson);
    }

    public static string GetBillingStatusDisplay(string? metadataJson, bool isFree) =>
        GetBillingStatusDisplay(null, metadataJson, isFree ? "None" : null, isFree ? 0 : null);

    public static string GetBillingStatusDisplay(
        string? billingStatus,
        string? metadataJson,
        string? paymentProvider,
        decimal? amount) =>
        BillingStatusDisplayHelper.GetDisplayName(billingStatus, metadataJson, paymentProvider, amount);

    public static string FormatBillingSummary(
        string? paymentProvider,
        decimal? amount,
        string? billingEmail,
        string? billingCompanyName,
        string? metadataJson)
    {
        var isFree = IsFreeSignup(paymentProvider, amount);
        if (isFree)
        {
            return "Nicht erforderlich";
        }

        if (HasBillingData(billingEmail, billingCompanyName))
        {
            var email = billingEmail ?? "—";
            var status = GetBillingStatusDisplay(metadataJson, isFree: false);
            return $"{email} · {status}";
        }

        return "Rechnungsdaten fehlen";
    }

    public static string BillingStatusBadgeVariant(string? metadataJson, bool isFree) =>
        BillingStatusBadgeVariant(null, metadataJson, isFree ? "None" : null, isFree ? 0 : null);

    public static string BillingStatusBadgeVariant(
        string? billingStatus,
        string? metadataJson,
        string? paymentProvider,
        decimal? amount) =>
        BillingStatusDisplayHelper.StatusVariant(billingStatus, metadataJson, paymentProvider, amount);
}

