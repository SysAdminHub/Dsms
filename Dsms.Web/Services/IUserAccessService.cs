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

    /// <summary>Export und Löschanforderung: Superuser oder Mandanten-Admin mit Zugriff auf aktuellen Mandanten.</summary>
    Task<bool> CanManageTenantDataAsync();

    /// <summary>Aktive Mandanten-ID aus dem Mandantenkontext (Session); null wenn keiner gewählt.</summary>
    Task<int?> GetCurrentTenantIdAsync();

    /// <summary>Prüft, ob der aktuelle Benutzer den angegebenen Mandanten sehen/bearbeiten darf.</summary>
    Task<bool> CanAccessTenantAsync(int tenantId);

    /// <summary>Prüft Lesen/Bearbeiten eines Zielbenutzers inkl. Rollen- und Mandantengrenzen.</summary>
    Task<bool> CanManageUserAsync(ApplicationUser target);

    /// <summary>Rollen, die der aktuelle Benutzer beim Anlegen/Bearbeiten zuweisen darf.</summary>
    Task<IReadOnlyList<string>> GetAssignableRolesAsync();

    /// <summary>True, wenn der Benutzer die reine Prüferrolle Auditor hat (ohne Admin/Superuser).</summary>
    Task<bool> IsAuditorAsync();

    /// <summary>
    /// Bearbeitung von Compliance-Stammdaten (VVT, DSFA, TOMs, Dienstleister, Audit-Durchläufe, Vorlagen).
    /// Admin und Superuser; nicht Auditor.
    /// </summary>
    Task<bool> CanEditComplianceContentAsync();

    /// <summary>
    /// Bearbeitung operativer Mandanteninhalte (Maßnahmen, Audit-Antworten, Dokumente).
    /// Admin, User und Superuser; nicht Auditor.
    /// </summary>
    Task<bool> CanEditTenantOperationalContentAsync();

    /// <summary>True, wenn die Rolle Superuser ist (plattformweit, kein Mandant erforderlich).</summary>
    static bool RoleRequiresNoTenant(string role) => role == Domain.DsmsRoles.Superuser;
}
