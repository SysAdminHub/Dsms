using Dsms.Web.Services.Licenses;
using Dsms.Web.Services.Logging;
using Dsms.Web.Services.PendingSignups;
using Dsms.Web.Services.Provisioning;
using Dsms.Web.Services.SubscriptionPlans;

namespace Dsms.Web.Services.Signup;

public sealed class PublicSignupService(
    ISubscriptionPlanService planService,
    IProvisioningService provisioningService,
    IPendingSignupService pendingSignupService,
    ILogService logService) : IPublicSignupService
{
    public async Task<IReadOnlyList<PublicSignupPlanDto>> GetPublicSignupPlansAsync()
    {
        var plans = await planService.GetPublicSignupPlansAsync();
        return plans.Select(MapToPublicPlan).ToList();
    }

    public async Task<PublicSignupSubmitResult> SubmitSignupAsync(PublicSignupFormDto form)
    {
        if (!string.IsNullOrWhiteSpace(form.Website))
        {
            return PublicSignupSubmitResult.Failed(
                "Die Registrierung konnte nicht abgeschlossen werden. Bitte versuchen Sie es später erneut oder wenden Sie sich an den Support.");
        }

        var validationError = ValidateForm(form);
        if (validationError is not null)
        {
            return PublicSignupSubmitResult.Failed(validationError);
        }

        var selectedPlan = await planService.GetPublicSignupPlanByIdAsync(form.SelectedPlanId);
        if (selectedPlan is null)
        {
            return PublicSignupSubmitResult.Failed(
                "Der ausgewählte Tarif ist nicht mehr verfügbar. Bitte wählen Sie einen anderen Tarif.");
        }

        await TryLogSubmittedAsync(selectedPlan.Id, form.AdminEmail.Trim());

        if (selectedPlan.IsFree)
        {
            return await SubmitFreeSignupAsync(selectedPlan.Id, form);
        }

        return await SubmitPaidSignupAsync(selectedPlan, form);
    }

    private async Task<PublicSignupSubmitResult> SubmitFreeSignupAsync(Guid planId, PublicSignupFormDto form)
    {
        var request = BuildProvisionRequest(planId, form);
        ProvisionCustomerResultDto result;

        try
        {
            result = await provisioningService.ProvisionCustomerAsync(request);
        }
        catch (Exception ex)
        {
            await TryLogFailedAsync(planId, form.AdminEmail.Trim(), ex.Message);
            return PublicSignupSubmitResult.Failed(
                "Die Registrierung konnte nicht abgeschlossen werden. Bitte versuchen Sie es später erneut oder wenden Sie sich an den Support.");
        }

        if (!result.Success)
        {
            await TryLogFailedAsync(planId, form.AdminEmail.Trim(), result.Message);
            return PublicSignupSubmitResult.Failed(MapFreeProvisioningError(result));
        }

        return PublicSignupSubmitResult.FreeSucceeded(result.PasswordSetupEmailSent);
    }

    private async Task<PublicSignupSubmitResult> SubmitPaidSignupAsync(
        SubscriptionPlanDetailsDto selectedPlan,
        PublicSignupFormDto form)
    {
        var adminEmail = form.AdminEmail.Trim();
        var customerEmail = string.IsNullOrWhiteSpace(form.CustomerEmail)
            ? adminEmail
            : form.CustomerEmail.Trim();

        var dto = new CreatePendingSignupDto
        {
            PlanId = selectedPlan.Id,
            CustomerName = form.CustomerName.Trim(),
            CustomerEmail = customerEmail,
            TenantName = form.TenantName.Trim(),
            TenantLegalName = string.IsNullOrWhiteSpace(form.TenantLegalName) ? null : form.TenantLegalName.Trim(),
            AdminEmail = adminEmail,
            AdminDisplayName = form.AdminDisplayName.Trim(),
            Source = "PublicSignup"
        };

        try
        {
            await pendingSignupService.CreatePublicAsync(dto);
        }
        catch (InvalidOperationException ex)
        {
            await TryLogFailedAsync(selectedPlan.Id, adminEmail, ex.Message);
            return PublicSignupSubmitResult.Failed(MapPaidSignupError(ex.Message));
        }
        catch (Exception ex)
        {
            await TryLogFailedAsync(selectedPlan.Id, adminEmail, ex.Message);
            return PublicSignupSubmitResult.Failed(
                "Die Registrierung konnte nicht gespeichert werden. Bitte versuchen Sie es später erneut oder wenden Sie sich an den Support.");
        }

        return PublicSignupSubmitResult.PaidPendingSucceeded();
    }

    private static string? ValidateForm(PublicSignupFormDto form)
    {
        if (form.SelectedPlanId == Guid.Empty)
        {
            return "Bitte wählen Sie einen Tarif aus.";
        }

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

    private static ProvisionCustomerRequestDto BuildProvisionRequest(Guid planId, PublicSignupFormDto form)
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
            Source = "PublicSignup"
        };
    }

    private static PublicSignupPlanDto MapToPublicPlan(SubscriptionPlanDetailsDto plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        DisplayName = plan.DisplayName,
        Description = plan.Description,
        IsFree = plan.IsFree,
        PriceMonthly = plan.PriceMonthly,
        PriceYearly = plan.PriceYearly,
        Currency = plan.Currency,
        SortOrder = plan.SortOrder,
        MaxTenants = plan.MaxTenants,
        MaxAdmins = plan.MaxAdmins,
        MaxUsersPerTenant = plan.MaxUsersPerTenant,
        MaxAuditorsPerTenant = plan.MaxAuditorsPerTenant,
        MaxDpiaPerTenant = plan.MaxDpiaPerTenant,
        MaxTomsPerTenant = plan.MaxTomsPerTenant,
        MaxProcessorsPerTenant = plan.MaxProcessorsPerTenant,
        MaxActiveMeasuresPerTenant = plan.MaxActiveMeasuresPerTenant,
        MaxStorageMb = plan.MaxStorageMb
    };

    private static string MapFreeProvisioningError(ProvisionCustomerResultDto result)
    {
        if (result.Errors.Any(e => e.Contains("existiert bereits", StringComparison.OrdinalIgnoreCase))
            || result.Message.Contains("existiert bereits", StringComparison.OrdinalIgnoreCase))
        {
            return "Für diese E-Mail-Adresse existiert bereits ein Benutzer.";
        }

        if (result.Message.Contains("Tarif", StringComparison.OrdinalIgnoreCase))
        {
            return "Der ausgewählte Tarif ist nicht mehr verfügbar. Bitte wählen Sie einen anderen Tarif.";
        }

        return "Die Registrierung konnte nicht abgeschlossen werden.";
    }

    private static string MapPaidSignupError(string message)
    {
        if (message.Contains("existiert bereits ein Benutzer", StringComparison.OrdinalIgnoreCase)
            || message.Contains("existiert bereits eine offene Registrierung", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Tarif ist nicht aktiv", StringComparison.OrdinalIgnoreCase)
            || message.Contains("kein bezahlter Tarif", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Bitte wählen Sie einen Tarif", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Bitte geben Sie", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Bitte akzeptieren Sie", StringComparison.OrdinalIgnoreCase))
        {
            return message;
        }

        return "Die Registrierung konnte nicht gespeichert werden. Bitte versuchen Sie es später erneut oder wenden Sie sich an den Support.";
    }

    private async Task TryLogSubmittedAsync(Guid planId, string adminEmail)
    {
        try
        {
            await logService.LogSystemAsync(
                action: "PublicSignupSubmitted",
                description: "Öffentliche Registrierung wurde abgesendet.",
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
                action: "PublicSignupFailed",
                description: "Öffentliche Registrierung fehlgeschlagen.",
                severity: "Warning",
                metadata: new { PlanId = planId, AdminEmail = adminEmail, Detail = detail });
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }
}
