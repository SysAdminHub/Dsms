namespace Dsms.Web.Services;

/// <summary>
/// Scoped-Laufzeit-Cache für die aktuelle Mandanten-ID im laufenden Request/Circuit.
/// Die autoritative Quelle ist die ASP.NET-Session (<see cref="TenantContextService"/>);
/// bei leerem Cache wird der Mandant daraus wiederhergestellt.
/// </summary>
public class TenantContextAccessor
{
    /// <summary>Gecachte Mandanten-ID; null wenn noch kein Cache-Eintrag vorliegt.</summary>
    public int? CurrentTenantId { get; set; }
}
