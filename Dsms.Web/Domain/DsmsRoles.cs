namespace Dsms.Web.Domain;

/// <summary>
/// Feste Rollennamen für ASP.NET Core Identity.
/// Autorisierung in Razor via <c>[Authorize(Roles = …)]</c> und <c>AuthorizeView Roles=</c>.
/// </summary>
public static class DsmsRoles
{
    public const string Admin = "Admin";
    public const string Auditor = "Auditor";
    public const string User = "User";

    /// <summary>Alle Rollen – z. B. für Seed und Rollen-Dropdown in der Benutzerverwaltung.</summary>
    public static readonly string[] All = [Admin, Auditor, User];
}
