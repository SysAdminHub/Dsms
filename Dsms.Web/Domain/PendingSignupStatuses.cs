namespace Dsms.Web.Domain;

/// <summary>Statuswerte für ausstehende Registrierungen (bezahlte Signups, Mollie-Vorbereitung).</summary>
public static class PendingSignupStatuses
{
    public const string Draft = "Draft";
    public const string Provisioning = "Provisioning";
    public const string PendingPayment = "PendingPayment";
    public const string Paid = "Paid";
    public const string Provisioned = "Provisioned";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";

    public static readonly IReadOnlyList<string> All =
    [
        Draft,
        Provisioning,
        PendingPayment,
        Paid,
        Provisioned,
        Failed,
        Cancelled,
        Expired
    ];

    public static readonly IReadOnlyList<string> EditableBySuperuser =
    [
        Draft,
        PendingPayment,
        Paid,
        Failed,
        Cancelled,
        Expired
    ];

    /// <summary>Status, die eine noch nicht abgeschlossene Registrierung darstellen.</summary>
    public static readonly IReadOnlyList<string> Open =
    [
        Draft,
        Provisioning,
        PendingPayment,
        Paid
    ];

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status) && All.Contains(status, StringComparer.OrdinalIgnoreCase);

    public static string GetDisplayName(string status) => status switch
    {
        Draft => "Entwurf",
        Provisioning => "Wird provisioniert",
        PendingPayment => "Zahlung ausstehend",
        Paid => "Bezahlt",
        Provisioned => "Provisioniert",
        Failed => "Fehlgeschlagen",
        Cancelled => "Abgebrochen",
        Expired => "Abgelaufen",
        _ => status
    };
}
