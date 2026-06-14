using Dsms.Web.Data;
using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Zeitlich begrenzte Freigabe für Plattform-Support auf mandantenspezifische Fachdaten.
/// Wird ausschließlich durch Mandanten-Admins erstellt.
/// </summary>
public class SupportAccessGrant : EntityBase
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string GrantedByUserId { get; set; } = string.Empty;
    public ApplicationUser GrantedByUser { get; set; } = null!;

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; set; }

    public DateTime? RevokedAt { get; set; }
    public string? RevokedByUserId { get; set; }

    public string? Reason { get; set; }
    public string? InternalNote { get; set; }

    public SupportAccessScope AccessScope { get; set; } = SupportAccessScope.AllTenantBusinessModules;

    public bool IsActive(DateTime utcNow) =>
        RevokedAt is null && ValidUntil > utcNow;
}
