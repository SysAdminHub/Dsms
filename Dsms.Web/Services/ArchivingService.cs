using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;
using ServiceProviderEntity = Dsms.Web.Domain.Entities.ServiceProvider;

namespace Dsms.Web.Services;

/// <summary>
/// Implementiert mandantensichere Archivierung und Wiederherstellung für alle IArchivable-Entities.
/// Abhängigkeitsprüfungen liefern Warnungen, blockieren aber nicht.
/// </summary>
public class ArchivingService(
    ApplicationDbContext db,
    ICurrentUserContext currentUser,
    IUserAccessService userAccess,
    IComplianceAuditLogService complianceAuditLog) : IArchivingService
{
    public async Task<ArchiveOperationResult> ArchiveAsync<TEntity>(int id, CancellationToken ct = default)
        where TEntity : ArchivableEntityBase, ITenantEntity
    {
        if (await userAccess.IsAuditorAsync())
        {
            return new ArchiveOperationResult(false, [], "Keine Berechtigung zum Archivieren.");
        }

        var tenantId = await currentUser.GetTenantIdAsync();
        if (tenantId is null)
        {
            return new ArchiveOperationResult(false, [], "Kein Mandant zugeordnet.");
        }

        var entity = await db.Set<TEntity>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId && !e.IsArchived, ct);
        if (entity is null)
        {
            return new ArchiveOperationResult(false, [], "Eintrag wurde nicht gefunden.");
        }

        var warnings = await GetDependencyWarningsAsync<TEntity>(id, ct);
        var userId = await currentUser.GetUserIdAsync();

        entity.IsArchived = true;
        entity.ArchivedAt = DateTime.UtcNow;
        entity.ArchivedByUserId = userId;
        entity.UpdatedAt = DateTime.UtcNow;
        if (entity is DataProtectionImpactAssessment dpia)
        {
            dpia.Status = DpiaStatus.Archived;
        }

        await db.SaveChangesAsync(ct);
        await LogArchiveAsync(entity, ct);
        return new ArchiveOperationResult(true, warnings);
    }

    public async Task<ArchiveOperationResult> RestoreAsync<TEntity>(int id, CancellationToken ct = default)
        where TEntity : ArchivableEntityBase, ITenantEntity
    {
        if (await userAccess.IsAuditorAsync())
        {
            return new ArchiveOperationResult(false, [], "Keine Berechtigung zum Wiederherstellen.");
        }

        var tenantId = await currentUser.GetTenantIdAsync();
        if (tenantId is null)
        {
            return new ArchiveOperationResult(false, [], "Kein Mandant zugeordnet.");
        }

        var entity = await db.Set<TEntity>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId && e.IsArchived, ct);
        if (entity is null)
        {
            return new ArchiveOperationResult(false, [], "Eintrag wurde nicht gefunden.");
        }

        entity.IsArchived = false;
        entity.ArchivedAt = null;
        entity.ArchivedByUserId = null;
        entity.UpdatedAt = DateTime.UtcNow;
        if (entity is DataProtectionImpactAssessment dpia && dpia.Status == DpiaStatus.Archived)
        {
            dpia.Status = DpiaStatus.Draft;
        }

        await db.SaveChangesAsync(ct);
        await LogRestoreAsync(entity, ct);
        return new ArchiveOperationResult(true, []);
    }

    private Task LogArchiveAsync<TEntity>(TEntity entity, CancellationToken ct)
        where TEntity : ArchivableEntityBase, ITenantEntity => entity switch
    {
        ProcessingActivity pa => complianceAuditLog.LogProcessingActivityArchivedAsync(pa.Id, pa.Name, pa.TenantId),
        DataProtectionImpactAssessment dpia => complianceAuditLog.LogDpiaArchivedAsync(dpia.Id, dpia.Title, dpia.TenantId),
        Tom tom => complianceAuditLog.LogTomArchivedAsync(tom.Id, tom.Title, tom.TenantId),
        ServiceProviderEntity sp => complianceAuditLog.LogProcessorArchivedAsync(sp.Id, sp.Name, sp.TenantId),
        AuditRun audit => complianceAuditLog.LogAuditArchivedAsync(audit.Id, audit.Title, audit.TenantId),
        Measure measure => complianceAuditLog.LogMeasureArchivedAsync(measure.Id, measure.Title, measure.TenantId),
        EvidenceDocument doc => complianceAuditLog.LogEvidenceDocumentArchivedAsync(doc.Id, doc.FileName, doc.TenantId),
        PrivacyIncident incident => complianceAuditLog.LogPrivacyIncidentArchivedAsync(incident.Id, incident.Title, incident.TenantId),
        _ => Task.CompletedTask
    };

    private Task LogRestoreAsync<TEntity>(TEntity entity, CancellationToken ct)
        where TEntity : ArchivableEntityBase, ITenantEntity => entity switch
    {
        ProcessingActivity pa => complianceAuditLog.LogProcessingActivityRestoredAsync(pa.Id, pa.Name, pa.TenantId),
        DataProtectionImpactAssessment dpia => complianceAuditLog.LogDpiaRestoredAsync(dpia.Id, dpia.Title, dpia.TenantId),
        Tom tom => complianceAuditLog.LogTomRestoredAsync(tom.Id, tom.Title, tom.TenantId),
        ServiceProviderEntity sp => complianceAuditLog.LogProcessorRestoredAsync(sp.Id, sp.Name, sp.TenantId),
        Measure measure => complianceAuditLog.LogMeasureRestoredAsync(measure.Id, measure.Title, measure.TenantId),
        EvidenceDocument doc => complianceAuditLog.LogEvidenceDocumentRestoredAsync(doc.Id, doc.FileName, doc.TenantId),
        PrivacyIncident incident => complianceAuditLog.LogPrivacyIncidentRestoredAsync(incident.Id, incident.Title, incident.TenantId),
        _ => Task.CompletedTask
    };

    public async Task<IReadOnlyList<string>> GetDependencyWarningsAsync<TEntity>(int id, CancellationToken ct = default)
        where TEntity : ArchivableEntityBase, ITenantEntity
    {
        return typeof(TEntity).Name switch
        {
            nameof(ProcessingActivity) => await GetProcessingActivityWarningsAsync(id, ct),
            nameof(DataProtectionImpactAssessment) => await GetDpiaWarningsAsync(id, ct),
            nameof(Tom) => await GetTomWarningsAsync(id, ct),
            nameof(Domain.Entities.ServiceProvider) => await GetServiceProviderWarningsAsync(id, ct),
            nameof(AuditTemplate) => await GetAuditTemplateWarningsAsync(id, ct),
            nameof(AuditRun) => await GetAuditRunWarningsAsync(id, ct),
            nameof(Measure) => await GetMeasureWarningsAsync(id, ct),
            nameof(EvidenceDocument) => [],
            nameof(PrivacyIncident) => await GetPrivacyIncidentWarningsAsync(id, ct),
            _ => []
        };
    }

    private async Task<IReadOnlyList<string>> GetProcessingActivityWarningsAsync(int id, CancellationToken ct)
    {
        var warnings = new List<string>();

        var tomCount = await db.ProcessingActivityToms.CountAsync(l => l.ProcessingActivityId == id, ct);
        if (tomCount > 0) warnings.Add($"{tomCount} verknüpfte TOM(s)");

        var measureCount = await db.ProcessingActivityMeasures.CountAsync(l => l.ProcessingActivityId == id, ct);
        if (measureCount > 0) warnings.Add($"{measureCount} verknüpfte Maßnahme(n)");

        var dpiaCount = await db.DataProtectionImpactAssessments.CountAsync(d => d.ProcessingActivityId == id, ct);
        if (dpiaCount > 0) warnings.Add($"{dpiaCount} DSFA-Einträge");

        var spCount = await db.ProcessingActivityServiceProviders.CountAsync(l => l.ProcessingActivityId == id, ct);
        if (spCount > 0) warnings.Add($"{spCount} verknüpfte Dienstleister");

        var docCount = await db.EvidenceDocuments.CountAsync(d => d.ProcessingActivityId == id, ct);
        if (docCount > 0) warnings.Add($"{docCount} verknüpfte Dokument(e)");

        var auditCount = await db.ProcessingActivityAuditAnswers.CountAsync(l => l.ProcessingActivityId == id, ct);
        if (auditCount > 0) warnings.Add($"{auditCount} Audit-Bezüge");

        return warnings;
    }

    private async Task<IReadOnlyList<string>> GetDpiaWarningsAsync(int id, CancellationToken ct)
    {
        var docCount = await db.EvidenceDocuments.CountAsync(d => d.DataProtectionImpactAssessmentId == id, ct);
        return docCount > 0 ? [$"{docCount} verknüpfte Dokument(e)"] : [];
    }

    private async Task<IReadOnlyList<string>> GetTomWarningsAsync(int id, CancellationToken ct)
    {
        var warnings = new List<string>();

        var paCount = await db.ProcessingActivityToms.CountAsync(l => l.TomId == id, ct);
        if (paCount > 0) warnings.Add($"{paCount} verknüpfte Verarbeitungstätigkeit(en)");

        var spCount = await db.ServiceProviderToms.CountAsync(l => l.TomId == id, ct);
        if (spCount > 0) warnings.Add($"{spCount} verknüpfte Dienstleister");

        return warnings;
    }

    private async Task<IReadOnlyList<string>> GetServiceProviderWarningsAsync(int id, CancellationToken ct)
    {
        var warnings = new List<string>();

        var paCount = await db.ProcessingActivityServiceProviders.CountAsync(l => l.ServiceProviderId == id, ct);
        if (paCount > 0) warnings.Add($"{paCount} verknüpfte Verarbeitungstätigkeit(en)");

        var tomCount = await db.ServiceProviderToms.CountAsync(l => l.ServiceProviderId == id, ct);
        if (tomCount > 0) warnings.Add($"{tomCount} verknüpfte TOM(s)");

        var docCount = await db.EvidenceDocuments.CountAsync(d => d.ServiceProviderId == id, ct);
        if (docCount > 0) warnings.Add($"{docCount} verknüpfte Dokument(e)");

        return warnings;
    }

    private async Task<IReadOnlyList<string>> GetAuditTemplateWarningsAsync(int id, CancellationToken ct)
    {
        var runCount = await db.AuditRuns.CountAsync(r => r.AuditTemplateId == id, ct);
        return runCount > 0 ? [$"{runCount} Audit-Durchlauf/Durchläufe basieren auf dieser Vorlage"] : [];
    }

    private async Task<IReadOnlyList<string>> GetAuditRunWarningsAsync(int id, CancellationToken ct)
    {
        var warnings = new List<string>();

        var measureCount = await db.Measures.CountAsync(m => m.AuditRunId == id, ct);
        if (measureCount > 0) warnings.Add($"{measureCount} verknüpfte Maßnahme(n)");

        var docCount = await db.EvidenceDocuments.CountAsync(d => d.AuditRunId == id, ct);
        if (docCount > 0) warnings.Add($"{docCount} verknüpfte Dokument(e)");

        var answerCount = await db.AuditAnswers.CountAsync(a => a.AuditRunId == id, ct);
        if (answerCount > 0) warnings.Add($"{answerCount} Audit-Antwort(en)");

        return warnings;
    }

    private async Task<IReadOnlyList<string>> GetPrivacyIncidentWarningsAsync(int id, CancellationToken ct)
    {
        var warnings = new List<string>();

        var paCount = await db.PrivacyIncidentProcessingActivities.CountAsync(l => l.PrivacyIncidentId == id, ct);
        if (paCount > 0) warnings.Add($"{paCount} verknüpfte Verarbeitungstätigkeit(en)");

        var spCount = await db.PrivacyIncidentServiceProviders.CountAsync(l => l.PrivacyIncidentId == id, ct);
        if (spCount > 0) warnings.Add($"{spCount} verknüpfte Dienstleister");

        var measureCount = await db.PrivacyIncidentMeasures.CountAsync(l => l.PrivacyIncidentId == id, ct);
        if (measureCount > 0) warnings.Add($"{measureCount} verknüpfte Maßnahme(n)");

        var tomCount = await db.PrivacyIncidentToms.CountAsync(l => l.PrivacyIncidentId == id, ct);
        if (tomCount > 0) warnings.Add($"{tomCount} verknüpfte TOM(s)");

        var docCount = await db.EvidenceDocuments.CountAsync(d => d.PrivacyIncidentId == id, ct);
        if (docCount > 0) warnings.Add($"{docCount} verknüpfte Dokument(e)");

        return warnings;
    }

    private async Task<IReadOnlyList<string>> GetMeasureWarningsAsync(int id, CancellationToken ct)
    {
        var warnings = new List<string>();

        var paCount = await db.ProcessingActivityMeasures.CountAsync(l => l.MeasureId == id, ct);
        if (paCount > 0) warnings.Add($"{paCount} verknüpfte Verarbeitungstätigkeit(en)");

        var docCount = await db.EvidenceDocuments.CountAsync(d => d.MeasureId == id, ct);
        if (docCount > 0) warnings.Add($"{docCount} verknüpfte Dokument(e)");

        return warnings;
    }
}
