namespace Dsms.Web.Services;

/// <summary>
/// Liefert Identität und Mandant des aktuell angemeldeten Benutzers für UI und Datenzugriff.
/// Zentrale Abstraktion, damit Seiten nicht direkt mit Claims oder UserManager arbeiten müssen.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>Identity-Benutzer-ID (NameIdentifier-Claim) oder null, wenn nicht angemeldet.</summary>
    Task<string?> GetUserIdAsync();

    /// <summary>
    /// Mandanten-ID aus dem Benutzerprofil. Null bei fehlender Anmeldung oder nicht zugeordnetem Mandanten.
    /// </summary>
    Task<int?> GetTenantIdAsync();

    /// <summary>Prüft die angegebene Identity-Rolle (siehe <see cref="Domain.DsmsRoles"/>).</summary>
    Task<bool> IsInRoleAsync(string role);
}
