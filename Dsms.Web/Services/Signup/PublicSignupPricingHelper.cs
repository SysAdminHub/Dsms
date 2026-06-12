using Dsms.Web.Domain;
using Dsms.Web.Services.SubscriptionPlans;

namespace Dsms.Web.Services.Signup;

internal static class PublicSignupPricingHelper
{
    public static decimal? GetRegularPrice(SubscriptionPlanDetailsDto plan, string? billingCycle) =>
        billingCycle switch
        {
            BillingCycles.Monthly => plan.PriceMonthly,
            BillingCycles.Yearly => plan.PriceYearly,
            _ => null
        };

    public static decimal? GetRegularPrice(PublicSignupPlanDto plan, string? billingCycle) =>
        billingCycle switch
        {
            BillingCycles.Monthly => plan.PriceMonthly,
            BillingCycles.Yearly => plan.PriceYearly,
            _ => null
        };

    public static decimal? GetEffectivePrice(SubscriptionPlanDetailsDto plan, string? billingCycle) =>
        billingCycle switch
        {
            BillingCycles.Monthly => ResolveEffectiveMonthlyPrice(plan),
            BillingCycles.Yearly => ResolveEffectiveYearlyPrice(plan),
            _ => null
        };

    public static decimal? GetEffectivePrice(PublicSignupPlanDto plan, string? billingCycle) =>
        billingCycle switch
        {
            BillingCycles.Monthly => plan.EffectiveMonthlyPrice,
            BillingCycles.Yearly => plan.EffectiveYearlyPrice,
            _ => null
        };

    public static bool HasPromotionalPriceForCycle(PublicSignupPlanDto plan, string? billingCycle) =>
        billingCycle switch
        {
            BillingCycles.Monthly => SubscriptionPlanDisplayHelper.ShowsPromotionalMonthly(
                plan.IsFree, plan.IsPromotionalPriceEnabled, plan.PromotionalMonthlyPrice),
            BillingCycles.Yearly => SubscriptionPlanDisplayHelper.ShowsPromotionalYearly(
                plan.IsFree, plan.IsPromotionalPriceEnabled, plan.PromotionalYearlyPrice),
            _ => false
        };

    private static decimal? ResolveEffectiveMonthlyPrice(SubscriptionPlanDetailsDto plan) =>
        plan.IsPromotionalPriceEnabled && plan.PromotionalMonthlyPrice.HasValue
            ? plan.PromotionalMonthlyPrice
            : plan.PriceMonthly;

    private static decimal? ResolveEffectiveYearlyPrice(SubscriptionPlanDetailsDto plan) =>
        plan.IsPromotionalPriceEnabled && plan.PromotionalYearlyPrice.HasValue
            ? plan.PromotionalYearlyPrice
            : plan.PriceYearly;
}
