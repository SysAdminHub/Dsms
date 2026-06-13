namespace Dsms.Web.Domain.Enums;

/// <summary>Status einer Schulungszuweisung (Teilnehmer ↔ konkrete Schulung).</summary>
public enum TrainingAssignmentStatus
{
    Assigned = 0,
    Invited = 1,
    CodeExpired = 2,
    Locked = 3,
    Started = 4,
    Completed = 5,
    Cancelled = 6
}
