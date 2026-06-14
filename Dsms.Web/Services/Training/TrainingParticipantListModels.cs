using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using TrainingEntity = Dsms.Web.Domain.Entities.Training;

namespace Dsms.Web.Services.Training;

public enum TrainingParticipantListFilter
{
    All,
    Active,
    Inactive,
    HasOpen,
    HasCompleted,
    NeverCompleted
}

public sealed record TrainingParticipantListRow(
    TrainingParticipant Participant,
    int TotalAssignments,
    int InvitedCount,
    int StartedCount,
    int CompletedCount,
    int OpenCount,
    DateTime? LastParticipationUtc);

public sealed record TrainingParticipantAssignmentHistoryRow(
    TrainingAssignment Assignment,
    TrainingEntity Training,
    bool? QuizPassed,
    int? LastQuizScorePercent,
    bool EmailSnapshotDiffers);

public sealed record TrainingParticipantDetail(
    TrainingParticipant Participant,
    IReadOnlyList<TrainingParticipantAssignmentHistoryRow> Assignments);

public sealed record TrainingParticipantSelectionItem(
    int Id,
    string Email,
    string? Name,
    string? Department);

public sealed record TrainingParticipantBulkImportLineResult(
    int LineNumber,
    string RawLine,
    bool IsValid,
    string? ErrorMessage,
    string? Email,
    string? Name,
    string? Department,
    bool IsExistingParticipant);

public sealed record TrainingParticipantBulkImportPreview(
    IReadOnlyList<TrainingParticipantBulkImportLineResult> Lines,
    int NewParticipantCount,
    int ExistingParticipantCount,
    int InvalidCount,
    int DuplicateInInputCount);

public sealed record TrainingParticipantBulkImportResult(
    int CreatedCount,
    int ExistingCount,
    int FailureCount,
    IReadOnlyList<string> Errors);
