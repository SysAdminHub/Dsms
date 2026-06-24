namespace Dsms.Web.Domain;

/// <summary>
/// Standardwerte für die globale Preis-/Produktkonfiguration der Datenschutz-Cloud.
/// Wird sowohl beim Seeding als auch als Referenz für die Migration verwendet.
/// </summary>
public static class ProductPricingDefaults
{
    public const decimal BaseMonthlyPrice = 29m;
    public const decimal BaseYearlyPrice = 299m;
    public const decimal AdditionalUserMonthlyPrice = 5m;
    public const decimal AdditionalUserYearlyPrice = 49m;
    public const int IncludedTenantCount = 1;
    public const decimal AdditionalTenantMonthlyPrice = 9m;
    public const decimal AdditionalTenantYearlyPrice = 99m;
    public const int IncludedStorageGb = 10;
    public const int AdditionalStoragePackageGb = 10;
    public const decimal AdditionalStorageMonthlyPrice = 5m;
    public const decimal AdditionalStorageYearlyPrice = 49m;
    public const int LargeStoragePackageGb = 50;
    public const decimal LargeStorageMonthlyPrice = 19m;
    public const decimal LargeStorageYearlyPrice = 199m;
    public const decimal TrainingModuleMonthlyPrice = 19m;
    public const decimal TrainingModuleYearlyPrice = 199m;
    public const int TrainingTrialDays = 30;

    /// <summary>Standardmäßig ist kein Sonderangebot aktiv; Sonderpreise und Badge-Text bleiben leer (null).</summary>
    public const bool SpecialOfferActive = false;

    public const string FairUseText =
        "Im bezahlten Zugang sind Verarbeitungstätigkeiten, DSFAs, Maßnahmen, TOMs und Dienstleister "
        + "grundsätzlich unbegrenzt enthalten. Die Nutzung erfolgt im Rahmen einer fairen und üblichen "
        + "geschäftlichen Nutzung für Datenschutzmanagement. Bei ungewöhnlich hoher, missbräuchlicher oder "
        + "zweckfremder Nutzung behalten wir uns vor, den Kunden zu kontaktieren und gemeinsam eine "
        + "angemessene Lösung zu finden.";
}
