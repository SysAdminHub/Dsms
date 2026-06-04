using Microsoft.AspNetCore.Identity;

namespace Dsms.Web.Data;

/// <summary>
/// Identity-Benutzer mit DSMS-spezifischen Profilfeldern.
/// <see cref="TenantId"/> steuert die einfache Mandantentrennung in Version 1 (kein Claim).
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Anzeigename in der UI (unabhängig von UserName/E-Mail).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Zugehöriger Mandant. Null nur bei Admin-Sonderfällen; normale Anwender haben immer einen Mandanten.
    /// </summary>
    public int? TenantId { get; set; }
}
