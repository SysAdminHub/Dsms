using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Data.Seed;

/// <summary>
/// Legt die globale Preis-/Produktkonfiguration idempotent an, falls noch kein aktiver Datensatz existiert.
/// Werte stammen aus <see cref="ProductPricingDefaults"/> und können später angepasst werden.
/// </summary>
public static class ProductPricingSettingsSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (await db.ProductPricingSettings.AnyAsync())
        {
            return;
        }

        db.ProductPricingSettings.Add(new ProductPricingSettings
        {
            BaseMonthlyPrice = ProductPricingDefaults.BaseMonthlyPrice,
            BaseYearlyPrice = ProductPricingDefaults.BaseYearlyPrice,
            AdditionalUserMonthlyPrice = ProductPricingDefaults.AdditionalUserMonthlyPrice,
            AdditionalUserYearlyPrice = ProductPricingDefaults.AdditionalUserYearlyPrice,
            IncludedStorageGb = ProductPricingDefaults.IncludedStorageGb,
            AdditionalStoragePackageGb = ProductPricingDefaults.AdditionalStoragePackageGb,
            AdditionalStorageMonthlyPrice = ProductPricingDefaults.AdditionalStorageMonthlyPrice,
            AdditionalStorageYearlyPrice = ProductPricingDefaults.AdditionalStorageYearlyPrice,
            LargeStoragePackageGb = ProductPricingDefaults.LargeStoragePackageGb,
            LargeStorageMonthlyPrice = ProductPricingDefaults.LargeStorageMonthlyPrice,
            LargeStorageYearlyPrice = ProductPricingDefaults.LargeStorageYearlyPrice,
            TrainingModuleMonthlyPrice = ProductPricingDefaults.TrainingModuleMonthlyPrice,
            TrainingModuleYearlyPrice = ProductPricingDefaults.TrainingModuleYearlyPrice,
            TrainingTrialDays = ProductPricingDefaults.TrainingTrialDays,
            FairUseText = ProductPricingDefaults.FairUseText,
            SpecialOfferActive = ProductPricingDefaults.SpecialOfferActive,
            SpecialBaseMonthlyPrice = null,
            SpecialBaseYearlyPrice = null,
            SpecialOfferBadgeText = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
