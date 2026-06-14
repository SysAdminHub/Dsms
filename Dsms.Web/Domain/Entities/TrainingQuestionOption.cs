namespace Dsms.Web.Domain.Entities;

/// <summary>Antwortoption zu einer <see cref="TrainingQuestion"/>.</summary>
public class TrainingQuestionOption : EntityBase
{
    /// <summary>Redundant für Mandantensicherheit; null bei globalen Vorlagen.</summary>
    public int? TenantId { get; set; }

    public int TrainingQuestionId { get; set; }
    public TrainingQuestion TrainingQuestion { get; set; } = null!;

    public int SortOrder { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string? Explanation { get; set; }
    public bool IsActive { get; set; } = true;

    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }
}
