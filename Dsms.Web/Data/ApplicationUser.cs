using Microsoft.AspNetCore.Identity;

namespace Dsms.Web.Data;

/// <summary>
/// Identity-Benutzer mit DSMS-spezifischen Profilfeldern.
/// <see cref="TenantId"/> steuert die Mandantentrennung in Version 1 (ein Mandant pro Benutzer).
/// Superuser haben typischerweise <c>null</c> – sie sind plattformweit, nicht an einen Mandanten gebunden.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Anzeigename in der UI (unabhängig von UserName/E-Mail).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Zugehöriger Mandant. Pflicht für Admin, Auditor und User; bei Superuser in der Regel null.
    /// Später erweiterbar auf mehrere Mandanten (eigene Zuordnungstabelle).
    /// </summary>
    public int? TenantId { get; set; }

    /// <summary>Inaktive Konten können sich nicht anmelden und werden in Listen weiterhin angezeigt.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Zeitpunkt der Kontoanlage (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Identity-ID des anlegenden Benutzers; null bei Seed oder System.</summary>
    public string? CreatedByUserId { get; set; }
}
