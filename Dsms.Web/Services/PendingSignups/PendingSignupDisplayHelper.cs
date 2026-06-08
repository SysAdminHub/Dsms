using Dsms.Web.Domain;

namespace Dsms.Web.Services.PendingSignups;

public static class PendingSignupDisplayHelper
{
    public static string GetStatusDisplayName(string status) =>
        PendingSignupStatuses.GetDisplayName(status);

    public static string StatusVariant(string status) => status switch
    {
        PendingSignupStatuses.Provisioned => "success",
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
}
