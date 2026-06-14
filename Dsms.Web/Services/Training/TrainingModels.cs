using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using TrainingEntity = Dsms.Web.Domain.Entities.Training;

namespace Dsms.Web.Services.Training;

public sealed record TrainingListRow(
    TrainingEntity Training,
    string? TemplateTitle,
    string ResponsibleDisplay,
    int LinkedDocumentCount,
    TrainingParticipantStats ParticipantStats,
    bool CanEdit);

public sealed class TrainingOperationResult
{
    public bool Success { get; init; }
    public int? TrainingId { get; init; }
    public string? ErrorMessage { get; init; }

    public static TrainingOperationResult Ok(int id) =>
        new() { Success = true, TrainingId = id };

    public static TrainingOperationResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}

public enum TrainingQuickFilter
{
    All,
    Active,
    Inactive,
    Archived
}

/// <summary>Aktive Vorlage zur Auswahl beim Anlegen oder Bearbeiten einer Schulung.</summary>
public sealed record TrainingTemplateSelectionItem(
    int Id,
    string Title,
    TrainingType TrainingType,
    string? TargetAudience,
    string? Description,
    int? RecommendedRepeatAfterMonths,
    bool IsGlobal,
    string OriginLabel,
    string DropdownLabel);

public enum TrainingTemplateLoadStatus
{
    Ok,
    NotFound,
    NotAvailable
}

public sealed record TrainingTemplateLoadResult(
    TrainingTemplateLoadStatus Status,
    TrainingTemplate? Template = null)
{
    public static TrainingTemplateLoadResult Ok(TrainingTemplate template) =>
        new(TrainingTemplateLoadStatus.Ok, template);

    public static TrainingTemplateLoadResult NotFound() =>
        new(TrainingTemplateLoadStatus.NotFound);

    public static TrainingTemplateLoadResult NotAvailable() =>
        new(TrainingTemplateLoadStatus.NotAvailable);
}
