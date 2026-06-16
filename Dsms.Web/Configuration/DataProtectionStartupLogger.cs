namespace Dsms.Web.Configuration;

/// <summary>Loggt die effektive DataProtection-Konfiguration beim Start (ohne Secrets).</summary>
public static class DataProtectionStartupLogger
{
    public static void LogEffectiveConfiguration(WebApplication app)
    {
        var section = app.Configuration.GetSection(DataProtectionOptions.SectionName);
        var options = section.Get<DataProtectionOptions>() ?? new DataProtectionOptions();

        var resolvedPath = Path.IsPathRooted(options.KeysPath)
            ? options.KeysPath
            : Path.Combine(app.Environment.ContentRootPath, options.KeysPath);

        app.Logger.LogInformation(
            "DataProtection-Konfiguration geladen: Environment={Environment}, ApplicationName={ApplicationName}, KeysPath={KeysPath}, KeysDirectoryExists={KeysDirectoryExists}, KeyFileCount={KeyFileCount}",
            app.Environment.EnvironmentName,
            options.ApplicationName,
            resolvedPath,
            Directory.Exists(resolvedPath),
            Directory.Exists(resolvedPath)
                ? Directory.GetFiles(resolvedPath, "*.xml").Length
                : 0);

        app.Logger.LogInformation(
            "Hinweis: Passwort-Reset-Tokens aus Dsms.Provisioning sind nur gültig, wenn ApplicationName und KeysPath mit Dsms.Provisioning übereinstimmen.");
    }
}
