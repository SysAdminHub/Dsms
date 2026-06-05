namespace Dsms.Web.Services;

/// <summary>
/// Scoped-Halter für die aktuelle Mandanten-ID im laufenden Request/Circuit.
/// Wird von <see cref="TenantContextService"/> befüllt und von EF Global Query Filters gelesen.
/// </summary>
public class TenantContextAccessor
{
    /// <summary>Aktuell gewählter Mandant; null wenn noch keiner gesetzt wurde.</summary>
    public int? CurrentTenantId { get; set; }
}
