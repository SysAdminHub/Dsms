namespace Dsms.Web.Domain;

/// <summary>
/// Feste Rollennamen für ASP.NET Core Identity.
/// Autorisierung in Razor via <c>[Authorize(Roles = …)]</c> und serverseitig über <see cref="Services.IUserAccessService"/>.
/// </summary>
public static class DsmsRoles
{
    /// <summary>Plattformweiter SaaS-Betreiber – alle Mandanten und Benutzer.</summary>
    public const string Superuser = "Superuser";

    /// <summary>Mandanten-Administrator – nur eigener Mandant, inkl. Benutzerverwaltung dort.</summary>
    public const string Admin = "Admin";

    public const string Auditor = "Auditor";
    public const string User = "User";

    /// <summary>Alle im System definierten Rollen (Seed, Anzeige).</summary>
    public static readonly string[] All = [Superuser, Admin, Auditor, User];

    /// <summary>
    /// Rollen, die ein Mandanten-Admin anlegen oder zuweisen darf (kein Superuser).
    /// Vorbereitung für spätere mandantenspezifische Rollen: gleiche Liste pro Tenant.
    /// </summary>
    public static readonly string[] AssignableByTenantAdmin = [Admin, Auditor, User];

    /// <summary>Rollen, die ein Superuser beliebigen Mandanten zuweisen darf.</summary>
    public static readonly string[] AssignableBySuperuser = [Superuser, Admin, Auditor, User];

    /// <summary>Identity-Rollen-String für Seiten der Benutzerverwaltung (Superuser + Admin).</summary>
    public const string UserManagement = $"{Superuser},{Admin}";
}
