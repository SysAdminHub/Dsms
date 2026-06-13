namespace Dsms.Web.Domain.Entities;

/// <summary>Ein Quiz-Versuch eines Schulungsteilnehmers.</summary>
public class TrainingQuizAttempt : EntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int TrainingAssignmentId { get; set; }
    public TrainingAssignment TrainingAssignment { get; set; } = null!;

    public DateTime StartedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public int ScorePercent { get; set; }
    public int TotalPoints { get; set; }
    public int AchievedPoints { get; set; }
    public bool Passed { get; set; }
    public int AttemptNumber { get; set; }

    public ICollection<TrainingQuizAnswer> Answers { get; set; } = [];
}
