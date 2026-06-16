using Dsms.Web.Data;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Many-to-Many-Zuordnung zwischen Identity-Benutzern und Mandanten.
/// Ein Benutzer kann mehreren Mandanten zugeordnet sein; der aktive Mandant
/// wird separat über <see cref="Services.TenantContextService"/> gesteuert.
/// </summary>
public class UserTenant
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Zeitpunkt der Zuordnung (UTC).</summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
