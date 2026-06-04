using Dsms.Web.Data;
using Dsms.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

/// <summary>
/// Aggregiert Kennzahlen und Listen für die Dashboard-Startseite – ausschließlich mandantenbezogen.
/// </summary>
public class DashboardService(ApplicationDbContext db)
{
    /// <summary>
    /// Liefert Zähler und Vorschau-Listen für einen Mandanten.
    /// Offene Maßnahmen schließen Done und Cancelled aus; aktive Audits sind nur InProgress.
    /// </summary>
    public async Task<DashboardSummary> GetSummaryAsync(int tenantId, CancellationToken ct = default)
    {
        var openMeasures = await db.Measures
            .Where(m => m.TenantId == tenantId && m.Status != MeasureStatus.Done && m.Status != MeasureStatus.Cancelled)
            .CountAsync(ct);

        var activeAudits = await db.AuditRuns
            .Where(r => r.TenantId == tenantId && r.Status == AuditRunStatus.InProgress)
            .CountAsync(ct);

        var draftAudits = await db.AuditRuns
            .Where(r => r.TenantId == tenantId && r.Status == AuditRunStatus.Draft)
            .CountAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var totalToms = await db.Toms
            .Where(t => t.TenantId == tenantId)
            .CountAsync(ct);

        var plannedToms = await db.Toms
            .Where(t => t.TenantId == tenantId && t.ImplementationStatus == TomImplementationStatus.Planned)
            .CountAsync(ct);

        var notImplementedToms = await db.Toms
            .Where(t => t.TenantId == tenantId && t.ImplementationStatus == TomImplementationStatus.NotImplemented)
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

        var activitiesWithHighRiskProviders = await db.ProcessingActivityServiceProviders
            .Where(l => l.TenantId == tenantId
                && (l.ServiceProvider.RiskAssessment == ServiceProviderRiskAssessment.High
                    || l.ServiceProvider.RiskAssessment == ServiceProviderRiskAssessment.Critical))
            .Select(l => l.ProcessingActivityId)
            .Distinct()
            .CountAsync(ct);

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
            activitiesWithHighRiskProviders,
            recentMeasures,
            recentAudits);
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
    int ProcessingActivitiesWithHighRiskProvidersCount,
    IReadOnlyList<Domain.Entities.Measure> RecentMeasures,
    IReadOnlyList<Domain.Entities.AuditRun> RecentAudits);
