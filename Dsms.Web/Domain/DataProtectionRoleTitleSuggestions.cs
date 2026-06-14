namespace Dsms.Web.Domain;

/// <summary>Vorschlagswerte für Rollenbezeichnungen (frei editierbar, keine Enum).</summary>
public static class DataProtectionRoleTitleSuggestions
{
    public const string Management = "Geschäftsführung / oberste Leitung";
    public const string DataProtectionOfficer = "Datenschutzbeauftragte Person";
    public const string DataProtectionCoordinator = "Datenschutzkoordinator";
    public const string ItResponsible = "IT-Verantwortlicher";
    public const string DepartmentResponsible = "Fachbereichsverantwortlicher";
    public const string HrResponsible = "HR-Verantwortlicher";
    public const string ContactPerson = "Ansprechpartner";
    public const string Other = "Sonstige Rolle";

    public static readonly IReadOnlyList<string> All =
    [
        Management,
        DataProtectionOfficer,
        DataProtectionCoordinator,
        ItResponsible,
        DepartmentResponsible,
        HrResponsible,
        ContactPerson,
        Other
    ];

    /// <summary>Priorität für kompakte Anzeige in Mandanten-Stammdaten (niedriger = wichtiger).</summary>
    public static readonly IReadOnlyList<string> HighlightedTitles =
    [
        DataProtectionOfficer,
        ItResponsible,
        HrResponsible,
        Management
    ];
}
