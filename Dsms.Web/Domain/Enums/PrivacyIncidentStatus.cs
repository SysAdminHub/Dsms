namespace Dsms.Web.Domain.Enums;

/// <summary>Status eines Datenschutzvorfalls.</summary>
public enum PrivacyIncidentStatus
{
    Draft,
    InReview,
    ActionsRunning,
    Reported,
    Closed,
    Archived
}
