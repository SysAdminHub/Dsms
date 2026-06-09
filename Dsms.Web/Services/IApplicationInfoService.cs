namespace Dsms.Web.Services;

/// <summary>
/// Anwendungsmetadaten aus der Konfiguration (z. B. UI-Version in der Sidebar).
/// </summary>
public interface IApplicationInfoService
{
    /// <summary>Rohwert aus <c>Application:Version</c> oder Fallback <c>dev</c>.</summary>
    string Version { get; }

    /// <summary>Formatierte Anzeige für die UI, z. B. <c>DSMS v1.0.0</c>.</summary>
    string VersionDisplay { get; }
}
