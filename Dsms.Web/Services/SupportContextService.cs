namespace Dsms.Web.Services;

/// <inheritdoc />
public class SupportContextService(
    IHttpContextAccessor httpContextAccessor,
    SupportContextAccessor accessor,
    ILogger<SupportContextService> logger) : ISupportContextService
{
    public const string SessionGrantIdKey = "Dsms.SupportAccess.GrantId";

    /// <inheritdoc />
    public async Task<int?> GetActiveGrantIdAsync()
    {
        if (!accessor.ActiveSupportAccessGrantId.HasValue)
        {
            await LoadFromSessionAsync();
        }

        return accessor.ActiveSupportAccessGrantId;
    }

    /// <inheritdoc />
    public async Task SetActiveGrantIdAsync(int grantId)
    {
        accessor.ActiveSupportAccessGrantId = grantId;

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.Response.HasStarted == true)
        {
            logger.LogDebug(
                "Support-Session nicht beschreibbar – Grant {GrantId} nur im Accessor",
                grantId);
            return;
        }

        try
        {
            var session = httpContext?.Session;
            if (session is null)
            {
                return;
            }

            await session.LoadAsync();
            session.SetInt32(SessionGrantIdKey, grantId);
            await session.CommitAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Support-Session speichern fehlgeschlagen für Grant {GrantId}", grantId);
        }
    }

    /// <inheritdoc />
    public async Task ClearAsync()
    {
        accessor.ActiveSupportAccessGrantId = null;

        try
        {
            var session = httpContextAccessor.HttpContext?.Session;
            if (session is null)
            {
                return;
            }

            await session.LoadAsync();
            session.Remove(SessionGrantIdKey);
            await session.CommitAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Support-Session löschen fehlgeschlagen");
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
            if (session.TryGetValue(SessionGrantIdKey, out _))
            {
                accessor.ActiveSupportAccessGrantId = session.GetInt32(SessionGrantIdKey);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Support-Session laden fehlgeschlagen");
        }
    }
}
