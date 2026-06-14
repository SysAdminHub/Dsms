using Dsms.Web.Domain.Entities;

namespace Dsms.Web.Services.Training;

public sealed record TrainingTemplateListRow(
    TrainingTemplate Template,
    int SectionCount,
    int QuestionCount,
    int AssetCount,
    bool CanEdit,
    bool CanArchive,
    bool CanCopy);

public sealed record TrainingCommunitySubmissionRow(
    TrainingTemplate Template,
    string TenantName,
    string? SubmittedByDisplayName,
    int SectionCount,
    int QuestionCount);

/// <summary>Daten für die Superuser-Prüfansicht einer eingereichten Community-Schulungsvorlage.</summary>
public sealed record CommunityTrainingTemplateReviewViewModel(
    TrainingTemplate Template,
    string TenantName,
    IReadOnlyList<TrainingTemplateSection> Sections,
    IReadOnlyDictionary<int, string> SectionPreviewHtml,
    IReadOnlyList<TrainingQuestion> Questions);

public sealed class TrainingTemplateValidationReport
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    public List<string> Successes { get; } = [];
}
