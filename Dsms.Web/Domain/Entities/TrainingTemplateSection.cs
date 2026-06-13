namespace Dsms.Web.Domain.Entities;

/// <summary>Einzelne Karte/Abschnitt einer <see cref="TrainingTemplate"/> mit Markdown-Inhalt.</summary>
public class TrainingTemplateSection : EntityBase
{
    /// <summary>Redundant für Mandantensicherheit; null bei globalen Vorlagen.</summary>
    public int? TenantId { get; set; }

    public int TrainingTemplateId { get; set; }
    public TrainingTemplate TrainingTemplate { get; set; } = null!;

    public int SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }
}
