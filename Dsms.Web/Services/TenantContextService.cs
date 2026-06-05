namespace Dsms.Web.Services;

/// <summary>
/// Speichert <c>CurrentTenantId</c> in Session und Scoped Accessor.
/// Alle Session-Operationen sind fehlertolerant (kein Circuit-Abbruch bei F5/Prerender).
/// </summary>
public class TenantContextService(
    IHttpContextAccessor httpContextAccessor,
    TenantContextAccessor accessor,
    ILogger<TenantContextService> logger) : ITenantContextService
{
    public const string SessionKey = "Dsms.CurrentTenantId";

    /// <inheritdoc />
    public async Task<int?> GetCurrentTenantIdAsync()
    {
        if (!accessor.CurrentTenantId.HasValue)
        {
            await LoadFromSessionAsync();
        }

        return accessor.CurrentTenantId;
    }

    /// <inheritdoc />
    public async Task<bool> HasTenantSelectedAsync()
    {
        if (!accessor.CurrentTenantId.HasValue)
        {
            await LoadFromSessionAsync();
        }

        return accessor.CurrentTenantId.HasValue;
    }

    /// <inheritdoc />
    public async Task SetCurrentTenantIdAsync(int tenantId)
    {
        accessor.CurrentTenantId = tenantId;

        try
        {
            var session = httpContextAccessor.HttpContext?.Session;
            if (session is null)
            {
                logger.LogDebug("Session nicht verfügbar – Mandant {TenantId} nur im Accessor gesetzt", tenantId);
                return;
            }

            await session.LoadAsync();
            session.SetInt32(SessionKey, tenantId);
            await session.CommitAsync();
            logger.LogDebug("Mandant {TenantId} in Session gespeichert", tenantId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Session-Speichern fehlgeschlagen – Mandant {TenantId} bleibt im Accessor", tenantId);
        }
    }

    /// <inheritdoc />
    public async Task ClearCurrentTenantIdAsync()
    {
        accessor.CurrentTenantId = null;

        try
        {
            var session = httpContextAccessor.HttpContext?.Session;
            if (session is null)
            {
                return;
            }

            await session.LoadAsync();
            session.Remove(SessionKey);
            await session.CommitAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Session-Löschen fehlgeschlagen");
        }
    }

    /// <inheritdoc />
    public async Task LoadFromSessionAsync()
    {
        try
        {
            var session = httpContextAccessor.HttpContext?.Session;
            if (session is null)
            {
                return;
            }

            await session.LoadAsync();

            if (session.TryGetValue(SessionKey, out _))
            {
                accessor.CurrentTenantId = session.GetInt32(SessionKey);
                logger.LogDebug("Mandant {TenantId} aus Session geladen", accessor.CurrentTenantId);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Session-Laden fehlgeschlagen – Accessor bleibt unverändert");
        }
    }
}
