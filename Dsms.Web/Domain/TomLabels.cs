using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain;

/// <summary>Deutsche Anzeigelabels für TOM-Kategorien, Schutzziele und Umsetzungsstatus in der UI.</summary>
public static class TomLabels
{
    public static string GetCategoryLabel(TomCategory category) => category switch
    {
        TomCategory.PhysicalAccessControl => "Zutrittskontrolle",
        TomCategory.AdmissionControl => "Zugangskontrolle",
        TomCategory.AccessControl => "Zugriffskontrolle",
        TomCategory.DisclosureControl => "Weitergabekontrolle",
        TomCategory.InputControl => "Eingabekontrolle",
        TomCategory.OrderControl => "Auftragskontrolle",
        TomCategory.AvailabilityControl => "Verfügbarkeitskontrolle",
        TomCategory.SeparationRequirement => "Trennungsgebot",
        TomCategory.Encryption => "Verschlüsselung",
        TomCategory.BackupAndRecovery => "Backup und Wiederherstellung",
        TomCategory.Logging => "Protokollierung",
        TomCategory.AuthorizationConcept => "Berechtigungskonzept",
        TomCategory.TrainingAndAwareness => "Schulung und Sensibilisierung",
        TomCategory.Other => "Sonstige",
        _ => category.ToString()
    };

    public static string GetProtectionGoalLabel(TomProtectionGoal goal) => goal switch
    {
        TomProtectionGoal.Confidentiality => "Vertraulichkeit",
        TomProtectionGoal.Integrity => "Integrität",
        TomProtectionGoal.Availability => "Verfügbarkeit",
        TomProtectionGoal.Resilience => "Belastbarkeit",
        TomProtectionGoal.Recoverability => "Wiederherstellbarkeit",
        TomProtectionGoal.Transparency => "Transparenz",
        TomProtectionGoal.Unlinkability => "Nichtverkettung",
        TomProtectionGoal.Intervenability => "Intervenierbarkeit",
        _ => goal.ToString()
    };

    public static string GetImplementationStatusLabel(TomImplementationStatus status) => status switch
    {
        TomImplementationStatus.Planned => "Geplant",
        TomImplementationStatus.InProgress => "In Umsetzung",
        TomImplementationStatus.Implemented => "Umgesetzt",
        TomImplementationStatus.InReview => "In Prüfung",
        TomImplementationStatus.NotImplemented => "Nicht umgesetzt",
        TomImplementationStatus.NotApplicable => "Nicht anwendbar",
        _ => status.ToString()
    };

    public static string GetImplementationStatusVariant(TomImplementationStatus status) => status switch
    {
        TomImplementationStatus.Implemented => "success",
        TomImplementationStatus.InProgress => "primary",
        TomImplementationStatus.InReview => "primary",
        TomImplementationStatus.NotImplemented => "danger",
        TomImplementationStatus.NotApplicable => "default",
        _ => "warning"
    };

    /// <summary>Prüft, ob die nächste Überprüfung überfällig ist (Datum liegt in der Vergangenheit).</summary>
    public static bool IsReviewOverdue(DateOnly? nextReviewAt) =>
        nextReviewAt.HasValue && nextReviewAt.Value < DateOnly.FromDateTime(DateTime.Today);
}
