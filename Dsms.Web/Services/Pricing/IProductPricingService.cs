namespace Dsms.Web.Services.Pricing;

/// <summary>
/// Stellt die globale Preis-/Produktkonfiguration der Datenschutz-Cloud bereit.
/// </summary>
public interface IProductPricingService
{
    /// <summary>
    /// Liefert die aktive Preis-/Produktkonfiguration oder <c>null</c>, wenn keine vorhanden ist.
    /// </summary>
    Task<ProductPricingSettingsDto?> GetActivePricingAsync(CancellationToken ct = default);

    /// <summary>
    /// Liefert den Fair-Use-Hinweistext aus der aktiven Preis-/Produktkonfiguration
    /// (für die Preis-/Tarifseite und den Tarif-Finder). Leerer String, wenn keine Konfiguration vorhanden ist.
    /// </summary>
    Task<string> GetFairUseTextAsync(CancellationToken ct = default);
}
