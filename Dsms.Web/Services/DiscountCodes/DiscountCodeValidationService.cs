using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.DiscountCodes;

public sealed class DiscountCodeValidationService(IDbContextFactory<ApplicationDbContext> dbFactory)
    : IDiscountCodeValidationService
{
    private const string GenericInvalidMessage =
        "Dieser Rabattcode ist nicht gültig oder passt nicht zum ausgewählten Tarif.";

    public async Task<DiscountCodeValidationResult> ValidateForSignupAsync(
        string? code,
        Guid planId,
        string billingCycle,
        decimal baseAmount,
        string currency)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return DiscountCodeValidationResult.NoDiscount();
        }

        if (baseAmount < 0)
        {
            return DiscountCodeValidationResult.Invalid(GenericInvalidMessage);
        }

        if (!BillingCycles.IsValid(billingCycle))
        {
            return DiscountCodeValidationResult.Invalid(GenericInvalidMessage);
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        await using var db = await dbFactory.CreateDbContextAsync();

        var entity = await db.DiscountCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == normalizedCode);

        if (entity is null)
        {
            return DiscountCodeValidationResult.Invalid(GenericInvalidMessage);
        }

        var entityValidation = ValidateEntityForSignup(entity, planId, billingCycle);
        if (entityValidation is not null)
        {
            return entityValidation;
        }

        var calculation = CalculateDiscount(entity, baseAmount);
        if (calculation is null)
        {
            return DiscountCodeValidationResult.Invalid(GenericInvalidMessage);
        }

        var (discountAmount, finalAmount, displayText) = calculation.Value;
        var successMessage = BuildSuccessMessage(entity.Code, displayText);

        return new DiscountCodeValidationResult
        {
            IsValid = true,
            IsApplied = true,
            DiscountCodeId = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            DiscountType = entity.DiscountType,
            PercentageValue = entity.PercentageValue,
            FixedAmountValue = entity.FixedAmountValue,
            FreeMonths = entity.FreeMonths,
            OriginalAmount = baseAmount,
            DiscountAmount = discountAmount,
            FinalAmount = finalAmount,
            Currency = currency,
            DisplayText = displayText,
            SuccessMessage = successMessage
        };
    }

    private static (decimal DiscountAmount, decimal FinalAmount, string DisplayText)? CalculateDiscount(
        Domain.Entities.DiscountCode entity,
        decimal baseAmount)
    {
        return entity.DiscountType switch
        {
            DiscountCodeType.Percentage when entity.PercentageValue is > 0 and <= 100 =>
                CalculatePercentage(baseAmount, entity.PercentageValue.Value),
            DiscountCodeType.FixedAmount when entity.FixedAmountValue is > 0 =>
                CalculateFixedAmount(baseAmount, entity.FixedAmountValue.Value),
            DiscountCodeType.FreeMonths when entity.FreeMonths is > 0 =>
                CalculateFreeMonths(baseAmount, entity.FreeMonths.Value),
            _ => null
        };
    }

    private static (decimal, decimal, string) CalculatePercentage(decimal baseAmount, decimal percentage)
    {
        var discountAmount = Math.Round(baseAmount * percentage / 100m, 2, MidpointRounding.AwayFromZero);
        discountAmount = Math.Min(discountAmount, baseAmount);
        var finalAmount = Math.Max(0m, baseAmount - discountAmount);
        var displayText = $"{percentage.ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("de-DE"))} % Rabatt";
        return (discountAmount, finalAmount, displayText);
    }

    private static (decimal, decimal, string) CalculateFixedAmount(decimal baseAmount, decimal fixedAmount)
    {
        var discountAmount = Math.Min(fixedAmount, baseAmount);
        var finalAmount = Math.Max(0m, baseAmount - discountAmount);
        var displayText = $"{discountAmount.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("de-DE"))} EUR Rabatt";
        return (discountAmount, finalAmount, displayText);
    }

    private static (decimal, decimal, string) CalculateFreeMonths(decimal baseAmount, int freeMonths)
    {
        // FreeMonths wird in einem späteren Provisioning-Schritt auf die Lizenzlaufzeit angewendet.
        var discountAmount = baseAmount;
        var finalAmount = 0m;
        var displayText = $"{freeMonths} Monat{(freeMonths == 1 ? "" : "e")} kostenlos";
        return (discountAmount, finalAmount, displayText);
    }

    private static string BuildSuccessMessage(string code, string displayText) =>
        $"Rabattcode {code} angewendet: {displayText}.";

    public async Task<DiscountCodeValidationResult> ValidateForProvisioningAsync(Guid pendingSignupId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var pending = await db.PendingSignups
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == pendingSignupId);

        if (pending is null)
        {
            return DiscountCodeValidationResult.Invalid("Die Registrierung wurde nicht gefunden.");
        }

        if (!pending.DiscountCodeId.HasValue)
        {
            return DiscountCodeValidationResult.NoDiscount();
        }

        if (pending.DiscountRedeemedAt.HasValue)
        {
            return DiscountCodeValidationResult.Invalid(
                "Der Rabattcode wurde für diese Registrierung bereits eingelöst.");
        }

        var entity = await db.DiscountCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == pending.DiscountCodeId.Value);

        if (entity is null)
        {
            return DiscountCodeValidationResult.Invalid(GenericInvalidMessage);
        }

        var entityValidation = ValidateEntityForSignup(entity, pending.PlanId, pending.BillingCycle);
        if (entityValidation is not null)
        {
            return entityValidation;
        }

        if (!MatchesSnapshot(entity, pending))
        {
            return DiscountCodeValidationResult.Invalid(GenericInvalidMessage);
        }

        var baseAmount = pending.OriginalAmount ?? pending.Amount ?? 0m;
        var calculation = CalculateDiscount(entity, baseAmount);
        if (calculation is null)
        {
            return DiscountCodeValidationResult.Invalid(GenericInvalidMessage);
        }

        var (discountAmount, finalAmount, displayText) = calculation.Value;
        if (!AmountsMatch(discountAmount, pending.DiscountAmount)
            || !AmountsMatch(finalAmount, pending.FinalAmount ?? pending.Amount))
        {
            return DiscountCodeValidationResult.Invalid(GenericInvalidMessage);
        }

        return new DiscountCodeValidationResult
        {
            IsValid = true,
            IsApplied = true,
            DiscountCodeId = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            DiscountType = entity.DiscountType,
            PercentageValue = entity.PercentageValue,
            FixedAmountValue = entity.FixedAmountValue,
            FreeMonths = entity.FreeMonths,
            OriginalAmount = baseAmount,
            DiscountAmount = discountAmount,
            FinalAmount = finalAmount,
            Currency = pending.Currency ?? "EUR",
            DisplayText = displayText
        };
    }

    private DiscountCodeValidationResult? ValidateEntityForSignup(
        DiscountCode entity,
        Guid planId,
        string? billingCycle)
    {
        if (!entity.IsActive)
        {
            return DiscountCodeValidationResult.Invalid(GenericInvalidMessage);
        }

        var now = DateTime.UtcNow;
        if (entity.ValidFrom.HasValue && now < entity.ValidFrom.Value)
        {
            return DiscountCodeValidationResult.Invalid(GenericInvalidMessage);
        }

        if (entity.ValidUntil.HasValue && now > entity.ValidUntil.Value)
        {
            return DiscountCodeValidationResult.Invalid(GenericInvalidMessage);
        }

        if (entity.AppliesToPlanId.HasValue && entity.AppliesToPlanId.Value != planId)
        {
            return DiscountCodeValidationResult.Invalid(GenericInvalidMessage);
        }

        if (!string.IsNullOrWhiteSpace(entity.AppliesToBillingCycle))
        {
            if (string.IsNullOrWhiteSpace(billingCycle)
                || !string.Equals(entity.AppliesToBillingCycle, billingCycle, StringComparison.OrdinalIgnoreCase))
            {
                return DiscountCodeValidationResult.Invalid(GenericInvalidMessage);
            }
        }

        if (entity.MaxRedemptions.HasValue && entity.CurrentRedemptions >= entity.MaxRedemptions.Value)
        {
            return DiscountCodeValidationResult.Invalid(GenericInvalidMessage);
        }

        return null;
    }

    private static bool MatchesSnapshot(DiscountCode entity, PendingSignup pending)
    {
        if (!string.Equals(entity.Code, pending.DiscountCodeSnapshot, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.Equals(entity.DiscountType.ToString(), pending.DiscountTypeSnapshot, StringComparison.Ordinal))
        {
            return false;
        }

        return entity.DiscountType switch
        {
            DiscountCodeType.Percentage =>
                entity.PercentageValue == pending.DiscountValueSnapshot,
            DiscountCodeType.FixedAmount =>
                entity.FixedAmountValue == pending.DiscountValueSnapshot,
            DiscountCodeType.FreeMonths =>
                entity.FreeMonths == pending.DiscountFreeMonthsSnapshot,
            _ => false
        };
    }

    private static bool AmountsMatch(decimal calculated, decimal? snapshot) =>
        snapshot.HasValue && Math.Abs(calculated - snapshot.Value) <= 0.01m;
}
