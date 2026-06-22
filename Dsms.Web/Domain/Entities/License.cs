using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Kundenlizenz – zentrale kaufmännische und technische Einheit.
/// Limits mit <c>null</c> bedeuten unbegrenzt.
/// </summary>
public class License
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string LicenseNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string PlanName { get; set; } = "Manual";
    public string Status { get; set; } = "Active";
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? InternalNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Lizenzweite Limits
    public int? MaxTenants { get; set; }
    public int? MaxAdmins { get; set; }

    // Mandantenbezogene Limits (gelten je Mandant)
    public int? MaxUsersPerTenant { get; set; }
    public int? MaxAuditorsPerTenant { get; set; }
    public int? MaxCustomAuditTemplatesPerTenant { get; set; }
    public int? MaxActiveAuditsPerTenant { get; set; }
    public int? MaxProcessingActivitiesPerTenant { get; set; }
    public int? MaxDpiaPerTenant { get; set; }
    public int? MaxTomsPerTenant { get; set; }
    public int? MaxProcessorsPerTenant { get; set; }
    public int? MaxActiveMeasuresPerTenant { get; set; }

    // Sonstige Limits
    public int? MaxStorageMb { get; set; }
    public int? MaxEmailRemindersPerMonth { get; set; }

    /// <summary>
    /// Wirksamer Feature-Status: Schulungsmodul für Mandanten dieser Lizenz.
    /// Legacy-Flag aus dem alten Tarifmodell; im neuen Modell ist <see cref="TrainingModuleStatus"/> maßgeblich.
    /// </summary>
    public bool HasTrainingModule { get; set; } = true;

    // --- Neues Preis-/Lizenzmodell der Datenschutz-Cloud ---------------------

    /// <summary>
    /// Bezahlter Zugang aktiv. Bei <c>true</c> entfallen die fachlichen Objekt-Limits
    /// (Verarbeitungstätigkeiten, DSFAs, Maßnahmen, TOMs, Dienstleister, Risiken);
    /// bei <c>false</c> gelten weiterhin die Free-Limits aus der bisherigen Konfiguration.
    /// </summary>
    public bool PaidPlanEnabled { get; set; }

    /// <summary>Anzahl der lizenzierten Benutzer (Abrechnungsgrundlage im bezahlten Zugang).</summary>
    public int LicensedUserCount { get; set; } = 1;

    /// <summary>Im Grundpreis enthaltener Speicherplatz in GB.</summary>
    public int IncludedStorageGb { get; set; }

    /// <summary>Zusätzlich hinzugebuchter Speicherplatz in GB.</summary>
    public int AdditionalStorageGb { get; set; }

    /// <summary>Zustand des optionalen Schulungsmoduls.</summary>
    public TrainingModuleStatus TrainingModuleStatus { get; set; } = TrainingModuleStatus.Disabled;

    /// <summary>Ende des kostenlosen Testzeitraums des Schulungsmoduls (bei <see cref="TrainingModuleStatus.Trial"/>).</summary>
    public DateTime? TrainingModuleTrialEndsAt { get; set; }

    /// <summary>Festes Ablaufdatum des Schulungsmoduls (bei <see cref="TrainingModuleStatus.ActiveUntil"/>).</summary>
    public DateTime? TrainingModuleValidUntil { get; set; }

    /// <summary>Schulungsmodul dauerhaft freigeschaltet (entspricht <see cref="TrainingModuleStatus.Unlimited"/>).</summary>
    public bool TrainingModuleUnlimited { get; set; }

    public ICollection<Tenant> Tenants { get; set; } = [];
}
