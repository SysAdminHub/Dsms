namespace Dsms.Web.Services.Signup;

public interface IPublicSignupService
{
    Task<IReadOnlyList<PublicSignupPlanDto>> GetPublicSignupPlansAsync();

    Task<PublicSignupSubmitResult> SubmitSignupAsync(PublicSignupFormDto form);
}
