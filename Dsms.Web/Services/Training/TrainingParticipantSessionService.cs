using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Dsms.Web.Services.Training;

/// <summary>Signierte HttpOnly-Cookie-Session für Schulungsteilnehmer (kein App-Login).</summary>
public class TrainingParticipantSessionService(
    IHttpContextAccessor httpContextAccessor,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<TrainingAccessOptions> options)
{
    private readonly IDataProtector _protector =
        dataProtectionProvider.CreateProtector("Dsms.Training.ParticipantSession.v1");
    private readonly TrainingAccessOptions _options = options.Value;

    public void SetSession(TrainingParticipantSessionData data)
    {
        var context = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP-Kontext nicht verfügbar.");

        var json = JsonSerializer.Serialize(data);
        var protectedValue = _protector.Protect(json);

        context.Response.Cookies.Append(
            _options.ParticipantSessionCookieName,
            protectedValue,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddHours(_options.SessionLifetimeHours)
            });
    }

    public TrainingParticipantSessionData? GetSession()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
            return null;

        if (!context.Request.Cookies.TryGetValue(_options.ParticipantSessionCookieName, out var protectedValue)
            || string.IsNullOrWhiteSpace(protectedValue))
        {
            return null;
        }

        try
        {
            var json = _protector.Unprotect(protectedValue);
            var data = JsonSerializer.Deserialize<TrainingParticipantSessionData>(json);
            if (data is null)
                return null;

            var maxAge = TimeSpan.FromHours(_options.SessionLifetimeHours);
            if (DateTime.UtcNow - data.IssuedAtUtc > maxAge)
                return null;

            return data;
        }
        catch
        {
            return null;
        }
    }

    public void ClearSession()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
            return;

        context.Response.Cookies.Delete(_options.ParticipantSessionCookieName, new CookieOptions { Path = "/" });
    }
}
