using Dsms.Web.Services.Licenses;
using Dsms.Web.Services.Logging;
using Dsms.Web.Services.Provisioning;
using Dsms.Web.Services.SubscriptionPlans;

namespace Dsms.Web.Services.Signup;

public sealed class FreeSignupService(
    ISubscriptionPlanService planService,
    IProvisioningService provisioningService,
    ILogService logService) : IFreeSignupService
{
    public Task<SubscriptionPlanDetailsDto?> GetActiveFreePlanAsync() =>
        planService.GetActiveFreePlanAsync();

    public async Task<FreeSignupSubmitResult> SubmitSignupAsync(FreeSignupFormDto form)
    {
        if (!string.IsNullOrWhiteSpace(form.Website))
        {
            return FreeSignupSubmitResult.Failed(
                "Die Registrierung konnte nicht abgeschlossen werden. Bitte versuchen Sie es später erneut oder wenden Sie sich an den Support.");
        }

        var validationError = ValidateForm(form);
        if (validationError is not null)
        {
            return FreeSignupSubmitResult.Failed(validationError);
        }

        var freePlan = await planService.GetActiveFreePlanAsync();
        if (freePlan is null)
        {
            return FreeSignupSubmitResult.Failed("Der kostenlose Tarif ist derzeit nicht verfügbar.");
        }

        await TryLogSubmittedAsync(freePlan.Id, form.AdminEmail.Trim());

        var request = BuildProvisionRequest(freePlan.Id, form);
        ProvisionCustomerResultDto result;

        try
        {
            result = await provisioningService.ProvisionCustomerAsync(request);
        }
        catch (Exception ex)
        {
            await TryLogFailedAsync(freePlan.Id, form.AdminEmail.Trim(), ex.Message);
            return FreeSignupSubmitResult.Failed(
                "Die Registrierung konnte nicht abgeschlossen werden. Bitte versuchen Sie es später erneut oder wenden Sie sich an den Support.");
        }

        if (!result.Success)
        {
            await TryLogFailedAsync(freePlan.Id, form.AdminEmail.Trim(), result.Message);
            return FreeSignupSubmitResult.Failed(MapPublicError(result));
        }

        return FreeSignupSubmitResult.Succeeded(result.PasswordSetupEmailSent);
    }

    private static string? ValidateForm(FreeSignupFormDto form)
    {
        if (string.IsNullOrWhiteSpace(form.CustomerName))
        {
            return "Bitte geben Sie den Unternehmensnamen ein.";
        }

        if (string.IsNullOrWhiteSpace(form.TenantName))
        {
            return "Bitte geben Sie den Namen des ersten Mandanten ein.";
        }

        if (string.IsNullOrWhiteSpace(form.AdminDisplayName))
        {
            return "Bitte geben Sie den Namen des Administrators ein.";
        }

        if (string.IsNullOrWhiteSpace(form.AdminEmail))
        {
            return "Bitte geben Sie eine gültige E-Mail-Adresse ein.";
        }

        if (!PlanToLicenseValidator.IsValidEmail(form.AdminEmail))
        {
            return "Bitte geben Sie eine gültige E-Mail-Adresse ein.";
        }

        if (!string.IsNullOrWhiteSpace(form.CustomerEmail)
            && !PlanToLicenseValidator.IsValidEmail(form.CustomerEmail))
        {
            return "Bitte geben Sie eine gültige E-Mail-Adresse ein.";
        }

        if (!form.AcceptTerms)
        {
            return "Bitte akzeptieren Sie die Nutzungsbedingungen und Datenschutzhinweise.";
        }

        return null;
    }

    private static ProvisionCustomerRequestDto BuildProvisionRequest(Guid planId, FreeSignupFormDto form)
    {
        var adminEmail = form.AdminEmail.Trim();
        var customerEmail = string.IsNullOrWhiteSpace(form.CustomerEmail)
            ? adminEmail
            : form.CustomerEmail.Trim();

        return new ProvisionCustomerRequestDto
        {
            PlanId = planId,
            CustomerName = form.CustomerName.Trim(),
            CustomerEmail = customerEmail,
            TenantName = form.TenantName.Trim(),
            TenantLegalName = string.IsNullOrWhiteSpace(form.TenantLegalName) ? null : form.TenantLegalName.Trim(),
            AdminEmail = adminEmail,
            AdminDisplayName = form.AdminDisplayName.Trim(),
            LicenseStatus = "Active",
            LicenseValidFrom = DateTime.UtcNow.Date,
            LicenseValidUntil = null,
            SendWelcomeEmail = true,
            Source = "FreeSignup"
        };
    }

    private static string MapPublicError(ProvisionCustomerResultDto result)
    {
        if (result.Errors.Any(e => e.Contains("existiert bereits", StringComparison.OrdinalIgnoreCase))
            || result.Message.Contains("existiert bereits", StringComparison.OrdinalIgnoreCase))
        {
            return "Für diese E-Mail-Adresse existiert bereits ein Benutzer.";
        }

        if (result.Message.Contains("Tarif", StringComparison.OrdinalIgnoreCase))
        {
            return "Der kostenlose Tarif ist derzeit nicht verfügbar.";
        }

        return "Der kostenlose Zugang konnte nicht erstellt werden.";
    }

    private async Task TryLogSubmittedAsync(Guid planId, string adminEmail)
    {
        try
        {
            await logService.LogSystemAsync(
                action: "FreeSignupSubmitted",
                description: "Kostenlose Registrierung wurde abgesendet.",
                metadata: new { PlanId = planId, AdminEmail = adminEmail });
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }

    private async Task TryLogFailedAsync(Guid planId, string adminEmail, string detail)
    {
        try
        {
            await logService.LogSystemAsync(
                action: "FreeSignupFailed",
                description: "Kostenlose Registrierung fehlgeschlagen.",
                severity: "Warning",
                metadata: new { PlanId = planId, AdminEmail = adminEmail, Detail = detail });
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }
}
