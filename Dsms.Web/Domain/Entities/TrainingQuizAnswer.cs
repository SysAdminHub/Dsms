namespace Dsms.Web.Domain.Entities;

/// <summary>Gespeicherte Antwort innerhalb eines Quiz-Versuchs.</summary>
public class TrainingQuizAnswer : EntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int TrainingQuizAttemptId { get; set; }
    public TrainingQuizAttempt TrainingQuizAttempt { get; set; } = null!;

    public int TrainingQuestionId { get; set; }
    public TrainingQuestion TrainingQuestion { get; set; } = null!;

    public int? TrainingQuestionOptionId { get; set; }
    public TrainingQuestionOption? TrainingQuestionOption { get; set; }

    public string? AnswerTextSnapshot { get; set; }
    public bool IsSelected { get; set; }
    public bool IsCorrect { get; set; }
    public int PointsAwarded { get; set; }
}
