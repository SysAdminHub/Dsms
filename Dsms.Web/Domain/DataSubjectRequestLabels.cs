using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain;

/// <summary>Deutsche Anzeigelabels für Betroffenenanfragen.</summary>
public static class DataSubjectRequestLabels
{
    public const string AnonymizedPlaceholder = "[anonymisiert]";

    public static string GetTypeLabel(DataSubjectRequestType type) => type switch
    {
        DataSubjectRequestType.Access => "Auskunft",
        DataSubjectRequestType.Rectification => "Berichtigung",
        DataSubjectRequestType.Erasure => "Löschung",
        DataSubjectRequestType.Restriction => "Einschränkung der Verarbeitung",
        DataSubjectRequestType.DataPortability => "Datenübertragbarkeit",
        DataSubjectRequestType.Objection => "Widerspruch",
        DataSubjectRequestType.ConsentWithdrawal => "Widerruf einer Einwilligung",
        DataSubjectRequestType.Other => "Sonstige Anfrage",
        _ => type.ToString()
    };

    public static string GetStatusLabel(DataSubjectRequestStatus status) => status switch
    {
        DataSubjectRequestStatus.New => "Neu",
        DataSubjectRequestStatus.UnderReview => "In Prüfung",
        DataSubjectRequestStatus.InProgress => "In Bearbeitung",
        DataSubjectRequestStatus.WaitingForResponse => "Wartet auf Rückmeldung",
        DataSubjectRequestStatus.Answered => "Beantwortet",
        DataSubjectRequestStatus.Rejected => "Abgelehnt",
        DataSubjectRequestStatus.Completed => "Abgeschlossen",
        DataSubjectRequestStatus.Archived => "Archiviert",
        _ => status.ToString()
    };

    public static string GetStatusVariant(DataSubjectRequestStatus status) => status switch
    {
        DataSubjectRequestStatus.New => "primary",
        DataSubjectRequestStatus.UnderReview => "warning",
        DataSubjectRequestStatus.InProgress => "warning",
        DataSubjectRequestStatus.WaitingForResponse => "warning",
        DataSubjectRequestStatus.Answered => "success",
        DataSubjectRequestStatus.Rejected => "muted",
        DataSubjectRequestStatus.Completed => "success",
        DataSubjectRequestStatus.Archived => "muted",
        _ => "neutral"
    };

    public static bool IsOpenStatus(DataSubjectRequestStatus status) =>
        status is DataSubjectRequestStatus.New
            or DataSubjectRequestStatus.UnderReview
            or DataSubjectRequestStatus.InProgress
            or DataSubjectRequestStatus.WaitingForResponse;

    public static bool AllowsAnonymization(DataSubjectRequestStatus status) =>
        status is DataSubjectRequestStatus.Answered
            or DataSubjectRequestStatus.Rejected
            or DataSubjectRequestStatus.Completed
            or DataSubjectRequestStatus.Archived;

    /// <summary>Neutraler Anzeigename für Auditlog und Dokumentenbezüge – ohne personenbezogene Daten.</summary>
    public static string GetAuditDisplayName(DataSubjectRequest request) =>
        $"Betroffenenanfrage #{request.Id} ({GetTypeLabel(request.RequestType)})";

    public static string GetDocumentLinkDisplayName(DataSubjectRequest request) =>
        GetAuditDisplayName(request);

    public static string GetSubjectDisplay(DataSubjectRequest request)
    {
        if (request.PersonalDataAnonymized)
        {
            return AnonymizedPlaceholder;
        }

        if (!string.IsNullOrWhiteSpace(request.DataSubjectName))
        {
            return request.DataSubjectName;
        }

        if (!string.IsNullOrWhiteSpace(request.DataSubjectReference))
        {
            return request.DataSubjectReference;
        }

        return "—";
    }

    public static string GetDeadlineCssClass(int? remainingDays, bool isOverdue)
    {
        if (isOverdue)
        {
            return "text-danger fw-semibold";
        }

        if (remainingDays is <= 7 and >= 0)
        {
            return "text-warning fw-semibold";
        }

        return string.Empty;
    }
}
