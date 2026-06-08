using Dsms.Web.Domain.Entities;

namespace Dsms.Web.Domain;

/// <summary>
/// Liefert Anzeigewerte für Audit-Antworten – bevorzugt Snapshot-Daten des Durchlaufs,
/// Fallback auf die Live-Vorlagenfrage für ältere Datensätze.
/// </summary>
public static class AuditAnswerDisplay
{
    public static string GetQuestionText(AuditAnswer answer) =>
        !string.IsNullOrEmpty(answer.QuestionText)
            ? answer.QuestionText
            : answer.AuditQuestion?.Text ?? "";

    public static int GetSortOrder(AuditAnswer answer) =>
        !string.IsNullOrEmpty(answer.QuestionText)
            ? answer.QuestionSortOrder
            : answer.AuditQuestion?.SortOrder ?? 0;

    public static string? GetCategory(AuditAnswer answer) =>
        !string.IsNullOrEmpty(answer.QuestionText)
            ? answer.QuestionCategory
            : answer.AuditQuestion?.Category;
}
