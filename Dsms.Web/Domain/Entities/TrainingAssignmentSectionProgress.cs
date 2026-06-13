namespace Dsms.Web.Domain.Entities;

/// <summary>Fortschritt eines Teilnehmers pro Schulungskarte.</summary>
public class TrainingAssignmentSectionProgress : EntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int TrainingAssignmentId { get; set; }
    public TrainingAssignment TrainingAssignment { get; set; } = null!;

    public int TrainingTemplateSectionId { get; set; }
    public TrainingTemplateSection TrainingTemplateSection { get; set; } = null!;

    public DateTime ViewedAtUtc { get; set; }
    public DateTime? LastViewedAtUtc { get; set; }
    public int ViewCount { get; set; }
    public int? TimeSpentSeconds { get; set; }
}
