using Dsms.Web.Configuration;
using Microsoft.Extensions.Options;

namespace Dsms.Web.Services;

/// <summary>
/// Liest Anwendungsmetadaten aus <c>appsettings.json</c> (Abschnitte <c>Application</c> und <c>AppBranding</c>).
/// </summary>
public sealed class ApplicationInfoService(
    IConfiguration configuration,
    IOptions<AppBrandingOptions> brandingOptions) : IApplicationInfoService
{
    private readonly AppBrandingOptions _branding = brandingOptions.Value;

    public string Version { get; } = ResolveVersion(configuration);

    public string VersionDisplay { get; } = FormatVersionDisplay(
        ResolveVersion(configuration),
        brandingOptions.Value.ProductName);

    public string ProductName => _branding.ProductName;

    public string ShortName => _branding.ShortName;

    public string LogoUrl => _branding.LogoUrl;

    public string Tagline => _branding.Tagline;

    public string Description => _branding.Description;

    public string WebsiteUrl => _branding.WebsiteUrl;

    public string AppUrl => _branding.AppUrl;

    public string SupportEmail => _branding.SupportEmail;

    private static string ResolveVersion(IConfiguration configuration)
    {
        var version = configuration["Application:Version"];
        return string.IsNullOrWhiteSpace(version) ? "dev" : version.Trim();
    }

    private static string FormatVersionDisplay(string version, string productName)
    {
        var normalized = version.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? version
            : $"v{version}";

        return $"{productName} {normalized}";
    }
}
