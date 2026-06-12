using Dsms.Web.Services.DiscountCodes;

namespace Dsms.Web.Services.Signup;

public interface IPublicSignupService
{
    Task<IReadOnlyList<PublicSignupPlanDto>> GetPublicSignupPlansAsync();

    Task<DiscountCodeValidationResult> ValidateDiscountCodeAsync(
        string? code,
        Guid planId,
        string? billingCycle);

    Task<PublicSignupSubmitResult> SubmitSignupAsync(PublicSignupFormDto form);
}
