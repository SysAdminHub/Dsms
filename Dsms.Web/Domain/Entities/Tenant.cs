namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Mandant / Organisationseinheit – Wurzel der einfachen Mandantentrennung in Version 1.
/// </summary>
public class Tenant : EntityBase
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Name des Verantwortlichen / der Organisation (Art. 30 Abs. 1 DSGVO).</summary>
    public string? LegalName { get; set; }

    public string? Street { get; set; }
    public string? HouseNumber { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? ContactName { get; set; }
    public string? VatId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    public string? DpoName { get; set; }
    public string? DpoStreet { get; set; }
    public string? DpoHouseNumber { get; set; }
    public string? DpoPostalCode { get; set; }
    public string? DpoCity { get; set; }
    public string? DpoPhone { get; set; }
    public string? DpoEmail { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Zugehörige Kundenlizenz; null bei noch nicht zugeordneten Mandanten.</summary>
    public Guid? LicenseId { get; set; }
    public License? License { get; set; }

    /// <summary>Mandant wurde zur Löschung angefordert (keine automatische Hard-Delete in V1).</summary>
    public bool IsDeletionRequested { get; set; }

    public DateTime? DeletionRequestedAt { get; set; }
    public string? DeletionRequestedByUserId { get; set; }

    /// <summary>Geplanter Zeitpunkt für spätere endgültige Löschung (V1: nur Markierung).</summary>
    public DateTime? DeletionScheduledAt { get; set; }

    public ICollection<AuditTemplate> AuditTemplates { get; set; } = [];
    public ICollection<AuditRun> AuditRuns { get; set; } = [];
    public ICollection<Measure> Measures { get; set; } = [];
    public ICollection<EvidenceDocument> Documents { get; set; } = [];
    public ICollection<ProcessingActivity> ProcessingActivities { get; set; } = [];
    public ICollection<Tom> Toms { get; set; } = [];
    public ICollection<ServiceProvider> ServiceProviders { get; set; } = [];
    public ICollection<DataProtectionImpactAssessment> DpiaAssessments { get; set; } = [];
    public ICollection<PrivacyIncident> PrivacyIncidents { get; set; } = [];
    public ICollection<DataSubjectRequest> DataSubjectRequests { get; set; } = [];
    public ICollection<TrainingTemplate> TrainingTemplates { get; set; } = [];
    public ICollection<Training> Trainings { get; set; } = [];
    public ICollection<TrainingParticipant> TrainingParticipants { get; set; } = [];
    public ICollection<TrainingAssignment> TrainingAssignments { get; set; } = [];
    public ICollection<UserTenant> UserTenants { get; set; } = [];
}
