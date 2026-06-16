namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Bild oder Medium, das direkt zu einer Schulungsvorlage gehört und im Markdown per Asset-Platzhalter referenziert wird.
/// Kein Bestandteil des allgemeinen Dokumentenmoduls.
/// </summary>
public class TrainingTemplateAsset : EntityBase
{
    /// <summary>Redundant für Mandantensicherheit; null bei globalen Vorlagen.</summary>
    public int? TenantId { get; set; }

    public int TrainingTemplateId { get; set; }
    public TrainingTemplate TrainingTemplate { get; set; } = null!;

    /// <summary>Eindeutiger Schlüssel innerhalb der Vorlage, z. B. phishing-mail-beispiel.</summary>
    public string AssetKey { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsActive { get; set; } = true;

    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }
}
