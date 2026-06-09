using Dsms.Web.Services.SubscriptionPlans;

namespace Dsms.Web.Services.Signup;

public interface IPaidSignupService
{
    Task<IReadOnlyList<SubscriptionPlanDetailsDto>> GetActivePaidPlansAsync();

    Task<PaidSignupSubmitResult> SubmitSignupAsync(PaidSignupFormDto form);
}
