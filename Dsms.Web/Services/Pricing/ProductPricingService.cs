using Dsms.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Pricing;

public sealed class ProductPricingService(
    IDbContextFactory<ApplicationDbContext> dbFactory) : IProductPricingService
{
    public async Task<ProductPricingSettingsDto?> GetActivePricingAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        return await db.ProductPricingSettings
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Select(p => new ProductPricingSettingsDto
            {
                Id = p.Id,
                BaseMonthlyPrice = p.BaseMonthlyPrice,
                BaseYearlyPrice = p.BaseYearlyPrice,
                AdditionalUserMonthlyPrice = p.AdditionalUserMonthlyPrice,
                AdditionalUserYearlyPrice = p.AdditionalUserYearlyPrice,
                IncludedTenantCount = p.IncludedTenantCount,
                AdditionalTenantMonthlyPrice = p.AdditionalTenantMonthlyPrice,
                AdditionalTenantYearlyPrice = p.AdditionalTenantYearlyPrice,
                IncludedStorageGb = p.IncludedStorageGb,
                AdditionalStoragePackageGb = p.AdditionalStoragePackageGb,
                AdditionalStorageMonthlyPrice = p.AdditionalStorageMonthlyPrice,
                AdditionalStorageYearlyPrice = p.AdditionalStorageYearlyPrice,
                LargeStoragePackageGb = p.LargeStoragePackageGb,
                LargeStorageMonthlyPrice = p.LargeStorageMonthlyPrice,
                LargeStorageYearlyPrice = p.LargeStorageYearlyPrice,
                TrainingModuleMonthlyPrice = p.TrainingModuleMonthlyPrice,
                TrainingModuleYearlyPrice = p.TrainingModuleYearlyPrice,
                TrainingTrialDays = p.TrainingTrialDays,
                FairUseText = p.FairUseText,
                SpecialOfferActive = p.SpecialOfferActive,
                SpecialBaseMonthlyPrice = p.SpecialBaseMonthlyPrice,
                SpecialBaseYearlyPrice = p.SpecialBaseYearlyPrice,
                SpecialOfferBadgeText = p.SpecialOfferBadgeText,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<string> GetFairUseTextAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var text = await db.ProductPricingSettings
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Select(p => p.FairUseText)
            .FirstOrDefaultAsync(ct);

        return text ?? string.Empty;
    }
}
