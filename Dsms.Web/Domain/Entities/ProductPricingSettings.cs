namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Globale Preis-/Produktkonfiguration der Datenschutz-Cloud (ein aktiver Datensatz, nicht mandantenbezogen).
/// Bildet die kaufmännische Grundlage des bezahlten Zugangs:
/// Grundpreis, Preis je zusätzlichem Benutzer, Speicherpakete und optionales Schulungsmodul.
/// </summary>
public class ProductPricingSettings : EntityBase
{
    /// <summary>Monatlicher Grundpreis des bezahlten Zugangs.</summary>
    public decimal BaseMonthlyPrice { get; set; }

    /// <summary>Jährlicher Grundpreis des bezahlten Zugangs.</summary>
    public decimal BaseYearlyPrice { get; set; }

    /// <summary>Monatlicher Preis je zusätzlichem Benutzer.</summary>
    public decimal AdditionalUserMonthlyPrice { get; set; }

    /// <summary>Jährlicher Preis je zusätzlichem Benutzer.</summary>
    public decimal AdditionalUserYearlyPrice { get; set; }

    /// <summary>Im Grundpreis enthaltener Speicherplatz in GB.</summary>
    public int IncludedStorageGb { get; set; }

    /// <summary>Größe des kleinen Speicher-Zusatzpakets in GB.</summary>
    public int AdditionalStoragePackageGb { get; set; }

    /// <summary>Monatlicher Preis je kleinem Speicher-Zusatzpaket.</summary>
    public decimal AdditionalStorageMonthlyPrice { get; set; }

    /// <summary>Jährlicher Preis je kleinem Speicher-Zusatzpaket.</summary>
    public decimal AdditionalStorageYearlyPrice { get; set; }

    /// <summary>Größe des großen Speicher-Zusatzpakets in GB (optional).</summary>
    public int? LargeStoragePackageGb { get; set; }

    /// <summary>Monatlicher Preis je großem Speicher-Zusatzpaket (optional).</summary>
    public decimal? LargeStorageMonthlyPrice { get; set; }

    /// <summary>Jährlicher Preis je großem Speicher-Zusatzpaket (optional).</summary>
    public decimal? LargeStorageYearlyPrice { get; set; }

    /// <summary>Monatlicher Preis des optionalen Schulungsmoduls.</summary>
    public decimal TrainingModuleMonthlyPrice { get; set; }

    /// <summary>Jährlicher Preis des optionalen Schulungsmoduls.</summary>
    public decimal TrainingModuleYearlyPrice { get; set; }

    /// <summary>Dauer des kostenlosen Test­zeitraums des Schulungsmoduls in Tagen.</summary>
    public int TrainingTrialDays { get; set; }

    /// <summary>Fair-Use-Hinweistext für unbegrenzte fachliche Objekte im bezahlten Zugang.</summary>
    public string FairUseText { get; set; } = string.Empty;

    // --- Sonderpreis (gilt nur für den Grundpreis des bezahlten Zugangs) ------

    /// <summary>Steuert, ob aktuell ein Sonderangebot für den Grundpreis aktiv ist.</summary>
    public bool SpecialOfferActive { get; set; }

    /// <summary>Rabattierter monatlicher Grundpreis (nur wirksam bei aktivem Sonderangebot).</summary>
    public decimal? SpecialBaseMonthlyPrice { get; set; }

    /// <summary>Rabattierter jährlicher Grundpreis (nur wirksam bei aktivem Sonderangebot).</summary>
    public decimal? SpecialBaseYearlyPrice { get; set; }

    /// <summary>Kurzer Badge-Text für die Preis-/Registrierungsseite, z. B. „Einführungspreis“.</summary>
    public string? SpecialOfferBadgeText { get; set; }

    /// <summary>Markiert den aktiven Preis-/Produktdatensatz. Es soll genau ein aktiver Datensatz existieren.</summary>
    public bool IsActive { get; set; } = true;
}
