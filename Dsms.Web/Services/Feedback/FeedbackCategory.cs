namespace Dsms.Web.Services.Feedback;

/// <summary>Kategorien für Benutzer-Feedback.</summary>
public static class FeedbackCategory
{
    public const string BugReport = "Fehler melden";
    public const string FeatureRequest = "Featurewunsch";
    public const string General = "Allgemeines Feedback";
    public const string Complaint = "Beschwerde";
    public const string Other = "Sonstiges";

    public static readonly string[] All =
    [
        BugReport,
        FeatureRequest,
        General,
        Complaint,
        Other
    ];
}
