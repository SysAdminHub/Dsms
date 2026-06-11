using Dsms.Web.Data;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Models.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

/// <summary>
/// Aggregiert Kennzahlen und Listen für die Dashboard-Startseite – ausschließlich mandantenbezogen.
/// </summary>
public class DashboardService(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    /// <summary>
    /// Liefert Zähler und Vorschau-Listen für einen Mandanten.
    /// Offene Maßnahmen schließen Done und Cancelled aus; aktive Audits sind nur InProgress.
    /// </summary>
    public async Task<DashboardSummary> GetSummaryAsync(int tenantId, CancellationToken ct = default)
    {
        // Eigener DbContext pro Aufruf – parallel zur Tenant-Initialisierung (F5) sicher.
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.Today);

        var openMeasures = await db.Measures
            .Where(m => m.TenantId == tenantId && m.Status != MeasureStatus.Done && m.Status != MeasureStatus.Cancelled)
            .CountAsync(ct);

        var activeAudits = await db.AuditRuns
            .Where(r => r.TenantId == tenantId && r.Status == AuditRunStatus.InProgress)
            .CountAsync(ct);

        var draftAudits = await db.AuditRuns
            .Where(r => r.TenantId == tenantId && r.Status == AuditRunStatus.Draft)
            .CountAsync(ct);

        var totalToms = await db.Toms
            .Where(t => t.TenantId == tenantId)
            .CountAsync(ct);

        var plannedToms = await db.Toms
            .Where(t => t.TenantId == tenantId && t.ImplementationStatus == TomImplementationStatus.Planned)
            .CountAsync(ct);

        var notImplementedToms = await db.Toms
            .Where(t => t.TenantId == tenantId && t.ImplementationStatus == TomImplementationStatus.NotImplemented)
            .CountAsync(ct);

        var implementedToms = await db.Toms
            .Where(t => t.TenantId == tenantId && t.ImplementationStatus == TomImplementationStatus.Implemented)
            .CountAsync(ct);

        var overdueTomReviews = await db.Toms
            .Where(t => t.TenantId == tenantId && t.NextReviewAt != null && t.NextReviewAt < today)
            .CountAsync(ct);

        var totalServiceProviders = await db.ServiceProviders
            .Where(s => s.TenantId == tenantId)
            .CountAsync(ct);

        var activeDataProcessors = await db.ServiceProviders
            .Where(s => s.TenantId == tenantId
                && s.IsDataProcessor
                && (s.Status == ServiceProviderStatus.Active || s.Status == ServiceProviderStatus.Approved))
            .CountAsync(ct);

        var processorsWithoutAvv = await db.ServiceProviders
            .Where(s => s.TenantId == tenantId
                && s.IsDataProcessor
                && !s.DataProcessingAgreementExists
                && (s.Status == ServiceProviderStatus.Active || s.Status == ServiceProviderStatus.Approved))
            .CountAsync(ct);

        var thirdCountryProviders = await db.ServiceProviders
            .Where(s => s.TenantId == tenantId && s.ThirdCountryInvolvement)
            .CountAsync(ct);

        var highRiskProviders = await db.ServiceProviders
            .Where(s => s.TenantId == tenantId
                && (s.RiskAssessment == ServiceProviderRiskAssessment.High
                    || s.RiskAssessment == ServiceProviderRiskAssessment.Critical))
            .CountAsync(ct);

        var overdueAvvReviews = await db.ServiceProviders
            .Where(s => s.TenantId == tenantId
                && s.DataProcessingAgreementReviewedAt != null
                && s.DataProcessingAgreementReviewedAt < today)
            .CountAsync(ct);

        var totalProcessingActivities = await db.ProcessingActivities
            .Where(p => p.TenantId == tenantId)
            .CountAsync(ct);

        var activitiesWithoutToms = await db.ProcessingActivities
            .Where(p => p.TenantId == tenantId
                && !db.ProcessingActivityToms.Any(l => l.ProcessingActivityId == p.Id && l.TenantId == tenantId))
            .CountAsync(ct);

        var activitiesWithoutDocuments = await db.ProcessingActivities
            .Where(p => p.TenantId == tenantId
                && !db.EvidenceDocuments.Any(d => d.ProcessingActivityId == p.Id && d.TenantId == tenantId))
            .CountAsync(ct);

        var activitiesWithOpenMeasures = await db.ProcessingActivityMeasures
            .Where(l => l.TenantId == tenantId
                && l.Measure.TenantId == tenantId
                && l.Measure.Status != MeasureStatus.Done
                && l.Measure.Status != MeasureStatus.Cancelled)
            .Select(l => l.ProcessingActivityId)
            .Distinct()
            .CountAsync(ct);

        var activitiesDpiaRequired = await db.ProcessingActivities
            .Where(p => p.TenantId == tenantId && p.DpiaRequired)
            .CountAsync(ct);

        var totalDpiaAssessments = await db.DataProtectionImpactAssessments
            .Where(d => d.TenantId == tenantId)
            .CountAsync(ct);

        var dpiaInReview = await db.DataProtectionImpactAssessments
            .Where(d => d.TenantId == tenantId && d.Status == DpiaStatus.InReview)
            .CountAsync(ct);

        var dpiaHighOrCriticalRisk = await db.DataProtectionImpactAssessments
            .Where(d => d.TenantId == tenantId
                && (d.ResidualRisk == DpiaResidualRisk.High || d.ResidualRisk == DpiaResidualRisk.Critical))
            .CountAsync(ct);

        var overdueDpiaReviews = await db.DataProtectionImpactAssessments
            .Where(d => d.TenantId == tenantId && d.NextReviewAt != null && d.NextReviewAt < today)
            .CountAsync(ct);

        // VVT mit DSFA-Pflicht, aber ohne mindestens einen DSFA-Eintrag.
        var activitiesDpiaRequiredWithoutAssessment = await db.ProcessingActivities
            .Where(p => p.TenantId == tenantId && p.DpiaRequired
                && !db.DataProtectionImpactAssessments.Any(d =>
                    d.ProcessingActivityId == p.Id && d.TenantId == tenantId))
            .CountAsync(ct);

        var activitiesWithHighRiskProviders = await db.ProcessingActivityServiceProviders
            .Where(l => l.TenantId == tenantId
                && (l.ServiceProvider.RiskAssessment == ServiceProviderRiskAssessment.High
                    || l.ServiceProvider.RiskAssessment == ServiceProviderRiskAssessment.Critical))
            .Select(l => l.ProcessingActivityId)
            .Distinct()
            .CountAsync(ct);

        var openPrivacyIncidents = await db.PrivacyIncidents
            .Where(i => i.TenantId == tenantId
                && i.Status != PrivacyIncidentStatus.Closed
                && i.Status != PrivacyIncidentStatus.Archived)
            .CountAsync(ct);

        var privacyIncidentsInReview = await db.PrivacyIncidents
            .Where(i => i.TenantId == tenantId && i.Status == PrivacyIncidentStatus.InReview)
            .CountAsync(ct);

        var notificationRequiredIncidents = await db.PrivacyIncidents
            .Where(i => i.TenantId == tenantId
                && i.SupervisoryAuthorityNotificationRequired == DecisionStatus.Yes)
            .CountAsync(ct);

        var highRiskPrivacyIncidents = await db.PrivacyIncidents
            .Where(i => i.TenantId == tenantId
                && (i.RiskLevel == PrivacyIncidentRiskLevel.HighRisk
                    || i.Severity == PrivacyIncidentSeverity.High
                    || i.Severity == PrivacyIncidentSeverity.Critical))
            .CountAsync(ct);

        var totalMeasures = await db.Measures
            .Where(m => m.TenantId == tenantId)
            .CountAsync(ct);

        var inProgressMeasures = await db.Measures
            .Where(m => m.TenantId == tenantId && m.Status == MeasureStatus.InProgress)
            .CountAsync(ct);

        var completedMeasures = await db.Measures
            .Where(m => m.TenantId == tenantId && m.Status == MeasureStatus.Done)
            .CountAsync(ct);

        var overdueMeasures = await db.Measures
            .Where(m => m.TenantId == tenantId
                && m.Status != MeasureStatus.Done
                && m.Status != MeasureStatus.Cancelled
                && m.DueDate != null
                && m.DueDate < today)
            .CountAsync(ct);

        var totalPrivacyIncidents = await db.PrivacyIncidents
            .Where(i => i.TenantId == tenantId)
            .CountAsync(ct);

        var closedPrivacyIncidents = await db.PrivacyIncidents
            .Where(i => i.TenantId == tenantId && i.Status == PrivacyIncidentStatus.Closed)
            .CountAsync(ct);

        var totalAudits = await db.AuditRuns
            .Where(r => r.TenantId == tenantId)
            .CountAsync(ct);

        var completedAudits = await db.AuditRuns
            .Where(r => r.TenantId == tenantId && r.Status == AuditRunStatus.Completed)
            .CountAsync(ct);

        var processingActivityStatusGroups = await ClassifyProcessingActivitiesAsync(db, tenantId, ct);
        var tomStatusGroups = await ClassifyTomsAsync(db, tenantId, today, ct);
        var dpiaStatusGroups = await ClassifyDpiaAssessmentsAsync(db, tenantId, today, ct);
        var serviceProviderStatusGroups = await ClassifyServiceProvidersAsync(db, tenantId, today, ct);
        var privacyIncidentStatusGroups = await ClassifyPrivacyIncidentsAsync(db, tenantId, ct);
        var measureStatusGroups = await ClassifyMeasuresAsync(db, tenantId, today, ct);
        var auditStatusGroups = await ClassifyAuditsAsync(db, tenantId, ct);

        // Fälligkeit zuerst, damit dringende Maßnahmen oben erscheinen.
        var recentMeasures = await db.Measures
            .Where(m => m.TenantId == tenantId && m.Status != MeasureStatus.Done && m.Status != MeasureStatus.Cancelled)
            .OrderBy(m => m.DueDate)
            .Take(5)
            .ToListAsync(ct);

        var recentAudits = await db.AuditRuns
            .Where(r => r.TenantId == tenantId && r.Status != AuditRunStatus.Completed)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Include(r => r.AuditTemplate)
            .ToListAsync(ct);

        return new DashboardSummary(
            openMeasures,
            activeAudits,
            draftAudits,
            totalToms,
            plannedToms,
            notImplementedToms,
            implementedToms,
            overdueTomReviews,
            totalServiceProviders,
            activeDataProcessors,
            processorsWithoutAvv,
            thirdCountryProviders,
            highRiskProviders,
            overdueAvvReviews,
            totalProcessingActivities,
            activitiesWithoutToms,
            activitiesWithoutDocuments,
            activitiesWithOpenMeasures,
            activitiesDpiaRequired,
            totalDpiaAssessments,
            dpiaInReview,
            dpiaHighOrCriticalRisk,
            overdueDpiaReviews,
            activitiesDpiaRequiredWithoutAssessment,
            activitiesWithHighRiskProviders,
            openPrivacyIncidents,
            privacyIncidentsInReview,
            notificationRequiredIncidents,
            highRiskPrivacyIncidents,
            totalMeasures,
            inProgressMeasures,
            completedMeasures,
            overdueMeasures,
            totalPrivacyIncidents,
            closedPrivacyIncidents,
            totalAudits,
            completedAudits,
            processingActivityStatusGroups,
            tomStatusGroups,
            dpiaStatusGroups,
            serviceProviderStatusGroups,
            privacyIncidentStatusGroups,
            measureStatusGroups,
            auditStatusGroups,
            recentMeasures,
            recentAudits);
    }

    private static async Task<DashboardStatusGroupCounts> ClassifyProcessingActivitiesAsync(
        ApplicationDbContext db,
        int tenantId,
        CancellationToken ct)
    {
        var activities = await db.ProcessingActivities
            .Where(p => p.TenantId == tenantId)
            .Select(p => new { p.Id, p.Status, p.DpiaRequired })
            .ToListAsync(ct);

        if (activities.Count == 0)
        {
            return new DashboardStatusGroupCounts(0, 0, 0, 0);
        }

        var withToms = (await db.ProcessingActivityToms
            .Where(l => l.TenantId == tenantId)
            .Select(l => l.ProcessingActivityId)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();

        var withDocuments = (await db.EvidenceDocuments
            .Where(d => d.TenantId == tenantId && d.ProcessingActivityId != null)
            .Select(d => d.ProcessingActivityId!.Value)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();

        var withOpenMeasures = (await db.ProcessingActivityMeasures
            .Where(l => l.TenantId == tenantId
                && l.Measure.Status != MeasureStatus.Done
                && l.Measure.Status != MeasureStatus.Cancelled)
            .Select(l => l.ProcessingActivityId)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();

        var withHighRiskProviders = (await db.ProcessingActivityServiceProviders
            .Where(l => l.TenantId == tenantId
                && (l.ServiceProvider.RiskAssessment == ServiceProviderRiskAssessment.High
                    || l.ServiceProvider.RiskAssessment == ServiceProviderRiskAssessment.Critical))
            .Select(l => l.ProcessingActivityId)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();

        var withDpia = (await db.DataProtectionImpactAssessments
            .Where(d => d.TenantId == tenantId)
            .Select(d => d.ProcessingActivityId)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();

        var critical = 0;
        var warning = 0;
        var good = 0;
        var neutral = 0;

        foreach (var activity in activities)
        {
            var isCritical = withHighRiskProviders.Contains(activity.Id)
                || (activity.DpiaRequired && !withDpia.Contains(activity.Id));

            if (isCritical)
            {
                critical++;
                continue;
            }

            var isWarning = !withToms.Contains(activity.Id)
                || !withDocuments.Contains(activity.Id)
                || withOpenMeasures.Contains(activity.Id);

            if (isWarning)
            {
                warning++;
                continue;
            }

            if (activity.Status is ProcessingActivityStatus.Draft or ProcessingActivityStatus.InReview)
            {
                neutral++;
                continue;
            }

            good++;
        }

        return new DashboardStatusGroupCounts(critical, warning, good, neutral);
    }

    private static async Task<DashboardStatusGroupCounts> ClassifyTomsAsync(
        ApplicationDbContext db,
        int tenantId,
        DateOnly today,
        CancellationToken ct)
    {
        var toms = await db.Toms
            .Where(t => t.TenantId == tenantId)
            .Select(t => new { t.ImplementationStatus, t.NextReviewAt })
            .ToListAsync(ct);

        var critical = 0;
        var warning = 0;
        var good = 0;
        var neutral = 0;

        foreach (var tom in toms)
        {
            var isCritical = tom.ImplementationStatus == TomImplementationStatus.NotImplemented
                || (tom.NextReviewAt != null && tom.NextReviewAt < today);

            if (isCritical)
            {
                critical++;
                continue;
            }

            if (tom.ImplementationStatus == TomImplementationStatus.Planned)
            {
                warning++;
                continue;
            }

            if (tom.ImplementationStatus == TomImplementationStatus.Implemented)
            {
                good++;
                continue;
            }

            neutral++;
        }

        return new DashboardStatusGroupCounts(critical, warning, good, neutral);
    }

    private static async Task<DashboardStatusGroupCounts> ClassifyDpiaAssessmentsAsync(
        ApplicationDbContext db,
        int tenantId,
        DateOnly today,
        CancellationToken ct)
    {
        var dpias = await db.DataProtectionImpactAssessments
            .Where(d => d.TenantId == tenantId)
            .Select(d => new { d.Status, d.ResidualRisk, d.NextReviewAt })
            .ToListAsync(ct);

        var critical = 0;
        var warning = 0;
        var good = 0;
        var neutral = 0;

        foreach (var dpia in dpias)
        {
            var isCritical = dpia.ResidualRisk is DpiaResidualRisk.High or DpiaResidualRisk.Critical
                || (dpia.NextReviewAt != null && dpia.NextReviewAt < today);

            if (isCritical)
            {
                critical++;
                continue;
            }

            if (dpia.Status is DpiaStatus.InReview or DpiaStatus.Rejected or DpiaStatus.RevisionRequired)
            {
                warning++;
                continue;
            }

            if (dpia.Status == DpiaStatus.Approved)
            {
                good++;
                continue;
            }

            neutral++;
        }

        return new DashboardStatusGroupCounts(critical, warning, good, neutral);
    }

    private static async Task<DashboardStatusGroupCounts> ClassifyServiceProvidersAsync(
        ApplicationDbContext db,
        int tenantId,
        DateOnly today,
        CancellationToken ct)
    {
        var providers = await db.ServiceProviders
            .Where(s => s.TenantId == tenantId)
            .Select(s => new
            {
                s.IsDataProcessor,
                s.Status,
                s.DataProcessingAgreementExists,
                s.ThirdCountryInvolvement,
                s.RiskAssessment,
                s.DataProcessingAgreementReviewedAt
            })
            .ToListAsync(ct);

        var critical = 0;
        var warning = 0;
        var good = 0;
        var neutral = 0;

        foreach (var provider in providers)
        {
            var isActive = provider.Status is ServiceProviderStatus.Active or ServiceProviderStatus.Approved;

            var isCritical = provider.RiskAssessment is ServiceProviderRiskAssessment.High or ServiceProviderRiskAssessment.Critical
                || (provider.DataProcessingAgreementReviewedAt != null && provider.DataProcessingAgreementReviewedAt < today)
                || (provider.IsDataProcessor && !provider.DataProcessingAgreementExists && isActive);

            if (isCritical)
            {
                critical++;
                continue;
            }

            if (provider.ThirdCountryInvolvement)
            {
                warning++;
                continue;
            }

            if (isActive)
            {
                good++;
                continue;
            }

            neutral++;
        }

        return new DashboardStatusGroupCounts(critical, warning, good, neutral);
    }

    private static async Task<DashboardStatusGroupCounts> ClassifyPrivacyIncidentsAsync(
        ApplicationDbContext db,
        int tenantId,
        CancellationToken ct)
    {
        var incidents = await db.PrivacyIncidents
            .Where(i => i.TenantId == tenantId)
            .Select(i => new
            {
                i.Status,
                i.SupervisoryAuthorityNotificationRequired,
                i.RiskLevel,
                i.Severity
            })
            .ToListAsync(ct);

        var critical = 0;
        var warning = 0;
        var good = 0;
        var neutral = 0;

        foreach (var incident in incidents)
        {
            var isCritical = incident.SupervisoryAuthorityNotificationRequired == DecisionStatus.Yes
                || incident.RiskLevel == PrivacyIncidentRiskLevel.HighRisk
                || incident.Severity is PrivacyIncidentSeverity.High or PrivacyIncidentSeverity.Critical;

            if (isCritical)
            {
                critical++;
                continue;
            }

            if (incident.Status is PrivacyIncidentStatus.Draft
                or PrivacyIncidentStatus.InReview
                or PrivacyIncidentStatus.ActionsRunning
                or PrivacyIncidentStatus.Reported)
            {
                warning++;
                continue;
            }

            if (incident.Status == PrivacyIncidentStatus.Closed)
            {
                good++;
                continue;
            }

            neutral++;
        }

        return new DashboardStatusGroupCounts(critical, warning, good, neutral);
    }

    private static async Task<DashboardStatusGroupCounts> ClassifyMeasuresAsync(
        ApplicationDbContext db,
        int tenantId,
        DateOnly today,
        CancellationToken ct)
    {
        var measures = await db.Measures
            .Where(m => m.TenantId == tenantId)
            .Select(m => new { m.Status, m.DueDate })
            .ToListAsync(ct);

        var critical = 0;
        var warning = 0;
        var good = 0;
        var neutral = 0;

        foreach (var measure in measures)
        {
            var isOpen = measure.Status is MeasureStatus.Open or MeasureStatus.InProgress;
            var isCritical = isOpen && measure.DueDate != null && measure.DueDate < today;

            if (isCritical)
            {
                critical++;
                continue;
            }

            if (isOpen)
            {
                warning++;
                continue;
            }

            if (measure.Status == MeasureStatus.Done)
            {
                good++;
                continue;
            }

            neutral++;
        }

        return new DashboardStatusGroupCounts(critical, warning, good, neutral);
    }

    private static async Task<DashboardStatusGroupCounts> ClassifyAuditsAsync(
        ApplicationDbContext db,
        int tenantId,
        CancellationToken ct)
    {
        var audits = await db.AuditRuns
            .Where(r => r.TenantId == tenantId)
            .Select(r => r.Status)
            .ToListAsync(ct);

        var critical = 0;
        var warning = 0;
        var good = 0;
        var neutral = 0;

        foreach (var status in audits)
        {
            if (status == AuditRunStatus.Completed)
            {
                good++;
                continue;
            }

            if (status is AuditRunStatus.InProgress or AuditRunStatus.Draft)
            {
                warning++;
                continue;
            }

            neutral++;
        }

        return new DashboardStatusGroupCounts(critical, warning, good, neutral);
    }
}

/// <summary>Read-only-Snapshot der Dashboard-Daten für eine Anzeige.</summary>
public record DashboardSummary(
    int OpenMeasuresCount,
    int ActiveAuditsCount,
    int DraftAuditsCount,
    int TotalTomsCount,
    int PlannedTomsCount,
    int NotImplementedTomsCount,
    int ImplementedTomsCount,
    int OverdueTomReviewsCount,
    int TotalServiceProvidersCount,
    int ActiveDataProcessorsCount,
    int ProcessorsWithoutAvvCount,
    int ThirdCountryProvidersCount,
    int HighRiskProvidersCount,
    int OverdueAvvReviewsCount,
    int TotalProcessingActivitiesCount,
    int ProcessingActivitiesWithoutTomsCount,
    int ProcessingActivitiesWithoutDocumentsCount,
    int ProcessingActivitiesWithOpenMeasuresCount,
    int ProcessingActivitiesDpiaRequiredCount,
    int TotalDpiaAssessmentsCount,
    int DpiaInReviewCount,
    int DpiaHighOrCriticalRiskCount,
    int OverdueDpiaReviewsCount,
    int ProcessingActivitiesDpiaRequiredWithoutAssessmentCount,
    int ProcessingActivitiesWithHighRiskProvidersCount,
    int OpenPrivacyIncidentsCount,
    int PrivacyIncidentsInReviewCount,
    int NotificationRequiredPrivacyIncidentsCount,
    int HighRiskPrivacyIncidentsCount,
    int TotalMeasuresCount,
    int InProgressMeasuresCount,
    int CompletedMeasuresCount,
    int OverdueMeasuresCount,
    int TotalPrivacyIncidentsCount,
    int ClosedPrivacyIncidentsCount,
    int TotalAuditsCount,
    int CompletedAuditsCount,
    DashboardStatusGroupCounts ProcessingActivityStatusGroups,
    DashboardStatusGroupCounts TomStatusGroups,
    DashboardStatusGroupCounts DpiaStatusGroups,
    DashboardStatusGroupCounts ServiceProviderStatusGroups,
    DashboardStatusGroupCounts PrivacyIncidentStatusGroups,
    DashboardStatusGroupCounts MeasureStatusGroups,
    DashboardStatusGroupCounts AuditStatusGroups,
    IReadOnlyList<Domain.Entities.Measure> RecentMeasures,
    IReadOnlyList<Domain.Entities.AuditRun> RecentAudits);
