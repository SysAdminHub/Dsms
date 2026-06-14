namespace Dsms.Web.Domain.Enums;

/// <summary>Bearbeitungsstatus einer Betroffenenanfrage.</summary>
public enum DataSubjectRequestStatus
{
    New,
    UnderReview,
    InProgress,
    WaitingForResponse,
    Answered,
    Rejected,
    Completed,
    Archived
}
