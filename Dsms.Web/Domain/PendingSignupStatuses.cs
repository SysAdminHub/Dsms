namespace Dsms.Web.Domain;

/// <summary>Statuswerte für ausstehende Registrierungen (bezahlte Signups, Mollie-Vorbereitung).</summary>
public static class PendingSignupStatuses
{
    public const string Draft = "Draft";
    public const string PendingPayment = "PendingPayment";
    public const string Paid = "Paid";
    public const string Provisioned = "Provisioned";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";

    public static readonly IReadOnlyList<string> All =
    [
        Draft,
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
        PendingPayment,
        Paid
    ];

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status) && All.Contains(status, StringComparer.OrdinalIgnoreCase);

    public static string GetDisplayName(string status) => status switch
    {
        Draft => "Entwurf",
        PendingPayment => "Zahlung ausstehend",
        Paid => "Bezahlt",
        Provisioned => "Provisioniert",
        Failed => "Fehlgeschlagen",
        Cancelled => "Abgebrochen",
        Expired => "Abgelaufen",
        _ => status
    };
}
