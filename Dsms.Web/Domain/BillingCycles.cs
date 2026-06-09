namespace Dsms.Web.Domain;

/// <summary>Abrechnungszeitraum für kostenpflichtige Registrierungen.</summary>
public static class BillingCycles
{
    public const string Monthly = "Monthly";
    public const string Yearly = "Yearly";

    public static readonly IReadOnlyList<string> All = [Monthly, Yearly];

    public static bool IsValid(string? cycle) =>
        !string.IsNullOrWhiteSpace(cycle) && All.Contains(cycle, StringComparer.OrdinalIgnoreCase);

    public static bool IsAvailableForPlan(string cycle, decimal? priceMonthly, decimal? priceYearly) =>
        cycle switch
        {
            Monthly when priceMonthly.HasValue => true,
            Yearly when priceYearly.HasValue => true,
            _ => false
        };

    /// <summary>Jährlich bevorzugen, falls gesetzt; sonst monatlich.</summary>
    public static string? ResolveDefault(decimal? priceMonthly, decimal? priceYearly)
    {
        if (priceYearly.HasValue)
        {
            return Yearly;
        }

        if (priceMonthly.HasValue)
        {
            return Monthly;
        }

        return null;
    }
}
