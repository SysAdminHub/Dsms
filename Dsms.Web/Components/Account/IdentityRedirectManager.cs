using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Dsms.Web.Data;

namespace Dsms.Web.Components.Account;

/// <summary>
/// Sichere Navigation und Statusmeldungen für Identity-Seiten (kein Open-Redirect).
/// </summary>
internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    public const string StatusCookieName = "Identity.StatusMessage";

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    public void RedirectTo(
        string? uri,
        Dictionary<string, object?>? queryParameters = null,
        bool forceLoad = false)
    {
        uri ??= "";

        if (queryParameters is not null)
        {
            var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
            uri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        }

        // Nur relative URLs zulassen – Schutz vor Open-Redirect-Angriffen.
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            uri = navigationManager.ToBaseRelativePath(uri);
        }

        navigationManager.NavigateTo(uri, forceLoad);
    }

    /// <summary>Kurzlebige Statusmeldung per Cookie für die Zielseite (z. B. Erfolg/Fehler).</summary>
    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    private string CurrentPath => navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path);

    public void RedirectToCurrentPage() => RedirectTo(CurrentPath);

    public void RedirectToCurrentPageWithStatus(string message, HttpContext context)
        => RedirectToWithStatus(CurrentPath, message, context);

    public void RedirectToInvalidUser(UserManager<ApplicationUser> userManager, HttpContext context)
        => RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
}
