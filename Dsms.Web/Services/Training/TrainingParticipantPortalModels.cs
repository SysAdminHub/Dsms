using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using TrainingEntity = Dsms.Web.Domain.Entities.Training;

namespace Dsms.Web.Services.Training;

public sealed record TrainingParticipantSessionData(
    int AssignmentId,
    int TenantId,
    DateTime IssuedAtUtc);

public enum TrainingParticipantLoginStatus
{
    Success,
    InvalidCredentials,
    Expired,
    Locked,
    Cancelled,
    TrainingInactive,
    TrainingArchived,
    AlreadyCompleted
}

public sealed record TrainingParticipantLoginResult(
    TrainingParticipantLoginStatus Status,
    string? Message = null)
{
    public bool Success => Status is TrainingParticipantLoginStatus.Success
        or TrainingParticipantLoginStatus.AlreadyCompleted;
}

public sealed record TrainingParticipantContext(
    TrainingAssignment Assignment,
    TrainingEntity Training,
    TrainingTemplate? Template,
    IReadOnlyList<TrainingTemplateSection> Sections,
    IReadOnlyList<TrainingQuestion> Questions,
    int ViewedSectionCount,
    TrainingQuizAttempt? LatestQuizAttempt,
    bool IsCompleted);

public sealed record TrainingParticipantProgressInfo(
    int TotalSections,
    int ViewedSections);

public sealed record TrainingParticipantQuizSubmitResult(
    bool Success,
    string? Message,
    int ScorePercent,
    int AchievedPoints,
    int TotalPoints,
    bool Passed,
    int AttemptNumber);

public sealed record TrainingParticipantCompletionResult(
    bool Success,
    string? Message,
    TrainingParticipantCertificateInfo? Certificate = null);

public enum TrainingParticipantPortalStep
{
    Cards,
    Quiz,
    Confirm,
    Completed
}
