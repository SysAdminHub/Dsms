using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain;

/// <summary>Normalisiert Schulungsstatus (inkl. Legacy-Werte vor Migration).</summary>
public static class TrainingStatusMapper
{
    public static TrainingStatus Normalize(TrainingStatus status) => (int)status switch
    {
        0 => TrainingStatus.Active,
        1 => TrainingStatus.Inactive,
        2 => TrainingStatus.Archived,
        3 or 4 => TrainingStatus.Inactive,
        5 => TrainingStatus.Archived,
        _ => TrainingStatus.Inactive
    };

    public static bool IsValid(TrainingStatus status) =>
        TrainingStatusMapper.Normalize(status) is TrainingStatus.Active
            or TrainingStatus.Inactive
            or TrainingStatus.Archived;
}
