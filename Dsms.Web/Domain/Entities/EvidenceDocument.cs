namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Metadaten zu einer hochgeladenen Nachweisdatei.
/// Binärdaten liegen im Dateisystem (<see cref="StoragePath"/>), nicht in der DB.
/// </summary>
public class EvidenceDocument : ArchivableEntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public string StoragePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    public int? AuditRunId { get; set; }
    public AuditRun? AuditRun { get; set; }

    public int? MeasureId { get; set; }
    public Measure? Measure { get; set; }

    public int? ServiceProviderId { get; set; }
    public ServiceProvider? ServiceProvider { get; set; }

    /// <summary>Optionale Zuordnung zu einer Verarbeitungstätigkeit (VVT-Nachweis).</summary>
    public int? ProcessingActivityId { get; set; }
    public ProcessingActivity? ProcessingActivity { get; set; }

    /// <summary>Optionale Zuordnung zu einer DSFA (z. B. Risikobewertung, Freigabe).</summary>
    public int? DataProtectionImpactAssessmentId { get; set; }
    public DataProtectionImpactAssessment? DataProtectionImpactAssessment { get; set; }

    /// <summary>Optionale Zuordnung zu einem Datenschutzvorfall.</summary>
    public int? PrivacyIncidentId { get; set; }
    public PrivacyIncident? PrivacyIncident { get; set; }

    public string? UploadedByUserId { get; set; }
}
