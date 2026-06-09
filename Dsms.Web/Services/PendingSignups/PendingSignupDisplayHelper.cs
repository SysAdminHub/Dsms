using System.Text.Json;
using Dsms.Web.Domain;

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

    public static bool IsFreeSignup(string? paymentProvider, decimal? amount) =>
        string.Equals(paymentProvider, "None", StringComparison.OrdinalIgnoreCase)
        || amount == 0;

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

