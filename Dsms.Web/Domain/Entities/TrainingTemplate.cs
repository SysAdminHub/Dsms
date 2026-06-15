using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Wiederverwendbare Schulungsvorlage mit Karten, Assets und optionalem Quiz.
/// Mandantenvorlagen haben <see cref="TenantId"/>; globale Vorlagen haben <c>TenantId = null</c> und <see cref="IsGlobal"/> = true.
/// </summary>
public class TrainingTemplate : ArchivableEntityBase
{
    /// <summary>Mandant bei eigenen Vorlagen; null bei globalen Vorlagen.</summary>
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TrainingType TrainingType { get; set; } = TrainingType.PrivacyBasics;
    public string? TargetAudience { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public int? RecommendedRepeatAfterMonths { get; set; }
    public int PassingScorePercent { get; set; } = 80;
    public bool IsQuizRequired { get; set; }
    public bool IsGlobal { get; set; }
    public bool IsCommunityTemplate { get; set; }
    public CommunityTemplateStatus CommunityStatus { get; set; } = CommunityTemplateStatus.None;

    public DateTime? CommunitySubmittedAt { get; set; }
    public string? CommunitySubmittedByUserId { get; set; }
    public int? CommunitySubmittedByTenantId { get; set; }
    public Tenant? CommunitySubmittedByTenant { get; set; }
    public string? CommunitySubmissionNote { get; set; }
    public DateTime? CommunityReviewedAt { get; set; }
    public string? CommunityReviewedByUserId { get; set; }
    public string? CommunityReviewNote { get; set; }
    public string? CommunityRejectionReason { get; set; }

    /// <summary>Verweis auf die ursprüngliche Mandantenvorlage bei freigegebenen Community-Kopien.</summary>
    public int? SourceTemplateId { get; set; }
    public TrainingTemplate? SourceTemplate { get; set; }

    public bool IsActive { get; set; } = true;

    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }

    public ICollection<TrainingTemplateSection> Sections { get; set; } = [];
    public ICollection<TrainingTemplateAsset> Assets { get; set; } = [];
    public ICollection<TrainingQuestion> Questions { get; set; } = [];
}
