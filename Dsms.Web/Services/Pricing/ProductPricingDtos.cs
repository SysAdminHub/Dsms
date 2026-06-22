namespace Dsms.Web.Services.Pricing;

/// <summary>
/// Schreibgeschützte Sicht auf die globale Preis-/Produktkonfiguration der Datenschutz-Cloud.
/// </summary>
public sealed class ProductPricingSettingsDto
{
    public int Id { get; init; }
    public decimal BaseMonthlyPrice { get; init; }
    public decimal BaseYearlyPrice { get; init; }
    public decimal AdditionalUserMonthlyPrice { get; init; }
    public decimal AdditionalUserYearlyPrice { get; init; }
    public int IncludedStorageGb { get; init; }
    public int AdditionalStoragePackageGb { get; init; }
    public decimal AdditionalStorageMonthlyPrice { get; init; }
    public decimal AdditionalStorageYearlyPrice { get; init; }
    public int? LargeStoragePackageGb { get; init; }
    public decimal? LargeStorageMonthlyPrice { get; init; }
    public decimal? LargeStorageYearlyPrice { get; init; }
    public decimal TrainingModuleMonthlyPrice { get; init; }
    public decimal TrainingModuleYearlyPrice { get; init; }
    public int TrainingTrialDays { get; init; }
    public string FairUseText { get; init; } = string.Empty;
    public bool SpecialOfferActive { get; init; }
    public decimal? SpecialBaseMonthlyPrice { get; init; }
    public decimal? SpecialBaseYearlyPrice { get; init; }
    public string? SpecialOfferBadgeText { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
