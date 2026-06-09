using Dsms.Web.Services.Licenses;
using Dsms.Web.Services.Logging;
using Dsms.Web.Services.PendingSignups;
using Dsms.Web.Services.SubscriptionPlans;

namespace Dsms.Web.Services.Signup;

public sealed class PaidSignupService(
    ISubscriptionPlanService planService,
    IPendingSignupService pendingSignupService,
    ILogService logService) : IPaidSignupService
{
    public Task<IReadOnlyList<SubscriptionPlanDetailsDto>> GetActivePaidPlansAsync() =>
        planService.GetActivePaidPlansAsync();

    public async Task<PaidSignupSubmitResult> SubmitSignupAsync(PaidSignupFormDto form)
    {
        if (!string.IsNullOrWhiteSpace(form.Website))
        {
            return PaidSignupSubmitResult.Failed(
                "Die Registrierung konnte nicht gespeichert werden. Bitte versuchen Sie es später erneut oder wenden Sie sich an den Support.");
        }

        var validationError = ValidateForm(form);
        if (validationError is not null)
        {
            return PaidSignupSubmitResult.Failed(validationError);
        }

        var paidPlans = await planService.GetActivePaidPlansAsync();
        var selectedPlan = paidPlans.FirstOrDefault(p => p.Id == form.PlanId);
        if (selectedPlan is null)
        {
            return PaidSignupSubmitResult.Failed("Bitte wählen Sie einen Tarif aus.");
        }

        if (selectedPlan.IsFree)
        {
            return PaidSignupSubmitResult.Failed(
                "Bitte verwenden Sie für den kostenlosen Tarif die kostenlose Registrierung.");
        }

        if (!selectedPlan.IsActive)
        {
            return PaidSignupSubmitResult.Failed("Der ausgewählte Tarif ist nicht aktiv.");
        }

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
            Source = "PaidSignup"
        };

        Guid pendingSignupId;
        try
        {
            pendingSignupId = await pendingSignupService.CreatePublicAsync(dto);
        }
        catch (InvalidOperationException ex)
        {
            await TryLogFailedAsync(selectedPlan.Id, form.CustomerName.Trim(), adminEmail, ex.Message);
            return PaidSignupSubmitResult.Failed(MapPublicError(ex.Message));
        }
        catch (Exception ex)
        {
            await TryLogFailedAsync(selectedPlan.Id, form.CustomerName.Trim(), adminEmail, ex.Message);
            return PaidSignupSubmitResult.Failed(
                "Die Registrierung konnte nicht gespeichert werden. Bitte versuchen Sie es später erneut oder wenden Sie sich an den Support.");
        }

        await TryLogSubmittedAsync(
            pendingSignupId,
            selectedPlan.Id,
            selectedPlan.DisplayName,
            form.CustomerName.Trim(),
            adminEmail);

        return PaidSignupSubmitResult.Succeeded();
    }

    private static string? ValidateForm(PaidSignupFormDto form)
    {
        if (form.PlanId == Guid.Empty)
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

    private static string MapPublicError(string message)
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

    private async Task TryLogSubmittedAsync(
        Guid pendingSignupId,
        Guid planId,
        string planDisplayName,
        string customerName,
        string adminEmail)
    {
        try
        {
            await logService.LogAuditAsync(
                action: "PaidSignupSubmitted",
                description: "Bezahlte Registrierung wurde abgesendet.",
                entityType: "PendingSignup",
                entityId: pendingSignupId.ToString(),
                entityName: customerName,
                metadata: new
                {
                    PendingSignupId = pendingSignupId,
                    PlanId = planId,
                    PlanDisplayNameSnapshot = planDisplayName,
                    CustomerName = customerName,
                    AdminEmail = adminEmail,
                    Source = "PaidSignup"
                },
                isVisibleToAdmin: false);
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }

    private async Task TryLogFailedAsync(Guid? planId, string? customerName, string? adminEmail, string errorMessage)
    {
        try
        {
            await logService.LogSystemAsync(
                action: "PaidSignupFailed",
                description: "Bezahlte Registrierung fehlgeschlagen.",
                severity: "Warning",
                metadata: new
                {
                    PlanId = planId,
                    CustomerName = customerName,
                    AdminEmail = adminEmail,
                    ErrorMessage = errorMessage
                });
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }
}
