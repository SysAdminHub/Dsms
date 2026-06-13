using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Services.Training;

public sealed record TrainingParticipantListItem(
    TrainingParticipant Participant,
    int AssignmentCount);

public sealed record TrainingAssignmentRow(
    TrainingAssignment Assignment,
    string DisplayName,
    bool IsCodeExpired,
    bool IsLocked,
    bool? QuizPassed,
    int? LastQuizScorePercent);

public sealed record TrainingParticipantStats(
    int TotalAssigned,
    int InvitedCount,
    int CompletedCount);

public sealed record TrainingAssignmentDetail(
    TrainingAssignment Assignment,
    int ViewedSectionCount,
    int TotalSectionCount,
    IReadOnlyList<TrainingQuizAttempt> QuizAttempts);

public sealed record TrainingAssignmentOperationResult(
    bool Success,
    string? Message = null,
    int? AssignmentId = null)
{
    public static TrainingAssignmentOperationResult Ok(int assignmentId, string? message = null) =>
        new(true, message, assignmentId);

    public static TrainingAssignmentOperationResult Fail(string message) =>
        new(false, message);
}

public sealed record TrainingBulkImportLineResult(
    int LineNumber,
    string RawLine,
    bool IsValid,
    string? ErrorMessage,
    string? Email,
    string? Name,
    string? Department,
    bool AlreadyAssigned,
    bool IsExistingParticipant);

public sealed record TrainingBulkImportPreview(
    IReadOnlyList<TrainingBulkImportLineResult> Lines,
    int ValidCount,
    int AlreadyAssignedCount,
    int InvalidCount,
    int ReusedParticipantCount,
    int NewParticipantCount);

public sealed record TrainingInvitationBatchResult(
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<string> Errors);

public enum TrainingAccessCodeValidationResult
{
    Valid,
    InvalidCode,
    Expired,
    Locked,
    Cancelled,
    NotFound
}
