using Dsms.Web.Domain.Entities;

namespace Dsms.Web.Services;

/// <summary>
/// Mandantenverwaltung: verfügbare Mandanten laden, Wechsel mit Berechtigungsprüfung, Kontextinitialisierung.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Lädt Session in den Accessor und wählt automatisch den Mandanten,
    /// wenn der Benutzer genau einen zugewiesenen Mandanten hat.
    /// Wirft keine Exceptions – Fehler werden geloggt.
    /// </summary>
    Task InitializeContextAsync();

    /// <summary>
    /// Stellt sicher, dass der Accessor einen gültigen Mandanten enthält.
    /// Lädt bei leerem Cache aus der Session, prüft Zugriff und wählt bei genau einem
    /// zugewiesenen Mandanten automatisch. Wirft keine Exceptions.
    /// </summary>
    /// <returns>Aktive Mandanten-ID oder null, wenn keiner gesetzt werden konnte.</returns>
    Task<int?> EnsureTenantContextAsync();

    /// <summary>
    /// Sichere, zentrale Initialisierung für UI-Komponenten (z. B. TenantSwitcher nach F5).
    /// Lädt Session, Mandantenliste und aktiven Mandanten ohne Circuit-Abbruch bei Fehlern.
    /// </summary>
    Task<TenantBootstrapState> LoadTenantAsync();

    /// <summary>Mandanten, die der aktuelle Benutzer im Switcher sehen darf.</summary>
    Task<IReadOnlyList<Tenant>> GetAccessibleTenantsAsync();

    /// <summary>Informationen zum aktuell aktiven Mandanten.</summary>
    Task<Tenant?> GetCurrentTenantAsync();

    /// <summary>Wechselt den aktiven Mandanten nach Berechtigungsprüfung.</summary>
    Task<TenantSwitchResult> SwitchTenantAsync(int tenantId);

    /// <summary>Prüft, ob für die angegebene Route ein aktiver Mandant erforderlich ist.</summary>
    bool IsTenantRequiredForRoute(string relativePath);

    /// <summary>Prüft, ob der Benutzer Zugriff auf den Mandanten hat.</summary>
    Task<bool> CanAccessTenantAsync(int tenantId);
}

/// <summary>Ergebnis des fehlertoleranten Mandanten-Bootstraps für die UI.</summary>
public sealed class TenantBootstrapState
{
    public bool Succeeded { get; init; }
    public IReadOnlyList<Tenant> AccessibleTenants { get; init; } = [];
    public int? CurrentTenantId { get; init; }
    public Tenant? CurrentTenant { get; init; }
}

public sealed class TenantSwitchResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }

    public static TenantSwitchResult Ok() => new() { Succeeded = true };

    public static TenantSwitchResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}

