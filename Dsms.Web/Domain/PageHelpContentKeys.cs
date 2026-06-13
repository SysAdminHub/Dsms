namespace Dsms.Web.Domain;

/// <summary>Stabile Schlüssel für globale Seiten-Hilfetexte.</summary>
public static class PageHelpContentKeys
{
    public const string ProcessingActivities = "processing-activities";
    public const string Toms = "toms";
    public const string Dsfa = "dsfa";
    public const string ServiceProviders = "service-providers";
    public const string AuditTemplates = "audit-templates";
    public const string AuditRuns = "audit-runs";
    public const string Measures = "measures";
    public const string PrivacyIncidents = "privacy-incidents";
    public const string DataSubjectRequests = "data-subject-requests";
    public const string Organization = "organization";
    public const string OrganizationOrgChart = "organization-org-chart";
    public const string Documents = "documents";
    public const string TrainingTemplates = "training-templates";
    public const string Trainings = "trainings";
    public const string TenantData = "tenant-data";
    public const string Users = "users";
    public const string License = "license";

    public static readonly IReadOnlyList<string> All =
    [
        ProcessingActivities,
        Toms,
        Dsfa,
        ServiceProviders,
        AuditTemplates,
        AuditRuns,
        Measures,
        PrivacyIncidents,
        DataSubjectRequests,
        Organization,
        OrganizationOrgChart,
        Documents,
        TrainingTemplates,
        Trainings,
        TenantData,
        Users,
        License
    ];

    public static bool IsValid(string? key) =>
        !string.IsNullOrWhiteSpace(key) && All.Contains(key, StringComparer.Ordinal);
}
