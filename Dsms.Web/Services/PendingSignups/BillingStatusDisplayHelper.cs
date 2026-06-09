using Dsms.Web.Domain;

namespace Dsms.Web.Services.PendingSignups;

public static class BillingStatusDisplayHelper
{
    public static string ResolveBillingStatus(
        string? billingStatus,
        string? metadataJson,
        string? paymentProvider,
        decimal? amount)
    {
        if (PendingSignupDisplayHelper.IsFreeSignup(paymentProvider, amount))
        {
            return BillingStatuses.NotRequired;
        }

        if (!string.IsNullOrWhiteSpace(billingStatus))
        {
            return billingStatus.Trim();
        }

        var fromMetadata = PendingSignupDisplayHelper.GetBillingStatus(metadataJson);
        if (!string.IsNullOrWhiteSpace(fromMetadata))
        {
            return fromMetadata;
        }

        return BillingStatuses.InvoicePending;
    }

    public static string GetDisplayName(
        string? billingStatus,
        string? metadataJson,
        string? paymentProvider,
        decimal? amount) =>
        BillingStatuses.GetDisplayName(ResolveBillingStatus(billingStatus, metadataJson, paymentProvider, amount));

    public static string StatusVariant(
        string? billingStatus,
        string? metadataJson,
        string? paymentProvider,
        decimal? amount) =>
        BillingStatuses.StatusVariant(ResolveBillingStatus(billingStatus, metadataJson, paymentProvider, amount));

    public static string FormatNextInvoiceDate(DateOnly? nextInvoiceDate, bool isFree)
    {
        if (isFree)
        {
            return "Nicht erforderlich";
        }

        return nextInvoiceDate?.ToString("d") ?? "—";
    }

    public static string FormatInvoiceTimestamp(DateTime? value) =>
        value?.ToLocalTime().ToString("g") ?? "—";
}
