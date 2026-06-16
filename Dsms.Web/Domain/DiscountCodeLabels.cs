using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain;

public static class DiscountCodeLabels
{
    public static string GetTypeLabel(DiscountCodeType type) => type switch
    {
        DiscountCodeType.Percentage => "Prozent-Rabatt",
        DiscountCodeType.FixedAmount => "Fester Betrag",
        DiscountCodeType.FreeMonths => "Kostenlose Monate",
        _ => type.ToString()
    };

    public static string GetBillingCycleScopeLabel(string? cycle) => cycle switch
    {
        BillingCycles.Monthly => "Nur monatlich",
        BillingCycles.Yearly => "Nur jährlich",
        null or "" => "Monatlich und jährlich",
        _ => cycle
    };
}
