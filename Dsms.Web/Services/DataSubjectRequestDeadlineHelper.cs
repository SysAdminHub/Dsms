using Dsms.Web.Domain.Entities;

namespace Dsms.Web.Services;

/// <summary>Fristberechnung und -auswertung für Betroffenenanfragen.</summary>
public static class DataSubjectRequestDeadlineHelper
{
    public const int DueSoonDaysThreshold = 7;

    public static DateTime CalculateDefaultDueAt(DateTime receivedAtUtc) =>
        receivedAtUtc.Date.AddMonths(1);

    public static DateTime GetEffectiveDueAt(DataSubjectRequest request) =>
        request.DeadlineExtended && request.ExtendedDueAt.HasValue
            ? request.ExtendedDueAt.Value
            : request.DueAt;

    public static int GetRemainingDays(DataSubjectRequest request, DateTime? referenceUtc = null)
    {
        var reference = (referenceUtc ?? DateTime.UtcNow).Date;
        var due = GetEffectiveDueAt(request).Date;
        return (due - reference).Days;
    }

    public static bool IsOverdue(DataSubjectRequest request, DateTime? referenceUtc = null) =>
        GetRemainingDays(request, referenceUtc) < 0
        && !IsClosedStatus(request);

    public static bool IsDueSoon(DataSubjectRequest request, DateTime? referenceUtc = null)
    {
        var remaining = GetRemainingDays(request, referenceUtc);
        return remaining is >= 0 and <= DueSoonDaysThreshold && !IsClosedStatus(request);
    }

    public static bool IsClosedStatus(DataSubjectRequest request) =>
        request.Status is Domain.Enums.DataSubjectRequestStatus.Answered
            or Domain.Enums.DataSubjectRequestStatus.Rejected
            or Domain.Enums.DataSubjectRequestStatus.Completed
            or Domain.Enums.DataSubjectRequestStatus.Archived;
}
