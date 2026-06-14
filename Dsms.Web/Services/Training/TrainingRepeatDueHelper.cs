using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using TrainingEntity = Dsms.Web.Domain.Entities.Training;

namespace Dsms.Web.Services.Training;

public enum TrainingRepeatDueDisplay
{
    None,
    DueSoon,
    RepeatDue,
    Overdue
}

/// <summary>Berechnete Fälligkeitsanzeige für Wiederholungstermine (ohne automatische Statusänderung).</summary>
public static class TrainingRepeatDueHelper
{
    public const int DueSoonDays = 30;

    public static DateTime? CalculateRepeatDueAt(
        DateTime? completedAt,
        DateTime? scheduledAt,
        int? recommendedRepeatAfterMonths)
    {
        if (!recommendedRepeatAfterMonths.HasValue || recommendedRepeatAfterMonths.Value <= 0)
            return null;

        var baseDate = completedAt ?? scheduledAt;
        return baseDate?.AddMonths(recommendedRepeatAfterMonths.Value);
    }

    public static TrainingRepeatDueDisplay GetDisplayStatus(TrainingEntity training, DateTime todayUtc)
    {
        if (training.IsArchived || training.Status == TrainingStatus.Archived)
            return TrainingRepeatDueDisplay.None;

        if (!training.RepeatDueAt.HasValue)
            return TrainingRepeatDueDisplay.None;

        var dueDate = training.RepeatDueAt.Value.Date;
        var today = todayUtc.Date;

        if (dueDate < today)
            return TrainingRepeatDueDisplay.Overdue;

        if (dueDate <= today)
            return TrainingRepeatDueDisplay.RepeatDue;

        if (dueDate <= today.AddDays(DueSoonDays))
            return TrainingRepeatDueDisplay.DueSoon;

        return TrainingRepeatDueDisplay.None;
    }

    public static int? GetOverdueDays(TrainingEntity training, DateTime todayUtc)
    {
        if (!training.RepeatDueAt.HasValue || training.IsArchived)
            return null;

        var dueDate = training.RepeatDueAt.Value.Date;
        var today = todayUtc.Date;
        if (dueDate >= today)
            return null;

        return (today - dueDate).Days;
    }

    public static string GetDisplayLabel(TrainingEntity training, DateTime todayUtc)
    {
        var status = GetDisplayStatus(training, todayUtc);
        return status switch
        {
            TrainingRepeatDueDisplay.Overdue => GetOverdueDays(training, todayUtc) is int days
                ? $"Überfällig seit {days} Tag{(days == 1 ? "" : "en")}"
                : "Überfällig",
            TrainingRepeatDueDisplay.RepeatDue => "Wiederholung fällig",
            TrainingRepeatDueDisplay.DueSoon => "Bald fällig",
            _ => string.Empty
        };
    }

    public static string GetDisplayVariant(TrainingRepeatDueDisplay status) => status switch
    {
        TrainingRepeatDueDisplay.Overdue => "danger",
        TrainingRepeatDueDisplay.RepeatDue => "danger",
        TrainingRepeatDueDisplay.DueSoon => "warning",
        _ => "default"
    };

    public static bool IsRepeatDueOrOverdue(TrainingEntity training, DateTime todayUtc) =>
        GetDisplayStatus(training, todayUtc) is TrainingRepeatDueDisplay.RepeatDue
            or TrainingRepeatDueDisplay.Overdue;

    public static bool IsDueSoon(TrainingEntity training, DateTime todayUtc) =>
        GetDisplayStatus(training, todayUtc) == TrainingRepeatDueDisplay.DueSoon;
}
