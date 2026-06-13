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

    public static string GetOriginBadgeVariant(TrainingTemplate template) =>
        template.IsGlobal && template.TenantId is null ? "primary" : "default";

    public static string? GetCommunityStatusBadge(CommunityTemplateStatus status) => status switch
    {
        CommunityTemplateStatus.Submitted => "Eingereicht",
        CommunityTemplateStatus.Approved => "Freigegeben",
        CommunityTemplateStatus.Rejected => "Abgelehnt",
        CommunityTemplateStatus.Draft => "Entwurf",
        _ => null
    };

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
}
