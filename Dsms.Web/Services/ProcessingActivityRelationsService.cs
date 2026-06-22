using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

/// <summary>
/// Lädt Verknüpfungen einer Verarbeitungstätigkeit und berechnet einfache Warnhinweise – mandantenbezogen.
/// </summary>
public class ProcessingActivityRelationsService(ApplicationDbContext db)
{
    /// <summary>
    /// Prüft, ob die Verarbeitungstätigkeit zum Mandanten gehört.
    /// </summary>
    public Task<bool> ExistsForTenantAsync(int tenantId, int processingActivityId, CancellationToken ct = default) =>
        db.ProcessingActivities.AnyAsync(p => p.Id == processingActivityId && p.TenantId == tenantId, ct);

    /// <summary>
    /// Aggregiert alle Verknüpfungsdaten und Warnhinweise für die Detailansicht.
    /// </summary>
    public async Task<ProcessingActivityRelationsSnapshot?> GetSnapshotAsync(
        int tenantId,
        int processingActivityId,
        CancellationToken ct = default)
    {
        var activity = await db.ProcessingActivities
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == processingActivityId && p.TenantId == tenantId, ct);

        if (activity is null)
        {
            return null;
        }

        var toms = await db.Toms
            .AsNoTracking()
            .Include(t => t.TomCategory)
            .Where(t => db.ProcessingActivityToms
                .Any(l => l.TomId == t.Id
                    && l.ProcessingActivityId == processingActivityId
                    && l.TenantId == tenantId))
            .OrderBy(t => t.Title)
            .ToListAsync(ct);

        var serviceProviderLinks = await db.ProcessingActivityServiceProviders
            .AsNoTracking()
            .Where(l => l.ProcessingActivityId == processingActivityId && l.TenantId == tenantId)
            .Include(l => l.ServiceProvider)
            .OrderBy(l => l.ServiceProvider.Name)
            .ToListAsync(ct);

        var documents = await LoadLinkedDocumentsAsync(tenantId, processingActivityId, ct);

        var measureLinks = await db.ProcessingActivityMeasures
            .AsNoTracking()
            .Where(l => l.ProcessingActivityId == processingActivityId && l.TenantId == tenantId)
            .Include(l => l.Measure)
            .OrderBy(l => l.Measure.DueDate)
            .ThenBy(l => l.Measure.Title)
            .Select(l => l.Measure)
            .ToListAsync(ct);

        var dpiaAssessments = await db.DataProtectionImpactAssessments
            .AsNoTracking()
            .Where(d => d.ProcessingActivityId == processingActivityId && d.TenantId == tenantId)
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .ToListAsync(ct);

        var auditAnswerRows = await db.ProcessingActivityAuditAnswers
            .AsNoTracking()
            .Where(l => l.ProcessingActivityId == processingActivityId && l.TenantId == tenantId)
            .Include(l => l.AuditAnswer)
                .ThenInclude(a => a.AuditQuestion)
            .Include(l => l.AuditAnswer)
                .ThenInclude(a => a.AuditRun)
            .OrderBy(l => l.AuditAnswer.AuditRun.Title)
            .ThenBy(l => !string.IsNullOrEmpty(l.AuditAnswer.QuestionText)
                ? l.AuditAnswer.QuestionSortOrder
                : l.AuditAnswer.AuditQuestion.SortOrder)
            .Select(l => new LinkedAuditAnswerRow(
                l.AuditAnswerId,
                l.AuditAnswer.AuditRunId,
                l.AuditAnswer.AuditRun.Title,
                !string.IsNullOrEmpty(l.AuditAnswer.QuestionText)
                    ? l.AuditAnswer.QuestionSortOrder
                    : l.AuditAnswer.AuditQuestion.SortOrder,
                !string.IsNullOrEmpty(l.AuditAnswer.QuestionText)
                    ? l.AuditAnswer.QuestionText!
                    : l.AuditAnswer.AuditQuestion.Text,
                l.AuditAnswer.AnswerText,
                l.AuditAnswer.ComplianceLevel,
                l.AuditAnswer.Notes))
            .ToListAsync(ct);

        var warnings = BuildWarnings(activity, toms, serviceProviderLinks, documents, measureLinks, auditAnswerRows, dpiaAssessments);

