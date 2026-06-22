using Dsms.Web.Data;

namespace Dsms.Web.Services;

/// <summary>
/// Zentrale Berechtigungs- und Mandantenlogik für Benutzer- und Tenant-Verwaltung (SaaS V1).
/// Razor-Seiten sollen Prüfungen hier bündeln statt Rollenlogik zu duplizieren.
/// </summary>
public interface IUserAccessService
{
    Task<bool> IsSuperuserAsync();

    /// <summary>True, wenn der Benutzer einem Mandanten zugeordnet ist und kein Superuser ist.</summary>
    Task<bool> IsTenantUserAsync();

    /// <summary>True, wenn ein aktiver Mandantenkontext (Session) gesetzt ist.</summary>
    Task<bool> HasTenantContextAsync();

    /// <summary>Plattform-Administration (Mandanten, Lizenzen, globale Vorlagen): nur Superuser.</summary>
    Task<bool> CanAccessPlatformAdministrationAsync();

    /// <summary>Mandantenspezifische Fachmodule: Mandanten-Benutzer mit gesetztem TenantContext, nicht Superuser.</summary>
    Task<bool> CanAccessTenantBusinessModulesAsync();

    /// <summary>Globale Audit-Vorlagen verwalten: nur Superuser.</summary>
    Task<bool> CanManageGlobalAuditTemplatesAsync();

    /// <summary>Globale Schulungsvorlagen verwalten: nur Superuser.</summary>
    Task<bool> CanManageGlobalTrainingTemplatesAsync();

    /// <summary>Community-Schulungsvorlagen prüfen und freigeben: nur Superuser.</summary>
    Task<bool> CanReviewCommunityTrainingTemplatesAsync();

    /// <summary>Konkrete Mandanten-Schulungen (Durchführungen, Teilnehmer, Nachweise): Mandanten-Benutzer oder Superuser im Supportmodus.</summary>
    Task<bool> CanAccessTenantTrainingsAsync();

    /// <summary>Mandantenspezifische Audits (Durchläufe, Ergebnisse): Mandanten-Benutzer oder Superuser im gültigen Supportmodus.</summary>
    Task<bool> CanAccessTenantAuditsAsync();

    /// <summary>True, wenn Superuser einen gültigen Supportmodus für den aktuellen Mandanten nutzt.</summary>
    Task<bool> IsSupportModeAsync();

    Task<bool> IsTenantAdminAsync();

    /// <summary>
    /// Mandanten-Admin-Rechte für Fachdaten: echter TenantAdmin oder Superuser im gültigen Supportmodus
    /// für den aktuellen Mandanten (ohne dauerhafte Mandantenzuordnung).
    /// </summary>
    Task<bool> HasEffectiveTenantAdminPermissionsAsync();

    /// <summary>
    /// Wie <see cref="HasEffectiveTenantAdminPermissionsAsync"/>, aber für einen konkreten Mandanten
    /// (z. B. vor Schreibaktionen mit expliziter TenantId).
    /// </summary>
    Task<bool> HasEffectiveTenantAdminPermissionsForTenantAsync(int tenantId);

    /// <summary>
    /// Fehlermeldung bei verweigerter Schreibaktion auf Mandanten-Fachdaten.
    /// </summary>
    Task<string> GetTenantBusinessWriteDeniedMessageAsync();

    Task<bool> CanManageUsersAsync();
    Task<bool> CanManageTenantsAsync();

    /// <summary>Export, Löschanforderung und DSGVO-Stammdaten: Mandanten-Admin oder Superuser im Supportmodus.</summary>
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
    /// Mandanten-Admin oder Superuser im gültigen Supportmodus; nicht Auditor.
    /// </summary>
    Task<bool> CanEditComplianceContentAsync();

    /// <summary>
    /// Bearbeitung operativer Mandanteninhalte (Maßnahmen, Audit-Antworten, Dokumente).
    /// Mandanten-Admin/User oder Superuser im gültigen Supportmodus (Admin-Niveau); nicht Auditor.
    /// </summary>
    Task<bool> CanEditTenantOperationalContentAsync();

    /// <summary>Neue Datenschutzvorfälle anlegen: Mandanten-Admin oder Superuser im Supportmodus.</summary>
    Task<bool> CanCreatePrivacyIncidentsAsync();

    /// <summary>Bestehende Datenschutzvorfälle bearbeiten: Superuser, Admin und User; nicht Auditor.</summary>
    Task<bool> CanEditPrivacyIncidentsAsync();

    /// <summary>Neue Betroffenenanfragen anlegen: Superuser und Admin; nicht User/Auditor.</summary>
    Task<bool> CanCreateDataSubjectRequestsAsync();

    /// <summary>Bestehende Betroffenenanfragen bearbeiten: Superuser, Admin und User; nicht Auditor.</summary>
    Task<bool> CanEditDataSubjectRequestsAsync();

    /// <summary>Falldaten anonymisieren: nur Superuser und Admin.</summary>
    Task<bool> CanAnonymizeDataSubjectRequestsAsync();

    /// <summary>Dokumentkategorien verwalten: Mandanten-Admin oder Superuser im Supportmodus.</summary>
    Task<bool> CanManageDocumentCategoriesAsync();

    /// <summary>TOM-Kategorien verwalten: Mandanten-Admin oder Superuser im Supportmodus.</summary>
    Task<bool> CanManageTomCategoriesAsync();

    /// <summary>Dienstleister-Arten verwalten: Mandanten-Admin oder Superuser im Supportmodus.</summary>
    Task<bool> CanManageServiceProviderCategoriesAsync();

    /// <summary>Organisatorische Datenschutzrollen verwalten: Mandanten-Admin oder Superuser im Supportmodus.</summary>
    Task<bool> CanManageDataProtectionRolesAsync();

    /// <summary>Supportzugriff für den aktuellen Mandanten verwalten (nur Mandanten-Admin).</summary>
    Task<bool> CanManageSupportAccessAsync();

    /// <summary>True, wenn die Rolle Superuser ist (plattformweit, kein Mandant erforderlich).</summary>
    static bool RoleRequiresNoTenant(string role) => role == Domain.DsmsRoles.Superuser;
}
