namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Metadaten zu einer hochgeladenen Nachweisdatei.
/// Binärdaten liegen im Dateisystem (<see cref="StoragePath"/>), nicht in der DB.
/// </summary>
public class EvidenceDocument : EntityBase
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

    public string? UploadedByUserId { get; set; }
}
