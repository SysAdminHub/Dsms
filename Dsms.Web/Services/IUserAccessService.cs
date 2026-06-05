using Dsms.Web.Data;

namespace Dsms.Web.Services;

/// <summary>
/// Zentrale Berechtigungs- und Mandantenlogik für Benutzer- und Tenant-Verwaltung (SaaS V1).
/// Razor-Seiten sollen Prüfungen hier bündeln statt Rollenlogik zu duplizieren.
/// </summary>
public interface IUserAccessService
{
    Task<bool> IsSuperuserAsync();
    Task<bool> IsTenantAdminAsync();
    Task<bool> CanManageUsersAsync();
    Task<bool> CanManageTenantsAsync();

    /// <summary>Aktive Mandanten-ID aus dem Mandantenkontext (Session); null wenn keiner gewählt.</summary>
    Task<int?> GetCurrentTenantIdAsync();

    /// <summary>Prüft, ob der aktuelle Benutzer den angegebenen Mandanten sehen/bearbeiten darf.</summary>
    Task<bool> CanAccessTenantAsync(int tenantId);

    /// <summary>Prüft Lesen/Bearbeiten eines Zielbenutzers inkl. Rollen- und Mandantengrenzen.</summary>
    Task<bool> CanManageUserAsync(ApplicationUser target);

    /// <summary>Rollen, die der aktuelle Benutzer beim Anlegen/Bearbeiten zuweisen darf.</summary>
    Task<IReadOnlyList<string>> GetAssignableRolesAsync();

    /// <summary>True, wenn die Rolle Superuser ist (plattformweit, kein Mandant erforderlich).</summary>
    static bool RoleRequiresNoTenant(string role) => role == Domain.DsmsRoles.Superuser;
}
