using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>Multiple-Choice-Frage im Quiz einer Schulungsvorlage.</summary>
public class TrainingQuestion : EntityBase
{
    /// <summary>Redundant für Mandantensicherheit; null bei globalen Vorlagen.</summary>
    public int? TenantId { get; set; }

    public int TrainingTemplateId { get; set; }
    public TrainingTemplate TrainingTemplate { get; set; } = null!;

    public int SortOrder { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public TrainingQuestionType QuestionType { get; set; } = TrainingQuestionType.SingleChoice;
    public string? Explanation { get; set; }
    public int Points { get; set; } = 1;
    public bool IsRequired { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }

    public ICollection<TrainingQuestionOption> Options { get; set; } = [];
}
