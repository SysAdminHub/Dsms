using Dsms.Web.Services;

namespace Dsms.Web.Models.Dashboard;

/// <summary>Erzeugt Donut-Segmente und Legenden aus DashboardSummary-Kennzahlen.</summary>
public static class DashboardChartBuilder
{
    public static IReadOnlyList<DashboardDonutSegment> BuildStatusSegments(
        DashboardStatusGroupCounts groups,
        int displayTotal)
    {
        return
        [
            CreateSegment("Kritisch", groups.Critical, DashboardChartColors.Danger, DashboardChartColors.DangerClass, displayTotal),
            CreateSegment("Hinweis", groups.Warning, DashboardChartColors.Warning, DashboardChartColors.WarningClass, displayTotal),
            CreateSegment("Gut", groups.Good, DashboardChartColors.Success, DashboardChartColors.SuccessClass, displayTotal),
            CreateSegment("Neutral", groups.Neutral, DashboardChartColors.Neutral, DashboardChartColors.NeutralClass, displayTotal)
        ];
    }

    private static DashboardDonutSegment CreateSegment(
        string label,
        int value,
        string color,
        string cssClass,
        int total)
    {
        return new DashboardDonutSegment
        {
            Label = label,
            Value = value,
            Color = color,
            CssClass = cssClass,
            Tooltip = total > 0 ? $"{label}: {value} von {total}" : $"{label}: 0"
        };
    }

    public static IReadOnlyList<DashboardLegendItem> ProcessingActivityLegend(DashboardSummary s) =>
    [
        Legend("ohne TOMs", s.ProcessingActivitiesWithoutTomsCount, DashboardChartColors.Warning),
        Legend("ohne Dokumente", s.ProcessingActivitiesWithoutDocumentsCount, DashboardChartColors.Warning),
        Legend("mit offenen Maßnahmen", s.ProcessingActivitiesWithOpenMeasuresCount, DashboardChartColors.Warning),
        Legend("DSFA erforderlich", s.ProcessingActivitiesDpiaRequiredCount, DashboardChartColors.Danger),
        Legend("mit Risiko-Dienstleister", s.ProcessingActivitiesWithHighRiskProvidersCount, DashboardChartColors.Danger)
    ];

    public static IReadOnlyList<DashboardLegendItem> TomLegend(DashboardSummary s) =>
    [
        Legend("umgesetzt", s.ImplementedTomsCount, DashboardChartColors.Success),
        Legend("geplant", s.PlannedTomsCount, DashboardChartColors.Warning),
        Legend("nicht umgesetzt", s.NotImplementedTomsCount, DashboardChartColors.Danger),
        Legend("Prüfung überfällig", s.OverdueTomReviewsCount, DashboardChartColors.Danger)
    ];

    public static IReadOnlyList<DashboardLegendItem> DpiaLegend(DashboardSummary s) =>
    [
        Legend("in Prüfung", s.DpiaInReviewCount, DashboardChartColors.Warning),
        Legend("Restrisiko hoch/kritisch", s.DpiaHighOrCriticalRiskCount, DashboardChartColors.Danger),
        Legend("Prüfung überfällig", s.OverdueDpiaReviewsCount, DashboardChartColors.Danger),
        Legend("DSFA-Pflicht ohne DSFA", s.ProcessingActivitiesDpiaRequiredWithoutAssessmentCount, DashboardChartColors.Danger)
    ];

    public static IReadOnlyList<DashboardLegendItem> ServiceProviderLegend(DashboardSummary s) =>
    [
        Legend("aktive Auftragsverarbeiter", s.ActiveDataProcessorsCount, DashboardChartColors.Success),
        Legend("ohne AVV", s.ProcessorsWithoutAvvCount, DashboardChartColors.Danger),
        Legend("Drittlandbezug", s.ThirdCountryProvidersCount, DashboardChartColors.Warning),
        Legend("Risiko hoch/kritisch", s.HighRiskProvidersCount, DashboardChartColors.Danger),
        Legend("AVV-Prüfung überfällig", s.OverdueAvvReviewsCount, DashboardChartColors.Danger)
    ];

    public static IReadOnlyList<DashboardLegendItem> PrivacyIncidentLegend(DashboardSummary s) =>
    [
        Legend("offen", s.OpenPrivacyIncidentsCount, DashboardChartColors.Warning),
        Legend("in Prüfung", s.PrivacyIncidentsInReviewCount, DashboardChartColors.Warning),
        Legend("meldepflichtig", s.NotificationRequiredPrivacyIncidentsCount, DashboardChartColors.Danger),
        Legend("Risiko hoch/kritisch", s.HighRiskPrivacyIncidentsCount, DashboardChartColors.Danger),
        Legend("abgeschlossen", s.ClosedPrivacyIncidentsCount, DashboardChartColors.Success)
    ];

    public static IReadOnlyList<DashboardLegendItem> DataSubjectRequestLegend(DashboardSummary s) =>
    [
        Legend("offen", s.OpenDataSubjectRequestsCount, DashboardChartColors.Warning),
        Legend("überfällig", s.OverdueDataSubjectRequestsCount, DashboardChartColors.Danger),
        Legend("bald fällig", s.DueSoonDataSubjectRequestsCount, DashboardChartColors.Warning),
        Legend("abgeschlossen", s.CompletedDataSubjectRequestsCount, DashboardChartColors.Success)
    ];

    public static IReadOnlyList<DashboardLegendItem> MeasureLegend(DashboardSummary s) =>
    [
        Legend("offen", s.OpenMeasuresCount, DashboardChartColors.Warning),
        Legend("überfällig", s.OverdueMeasuresCount, DashboardChartColors.Danger),
        Legend("in Bearbeitung", s.InProgressMeasuresCount, DashboardChartColors.Warning),
        Legend("abgeschlossen", s.CompletedMeasuresCount, DashboardChartColors.Success)
    ];

    public static IReadOnlyList<DashboardLegendItem> AuditLegend(DashboardSummary s) =>
    [
        Legend("laufende Audits", s.ActiveAuditsCount, DashboardChartColors.Warning),
        Legend("Audit-Entwürfe", s.DraftAuditsCount, DashboardChartColors.Neutral),
        Legend("abgeschlossen", s.CompletedAuditsCount, DashboardChartColors.Success)
    ];

    private static DashboardLegendItem Legend(string label, int value, string color) => new()
    {
        Label = label,
        Value = value,
        Color = color,
        CssClass = value > 0 ? string.Empty : "dsms-dashboard-legend-value--zero"
    };
}
