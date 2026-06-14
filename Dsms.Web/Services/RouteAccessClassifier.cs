namespace Dsms.Web.Services;

/// <summary>
/// Klassifiziert Routen für Mandantenkontext, Plattform-Administration und Fachmodule.
/// </summary>
public static class RouteAccessClassifier
{
    public const string TenantBusinessAccessDeniedMessage =
        "Diese Ansicht enthält mandantenspezifische Fachdaten. Als Plattform-Administrator haben Sie darauf standardmäßig keinen Zugriff.";

    public const string SupportAccessInvalidMessage =
        "Der Supportzugriff ist nicht mehr gültig oder wurde widerrufen.";

    public const string NoActiveSupportGrantMessage =
        "Für diesen Mandanten liegt kein aktiver Supportzugriff vor.";

    /// <summary>Normalisiert einen relativen Pfad (ohne führenden Slash, lowercase).</summary>
    public static string NormalizePath(string relativePath)
    {
        var path = relativePath.Trim('/');
        var queryIndex = path.IndexOf('?');
        if (queryIndex >= 0)
        {
            path = path[..queryIndex];
        }

        return path.ToLowerInvariant();
    }

    /// <summary>Plattform-Routen ohne Mandantenkontext (Superuser-Administration).</summary>
    public static bool IsPlatformRoute(string relativePath)
    {
        var path = NormalizePath(relativePath);

        if (path == ""
            || path.StartsWith("account/", StringComparison.Ordinal)
            || path == "select-tenant"
            || path.StartsWith("tenants", StringComparison.Ordinal)
            || path.StartsWith("users", StringComparison.Ordinal)
            || path.StartsWith("platform/", StringComparison.Ordinal)
            || path.StartsWith("admin/erinnerungen", StringComparison.Ordinal)
            || path == "passwort-vergessen"
            || path == "passwort-zuruecksetzen"
            || path == "signup"
            || path.StartsWith("signup/", StringComparison.Ordinal)
            || path == "not-found"
            || path == "error")
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Globale Audit-Vorlagen (Plattform): Superuser ohne Mandantenkontext.
    /// Mandantenspezifische Audit-Durchläufe sind davon ausgenommen.
    /// </summary>
    public static bool IsGlobalAuditTemplateRoute(string relativePath)
    {
        var path = NormalizePath(relativePath);

        if (path.StartsWith("platform/audit-templates", StringComparison.Ordinal))
        {
            return true;
        }

        return path == "audit-templates" || path.StartsWith("audit-templates/", StringComparison.Ordinal);
    }

    /// <summary>
    /// Globale Schulungsvorlagen (Plattform): Superuser ohne Mandantenkontext.
    /// Konkrete Schulungen und Teilnehmer bleiben Mandanten-Fachmodule.
    /// </summary>
    public static bool IsGlobalTrainingTemplateRoute(string relativePath)
    {
        var path = NormalizePath(relativePath);

        if (path.StartsWith("platform/training-templates", StringComparison.Ordinal))
        {
            return true;
        }

        return path == "training-templates" || path.StartsWith("training-templates/", StringComparison.Ordinal);
    }

    /// <summary>
    /// Mandantenspezifische Fachmodule – erfordern TenantContext und Mandanten-Berechtigung.
    /// </summary>
    public static bool IsBusinessModuleRoute(string relativePath)
    {
        if (IsPlatformRoute(relativePath)
            || IsGlobalAuditTemplateRoute(relativePath)
            || IsGlobalTrainingTemplateRoute(relativePath))
        {
            return false;
        }

        var path = NormalizePath(relativePath);

        if (path.StartsWith("schulung/teilnahme", StringComparison.Ordinal))
        {
            return false;
        }

        return path.StartsWith("processing-activities", StringComparison.Ordinal)
            || path.StartsWith("dsfa", StringComparison.Ordinal)
            || path.StartsWith("toms", StringComparison.Ordinal)
            || path.StartsWith("service-providers", StringComparison.Ordinal)
            || path.StartsWith("incidents", StringComparison.Ordinal)
            || path.StartsWith("data-subject-requests", StringComparison.Ordinal)
            || path.StartsWith("measures", StringComparison.Ordinal)
            || path.StartsWith("organization", StringComparison.Ordinal)
            || path.StartsWith("audit-runs", StringComparison.Ordinal)
            || path.StartsWith("documents", StringComparison.Ordinal)
            || path.StartsWith("trainings", StringComparison.Ordinal)
            || path.StartsWith("tenant-daten", StringComparison.Ordinal)
            || path.StartsWith("admin/license", StringComparison.Ordinal)
            || path.StartsWith("admin/auditlog", StringComparison.Ordinal);
    }

    /// <summary>Prüft, ob für die Route ein Mandantenkontext erforderlich ist.</summary>
    public static bool IsTenantRequiredForRoute(string relativePath) =>
        !IsPlatformRoute(relativePath)
        && !IsGlobalAuditTemplateRoute(relativePath)
        && !IsGlobalTrainingTemplateRoute(relativePath);
}
