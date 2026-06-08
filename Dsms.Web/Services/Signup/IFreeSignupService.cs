using Dsms.Web.Services.Provisioning;
using Dsms.Web.Services.SubscriptionPlans;

namespace Dsms.Web.Services.Signup;

public interface IFreeSignupService
{
    Task<SubscriptionPlanDetailsDto?> GetActiveFreePlanAsync();

    Task<FreeSignupSubmitResult> SubmitSignupAsync(FreeSignupFormDto form);
}
