using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain;
/// <summary>Deutsche UI-Bezeichnungen für Schulungsvorlagen.</summary>
public static class TrainingLabels
{
    public static string GetTrainingTypeLabel(TrainingType type) => type switch
    {
        TrainingType.PrivacyBasics => "Grundlagenschulung Datenschutz",
        TrainingType.Awareness => "Awareness-Maßnahme",
        TrainingType.SpecialTraining => "Spezialschulung",
        TrainingType.PhishingSecurityAwareness => "Phishing-/Security-Awareness",
        TrainingType.Onboarding => "Onboarding-Schulung",
        TrainingType.Refresher => "Wiederholungsschulung",
        TrainingType.Other => "Sonstige Schulung",
        _ => type.ToString()
    };

    public static string GetCommunityStatusLabel(CommunityTemplateStatus status) => status switch
    {
        CommunityTemplateStatus.None => "Kein Community-Status",
        CommunityTemplateStatus.Draft => "Entwurf",
        CommunityTemplateStatus.Submitted => "Eingereicht",
        CommunityTemplateStatus.Approved => "Freigegeben",
        CommunityTemplateStatus.Rejected => "Abgelehnt",
        _ => status.ToString()
    };

    public static string GetQuestionTypeLabel(TrainingQuestionType type) => type switch
    {
        TrainingQuestionType.SingleChoice => "Eine richtige Antwort",
        TrainingQuestionType.MultipleChoice => "Mehrere richtige Antworten",
        _ => type.ToString()
    };

    public static string GetOriginLabel(TrainingTemplate template)
    {
        if (template.IsGlobal && template.TenantId is null)
            return template.IsCommunityTemplate ? "Community-Vorlage" : "Globale Vorlage";

        return template.IsCommunityTemplate ? "Community (Mandant)" : "Eigene Vorlage";
    }

    public static string GetOriginBadgeVariant(TrainingTemplate template)
    {
        if (template.IsGlobal && template.TenantId is null)
            return template.IsCommunityTemplate ? "success" : "primary";

        return "default";
    }

    public static string? GetCommunityStatusBadge(CommunityTemplateStatus status) => status switch
    {
        CommunityTemplateStatus.Submitted => "Eingereicht",
        CommunityTemplateStatus.Approved => "Freigegeben",
        CommunityTemplateStatus.Rejected => "Abgelehnt",
        CommunityTemplateStatus.Draft => "Entwurf",
        _ => null
    };

    public const string CommunitySubmitHint =
        "Diese Vorlage wird zur Prüfung an den Plattformbetreiber gesendet. Nach Freigabe kann sie allen Mandanten als Community-Vorlage zur Verfügung stehen.";

    public const string CommunitySubmittedHint =
        "Diese Vorlage wurde zur Community-Prüfung eingereicht und kann während der Prüfung nicht bearbeitet werden.";

    public const string CommunityReviewPrivacyHint =
        "Community-Schulungsvorlagen wurden von Mandanten zur Prüfung eingereicht. Nach Freigabe stehen sie allen Mandanten als Vorlage zur Verfügung.";

    public const string CommunitySubmitSuccess = "Die Vorlage wurde als Community-Vorlage eingereicht.";
    public const string CommunityApproveSuccess = "Die Vorlage wurde als Community-Vorlage freigegeben.";
    public const string CommunityRejectSuccess = "Die Community-Einreichung wurde abgelehnt.";
    public const string CommunityRejectHint = "Bitte geben Sie optional einen Grund für die Ablehnung an.";
    public const string CommunityPrivacyConfirmation =
        "Ich bestätige, dass die Vorlage keine vertraulichen, personenbezogenen oder mandantenspezifischen Informationen enthält.";

    public static readonly TrainingType[] AllTrainingTypes =
    [
        TrainingType.PrivacyBasics,
        TrainingType.Awareness,
        TrainingType.SpecialTraining,
        TrainingType.PhishingSecurityAwareness,
        TrainingType.Onboarding,
        TrainingType.Refresher,
        TrainingType.Other
    ];

    public const string AccessDenied = "Keine Berechtigung für diese Schulungsvorlage.";
    public const string TemplateNotFound = "Schulungsvorlage wurde nicht gefunden.";
    public const string AssetNotFound = "Schulungsasset wurde nicht gefunden.";
    public const string InvalidAssetKey = "Ungültiger Asset-Schlüssel.";

    public const string TrainingNotFound = "Schulung wurde nicht gefunden.";
    public const string TrainingAccessDenied = "Keine Berechtigung für diese Schulung.";

    public static string GetStatusLabel(TrainingStatus status) =>
        TrainingStatusMapper.Normalize(status) switch
        {
            TrainingStatus.Active => "Aktiv",
            TrainingStatus.Inactive => "Inaktiv",
            TrainingStatus.Archived => "Archiviert",
            _ => status.ToString()
        };

    public static string GetStatusVariant(TrainingStatus status) =>
        TrainingStatusMapper.Normalize(status) switch
        {
            TrainingStatus.Active => "success",
            TrainingStatus.Inactive => "default",
            TrainingStatus.Archived => "default",
            _ => "default"
        };

    public static readonly TrainingStatus[] AllTrainingStatuses =
    [
        TrainingStatus.Active,
        TrainingStatus.Inactive,
        TrainingStatus.Archived
    ];

    public static readonly TrainingStatus[] EditableTrainingStatuses =
    [
        TrainingStatus.Inactive,
        TrainingStatus.Active,
        TrainingStatus.Archived
    ];

    public static string GetAssignmentStatusLabel(TrainingAssignmentStatus status) => status switch
    {
        TrainingAssignmentStatus.Assigned => "Zugewiesen",
        TrainingAssignmentStatus.Invited => "Eingeladen",
        TrainingAssignmentStatus.CodeExpired => "Code abgelaufen",
        TrainingAssignmentStatus.Locked => "Gesperrt",
        TrainingAssignmentStatus.Started => "Gestartet",
        TrainingAssignmentStatus.Completed => "Abgeschlossen",
        TrainingAssignmentStatus.Cancelled => "Abgebrochen",
        _ => status.ToString()
    };

    public static string GetAssignmentStatusVariant(TrainingAssignmentStatus status) => status switch
    {
        TrainingAssignmentStatus.Assigned => "default",
        TrainingAssignmentStatus.Invited => "primary",
        TrainingAssignmentStatus.CodeExpired => "warning",
        TrainingAssignmentStatus.Locked => "danger",
        TrainingAssignmentStatus.Started => "info",
        TrainingAssignmentStatus.Completed => "success",
        TrainingAssignmentStatus.Cancelled => "default",
        _ => "default"
    };

    public const string ParticipantAlreadyAssigned = "Dieser Teilnehmer ist dieser Schulung bereits zugewiesen.";
    public const string ParticipantNotFound = "Schulungsteilnehmer wurde nicht gefunden.";
    public const string AssignmentNotFound = "Schulungszuweisung wurde nicht gefunden.";
}
