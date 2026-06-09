using System.Text.Json;
using Dsms.Web.Domain;
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
    private const string ProvisioningFailedMessage =
        "Die Registrierung konnte nicht vollständig abgeschlossen werden. Bitte versuchen Sie es erneut oder wenden Sie sich an den Support.";

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

        var validationError = ValidateCommonForm(form);
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

        if (!selectedPlan.IsFree)
        {
            var billingError = ValidateBillingForm(form);
            if (billingError is not null)
            {
                return PublicSignupSubmitResult.Failed(billingError);
            }
        }

        await TryLogSubmittedAsync(selectedPlan.Id, form.AdminEmail.Trim());

        return await SubmitWithProvisioningAsync(selectedPlan, form);
    }

    private async Task<PublicSignupSubmitResult> SubmitWithProvisioningAsync(
        SubscriptionPlanDetailsDto selectedPlan,
        PublicSignupFormDto form)
    {
        var adminEmail = form.AdminEmail.Trim();
        var customerEmail = string.IsNullOrWhiteSpace(form.CustomerEmail)
            ? adminEmail
            : form.CustomerEmail.Trim();

        var (amount, billingCycle) = ResolveBillingAmount(selectedPlan);
        var billingMetadata = BuildBillingMetadata(selectedPlan.IsFree, billingCycle);

        var pendingDto = new CreatePublicPendingSignupDto
        {
            PlanId = selectedPlan.Id,
            CustomerName = form.CustomerName.Trim(),
            CustomerEmail = customerEmail,
            TenantName = form.TenantName.Trim(),
            TenantLegalName = string.IsNullOrWhiteSpace(form.TenantLegalName) ? null : form.TenantLegalName.Trim(),
            AdminEmail = adminEmail,
            AdminDisplayName = form.AdminDisplayName.Trim(),
            Source = "PublicSignup",
            Amount = amount,
            Currency = selectedPlan.Currency,
            PaymentProvider = selectedPlan.IsFree ? "None" : "ManualInvoice",
            MetadataJson = billingMetadata,
            BillingCompanyName = selectedPlan.IsFree ? null : form.BillingCompanyName.Trim(),
            BillingEmail = selectedPlan.IsFree ? null : form.BillingEmail.Trim(),
            BillingStreet = selectedPlan.IsFree ? null : form.BillingStreet.Trim(),
            BillingPostalCode = selectedPlan.IsFree ? null : form.BillingPostalCode.Trim(),
            BillingCity = selectedPlan.IsFree ? null : form.BillingCity.Trim(),
            BillingCountry = selectedPlan.IsFree ? null : form.BillingCountry.Trim(),
            BillingVatId = selectedPlan.IsFree ? null : NormalizeOptional(form.BillingVatId),
            BillingReference = selectedPlan.IsFree ? null : NormalizeOptional(form.BillingReference)
        };

        Guid pendingSignupId;
        try
        {
            pendingSignupId = await pendingSignupService.CreateForPublicSignupAsync(pendingDto);
        }
        catch (InvalidOperationException ex)
        {
            await TryLogFailedAsync(selectedPlan.Id, adminEmail, ex.Message);
            return PublicSignupSubmitResult.Failed(MapSignupError(ex.Message));
        }
        catch (Exception ex)
        {
            await TryLogFailedAsync(selectedPlan.Id, adminEmail, ex.Message);
            return PublicSignupSubmitResult.Failed(
                "Die Registrierung konnte nicht gespeichert werden. Bitte versuchen Sie es später erneut oder wenden Sie sich an den Support.");
        }

        await pendingSignupService.SetStatusForPublicSignupAsync(pendingSignupId, PendingSignupStatuses.Provisioning);

        var request = BuildProvisionRequest(selectedPlan.Id, form);
        ProvisionCustomerResultDto result;

        try
        {
            result = await provisioningService.ProvisionCustomerAsync(request);
        }
        catch (Exception ex)
        {
            await MarkProvisioningFailedAsync(pendingSignupId, ex.Message);
            await TryLogFailedAsync(selectedPlan.Id, adminEmail, ex.Message);
            return PublicSignupSubmitResult.Failed(ProvisioningFailedMessage);
        }

        if (!result.Success)
        {
            var detail = result.Message;
            await MarkProvisioningFailedAsync(pendingSignupId, detail);
            await TryLogFailedAsync(selectedPlan.Id, adminEmail, detail);
            return PublicSignupSubmitResult.Failed(MapProvisioningError(result));
        }

        await pendingSignupService.MarkAsProvisionedForPublicSignupAsync(
            pendingSignupId,
            result.LicenseId!.Value,
            result.LicenseNumber,
            result.TenantId!.Value,
            result.TenantName,
            result.AdminUserId!);

        return PublicSignupSubmitResult.Succeeded(
            isPaidPlan: !selectedPlan.IsFree,
            passwordSetupEmailSent: result.PasswordSetupEmailSent,
            planDisplayName: selectedPlan.DisplayName);
    }

    private async Task MarkProvisioningFailedAsync(Guid pendingSignupId, string detail)
    {
        try
        {
            await pendingSignupService.MarkAsFailedForPublicSignupAsync(pendingSignupId, detail);
        }
        catch
        {
            // Statusaktualisierung darf keine weitere Fehlermeldung verursachen.
        }
    }

    private static (decimal? amount, string billingCycle) ResolveBillingAmount(SubscriptionPlanDetailsDto plan)
    {
        if (plan.IsFree)
        {
            return (0m, "None");
        }

        // BillingCycle-Auswahl (monatlich/jährlich) kann später ergänzt werden.
        if (plan.PriceYearly.HasValue)
        {
            return (plan.PriceYearly, "Yearly");
        }

        return (plan.PriceMonthly, "Monthly");
    }

    private static string BuildBillingMetadata(bool isFree, string billingCycle) =>
        JsonSerializer.Serialize(new
        {
            BillingStatus = isFree ? "NotRequired" : "InvoicePending",
            BillingCycle = billingCycle
        });

    private static string? ValidateCommonForm(PublicSignupFormDto form)
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

    private static string? ValidateBillingForm(PublicSignupFormDto form)
    {
        if (string.IsNullOrWhiteSpace(form.BillingCompanyName))
        {
            return "Bitte geben Sie den Rechnungsempfänger bzw. Firmennamen ein.";
        }

        if (string.IsNullOrWhiteSpace(form.BillingEmail))
        {
            return "Bitte geben Sie eine Rechnungs-E-Mail-Adresse ein.";
        }

        if (!PlanToLicenseValidator.IsValidEmail(form.BillingEmail))
        {
            return "Bitte geben Sie eine gültige Rechnungs-E-Mail-Adresse ein.";
        }

        if (string.IsNullOrWhiteSpace(form.BillingStreet))
        {
            return "Bitte geben Sie Straße und Hausnummer ein.";
        }

        if (string.IsNullOrWhiteSpace(form.BillingPostalCode))
        {
            return "Bitte geben Sie die Postleitzahl ein.";
        }

        if (string.IsNullOrWhiteSpace(form.BillingCity))
        {
            return "Bitte geben Sie den Ort ein.";
        }

        if (string.IsNullOrWhiteSpace(form.BillingCountry))
        {
            return "Bitte geben Sie das Land ein.";
        }

        return null;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ProvisionCustomerRequestDto BuildProvisionRequest(Guid planId, PublicSignupFormDto form)
    {
        var adminEmail = form.AdminEmail.Trim();
        var customerEmail = string.IsNullOrWhiteSpace(form.CustomerEmail)
            ? adminEmail
            : form.CustomerEmail.Trim();

        var validFrom = DateTime.UtcNow.Date;
        var validUntil = validFrom.AddMonths(1);

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
            LicenseValidFrom = validFrom,
            LicenseValidUntil = validUntil,
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

    private static string MapProvisioningError(ProvisionCustomerResultDto result)
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

        return ProvisioningFailedMessage;
    }

    private static string MapSignupError(string message)
    {
        if (message.Contains("existiert bereits ein Benutzer", StringComparison.OrdinalIgnoreCase)
            || message.Contains("existiert bereits eine offene Registrierung", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Tarif ist nicht aktiv", StringComparison.OrdinalIgnoreCase)
            || message.Contains("nicht mehr verfügbar", StringComparison.OrdinalIgnoreCase)
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
