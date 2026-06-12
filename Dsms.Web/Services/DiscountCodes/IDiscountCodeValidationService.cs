namespace Dsms.Web.Services.DiscountCodes;

public interface IDiscountCodeValidationService
{
    Task<DiscountCodeValidationResult> ValidateForSignupAsync(
        string? code,
        Guid planId,
        string billingCycle,
        decimal baseAmount,
        string currency);

    /// <summary>
    /// Erneute serverseitige Prüfung vor Provisionierung anhand des PendingSignup-Snapshots.
    /// </summary>
    Task<DiscountCodeValidationResult> ValidateForProvisioningAsync(Guid pendingSignupId);
}
