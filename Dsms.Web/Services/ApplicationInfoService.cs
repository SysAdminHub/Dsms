namespace Dsms.Web.Services;

/// <summary>
/// Liest Anwendungsmetadaten aus <c>appsettings.json</c> (Abschnitt <c>Application</c>).
/// </summary>
public sealed class ApplicationInfoService(IConfiguration configuration) : IApplicationInfoService
{
    public string Version { get; } = ResolveVersion(configuration);

    public string VersionDisplay { get; } = FormatVersionDisplay(ResolveVersion(configuration));

    private static string ResolveVersion(IConfiguration configuration)
    {
        var version = configuration["Application:Version"];
        return string.IsNullOrWhiteSpace(version) ? "dev" : version.Trim();
    }

    private static string FormatVersionDisplay(string version)
    {
        var normalized = version.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? version
            : $"v{version}";

        return $"DSMS {normalized}";
    }
}
