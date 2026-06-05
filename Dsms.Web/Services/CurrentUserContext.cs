using System.Security.Claims;
using Dsms.Web.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Dsms.Web.Services;

/// <summary>
/// Ermittelt Identität und Mandant des angemeldeten Benutzers.
/// Nutzt <see cref="IHttpContextAccessor"/> in Middleware und HTTP-Pipeline;
/// fällt in Blazor-Circuits ohne HttpContext auf <see cref="AuthenticationStateProvider"/> zurück.
/// </summary>
public class CurrentUserContext(
    IHttpContextAccessor httpContextAccessor,
    AuthenticationStateProvider authenticationStateProvider,
    UserManager<ApplicationUser> userManager,
    ITenantContextService tenantContext) : ICurrentUserContext
{
    /// <inheritdoc />
    public async Task<string?> GetUserIdAsync()
    {
        var principal = await GetPrincipalAsync();
        return principal?.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    /// <inheritdoc />
    public async Task<int?> GetTenantIdAsync()
    {
        if (await GetUserIdAsync() is null)
        {
            return null;
        }

        return await tenantContext.GetCurrentTenantIdAsync();
    }

    /// <inheritdoc />
    public async Task<bool> IsInRoleAsync(string role)
    {
        var principal = await GetPrincipalAsync();
        return principal?.Identity?.IsAuthenticated == true && principal.IsInRole(role);
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetUserAsync()
    {
        var userId = await GetUserIdAsync();
        return userId is null ? null : await userManager.FindByIdAsync(userId);
    }

    /// <summary>
    /// HttpContext.User in Middleware; im Blazor-Circuit oft ohne Rollen-Claims – dann AuthState nutzen.
    /// </summary>
    private async Task<ClaimsPrincipal?> GetPrincipalAsync()
    {
        var httpUser = httpContextAccessor.HttpContext?.User;

        // Pipeline/Middleware: HttpContext.User enthält typischerweise alle Claims inkl. Rollen.
        if (httpUser?.Identity?.IsAuthenticated == true
            && httpUser.Claims.Any(c => c.Type == ClaimTypes.Role))
        {
            return httpUser;
        }

        // Blazor Interactive: AuthenticationStateProvider liefert vollständige Rollen-Claims.
        try
        {
            var state = await authenticationStateProvider.GetAuthenticationStateAsync();
            if (state.User.Identity?.IsAuthenticated == true)
            {
                return state.User;
            }
        }
        catch (InvalidOperationException)
        {
            // Außerhalb des Blazor-Komponenten-Scopes (z. B. Middleware ohne Rollen im HttpContext).
        }

        return httpUser;
    }
}