        return new ProcessingActivityRelationsSnapshot(
            activity,
            toms,
            serviceProviderLinks,
            documents,
            measureLinks,
            auditAnswerRows,
            dpiaAssessments,
            warnings);
    }

    /// <summary>
    /// Einfache Compliance-Hinweise ohne Risiko-Engine.
    /// </summary>
    public static IReadOnlyList<string> BuildWarnings(
        ProcessingActivity activity,
        IReadOnlyList<Tom> toms,
        IReadOnlyList<ProcessingActivityServiceProvider> serviceProviderLinks,
        IReadOnlyList<EvidenceDocument> documents,
        IReadOnlyList<Measure> measures,
        IReadOnlyList<LinkedAuditAnswerRow> auditAnswers,
        IReadOnlyList<DataProtectionImpactAssessment> dpiaAssessments)
    {
        var warnings = new List<string>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (toms.Count == 0)
        {
            warnings.Add("Keine TOMs zugeordnet.");
        }

        if (documents.Count == 0)
        {
            warnings.Add("Keine Dokumente zugeordnet.");
        }

        foreach (var link in serviceProviderLinks)
        {
            var sp = link.ServiceProvider;
            if (ServiceProviderLabels.ShouldWarnMissingAvv(sp))
            {
                warnings.Add($"Auftragsverarbeiter ohne AVV: {sp.Name}.");
            }

            if (sp.ThirdCountryInvolvement)
            {
                warnings.Add($"Dienstleister mit Drittlandbezug: {sp.Name}.");
            }

            if (sp.RiskAssessment is ServiceProviderRiskAssessment.High or ServiceProviderRiskAssessment.Critical)
            {
                warnings.Add($"Dienstleister mit Risiko „{ServiceProviderLabels.GetRiskLabel(sp.RiskAssessment)}“: {sp.Name}.");
            }

            if (sp.IsDataProcessor && sp.DataProcessingAgreementExists
                && ServiceProviderLabels.IsAvvReviewOverdue(sp.DataProcessingAgreementReviewedAt))
            {
                warnings.Add($"Überfällige AVV-Prüfung beim Dienstleister: {sp.Name}.");
            }
        }

        var openMeasures = measures.Where(m => MeasureLabels.IsOpen(m.Status)).ToList();
        if (openMeasures.Count > 0)
        {
            warnings.Add($"{openMeasures.Count} offene Maßnahme(n) zugeordnet.");
        }

        var overdueMeasures = measures.Where(m =>
            MeasureLabels.IsOpen(m.Status) && m.DueDate.HasValue && m.DueDate.Value < today).ToList();
        if (overdueMeasures.Count > 0)
        {
            warnings.Add($"{overdueMeasures.Count} überfällige Maßnahme(n) zugeordnet.");
        }

        var measuresWithoutOwner = measures.Where(m =>
            MeasureLabels.IsOpen(m.Status) && string.IsNullOrWhiteSpace(m.AssignedUserId)).ToList();
        if (measuresWithoutOwner.Count > 0)
        {
            warnings.Add($"{measuresWithoutOwner.Count} offene Maßnahme(n) ohne verantwortliche Person.");
        }

        foreach (var row in auditAnswers.Where(a => ComplianceLabels.IsProblematic(a.ComplianceLevel)))
        {
            warnings.Add($"Audit-Antwort „{ComplianceLabels.GetLevelLabel(row.ComplianceLevel)}“: {row.AuditRunTitle} – Frage {row.QuestionSortOrder}.");
        }

        warnings.AddRange(DsfaLabels.BuildProcessingActivityWarnings(activity, dpiaAssessments));

        return warnings;
    }

    /// <summary>
    /// Speichert Verknüpfungen aus der Bearbeitungsseite; alle IDs werden mandantenseitig validiert.
    /// </summary>
    public async Task<SaveLinksResult> SaveLinksAsync(
        int tenantId,
        int processingActivityId,
        IReadOnlyCollection<int> tomIds,
        IReadOnlyCollection<int> serviceProviderIds,
        IReadOnlyCollection<int> documentIds,
        IReadOnlyCollection<int> measureIds,
        IReadOnlyCollection<int> auditAnswerIds,
        CancellationToken ct = default)
    {
        if (!await ExistsForTenantAsync(tenantId, processingActivityId, ct))
        {
            return SaveLinksResult.NotFound;
        }

        var validTomIds = await db.Toms
            .Where(t => t.TenantId == tenantId && tomIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync(ct);
        if (validTomIds.Count != tomIds.Count)
        {
            return SaveLinksResult.InvalidTenantReference;
        }

        var validServiceProviderIds = await db.ServiceProviders
            .Where(s => s.TenantId == tenantId && serviceProviderIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(ct);
        if (validServiceProviderIds.Count != serviceProviderIds.Count)
        {
            return SaveLinksResult.InvalidTenantReference;
        }

        var validDocumentIds = await db.EvidenceDocuments
            .Where(d => d.TenantId == tenantId && documentIds.Contains(d.Id))
            .Select(d => d.Id)
            .ToListAsync(ct);
        if (validDocumentIds.Count != documentIds.Count)
        {
            return SaveLinksResult.InvalidTenantReference;
        }

        var validMeasureIds = await db.Measures
            .Where(m => m.TenantId == tenantId && measureIds.Contains(m.Id))
            .Select(m => m.Id)
            .ToListAsync(ct);
        if (validMeasureIds.Count != measureIds.Count)
        {
            return SaveLinksResult.InvalidTenantReference;
        }

        // Audit-Antworten müssen zu einem Audit-Durchlauf des Mandanten gehören.
        var validAuditAnswerIds = await db.AuditAnswers
            .Where(a => auditAnswerIds.Contains(a.Id) && a.AuditRun.TenantId == tenantId)
            .Select(a => a.Id)
            .ToListAsync(ct);
        if (validAuditAnswerIds.Count != auditAnswerIds.Count)
        {
            return SaveLinksResult.InvalidTenantReference;
        }

        await SyncTomLinksAsync(tenantId, processingActivityId, validTomIds, ct);
        await SyncServiceProviderLinksAsync(tenantId, processingActivityId, validServiceProviderIds, ct);
        await SyncDocumentLinksAsync(tenantId, processingActivityId, validDocumentIds, ct);
        await SyncMeasureLinksAsync(tenantId, processingActivityId, validMeasureIds, ct);
        await SyncAuditAnswerLinksAsync(tenantId, processingActivityId, validAuditAnswerIds, ct);

        await db.SaveChangesAsync(ct);
        return SaveLinksResult.Success;
    }

    private async Task SyncTomLinksAsync(int tenantId, int processingActivityId, List<int> targetIds, CancellationToken ct)
    {
        var existing = await db.ProcessingActivityToms
            .Where(l => l.ProcessingActivityId == processingActivityId && l.TenantId == tenantId)
            .ToListAsync(ct);

        var toRemove = existing.Where(l => !targetIds.Contains(l.TomId)).ToList();
        if (toRemove.Count > 0)
        {
            db.ProcessingActivityToms.RemoveRange(toRemove);
        }

        var existingTomIds = existing.Select(l => l.TomId).ToHashSet();
        foreach (var tomId in targetIds.Where(id => !existingTomIds.Contains(id)))
        {
            db.ProcessingActivityToms.Add(new ProcessingActivityTom
            {
                TenantId = tenantId,
                ProcessingActivityId = processingActivityId,
                TomId = tomId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private async Task SyncServiceProviderLinksAsync(int tenantId, int processingActivityId, List<int> targetIds, CancellationToken ct)
    {
        var existing = await db.ProcessingActivityServiceProviders
            .Where(l => l.ProcessingActivityId == processingActivityId && l.TenantId == tenantId)
            .ToListAsync(ct);

        var toRemove = existing.Where(l => !targetIds.Contains(l.ServiceProviderId)).ToList();
        if (toRemove.Count > 0)
        {
            db.ProcessingActivityServiceProviders.RemoveRange(toRemove);
        }

        var existingIds = existing.Select(l => l.ServiceProviderId).ToHashSet();
        foreach (var spId in targetIds.Where(id => !existingIds.Contains(id)))
        {
            db.ProcessingActivityServiceProviders.Add(new ProcessingActivityServiceProvider
            {
                TenantId = tenantId,
                ProcessingActivityId = processingActivityId,
                ServiceProviderId = spId,
                RoleInProcessing = ProcessingRole.DataProcessor,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Synchronisiert DocumentLinks für eine Verarbeitungstätigkeit; hebt Zuordnungen für abgewählte Dokumente auf.
    /// </summary>
    private async Task SyncDocumentLinksAsync(int tenantId, int processingActivityId, List<int> targetIds, CancellationToken ct)
    {
        var currentlyLinked = await db.DocumentLinks
            .Where(l => l.TenantId == tenantId
                && l.LinkedEntityType == DocumentLinkedEntityType.ProcessingActivity
                && l.LinkedEntityId == processingActivityId)
            .ToListAsync(ct);

        var toRemove = currentlyLinked.Where(l => !targetIds.Contains(l.DocumentId)).ToList();
        if (toRemove.Count > 0)
        {
            db.DocumentLinks.RemoveRange(toRemove);
        }

        var existingDocIds = currentlyLinked.Select(l => l.DocumentId).ToHashSet();
        foreach (var docId in targetIds.Where(id => !existingDocIds.Contains(id)))
        {
            db.DocumentLinks.Add(new DocumentLink
            {
                TenantId = tenantId,
                DocumentId = docId,
                LinkedEntityType = DocumentLinkedEntityType.ProcessingActivity,
                LinkedEntityId = processingActivityId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private async Task<List<EvidenceDocument>> LoadLinkedDocumentsAsync(
        int tenantId,
        int processingActivityId,
        CancellationToken ct)
    {
        var documentIds = await db.DocumentLinks
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId
                && l.LinkedEntityType == DocumentLinkedEntityType.ProcessingActivity
                && l.LinkedEntityId == processingActivityId)
            .Select(l => l.DocumentId)
            .ToListAsync(ct);

        if (documentIds.Count == 0)
        {
            return [];
        }

        return await db.EvidenceDocuments
            .AsNoTracking()
            .Include(d => d.DocumentCategory)
            .Where(d => documentIds.Contains(d.Id))
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
    }

    private async Task SyncMeasureLinksAsync(int tenantId, int processingActivityId, List<int> targetIds, CancellationToken ct)
    {
        var existing = await db.ProcessingActivityMeasures
            .Where(l => l.ProcessingActivityId == processingActivityId && l.TenantId == tenantId)
            .ToListAsync(ct);

        var toRemove = existing.Where(l => !targetIds.Contains(l.MeasureId)).ToList();
        if (toRemove.Count > 0)
        {
            db.ProcessingActivityMeasures.RemoveRange(toRemove);
        }

        var existingIds = existing.Select(l => l.MeasureId).ToHashSet();
        foreach (var measureId in targetIds.Where(id => !existingIds.Contains(id)))
        {
            db.ProcessingActivityMeasures.Add(new ProcessingActivityMeasure
            {
                TenantId = tenantId,
                ProcessingActivityId = processingActivityId,
                MeasureId = measureId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private async Task SyncAuditAnswerLinksAsync(int tenantId, int processingActivityId, List<int> targetIds, CancellationToken ct)
    {
        var existing = await db.ProcessingActivityAuditAnswers
            .Where(l => l.ProcessingActivityId == processingActivityId && l.TenantId == tenantId)
            .ToListAsync(ct);

        var toRemove = existing.Where(l => !targetIds.Contains(l.AuditAnswerId)).ToList();
        if (toRemove.Count > 0)
        {
            db.ProcessingActivityAuditAnswers.RemoveRange(toRemove);
        }

        var existingIds = existing.Select(l => l.AuditAnswerId).ToHashSet();
        foreach (var answerId in targetIds.Where(id => !existingIds.Contains(id)))
        {
            db.ProcessingActivityAuditAnswers.Add(new ProcessingActivityAuditAnswer
            {
                TenantId = tenantId,
                ProcessingActivityId = processingActivityId,
                AuditAnswerId = answerId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Synchronisiert Maßnahmen-Verknüpfungen von der Maßnahmen-Bearbeitungsmaske (nur eine Maßnahme).
    /// </summary>
    public async Task<SaveLinksResult> SaveMeasureProcessingActivitiesAsync(
        int tenantId,
        int measureId,
        IReadOnlyCollection<int> processingActivityIds,
        CancellationToken ct = default)
    {
        var measure = await db.Measures.FirstOrDefaultAsync(m => m.Id == measureId && m.TenantId == tenantId, ct);
        if (measure is null)
        {
            return SaveLinksResult.NotFound;
        }

        var validIds = await db.ProcessingActivities
            .Where(p => p.TenantId == tenantId && processingActivityIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(ct);

        if (validIds.Count != processingActivityIds.Count)
        {
            return SaveLinksResult.InvalidTenantReference;
        }

        var existing = await db.ProcessingActivityMeasures
            .Where(l => l.MeasureId == measureId && l.TenantId == tenantId)
            .ToListAsync(ct);

        var toRemove = existing.Where(l => !validIds.Contains(l.ProcessingActivityId)).ToList();
        if (toRemove.Count > 0)
        {
            db.ProcessingActivityMeasures.RemoveRange(toRemove);
        }

        var existingPaIds = existing.Select(l => l.ProcessingActivityId).ToHashSet();
        foreach (var paId in validIds.Where(id => !existingPaIds.Contains(id)))
        {
            db.ProcessingActivityMeasures.Add(new ProcessingActivityMeasure
            {
                TenantId = tenantId,
                ProcessingActivityId = paId,
                MeasureId = measureId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
        return SaveLinksResult.Success;
    }
}

public enum SaveLinksResult
{
    Success,
    NotFound,
    InvalidTenantReference
}

public record LinkedAuditAnswerRow(
    int AuditAnswerId,
    int AuditRunId,
    string AuditRunTitle,
    int QuestionSortOrder,
    string QuestionText,
    string? AnswerText,
    ComplianceLevel ComplianceLevel,
    string? Notes);

public record ProcessingActivityRelationsSnapshot(
    ProcessingActivity Activity,
    IReadOnlyList<Tom> Toms,
    IReadOnlyList<ProcessingActivityServiceProvider> ServiceProviderLinks,
    IReadOnlyList<EvidenceDocument> Documents,
    IReadOnlyList<Measure> Measures,
    IReadOnlyList<LinkedAuditAnswerRow> AuditAnswers,
    IReadOnlyList<DataProtectionImpactAssessment> DpiaAssessments,
    IReadOnlyList<string> Warnings);
