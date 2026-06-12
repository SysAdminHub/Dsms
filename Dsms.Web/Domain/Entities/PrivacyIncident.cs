using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Datenschutzvorfall / Datenschutzpanne – mandantenbezogenes Vorfallregister.
/// </summary>
public class PrivacyIncident : ArchivableEntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string IncidentNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public PrivacyIncidentStatus Status { get; set; } = PrivacyIncidentStatus.Draft;
    public PrivacyIncidentSeverity Severity { get; set; } = PrivacyIncidentSeverity.Medium;
    public PrivacyIncidentSource Source { get; set; } = PrivacyIncidentSource.Internal;
    public PrivacyIncidentOwnRole OwnRole { get; set; } = PrivacyIncidentOwnRole.Unclear;

    public DateTime? DiscoveredAt { get; set; }
    public DateTime? OccurredAt { get; set; }
    public DateTime? ReportedToUsAt { get; set; }

    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }

    public string? ResponsiblePerson { get; set; }
    public string? InternalReference { get; set; }

    public string? Description { get; set; }
    public string? HowDetected { get; set; }
    public string? Cause { get; set; }
    public string? AffectedSystems { get; set; }
    public bool? IncidentStillActive { get; set; }
    public DateTime? IncidentStoppedAt { get; set; }

    public bool ConfidentialityAffected { get; set; }
    public bool IntegrityAffected { get; set; }
    public bool AvailabilityAffected { get; set; }
    public string? BreachTypeDescription { get; set; }

    public string? AffectedDataCategories { get; set; }
    public string? AffectedPersonGroups { get; set; }
    public int? ApproxAffectedPersons { get; set; }
    public int? ApproxAffectedRecords { get; set; }
    public bool SpecialCategoriesAffected { get; set; }

    public string? LikelyConsequences { get; set; }
    public PrivacyIncidentRiskLevel RiskLevel { get; set; } = PrivacyIncidentRiskLevel.Unknown;
    public string? RiskAssessmentReason { get; set; }

    public DecisionStatus SupervisoryAuthorityNotificationRequired { get; set; } = DecisionStatus.Open;
    public string? SupervisoryAuthorityNotificationReason { get; set; }
    public string? SupervisoryAuthorityName { get; set; }
    public DateTime? SupervisoryAuthorityNotifiedAt { get; set; }
    public string? SupervisoryAuthorityReference { get; set; }
    public string? NotificationDelayReason { get; set; }

    public DecisionStatus DataSubjectsNotificationRequired { get; set; } = DecisionStatus.Open;
    public string? DataSubjectsNotificationReason { get; set; }
    public DateTime? DataSubjectsNotifiedAt { get; set; }
    public string? DataSubjectsNotificationMethod { get; set; }
    public string? DataSubjectsNotificationSummary { get; set; }

    public string? ImmediateActions { get; set; }
    public string? RemediationActions { get; set; }
    public string? PreventiveActions { get; set; }
    public string? ClosureSummary { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<PrivacyIncidentProcessingActivity> ProcessingActivityLinks { get; set; } = [];
    public ICollection<PrivacyIncidentServiceProvider> ServiceProviderLinks { get; set; } = [];
    public ICollection<PrivacyIncidentMeasure> MeasureLinks { get; set; } = [];
    public ICollection<PrivacyIncidentTom> TomLinks { get; set; } = [];
}
