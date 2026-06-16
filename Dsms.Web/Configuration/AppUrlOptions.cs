namespace Dsms.Web.Configuration;

/// <summary>URLs für Fachanwendung und Provisioning-App (externe Navigation).</summary>
public sealed class AppUrlOptions
{
    public const string SectionName = "AppUrls";

    public string MainAppBaseUrl { get; set; } = "https://app.datenschutz-cloud.eu";

    public string ProvisioningAppBaseUrl { get; set; } = "https://signup.datenschutz-cloud.eu";

    /// <summary>
    /// Ziel-URL für öffentliche Registrierung. Leer = <see cref="ProvisioningAppBaseUrl"/> + <c>/signup</c>.
    /// </summary>
    public string ProvisioningSignupUrl { get; set; } = "";

    public string ResolveProvisioningAppBaseUrl() =>
        string.IsNullOrWhiteSpace(ProvisioningAppBaseUrl)
            ? "https://signup.datenschutz-cloud.eu"
            : ProvisioningAppBaseUrl.Trim().TrimEnd('/');

    public string ResolveSignupUrl()
    {
        if (!string.IsNullOrWhiteSpace(ProvisioningSignupUrl))
        {
            return ProvisioningSignupUrl.Trim();
        }

        return $"{ResolveProvisioningAppBaseUrl()}/signup";
    }

    public string BuildProvisioningUrl(string relativePath, string? queryString = null)
    {
        var baseUrl = ResolveProvisioningAppBaseUrl();
        var path = relativePath.Trim().TrimStart('/');
        var url = string.IsNullOrEmpty(path) ? baseUrl : $"{baseUrl}/{path}";

        if (string.IsNullOrEmpty(queryString))
        {
            return url;
        }

        return queryString.StartsWith('?') ? url + queryString : url + "?" + queryString;
    }
}
