using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.DiscountCodes;
using Dsms.Web.Services.Licenses;
using Dsms.Web.Services.Legal;
using Dsms.Web.Services.Logging;
using Dsms.Web.Services.PasswordReset;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Provisioning;

public sealed class ProvisioningService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ILogService logService,
    IPasswordResetService passwordReset,
    IDiscountCodeValidationService discountCodeValidation,
    ILegalAcceptanceService legalAcceptanceService) : IProvisioningService
{
    public async Task<ProvisionCustomerResultDto> ProvisionCustomerAsync(ProvisionCustomerRequestDto dto)
    {
        var warnings = new List<string>();

        try
        {
            var validationError = ValidateRequest(dto);
            if (validationError is not null)
            {
                return ProvisionCustomerResultDto.Fail(validationError);
            }

            var existingUser = await userManager.FindByEmailAsync(dto.AdminEmail.Trim());
            if (existingUser is not null)
            {
                return ProvisionCustomerResultDto.Fail("Für diese E-Mail-Adresse existiert bereits ein Benutzer.");
            }

            if (!await roleManager.RoleExistsAsync(DsmsRoles.Admin))
            {
                return ProvisionCustomerResultDto.Fail("Die erforderliche Admin-Rolle wurde nicht gefunden.");
            }

            var plan = await db.SubscriptionPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == dto.PlanId);

            if (plan is null)
            {
                return ProvisionCustomerResultDto.Fail("Der ausgewählte Tarif wurde nicht gefunden.");
            }

            if (!plan.IsActive)
            {
                return ProvisionCustomerResultDto.Fail("Der ausgewählte Tarif ist nicht aktiv.");
            }

            PendingSignup? pendingSignup = null;
            DiscountCodeValidationResult? discountValidation = null;

            if (dto.PendingSignupId.HasValue)
            {
                pendingSignup = await db.PendingSignups
                    .FirstOrDefaultAsync(p => p.Id == dto.PendingSignupId.Value);

                if (pendingSignup is null)
                {
                    return ProvisionCustomerResultDto.Fail("Die Registrierung wurde nicht gefunden.");
                }

                if (pendingSignup.DiscountCodeId.HasValue && !pendingSignup.DiscountRedeemedAt.HasValue)
                {
                    discountValidation = await discountCodeValidation.ValidateForProvisioningAsync(dto.PendingSignupId.Value);
                    if (!discountValidation.IsValid)
                    {
                        await TryLogDiscountRedemptionFailedAsync(
                            pendingSignup,
                            discountValidation.ErrorMessage ?? "Rabattcode ungültig.");
                        return ProvisionCustomerResultDto.Fail(
                            discountValidation.ErrorMessage
                                ?? "Der Rabattcode ist nicht mehr gültig. Bitte wenden Sie sich an den Support.");
                    }
                }
            }

            var licenseDto = BuildLicenseDto(dto);
            PlanToLicenseValidator.ValidateForCreate(licenseDto);

            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var licenseNumber = await LicenseNumberGenerator.GenerateAsync(db);
                var license = PlanToLicenseMapper.MapToLicense(plan, licenseDto, licenseNumber);
                license.Id = Guid.NewGuid();
                license.CreatedAt = DateTime.UtcNow;

                db.Licenses.Add(license);

                var tenant = new Tenant
                {
                    Name = dto.TenantName.Trim(),
                    LegalName = string.IsNullOrWhiteSpace(dto.TenantLegalName)
                        ? dto.CustomerName.Trim()
                        : dto.TenantLegalName.Trim(),
                    Street = NormalizeOptional(dto.TenantStreet),
                    PostalCode = NormalizeOptional(dto.TenantPostalCode),
                    City = NormalizeOptional(dto.TenantCity),
                    Country = NormalizeOptional(dto.TenantCountry),
                    ContactName = string.IsNullOrWhiteSpace(dto.AdminDisplayName) ? null : dto.AdminDisplayName.Trim(),
                    Email = NormalizeOptional(dto.CustomerEmail) ?? dto.AdminEmail.Trim(),
                    Phone = NormalizeOptional(dto.TenantPhone),
                    VatId = NormalizeOptional(dto.TenantVatId),
                    IsActive = true,
                    LicenseId = license.Id,
                    CreatedAt = DateTime.UtcNow
                };

                db.Tenants.Add(tenant);
                await db.SaveChangesAsync();

                var adminEmail = dto.AdminEmail.Trim();
                var displayName = ResolveAdminDisplayName(dto);

                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    DisplayName = displayName,
                    TenantId = tenant.Id,
                    LicenseId = license.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = null
                };

                var createResult = await userManager.CreateAsync(admin);
                if (!createResult.Succeeded)
                {
                    var errors = createResult.Errors.Select(e => e.Description).ToList();
                    await transaction.RollbackAsync();
                    await TryLogProvisioningFailedAsync(dto, "AdminCreateFailed", errors);
                    return ProvisionCustomerResultDto.Fail(
                        "Der Kunde konnte nicht angelegt werden. Bitte prüfen Sie die Systemprotokolle.",
                        errors);
                }

                var roleResult = await userManager.AddToRoleAsync(admin, DsmsRoles.Admin);
                if (!roleResult.Succeeded)
                {
                    var errors = roleResult.Errors.Select(e => e.Description).ToList();
                    await transaction.RollbackAsync();
                    await TryLogProvisioningFailedAsync(dto, "AdminRoleAssignFailed", errors);
                    return ProvisionCustomerResultDto.Fail(
                        "Der Kunde konnte nicht angelegt werden. Bitte prüfen Sie die Systemprotokolle.",
                        errors);
                }

                db.UserTenants.Add(new UserTenant
                {
                    UserId = admin.Id,
                    TenantId = tenant.Id,
                    AssignedAt = DateTime.UtcNow
                });

                DiscountCode? redeemedDiscountCode = null;
                if (pendingSignup is not null
                    && discountValidation?.IsApplied == true
                    && !pendingSignup.DiscountRedeemedAt.HasValue)
                {
                    redeemedDiscountCode = await db.DiscountCodes
                        .FirstOrDefaultAsync(d => d.Id == pendingSignup.DiscountCodeId!.Value);

                    if (redeemedDiscountCode is null)
                    {
                        await transaction.RollbackAsync();
                        await TryLogDiscountRedemptionFailedAsync(pendingSignup, "Rabattcode nicht gefunden.");
                        return ProvisionCustomerResultDto.Fail(
                            "Der Rabattcode ist nicht mehr gültig. Bitte wenden Sie sich an den Support.");
                    }

                    if (redeemedDiscountCode.MaxRedemptions.HasValue
                        && redeemedDiscountCode.CurrentRedemptions >= redeemedDiscountCode.MaxRedemptions.Value)
                    {
                        await transaction.RollbackAsync();
                        await TryLogDiscountRedemptionFailedAsync(pendingSignup, "Nutzungslimit erreicht.");
                        return ProvisionCustomerResultDto.Fail(
                            "Der Rabattcode ist nicht mehr gültig. Bitte wenden Sie sich an den Support.");
                    }

                    redeemedDiscountCode.CurrentRedemptions++;
                    redeemedDiscountCode.UpdatedAt = DateTime.UtcNow;

                    pendingSignup.DiscountRedeemedAt = DateTime.UtcNow;
                    pendingSignup.UpdatedAt = DateTime.UtcNow;

                    if (redeemedDiscountCode.DiscountType == DiscountCodeType.FreeMonths)
                    {
                        ApplyFreeMonthsBilling(pendingSignup, redeemedDiscountCode);
                    }

                    await db.SaveChangesAsync();
                }

                if (dto.LegalAcceptance is not null)
                {
                    await legalAcceptanceService.AddWithinTransactionAsync(
                        db,
                        dto.LegalAcceptance,
                        tenant.Id,
                        admin.Id);
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                await TryLogCustomerProvisionedAsync(plan, license, tenant, admin, dto.Source);

                if (redeemedDiscountCode is not null && pendingSignup is not null && discountValidation is not null)
                {
                    await TryLogDiscountCodeRedeemedAsync(
                        pendingSignup,
                        redeemedDiscountCode,
                        discountValidation,
                        license,
                        tenant);

                    if (redeemedDiscountCode.DiscountType == DiscountCodeType.FreeMonths)
                    {
                        await TryLogLicenseValidityAdjustedByDiscountCodeAsync(
                            pendingSignup,
                            redeemedDiscountCode,
                            license,
                            tenant);
                    }
                }

                var passwordSetupEmailSent = false;
                if (dto.SendWelcomeEmail)
                {
                    var emailResult = await passwordReset.SendProvisioningWelcomeEmailAsync(admin.Id, tenant.Name);
                    passwordSetupEmailSent = emailResult.Succeeded;

                    if (!emailResult.Succeeded)
                    {
                        warnings.Add("Die Passwortvergabe-Mail konnte nicht versendet werden.");
                        await TryLogPasswordEmailFailedAsync(license.Id, tenant.Id, admin.Id, emailResult.Message);
                    }
                }
                else
                {
                    warnings.Add("Keine Willkommensmail versendet (SendWelcomeEmail = false).");
                }

                return ProvisionCustomerResultDto.Ok(
                    message: "Der Kunde wurde erfolgreich angelegt.",
                    licenseId: license.Id,
                    licenseNumber: license.LicenseNumber,
                    tenantId: tenant.Id,
                    tenantName: tenant.Name,
                    adminUserId: admin.Id,
                    adminEmail: admin.Email!,
                    passwordSetupEmailSent: passwordSetupEmailSent,
                    warnings: warnings);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await TryLogProvisioningFailedAsync(dto, "ProvisioningFailed", [ex.Message]);
                return ProvisionCustomerResultDto.Fail(
                    "Der Kunde konnte nicht angelegt werden. Bitte prüfen Sie die Systemprotokolle.",
                    [ex.Message]);
            }
        }
        catch (Exception ex)
        {
            await TryLogProvisioningFailedAsync(dto, "ProvisioningFailed", [ex.Message]);
            return ProvisionCustomerResultDto.Fail(
                "Der Kunde konnte nicht angelegt werden. Bitte prüfen Sie die Systemprotokolle.",
                [ex.Message]);
        }
    }

    private static string? ValidateRequest(ProvisionCustomerRequestDto dto)
    {
        if (dto.PlanId == Guid.Empty)
        {
            return "Bitte wählen Sie einen Tarif aus.";
        }

        if (string.IsNullOrWhiteSpace(dto.CustomerName))
        {
            return "Bitte geben Sie einen Kundennamen ein.";
        }

        if (string.IsNullOrWhiteSpace(dto.TenantName))
        {
            return "Bitte geben Sie einen Mandantennamen ein.";
        }

        if (string.IsNullOrWhiteSpace(dto.AdminEmail))
        {
            return "Bitte geben Sie eine Admin-E-Mail-Adresse ein.";
        }

        if (!PlanToLicenseValidator.IsValidEmail(dto.AdminEmail))
        {
            return "Die Admin-E-Mail-Adresse ist ungültig.";
        }

        if (string.IsNullOrWhiteSpace(dto.AdminDisplayName))
        {
            return "Bitte geben Sie einen Anzeigenamen für den Admin ein.";
        }

        if (!string.IsNullOrWhiteSpace(dto.CustomerEmail) && !PlanToLicenseValidator.IsValidEmail(dto.CustomerEmail))
        {
            return "Die Kunden-E-Mail-Adresse ist ungültig.";
        }

        var status = string.IsNullOrWhiteSpace(dto.LicenseStatus) ? "Active" : dto.LicenseStatus.Trim();
        if (status is not ("Active" or "Inactive" or "Suspended"))
        {
            return "Der Lizenzstatus ist ungültig.";
        }

        var validFrom = dto.LicenseValidFrom ?? DateTime.UtcNow.Date;
        if (dto.LicenseValidUntil.HasValue && dto.LicenseValidUntil.Value < validFrom)
        {
            return "Das Ablaufdatum darf nicht vor dem Startdatum liegen.";
        }

        return null;
    }

    private static CreateLicenseFromPlanDto BuildLicenseDto(ProvisionCustomerRequestDto dto)
    {
        var source = string.IsNullOrWhiteSpace(dto.Source) ? "ManualProvisioning" : dto.Source.Trim();
        var autoNote = $"Automatisch durch ProvisioningService erstellt. Quelle: {source}";
        var internalNote = string.IsNullOrWhiteSpace(dto.LicenseInternalNote)
            ? autoNote
            : $"{dto.LicenseInternalNote.Trim()}\n{autoNote}";

        return new CreateLicenseFromPlanDto
        {
            PlanId = dto.PlanId,
            CustomerName = dto.CustomerName.Trim(),
            CustomerEmail = string.IsNullOrWhiteSpace(dto.CustomerEmail) ? null : dto.CustomerEmail.Trim(),
            Status = string.IsNullOrWhiteSpace(dto.LicenseStatus) ? "Active" : dto.LicenseStatus.Trim(),
            ValidFrom = dto.LicenseValidFrom ?? DateTime.UtcNow.Date,
            ValidUntil = dto.LicenseValidUntil,
            InternalNote = internalNote
        };
    }

    private static string ResolveAdminDisplayName(ProvisionCustomerRequestDto dto) =>
        dto.AdminDisplayName.Trim();

    private async Task TryLogCustomerProvisionedAsync(
        SubscriptionPlan plan,
        License license,
        Tenant tenant,
        ApplicationUser admin,
        string? source)
    {
        try
        {
            await logService.LogAuditAsync(
                action: "CustomerProvisioned",
                description: "Neuer Kunde wurde provisioniert.",
                entityType: "License",
                entityId: license.Id.ToString(),
                entityName: license.LicenseNumber,
                tenantId: tenant.Id,
                licenseId: license.Id,
                metadata: new
                {
                    PlanId = plan.Id,
                    PlanName = plan.Name,
                    PlanDisplayName = plan.DisplayName,
                    LicenseId = license.Id,
                    LicenseNumber = license.LicenseNumber,
                    TenantId = tenant.Id,
                    TenantName = tenant.Name,
                    AdminUserId = admin.Id,
                    AdminEmail = admin.Email,
                    Source = source ?? "ManualProvisioning"
                },
                isVisibleToAdmin: false);
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }

    private async Task TryLogProvisioningFailedAsync(
        ProvisionCustomerRequestDto dto,
        string action,
        IReadOnlyList<string> errors)
    {
        try
        {
            await logService.LogSystemAsync(
                action: action,
                description: "Provisionierung fehlgeschlagen.",
                severity: "Error",
                metadata: new
                {
                    dto.PlanId,
                    dto.CustomerName,
                    dto.TenantName,
                    dto.AdminEmail,
                    dto.Source,
                    Errors = errors
                });
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }

    private static void ApplyFreeMonthsBilling(PendingSignup pending, DiscountCode discountCode)
    {
        var freeMonths = discountCode.FreeMonths ?? pending.DiscountFreeMonthsSnapshot ?? 0;
        if (freeMonths <= 0)
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        pending.Amount = 0m;
        pending.FinalAmount = 0m;
        pending.BillingStatus = BillingStatuses.NotRequired;
        pending.NextInvoiceDate = today.AddMonths(freeMonths);
        pending.CurrentBillingAmount ??= pending.OriginalAmount;
        pending.CurrentBillingCurrency ??= pending.Currency;
        pending.CurrentBillingCycle ??= pending.BillingCycle;

        var code = pending.DiscountCodeSnapshot ?? discountCode.Code;
        pending.BillingNote = $"Rabattcode {code}: {freeMonths} Monate kostenlos. Erste Rechnung ab Monat {freeMonths + 1}.";
    }

    private async Task TryLogDiscountCodeRedeemedAsync(
        PendingSignup pendingSignup,
        DiscountCode discountCode,
        DiscountCodeValidationResult discountValidation,
        License license,
        Tenant tenant)
    {
        try
        {
            await logService.LogAuditAsync(
                action: "DiscountCodeRedeemed",
                description: "Rabattcode wurde nach erfolgreicher Provisionierung eingelöst.",
                entityType: "PendingSignup",
                entityId: pendingSignup.Id.ToString(),
                entityName: pendingSignup.CustomerName,
                tenantId: tenant.Id,
                licenseId: license.Id,
                metadata: new
                {
                    PendingSignupId = pendingSignup.Id,
                    DiscountCodeId = discountCode.Id,
                    Code = discountCode.Code,
                    PlanId = pendingSignup.PlanId,
                    BillingCycle = pendingSignup.BillingCycle,
                    DiscountType = discountCode.DiscountType.ToString(),
                    discountValidation.OriginalAmount,
                    discountValidation.DiscountAmount,
                    discountValidation.FinalAmount,
                    FreeMonths = discountCode.FreeMonths,
                    LicenseId = license.Id,
                    TenantId = tenant.Id
                },
                isVisibleToAdmin: false);
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }

    private async Task TryLogLicenseValidityAdjustedByDiscountCodeAsync(
        PendingSignup pendingSignup,
        DiscountCode discountCode,
        License license,
        Tenant tenant)
    {
        try
        {
            await logService.LogAuditAsync(
                action: "LicenseValidityAdjustedByDiscountCode",
                description: "Lizenzlaufzeit wurde durch Rabattcode mit kostenlosen Monaten angepasst.",
                entityType: "License",
                entityId: license.Id.ToString(),
                entityName: license.LicenseNumber,
                tenantId: tenant.Id,
                licenseId: license.Id,
                metadata: new
                {
                    PendingSignupId = pendingSignup.Id,
                    DiscountCodeId = discountCode.Id,
                    Code = discountCode.Code,
                    FreeMonths = discountCode.FreeMonths,
                    LicenseValidFrom = license.ValidFrom,
                    LicenseValidUntil = license.ValidUntil,
                    NextInvoiceDate = pendingSignup.NextInvoiceDate
                },
                isVisibleToAdmin: false);
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }

    private async Task TryLogDiscountRedemptionFailedAsync(PendingSignup pendingSignup, string detail)
    {
        try
        {
            await logService.LogSystemAsync(
                action: "PublicSignupDiscountInvalidDuringProvisioning",
                description: "Rabattcode-Validierung vor Provisionierung fehlgeschlagen.",
                severity: "Warning",
                metadata: new
                {
                    PendingSignupId = pendingSignup.Id,
                    pendingSignup.DiscountCodeId,
                    pendingSignup.DiscountCodeSnapshot,
                    pendingSignup.PlanId,
                    pendingSignup.BillingCycle,
                    Detail = detail
                });
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }

    private async Task TryLogPasswordEmailFailedAsync(
        Guid licenseId,
        int tenantId,
        string adminUserId,
        string detail)
    {
        try
        {
            await logService.LogSystemAsync(
                action: "PasswordSetupEmailFailed",
                description: "Passwortvergabe-Mail konnte nach Provisionierung nicht versendet werden.",
                severity: "Warning",
                tenantId: tenantId,
                licenseId: licenseId,
                metadata: new { AdminUserId = adminUserId, Detail = detail });
        }
        catch
        {
            // Protokollierung darf Fachfunktion nicht blockieren.
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
