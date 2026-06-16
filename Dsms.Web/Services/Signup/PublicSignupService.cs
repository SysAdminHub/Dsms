using System.Text.Json;

using Dsms.Web.Domain;

using Dsms.Web.Services.Licenses;

using Dsms.Web.Services.Logging;

using Dsms.Web.Services.PendingSignups;

using Dsms.Web.Services.Provisioning;

using Dsms.Web.Domain.Enums;

using Dsms.Web.Services.DiscountCodes;

using Dsms.Web.Services.Legal;

using Dsms.Web.Services.Privacy;

using Dsms.Web.Services.SubscriptionPlans;

using Microsoft.AspNetCore.Http;



namespace Dsms.Web.Services.Signup;



public sealed class PublicSignupService(

    ISubscriptionPlanService planService,

    IProvisioningService provisioningService,

    IPendingSignupService pendingSignupService,

    ISignupNotificationService signupNotificationService,

    ISignupLegalEmailService signupLegalEmailService,

    IDiscountCodeValidationService discountCodeValidation,

    ILegalDocumentService legalDocumentService,

    IIpAnonymizationService ipAnonymizationService,

    IHttpContextAccessor httpContextAccessor,

    ILogService logService) : IPublicSignupService

{

    private const string ProvisioningFailedMessage =

        "Die Registrierung konnte nicht vollständig abgeschlossen werden. Bitte versuchen Sie es erneut oder wenden Sie sich an den Support.";



    public async Task<IReadOnlyList<PublicSignupPlanDto>> GetPublicSignupPlansAsync()

    {

        var plans = await planService.GetPublicSignupPlansAsync();

        return plans.Select(MapToPublicPlan).ToList();

    }



    public async Task<DiscountCodeValidationResult> ValidateDiscountCodeAsync(
        string? code,
        Guid planId,
        string? billingCycle)
    {
        var plan = await planService.GetPublicSignupPlanByIdAsync(planId);
        if (plan is null)
        {
            return DiscountCodeValidationResult.Invalid(
                "Der ausgewählte Tarif ist nicht mehr verfügbar. Bitte wählen Sie einen anderen Tarif.");
        }

        if (plan.IsFree)
        {
            return DiscountCodeValidationResult.NoDiscount();
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return DiscountCodeValidationResult.NoDiscount();
        }

        if (string.IsNullOrWhiteSpace(billingCycle))
        {
            return DiscountCodeValidationResult.Invalid("Bitte wählen Sie zuerst den Abrechnungszeitraum aus.");
        }

        var baseAmount = PublicSignupPricingHelper.GetEffectivePrice(plan, billingCycle);
        if (!baseAmount.HasValue)
        {
            return DiscountCodeValidationResult.Invalid(
                "Dieser Rabattcode ist nicht gültig oder passt nicht zum ausgewählten Tarif.");
        }

        return await discountCodeValidation.ValidateForSignupAsync(
            code,
            planId,
            billingCycle.Trim(),
            baseAmount.Value,
            plan.Currency);
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

            ApplyCompanyBillingAddress(form);

            var billingError = ValidateBillingForm(form);

            if (billingError is not null)

            {

                return PublicSignupSubmitResult.Failed(billingError);

            }

            var billingCycleError = ValidateBillingCycle(selectedPlan, form);

            if (billingCycleError is not null)

            {

                return PublicSignupSubmitResult.Failed(billingCycleError);

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



        var (amount, billingCycle) = ResolveBillingAmount(selectedPlan, form.BillingCycle);
        var discountResult = await ResolveDiscountForSubmitAsync(selectedPlan, form, billingCycle);
        if (!discountResult.IsValid)
        {
            return PublicSignupSubmitResult.Failed(discountResult.ErrorMessage!);
        }

        if (discountResult.IsApplied)
        {
            amount = discountResult.FinalAmount;
            ApplyDiscountToForm(form, discountResult);
        }
        else
        {
            ClearAppliedDiscount(form);
        }

        var billingMetadata = BuildBillingMetadata(selectedPlan, billingCycle, discountResult, form.HasDifferentBillingAddress);
        var (initialBillingStatus, initialNextInvoiceDate) = ResolveInitialBilling(selectedPlan.IsFree, billingCycle);



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

            BillingCycle = billingCycle,

            PaymentProvider = selectedPlan.IsFree ? "None" : "ManualInvoice",

            MetadataJson = billingMetadata,

            BillingCompanyName = selectedPlan.IsFree ? null : form.BillingCompanyName.Trim(),

            BillingEmail = selectedPlan.IsFree ? null : form.BillingEmail.Trim(),

            BillingStreet = selectedPlan.IsFree ? null : form.BillingStreet.Trim(),

            BillingPostalCode = selectedPlan.IsFree ? null : form.BillingPostalCode.Trim(),

            BillingCity = selectedPlan.IsFree ? null : form.BillingCity.Trim(),

            BillingCountry = selectedPlan.IsFree ? null : form.BillingCountry.Trim(),

            BillingVatId = selectedPlan.IsFree ? null : NormalizeOptional(form.BillingVatId),

            BillingReference = selectedPlan.IsFree ? null : NormalizeOptional(form.BillingReference),

            BillingStatus = initialBillingStatus,

            NextInvoiceDate = initialNextInvoiceDate,

            DiscountCodeId = discountResult.IsApplied ? discountResult.DiscountCodeId : null,

            DiscountCodeSnapshot = discountResult.IsApplied ? discountResult.Code : null,

            DiscountNameSnapshot = discountResult.IsApplied ? discountResult.Name : null,

            DiscountTypeSnapshot = discountResult.IsApplied ? discountResult.DiscountType?.ToString() : null,

            DiscountValueSnapshot = discountResult.IsApplied
                ? discountResult.DiscountType switch
                {
                    DiscountCodeType.Percentage => discountResult.PercentageValue,
                    DiscountCodeType.FixedAmount => discountResult.FixedAmountValue,
                    _ => null
                }
                : null,

            DiscountFreeMonthsSnapshot = discountResult.IsApplied && discountResult.DiscountType == DiscountCodeType.FreeMonths
                ? discountResult.FreeMonths
                : null,

            OriginalAmount = discountResult.IsApplied ? discountResult.OriginalAmount : null,

            DiscountAmount = discountResult.IsApplied ? discountResult.DiscountAmount : null,

            FinalAmount = discountResult.IsApplied ? discountResult.FinalAmount : null

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

        var provisioningDiscount = await discountCodeValidation.ValidateForProvisioningAsync(pendingSignupId);
        if (!provisioningDiscount.IsValid)
        {
            var discountError = provisioningDiscount.ErrorMessage
                ?? "Der Rabattcode ist nicht mehr gültig. Bitte wenden Sie sich an den Support.";
            await MarkProvisioningFailedAsync(pendingSignupId, discountError);
            await TryLogDiscountInvalidDuringProvisioningAsync(pendingSignupId, discountError);
            await TryLogFailedAsync(selectedPlan.Id, adminEmail, discountError);
            return PublicSignupSubmitResult.Failed(discountError);
        }

        var request = BuildProvisionRequest(selectedPlan.Id, form, provisioningDiscount);
        request.PendingSignupId = pendingSignupId;

        var legalAcceptanceResult = await BuildLegalAcceptanceInputAsync(form, pendingSignupId);
        if (legalAcceptanceResult.ErrorMessage is not null)
        {
            await MarkProvisioningFailedAsync(pendingSignupId, legalAcceptanceResult.ErrorMessage);
            await TryLogFailedAsync(selectedPlan.Id, adminEmail, legalAcceptanceResult.ErrorMessage);
            return PublicSignupSubmitResult.Failed(legalAcceptanceResult.ErrorMessage);
        }

        request.LegalAcceptance = legalAcceptanceResult.Input;

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



        await TrySendSignupNotificationAsync(pendingSignupId, result.PasswordSetupEmailSent);

        await TrySendSignupLegalConfirmationAsync(
            result.TenantId!.Value,
            adminEmail,
            form.AdminDisplayName.Trim(),
            DateTime.UtcNow);

        if (discountResult.IsApplied)
        {
            await TryLogDiscountAppliedAsync(selectedPlan.Id, form, billingCycle, discountResult);
        }



        return PublicSignupSubmitResult.Succeeded(

            isPaidPlan: !selectedPlan.IsFree,

            passwordSetupEmailSent: result.PasswordSetupEmailSent,

            planDisplayName: selectedPlan.DisplayName);

    }



    private async Task TrySendSignupNotificationAsync(Guid pendingSignupId, bool passwordSetupEmailSent)
    {
        try
        {
            await signupNotificationService.TrySendPublicSignupNotificationAsync(
                pendingSignupId,
                passwordSetupEmailSent);
        }
        catch
        {
            // Fehler werden im SignupNotificationService protokolliert; Signup bleibt erfolgreich.
        }
    }

    private async Task TrySendSignupLegalConfirmationAsync(
        int tenantId,
        string recipientEmail,
        string contactName,
        DateTime registrationDateUtc)
    {
        try
        {
            await signupLegalEmailService.TrySendSignupLegalConfirmationAsync(
                tenantId,
                recipientEmail,
                contactName,
                registrationDateUtc);
        }
        catch
        {
            // Fehler werden im SignupLegalEmailService protokolliert; Signup bleibt erfolgreich.
        }
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



    private static (decimal? amount, string? billingCycle) ResolveBillingAmount(
        SubscriptionPlanDetailsDto plan,
        string? selectedCycle)

    {

        if (plan.IsFree)

        {

            return (0m, null);

        }

        var cycle = selectedCycle!.Trim();

        return cycle switch

        {

            BillingCycles.Monthly => (PublicSignupPricingHelper.GetEffectivePrice(plan, BillingCycles.Monthly), BillingCycles.Monthly),

            BillingCycles.Yearly => (PublicSignupPricingHelper.GetEffectivePrice(plan, BillingCycles.Yearly), BillingCycles.Yearly),

            _ => (null, null)

        };

    }

    private async Task<DiscountCodeValidationResult> ResolveDiscountForSubmitAsync(
        SubscriptionPlanDetailsDto plan,
        PublicSignupFormDto form,
        string? billingCycle)
    {
        if (plan.IsFree)
        {
            return DiscountCodeValidationResult.NoDiscount();
        }

        var code = !string.IsNullOrWhiteSpace(form.DiscountCodeInput)
            ? form.DiscountCodeInput
            : form.AppliedDiscountCode;

        if (string.IsNullOrWhiteSpace(code))
        {
            return DiscountCodeValidationResult.NoDiscount();
        }

        if (string.IsNullOrWhiteSpace(billingCycle))
        {
            return DiscountCodeValidationResult.Invalid("Bitte wählen Sie den Abrechnungszeitraum aus.");
        }

        var baseAmount = PublicSignupPricingHelper.GetEffectivePrice(plan, billingCycle);
        if (!baseAmount.HasValue)
        {
            return DiscountCodeValidationResult.Invalid(
                "Dieser Rabattcode ist nicht gültig oder passt nicht zum ausgewählten Tarif.");
        }

        return await discountCodeValidation.ValidateForSignupAsync(
            code,
            plan.Id,
            billingCycle,
            baseAmount.Value,
            plan.Currency);
    }

    private static void ApplyDiscountToForm(PublicSignupFormDto form, DiscountCodeValidationResult result)
    {
        form.AppliedDiscountCodeId = result.DiscountCodeId;
        form.AppliedDiscountCode = result.Code;
        form.AppliedDiscountName = result.Name;
        form.AppliedDiscountType = result.DiscountType;
        form.AppliedDiscountDisplayText = result.DisplayText;
        form.OriginalAmount = result.OriginalAmount;
        form.DiscountAmount = result.DiscountAmount;
        form.FinalAmount = result.FinalAmount;
        form.DiscountCodeInput = result.Code;
    }

    private static void ClearAppliedDiscount(PublicSignupFormDto form)
    {
        form.AppliedDiscountCodeId = null;
        form.AppliedDiscountCode = null;
        form.AppliedDiscountName = null;
        form.AppliedDiscountType = null;
        form.AppliedDiscountDisplayText = null;
        form.OriginalAmount = null;
        form.DiscountAmount = null;
        form.FinalAmount = null;
    }



    private static string? ValidateBillingCycle(SubscriptionPlanDetailsDto plan, PublicSignupFormDto form)

    {

        if (plan.IsFree)

        {

            return null;

        }

        if (string.IsNullOrWhiteSpace(form.BillingCycle))

        {

            return "Bitte wählen Sie den Abrechnungszeitraum aus.";

        }

        var cycle = form.BillingCycle.Trim();

        if (!BillingCycles.IsValid(cycle))

        {

            return "Der gewählte Abrechnungszeitraum ist für diesen Tarif nicht verfügbar.";

        }

        if (!BillingCycles.IsAvailableForPlan(cycle, plan.PriceMonthly, plan.PriceYearly))

        {

            return "Der gewählte Abrechnungszeitraum ist für diesen Tarif nicht verfügbar.";

        }

        return null;

    }



    private static string BuildBillingMetadata(
        SubscriptionPlanDetailsDto plan,
        string? billingCycle,
        DiscountCodeValidationResult discountResult,
        bool hasDifferentBillingAddress) =>

        JsonSerializer.Serialize(new

        {

            BillingStatus = plan.IsFree ? BillingStatuses.NotRequired : BillingStatuses.InvoicePending,

            BillingCycle = billingCycle,

            HasDifferentBillingAddress = hasDifferentBillingAddress,

            PromotionalPrice = plan.IsPromotionalPriceEnabled && !plan.IsFree

                ? new

                {

                    IsEnabled = true,

                    BadgeText = plan.PromotionalBadgeText,

                    RegularMonthlyPrice = plan.PriceMonthly,

                    RegularYearlyPrice = plan.PriceYearly,

                    PromotionalMonthlyPrice = plan.PromotionalMonthlyPrice,

                    PromotionalYearlyPrice = plan.PromotionalYearlyPrice,

                    EffectiveMonthlyPrice = PublicSignupPricingHelper.GetEffectivePrice(plan, BillingCycles.Monthly),

                    EffectiveYearlyPrice = PublicSignupPricingHelper.GetEffectivePrice(plan, BillingCycles.Yearly)

                }

                : null,

            Discount = discountResult.IsApplied

                ? new

                {

                    discountResult.DiscountCodeId,

                    discountResult.Code,

                    discountResult.Name,

                    DiscountType = discountResult.DiscountType?.ToString(),

                    discountResult.DisplayText,

                    discountResult.OriginalAmount,

                    discountResult.DiscountAmount,

                    discountResult.FinalAmount,

                    discountResult.FreeMonths

                }

                : null

        });



    private static (string BillingStatus, DateOnly? NextInvoiceDate) ResolveInitialBilling(bool isFree, string? billingCycle)
    {
        if (isFree)
        {
            return (BillingStatuses.NotRequired, null);
        }

        var registrationDate = DateOnly.FromDateTime(DateTime.UtcNow);

        if (string.Equals(billingCycle, BillingCycles.Yearly, StringComparison.OrdinalIgnoreCase))
        {
            return (BillingStatuses.InvoicePending, registrationDate.AddYears(1));
        }

        return (BillingStatuses.InvoicePending, registrationDate.AddMonths(1));
    }



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



        var legalError = ValidateLegalConsent(form);
        if (legalError is not null)
        {
            return legalError;
        }



        return ValidateTenantAddress(form);



    }



    private static string? ValidateLegalConsent(PublicSignupFormDto form)
    {
        var errors = new List<string>();

        if (!form.AcceptAgb)
        {
            errors.Add("Bitte akzeptieren Sie die AGB / SaaS-Nutzungsbedingungen.");
        }

        if (!form.AcceptPrivacyPolicy)
        {
            errors.Add("Bitte bestätigen Sie, dass Sie die Datenschutzerklärung zur Kenntnis genommen haben.");
        }

        if (!form.AcceptDataProcessingAgreement)
        {
            errors.Add("Bitte akzeptieren Sie den Auftragsverarbeitungsvertrag einschließlich TOM-Anlage und Unterauftragnehmerliste.");
        }

        return errors.Count == 0 ? null : string.Join(" ", errors);
    }



    private static string? ValidateTenantAddress(PublicSignupFormDto form)

    {

        if (string.IsNullOrWhiteSpace(form.TenantStreet))

        {

            return "Bitte geben Sie Straße und Hausnummer ein.";

        }



        if (string.IsNullOrWhiteSpace(form.TenantPostalCode))

        {

            return "Bitte geben Sie die Postleitzahl ein.";

        }



        if (string.IsNullOrWhiteSpace(form.TenantCity))

        {

            return "Bitte geben Sie den Ort ein.";

        }



        if (string.IsNullOrWhiteSpace(form.TenantCountry))

        {

            return "Bitte geben Sie das Land ein.";

        }



        return null;

    }



    private static void ApplyCompanyBillingAddress(PublicSignupFormDto form)
    {
        if (form.HasDifferentBillingAddress)
        {
            return;
        }

        form.BillingCompanyName = form.CustomerName.Trim();
        form.BillingStreet = form.TenantStreet.Trim();
        form.BillingPostalCode = form.TenantPostalCode.Trim();
        form.BillingCity = form.TenantCity.Trim();
        form.BillingCountry = form.TenantCountry.Trim();
    }

    private static string? ValidateBillingForm(PublicSignupFormDto form)

    {

        if (string.IsNullOrWhiteSpace(form.BillingEmail))

        {

            return "Bitte geben Sie eine Rechnungs-E-Mail-Adresse ein.";

        }



        if (!PlanToLicenseValidator.IsValidEmail(form.BillingEmail))

        {

            return "Bitte geben Sie eine gültige Rechnungs-E-Mail-Adresse ein.";

        }

        if (!form.HasDifferentBillingAddress)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(form.BillingCompanyName))

        {

            return "Bitte geben Sie den Rechnungsempfänger bzw. Firmennamen ein.";

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



    private static ProvisionCustomerRequestDto BuildProvisionRequest(
        Guid planId,
        PublicSignupFormDto form,
        DiscountCodeValidationResult? provisioningDiscount = null)

    {

        var adminEmail = form.AdminEmail.Trim();

        var customerEmail = string.IsNullOrWhiteSpace(form.CustomerEmail)

            ? adminEmail

            : form.CustomerEmail.Trim();



        var validFrom = DateTime.UtcNow.Date;

        var validUntil = provisioningDiscount?.IsApplied == true
            && provisioningDiscount.DiscountType == DiscountCodeType.FreeMonths
            && provisioningDiscount.FreeMonths is > 0
            ? validFrom.AddMonths(provisioningDiscount.FreeMonths.Value)
            : validFrom.AddMonths(1);



        return new ProvisionCustomerRequestDto

        {

            PlanId = planId,

            CustomerName = form.CustomerName.Trim(),

            CustomerEmail = customerEmail,

            TenantName = form.TenantName.Trim(),

            TenantLegalName = string.IsNullOrWhiteSpace(form.TenantLegalName) ? null : form.TenantLegalName.Trim(),

            TenantStreet = form.TenantStreet.Trim(),

            TenantPostalCode = form.TenantPostalCode.Trim(),

            TenantCity = form.TenantCity.Trim(),

            TenantCountry = form.TenantCountry.Trim(),

            TenantPhone = NormalizeOptional(form.TenantPhone),

            TenantVatId = NormalizeOptional(form.TenantVatId),

            AdminEmail = adminEmail,

            AdminDisplayName = form.AdminDisplayName.Trim(),

            LicenseStatus = "Active",

            LicenseValidFrom = validFrom,

            LicenseValidUntil = validUntil,

            SendWelcomeEmail = true,

            Source = "PublicSignup"

        };

    }



    private async Task<(LegalAcceptanceInputDto? Input, string? ErrorMessage)> BuildLegalAcceptanceInputAsync(
        PublicSignupFormDto form,
        Guid pendingSignupId)
    {
        var metadata = await legalDocumentService.GetMetadataAsync();
        if (metadata is null || string.IsNullOrWhiteSpace(metadata.Version))
        {
            return (null, "Die rechtlichen Dokumente sind derzeit nicht verfügbar. Bitte versuchen Sie es später erneut.");
        }

        var httpContext = httpContextAccessor.HttpContext;
        var rawIpAddress = LogIpAnonymizer.GetClientIpAddress(httpContext);
        var anonymizedIpAddress = ipAnonymizationService.AnonymizeIpAddress(rawIpAddress);

        var userAgent = httpContext?.Request.Headers.UserAgent.FirstOrDefault();
        var acceptedAtUtc = DateTime.UtcNow;
        var adminEmail = form.AdminEmail.Trim();

        return (new LegalAcceptanceInputDto
        {
            AcceptedTerms = form.AcceptAgb,
            AcceptedPrivacyPolicy = form.AcceptPrivacyPolicy,
            AcceptedDataProcessingAgreement = form.AcceptDataProcessingAgreement,
            LegalVersion = metadata.Version.Trim(),
            EffectiveDate = metadata.EffectiveDate.Trim(),
            AcceptedAtUtc = acceptedAtUtc,
            AnonymizedIpAddress = anonymizedIpAddress,
            UserAgent = userAgent,
            PendingSignupId = pendingSignupId,
            SignupEmail = adminEmail,
            TenantNameSnapshot = form.TenantName.Trim(),
            CompanyNameSnapshot = form.CustomerName.Trim()
        }, null);
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

        EffectiveMonthlyPrice = PublicSignupPricingHelper.GetEffectivePrice(plan, BillingCycles.Monthly),

        EffectiveYearlyPrice = PublicSignupPricingHelper.GetEffectivePrice(plan, BillingCycles.Yearly),

        IsPromotionalPriceEnabled = plan.IsPromotionalPriceEnabled,

        PromotionalMonthlyPrice = plan.PromotionalMonthlyPrice,

        PromotionalYearlyPrice = plan.PromotionalYearlyPrice,

        PromotionalBadgeText = plan.PromotionalBadgeText,

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



    private async Task TryLogDiscountInvalidDuringProvisioningAsync(Guid pendingSignupId, string detail)
    {
        try
        {
            await logService.LogSystemAsync(
                action: "PublicSignupDiscountInvalidDuringProvisioning",
                description: "Rabattcode-Validierung vor Provisionierung fehlgeschlagen.",
                severity: "Warning",
                metadata: new { PendingSignupId = pendingSignupId, Detail = detail });
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }

    private async Task TryLogDiscountAppliedAsync(
        Guid planId,
        PublicSignupFormDto form,
        string? billingCycle,
        DiscountCodeValidationResult discountResult)
    {
        try
        {
            await logService.LogSystemAsync(
                action: "PublicSignupDiscountApplied",
                description: "Rabattcode wurde bei öffentlicher Registrierung angewendet.",
                metadata: new
                {
                    PlanId = planId,
                    Code = discountResult.Code,
                    BillingCycle = billingCycle,
                    DiscountType = discountResult.DiscountType?.ToString(),
                    discountResult.OriginalAmount,
                    discountResult.DiscountAmount,
                    discountResult.FinalAmount,
                    AdminEmail = form.AdminEmail.Trim()
                });
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


