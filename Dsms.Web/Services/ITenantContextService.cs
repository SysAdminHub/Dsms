namespace Dsms.Web.Services;

/// <summary>
/// Verwaltet den aktiven Mandantenkontext (<c>CurrentTenantId</c>) für den angemeldeten Benutzer.
/// Die ASP.NET-Session ist die persistente Quelle; der Scoped Accessor dient als Laufzeit-Cache.
/// </summary>
public interface ITenantContextService
{
    /// <summary>Aktuell gewählte Mandanten-ID oder null, wenn keiner aktiv ist.</summary>
    Task<int?> GetCurrentTenantIdAsync();

    /// <summary>True, wenn ein gültiger Mandant im Kontext gesetzt ist.</summary>
    Task<bool> HasTenantSelectedAsync();

    /// <summary>Setzt den aktiven Mandanten (Session + Scoped Accessor).</summary>
    Task SetCurrentTenantIdAsync(int tenantId);

    /// <summary>Entfernt die Mandantenauswahl aus Session und Accessor.</summary>
    Task ClearCurrentTenantIdAsync();

    /// <summary>Lädt die Mandanten-ID aus der Session in den Scoped Accessor.</summary>
    Task LoadFromSessionAsync();
}
