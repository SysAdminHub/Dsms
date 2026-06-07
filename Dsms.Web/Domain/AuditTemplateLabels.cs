using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain;

/// <summary>Deutsche UI-Bezeichnungen für Audit-Vorlagen.</summary>
public static class AuditTemplateLabels
{
    public static string GetTypeBadge(AuditTemplateType type) => type switch
    {
        AuditTemplateType.Official => "Offiziell",
        AuditTemplateType.Tenant => "Eigene Vorlage",
        AuditTemplateType.Community => "Community",
        _ => type.ToString()
    };

    public static string? GetCommunityStatusBadge(CommunityStatus status) => status switch
    {
        CommunityStatus.Submitted => "Zur Prüfung eingereicht",
        CommunityStatus.Rejected => "Community abgelehnt",
        _ => null
    };

    public static string GetTypeBadgeVariant(AuditTemplateType type) => type switch
    {
        AuditTemplateType.Official => "primary",
        AuditTemplateType.Community => "success",
        _ => "default"
    };

    public static string GetTypeHint(AuditTemplateType type) => type switch
    {
        AuditTemplateType.Official =>
            "Diese Vorlage wird vom Plattformbetreiber bereitgestellt und kann von Mandanten nicht bearbeitet werden.",
        AuditTemplateType.Community =>
            "Diese Vorlage wurde als Community-Vorlage freigegeben und kann von Mandanten nicht bearbeitet werden.",
        AuditTemplateType.Tenant =>
            "Diese Vorlage wurde in diesem Mandanten erstellt.",
        _ => ""
    };

    public const string OfficialCreateHint =
        "Diese Vorlage wird als offizielle Vorlage für alle Mandanten bereitgestellt.";

    public const string OfficialEditHint =
        "Diese Vorlage ist für alle Mandanten sichtbar und kann nur durch Superuser bearbeitet werden.";

    public const string CommunitySubmitHint =
        "Diese Vorlage wird zur Prüfung an den Plattformbetreiber übermittelt. Nach Freigabe kann sie für alle Mandanten sichtbar werden. Bitte stellen Sie sicher, dass keine vertraulichen Informationen, personenbezogenen Daten, Kundennamen oder internen Unternehmensdetails enthalten sind.";

    public const string CommunitySubmittedHint =
        "Diese Vorlage wurde zur Community-Prüfung eingereicht und kann während der Prüfung nicht bearbeitet werden.";

    public const string CommunityReviewPrivacyHint =
        "Bitte prüfen Sie, ob die Vorlage vertrauliche Informationen, personenbezogene Daten, Kundennamen oder interne Unternehmensdetails enthält.";

    public const string SubmitSuccess = "Die Vorlage wurde zur Community-Prüfung eingereicht.";
    public const string ApproveSuccess = "Die Vorlage wurde als Community-Vorlage freigegeben.";
    public const string RejectSuccess = "Die Community-Einreichung wurde abgelehnt.";
    public const string AccessDenied = "Sie haben keine Berechtigung für diese Aktion.";

    public const string QuestionUpdated = "Frage wurde aktualisiert.";
    public const string QuestionDeleted = "Frage wurde gelöscht.";
    public const string QuestionTextRequired = "Fragentext darf nicht leer sein.";
    public const string QuestionEditDenied = "Sie haben keine Berechtigung, diese Frage zu bearbeiten.";
    public const string QuestionDeleteDenied = "Sie haben keine Berechtigung, diese Frage zu löschen.";
    public const string QuestionDeleteInUse =
        "Diese Frage kann nicht gelöscht werden, da sie in Audit-Durchläufen verwendet wird. Bereits gestartete Audits bleiben davon unberührt.";
}
