using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Betroffenenanfrage nach DSGVO – mandantenbezogenes Anfragenregister.
/// </summary>
public class DataSubjectRequest : ArchivableEntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public DataSubjectRequestType RequestType { get; set; } = DataSubjectRequestType.Access;
    public DataSubjectRequestStatus Status { get; set; } = DataSubjectRequestStatus.New;

    public DateTime ReceivedAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? AnsweredAt { get; set; }

    public string? DataSubjectName { get; set; }
    public string? DataSubjectEmail { get; set; }
    public string? DataSubjectPhone { get; set; }
    public string? DataSubjectReference { get; set; }

    public string? ContactChannel { get; set; }

    public string? Description { get; set; }
    public string? ResultSummary { get; set; }
    public string? InternalNotes { get; set; }

    public string? AssignedUserId { get; set; }

    public bool IdentityVerified { get; set; }
    public string? IdentityVerificationNote { get; set; }

    public bool DeadlineExtended { get; set; }
    public string? DeadlineExtensionReason { get; set; }
    public DateTime? ExtendedDueAt { get; set; }

    public bool ContainsPersonalData { get; set; } = true;
    public bool PersonalDataAnonymized { get; set; }
    public DateTime? PersonalDataAnonymizedAt { get; set; }
    public string? PersonalDataAnonymizedByUserId { get; set; }
    public string? AnonymizationNote { get; set; }

    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }

    public ICollection<DataSubjectRequestProcessingActivity> ProcessingActivityLinks { get; set; } = [];
    public ICollection<DataSubjectRequestMeasure> MeasureLinks { get; set; } = [];
    public ICollection<DataSubjectRequestServiceProvider> ServiceProviderLinks { get; set; } = [];
}
