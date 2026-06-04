using Dsms.Web.Domain.Enums;
using ServiceProviderEntity = Dsms.Web.Domain.Entities.ServiceProvider;

namespace Dsms.Web.Domain;



/// <summary>Deutsche UI-Labels und einfache Plausibilitätshinweise für Dienstleister.</summary>

public static class ServiceProviderLabels

{

    public static string GetProviderTypeLabel(ServiceProviderType type) => type switch

    {

        ServiceProviderType.Hosting => "Hosting",

        ServiceProviderType.CloudService => "Cloud-Dienst",

        ServiceProviderType.ItSupport => "IT-Support",

        ServiceProviderType.SoftwareVendor => "Softwareanbieter",

        ServiceProviderType.Payroll => "Lohnabrechnung",

        ServiceProviderType.Accounting => "Buchhaltung",

        ServiceProviderType.Newsletter => "Newsletter",

        ServiceProviderType.Crm => "CRM",

        ServiceProviderType.DocumentDestruction => "Aktenvernichtung",

        ServiceProviderType.Maintenance => "Wartung",

        ServiceProviderType.Consulting => "Beratung",

        ServiceProviderType.Other => "Sonstige",

        _ => type.ToString()

    };



    public static string GetStatusLabel(ServiceProviderStatus status) => status switch

    {

        ServiceProviderStatus.InReview => "In Prüfung",

        ServiceProviderStatus.Active => "Aktiv",

        ServiceProviderStatus.Approved => "Freigegeben",

        ServiceProviderStatus.Blocked => "Gesperrt",

        ServiceProviderStatus.Terminated => "Beendet",

        ServiceProviderStatus.Archived => "Archiviert",

        _ => status.ToString()

    };



    public static string GetStatusVariant(ServiceProviderStatus status) => status switch

    {

        ServiceProviderStatus.Approved => "success",

        ServiceProviderStatus.Active => "primary",

        ServiceProviderStatus.InReview => "warning",

        ServiceProviderStatus.Blocked => "danger",

        ServiceProviderStatus.Terminated => "muted",

        ServiceProviderStatus.Archived => "default",

        _ => "default"

    };



    public static string GetRiskLabel(ServiceProviderRiskAssessment risk) => risk switch

    {

        ServiceProviderRiskAssessment.NotAssessed => "Noch nicht bewertet",

        ServiceProviderRiskAssessment.Low => "Niedrig",

        ServiceProviderRiskAssessment.Medium => "Mittel",

        ServiceProviderRiskAssessment.High => "Hoch",

        ServiceProviderRiskAssessment.Critical => "Kritisch",

        _ => risk.ToString()

    };



    public static string GetRiskVariant(ServiceProviderRiskAssessment risk) => risk switch

    {

        ServiceProviderRiskAssessment.Critical => "danger",

        ServiceProviderRiskAssessment.High => "danger",

        ServiceProviderRiskAssessment.Medium => "warning",

        ServiceProviderRiskAssessment.Low => "success",

        _ => "default"

    };



    public static string GetThirdCountryLegalBasisLabel(ThirdCountryTransferLegalBasis basis) => basis switch

    {

        ThirdCountryTransferLegalBasis.NoThirdCountryTransfer => "Kein Drittlandtransfer",

        ThirdCountryTransferLegalBasis.AdequacyDecision => "Angemessenheitsbeschluss",

        ThirdCountryTransferLegalBasis.EuStandardContractualClauses => "EU-Standardvertragsklauseln",

        ThirdCountryTransferLegalBasis.ExceptionRule => "Ausnahmeregelung",

        ThirdCountryTransferLegalBasis.ToBeReviewed => "Noch zu prüfen",

        ThirdCountryTransferLegalBasis.Other => "Sonstige",

        _ => basis.ToString()

    };



    public static string GetProcessingRoleLabel(ProcessingRole role) => role switch

    {

        ProcessingRole.DataProcessor => "Auftragsverarbeiter",

        ProcessingRole.SubProcessor => "Unterauftragsverarbeiter",

        ProcessingRole.Recipient => "Empfänger",

        ProcessingRole.MaintenanceProvider => "Wartungsdienstleister",

        ProcessingRole.SoftwareVendor => "Softwareanbieter",

        ProcessingRole.HostingProvider => "Hosting-Anbieter",

        ProcessingRole.Other => "Sonstige",

        _ => role.ToString()

    };



    /// <summary>AVV-Prüfung gilt als überfällig, wenn ein Prüfdatum gesetzt ist und in der Vergangenheit liegt.</summary>

    public static bool IsAvvReviewOverdue(DateOnly? reviewedAt) =>

        reviewedAt.HasValue && reviewedAt.Value < DateOnly.FromDateTime(DateTime.Today);



    /// <summary>

    /// Hinweis: Aktiver Auftragsverarbeiter ohne dokumentierten AVV (MVP-Warnung, keine Blockade).

    /// </summary>

    public static bool ShouldWarnMissingAvv(ServiceProviderEntity provider) =>

        provider.IsDataProcessor

        && !provider.DataProcessingAgreementExists

        && provider.Status is ServiceProviderStatus.Active or ServiceProviderStatus.Approved;

}

