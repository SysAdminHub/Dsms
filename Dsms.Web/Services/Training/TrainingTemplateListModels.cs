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

public sealed class TrainingTemplateValidationReport
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    public List<string> Successes { get; } = [];
}
