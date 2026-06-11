namespace Dsms.Web.Services;

/// <summary>
/// Anwendungsmetadaten aus der Konfiguration (Branding, Version für die UI).
/// </summary>
public interface IApplicationInfoService
{
    /// <summary>Rohwert aus <c>Application:Version</c> oder Fallback <c>dev</c>.</summary>
    string Version { get; }

    /// <summary>Formatierte Anzeige für die UI, z. B. <c>Datenschutz-Cloud v1.0.0</c>.</summary>
    string VersionDisplay { get; }

    string ProductName { get; }

    string ShortName { get; }

    /// <summary>Logo-Pfad aus <c>AppBranding:LogoUrl</c>; leer wenn kein Bild konfiguriert ist.</summary>
    string LogoUrl { get; }

    string Tagline { get; }

    string Description { get; }

    string WebsiteUrl { get; }

    string AppUrl { get; }

    string SupportEmail { get; }
}
