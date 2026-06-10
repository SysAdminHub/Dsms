namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Mandantenbezogene Aufgabe in der Dashboard-Checkliste „Erste Schritte“.
/// Pro Mandant und Key existiert höchstens ein Eintrag.
/// </summary>
public class TenantOnboardingTask : EntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Stabiler Schlüssel der Standardaufgabe (z. B. „processing-activities“).</summary>
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedByUserId { get; set; }
}
