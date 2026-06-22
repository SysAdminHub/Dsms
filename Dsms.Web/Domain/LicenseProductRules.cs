using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain;

/// <summary>
/// Zentrale Auswertungslogik für das Preis-/Lizenzmodell der Datenschutz-Cloud.
/// Reine Hilfsmethoden ohne Datenbankzugriff; arbeiten direkt auf der <see cref="License"/>.
/// </summary>
public static class LicenseProductRules
{
    /// <summary>Prüft, ob der bezahlte Zugang aktiv ist.</summary>
    public static bool IsPaidPlanActive(License license) => license.PaidPlanEnabled;

    /// <summary>
    /// Prüft, ob das Schulungsmodul aktuell aktiv ist.
    /// Aktiv bei <see cref="TrainingModuleStatus.Unlimited"/> (bzw. <c>TrainingModuleUnlimited</c>),
    /// bei <see cref="TrainingModuleStatus.ActiveUntil"/> solange <c>TrainingModuleValidUntil</c> nicht abgelaufen ist,
    /// und bei <see cref="TrainingModuleStatus.Trial"/> solange <c>TrainingModuleTrialEndsAt</c> nicht abgelaufen ist.
    /// Nach Ablauf gilt das Modul als nicht aktiv – Schulungsdaten bleiben jedoch erhalten.
    /// </summary>
    public static bool IsTrainingModuleActive(License license, DateTime? referenceDateUtc = null)
    {
        var today = (referenceDateUtc ?? DateTime.UtcNow).Date;

        if (license.TrainingModuleUnlimited
            || license.TrainingModuleStatus == TrainingModuleStatus.Unlimited)
        {
            return true;
        }

        return license.TrainingModuleStatus switch
        {
            TrainingModuleStatus.ActiveUntil =>
                license.TrainingModuleValidUntil.HasValue
                && license.TrainingModuleValidUntil.Value.Date >= today,
            TrainingModuleStatus.Trial =>
                license.TrainingModuleTrialEndsAt.HasValue
                && license.TrainingModuleTrialEndsAt.Value.Date >= today,
            _ => false
        };
    }

    /// <summary>Gesamter verfügbarer Speicherplatz in GB: enthaltener + zusätzlicher Speicher.</summary>
    public static int GetTotalStorageGb(License license) =>
        license.IncludedStorageGb + license.AdditionalStorageGb;

    /// <summary>Gesamter verfügbarer Speicherplatz in MB (für Limit-Vergleiche).</summary>
    public static int GetTotalStorageMb(License license) =>
        GetTotalStorageGb(license) * 1024;

    /// <summary>
    /// Im bezahlten Zugang entfallen die fachlichen Objekt-Limits
    /// (Verarbeitungstätigkeiten, DSFAs, Maßnahmen, TOMs, Dienstleister, Risiken).
    /// </summary>
    public static bool AreBusinessObjectLimitsWaived(License license) => IsPaidPlanActive(license);
}
