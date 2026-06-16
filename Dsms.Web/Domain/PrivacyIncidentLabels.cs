using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain;

/// <summary>Deutsche Anzeigelabels für Datenschutzvorfälle.</summary>
public static class PrivacyIncidentLabels
{
    public static string GetStatusLabel(PrivacyIncidentStatus status) => status switch
    {
        PrivacyIncidentStatus.Draft => "Entwurf",
        PrivacyIncidentStatus.InReview => "In Prüfung",
        PrivacyIncidentStatus.ActionsRunning => "Maßnahmen laufen",
        PrivacyIncidentStatus.Reported => "Gemeldet",
        PrivacyIncidentStatus.Closed => "Abgeschlossen",
        PrivacyIncidentStatus.Archived => "Archiviert",
        _ => status.ToString()
    };

    public static string GetStatusVariant(PrivacyIncidentStatus status) => status switch
    {
        PrivacyIncidentStatus.Closed => "success",
        PrivacyIncidentStatus.Reported => "primary",
        PrivacyIncidentStatus.ActionsRunning => "primary",
        PrivacyIncidentStatus.InReview => "warning",
        PrivacyIncidentStatus.Archived => "muted",
        _ => "muted"
    };

    public static string GetSeverityLabel(PrivacyIncidentSeverity severity) => severity switch
    {
        PrivacyIncidentSeverity.Low => "Niedrig",
        PrivacyIncidentSeverity.Medium => "Mittel",
        PrivacyIncidentSeverity.High => "Hoch",
        PrivacyIncidentSeverity.Critical => "Kritisch",
        _ => severity.ToString()
    };

    public static string GetSeverityVariant(PrivacyIncidentSeverity severity) => severity switch
    {
        PrivacyIncidentSeverity.Critical => "danger",
        PrivacyIncidentSeverity.High => "danger",
        PrivacyIncidentSeverity.Medium => "warning",
        _ => "muted"
    };

    public static string GetSourceLabel(PrivacyIncidentSource source) => source switch
    {
        PrivacyIncidentSource.Internal => "Intern festgestellt",
        PrivacyIncidentSource.ServiceProvider => "Durch Dienstleister gemeldet",
        PrivacyIncidentSource.DataSubject => "Durch betroffene Person gemeldet",
        PrivacyIncidentSource.CustomerOrPartner => "Durch Kunde / Partner gemeldet",
        PrivacyIncidentSource.Authority => "Durch Behörde gemeldet",
        PrivacyIncidentSource.SecuritySystem => "Durch IT-Security-System erkannt",
        PrivacyIncidentSource.Other => "Sonstiges",
        _ => source.ToString()
    };

    public static string GetOwnRoleLabel(PrivacyIncidentOwnRole role) => role switch
    {
        PrivacyIncidentOwnRole.Controller => "Verantwortlicher",
        PrivacyIncidentOwnRole.Processor => "Auftragsverarbeiter",
        PrivacyIncidentOwnRole.AffectedCustomer => "Betroffene Organisation / Kunde eines Dienstleisters",
        PrivacyIncidentOwnRole.Unclear => "Unklar / wird geprüft",
        _ => role.ToString()
    };

    public static string GetRiskLevelLabel(PrivacyIncidentRiskLevel level) => level switch
    {
        PrivacyIncidentRiskLevel.NoRisk => "Kein Risiko",
        PrivacyIncidentRiskLevel.LowRisk => "Geringes Risiko",
        PrivacyIncidentRiskLevel.Risk => "Risiko",
        PrivacyIncidentRiskLevel.HighRisk => "Hohes Risiko",
        PrivacyIncidentRiskLevel.Unknown => "Noch unklar",
        _ => level.ToString()
    };

    public static string GetRiskLevelVariant(PrivacyIncidentRiskLevel level) => level switch
    {
        PrivacyIncidentRiskLevel.HighRisk => "danger",
        PrivacyIncidentRiskLevel.Risk => "warning",
        PrivacyIncidentRiskLevel.LowRisk => "primary",
        PrivacyIncidentRiskLevel.NoRisk => "success",
        _ => "muted"
    };

    public static string GetDecisionStatusLabel(DecisionStatus status) => status switch
    {
        DecisionStatus.Open => "Noch offen",
        DecisionStatus.Yes => "Ja",
        DecisionStatus.No => "Nein",
        _ => status.ToString()
    };

    public static string GetDecisionStatusVariant(DecisionStatus status) => status switch
    {
        DecisionStatus.Yes => "danger",
        DecisionStatus.No => "success",
        DecisionStatus.Open => "warning",
        _ => "muted"
    };

    public static bool IsOpen(PrivacyIncidentStatus status) =>
        status is PrivacyIncidentStatus.Draft
            or PrivacyIncidentStatus.InReview
            or PrivacyIncidentStatus.ActionsRunning
            or PrivacyIncidentStatus.Reported;
}
