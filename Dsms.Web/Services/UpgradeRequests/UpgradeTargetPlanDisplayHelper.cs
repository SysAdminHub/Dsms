using Dsms.Web.Services.SubscriptionPlans;

namespace Dsms.Web.Services.UpgradeRequests;

public static class UpgradeTargetPlanDisplayHelper
{
    public const string NoPriceMessage = "Für diesen Tarif ist kein Preis hinterlegt.";
    public const string IndividualPriceMessage = "Nach Absprache";
    public const string RequestOnlyHint =
        "Dies ist nur eine Anfrage. Es erfolgt kein automatischer Tarifwechsel und keine automatische Berechnung.";
    public const string EmailRequestHint =
        "Es handelt sich um eine Anfrage. Es wurde kein automatischer Tarifwechsel durchgeführt.";

    public static string GetPlanTitle(UpgradeTargetPlanDto plan) =>
        string.IsNullOrWhiteSpace(plan.DisplayName) ? plan.Name : plan.DisplayName;

    public static string ResolveCurrency(UpgradeTargetPlanDto plan) =>
        string.IsNullOrWhiteSpace(plan.Currency) ? "EUR" : plan.Currency.Trim();

    public static bool HasMonthlyPrice(UpgradeTargetPlanDto plan) => plan.PriceMonthly.HasValue;

    public static bool HasYearlyPrice(UpgradeTargetPlanDto plan) => plan.PriceYearly.HasValue;

    public static bool HasAnyPrice(UpgradeTargetPlanDto plan) =>
        HasMonthlyPrice(plan) || HasYearlyPrice(plan);

    public static string FormatMonthlyLine(UpgradeTargetPlanDto plan) =>
        $"Monatlich: {SubscriptionPlanDisplayHelper.FormatMonthlyPrice(plan.PriceMonthly, ResolveCurrency(plan), plan.IsFree)}";

    public static string FormatYearlyLine(UpgradeTargetPlanDto plan) =>
        $"Jährlich: {SubscriptionPlanDisplayHelper.FormatYearlyPrice(plan.PriceYearly, ResolveCurrency(plan), plan.IsFree)}";

    public static IReadOnlyList<string> FormatPriceLines(UpgradeTargetPlanDto plan)
    {
        if (!HasAnyPrice(plan))
        {
            return [NoPriceMessage];
        }

        var lines = new List<string>(2);
        if (HasMonthlyPrice(plan))
        {
            lines.Add(FormatMonthlyLine(plan));
        }

        if (HasYearlyPrice(plan))
        {
            lines.Add(FormatYearlyLine(plan));
        }

        return lines;
    }

    public static string FormatPriceSummary(UpgradeTargetPlanDto plan) =>
        string.Join(Environment.NewLine, FormatPriceLines(plan));

    public static string FormatStorageLimit(int? maxStorageMb) =>
        maxStorageMb.HasValue
            ? $"{SubscriptionPlanDisplayHelper.FormatLimit(maxStorageMb)} MB"
            : SubscriptionPlanDisplayHelper.FormatLimit(maxStorageMb);
}
