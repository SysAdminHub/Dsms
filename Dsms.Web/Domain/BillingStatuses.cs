namespace Dsms.Web.Domain;

/// <summary>Rechnungs-/Zahlungsstatus einer Registrierung (unabhängig vom Signup-/Provisioning-Status).</summary>
public static class BillingStatuses
{
    public const string NotRequired = "NotRequired";
    public const string InvoicePending = "InvoicePending";
    public const string InvoiceSent = "InvoiceSent";
    public const string Paid = "Paid";
    public const string PaymentOverdue = "PaymentOverdue";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        NotRequired,
        InvoicePending,
        InvoiceSent,
        Paid,
        PaymentOverdue,
        Cancelled
    ];

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status) && All.Contains(status, StringComparer.OrdinalIgnoreCase);

    public static string GetDisplayName(string? status) => status switch
    {
        NotRequired => "Nicht erforderlich",
        InvoicePending => "Rechnung offen",
        InvoiceSent => "Rechnung gesendet",
        Paid => "Bezahlt",
        PaymentOverdue => "Überfällig",
        Cancelled => "Storniert",
        null or "" => "—",
        _ => status
    };

    public static string StatusVariant(string? status) => status switch
    {
        NotRequired => "default",
        InvoicePending => "warning",
        InvoiceSent => "info",
        Paid => "success",
        PaymentOverdue => "danger",
        Cancelled => "default",
        _ => "default"
    };
}
