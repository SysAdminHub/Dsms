using System.Security.Claims;
using Dsms.Web.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Dsms.Web.Services;

/// <summary>
/// Ermittelt Benutzerkontext aus dem Blazor-AuthenticationState und lädt Profildaten per UserManager nach.
/// Pro Request/SignalR-Circuit scoped registriert.
/// </summary>
public class CurrentUserContext(
    AuthenticationStateProvider authenticationStateProvider,
    UserManager<ApplicationUser> userManager) : ICurrentUserContext
{
    /// <inheritdoc />
    public async Task<string?> GetUserIdAsync()
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        return state.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    /// <inheritdoc />
    public async Task<int?> GetTenantIdAsync()
    {
        var userId = await GetUserIdAsync();
        if (userId is null)
        {
            return null;
        }

        // TenantId liegt nicht im Claim, sondern im erweiterten Benutzerprofil – daher DB-Lookup.
        var user = await userManager.FindByIdAsync(userId);
        return user?.TenantId;
    }

    /// <inheritdoc />
    public async Task<bool> IsInRoleAsync(string role)
    {
        var userId = await GetUserIdAsync();
        if (userId is null)
        {
            return false;
        }

        var user = await userManager.FindByIdAsync(userId);
        return user is not null && await userManager.IsInRoleAsync(user, role);
    }
}
