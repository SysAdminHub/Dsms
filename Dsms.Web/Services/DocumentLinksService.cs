using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

public enum UpdateDocumentLinksResult
{
    Success,
    NotFound,
    InvalidTenantReference,
    PermissionDenied,
    Failed
}

/// <summary>
/// Verwaltet Many-to-Many-Verknüpfungen zwischen Nachweisdokumenten und Fachobjekten.
/// </summary>
public class DocumentLinksService(
    ApplicationDbContext db,
    IUserAccessService userAccess,
    ICurrentUserContext currentUser,
    Logging.IComplianceAuditLogService complianceAuditLog)
{
    public async Task<IReadOnlyList<DocumentLink>> GetLinksForDocumentAsync(
        int documentId,
        int tenantId,
        CancellationToken ct = default) =>
        await db.DocumentLinks
            .AsNoTracking()
            .Where(l => l.DocumentId == documentId && l.TenantId == tenantId)
            .OrderBy(l => l.LinkedEntityType)
            .ThenBy(l => l.LinkedEntityId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EvidenceDocument>> GetDocumentsForEntityAsync(
        DocumentLinkedEntityType entityType,
        int entityId,
        int tenantId,
        CancellationToken ct = default)
    {
        var documentIds = await db.DocumentLinks
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId
                && l.LinkedEntityType == entityType
                && l.LinkedEntityId == entityId)
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

    public async Task<DocumentLinkSummaryViewModel> GetLinkSummaryForDocumentAsync(
        int documentId,
        int tenantId,
        CancellationToken ct = default)
    {
        var summaries = await GetLinkSummariesForDocumentsAsync(tenantId, [documentId], ct);
        return summaries.GetValueOrDefault(documentId) ?? EmptySummary();
    }

    public async Task<IReadOnlyDictionary<int, DocumentLinkSummaryViewModel>> GetLinkSummariesForDocumentsAsync(
        int tenantId,
        IReadOnlyList<int> documentIds,
        CancellationToken ct = default)
    {
        if (documentIds.Count == 0)
        {
            return new Dictionary<int, DocumentLinkSummaryViewModel>();
        }

        var links = await db.DocumentLinks
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId && documentIds.Contains(l.DocumentId))
            .ToListAsync(ct);

        if (links.Count == 0)
        {
            return documentIds.ToDictionary(id => id, _ => EmptySummary());
        }

        var displayNames = await ResolveDisplayNamesAsync(tenantId, links, ct);
        var result = new Dictionary<int, DocumentLinkSummaryViewModel>();

        foreach (var group in links.GroupBy(l => l.DocumentId))
        {
            var detailLines = group
                .Select(l =>
                {
                    var key = (l.LinkedEntityType, l.LinkedEntityId);
                    var name = displayNames.GetValueOrDefault(key) ?? $"#{l.LinkedEntityId}";
                    return $"{DocumentLinkLabels.GetTypeLabelSingular(l.LinkedEntityType)}: {name}";
                })
                .OrderBy(x => x)
                .ToList();

            var countsByType = group
                .GroupBy(l => l.LinkedEntityType)
                .ToDictionary(g => g.Key, g => g.Count());

            result[group.Key] = BuildSummary(group.Count(), countsByType, detailLines);
        }

        foreach (var id in documentIds.Where(id => !result.ContainsKey(id)))
        {
            result[id] = EmptySummary();
        }

        return result;
    }

    public async Task<IReadOnlyList<DocumentLinkSelectionViewModel>> GetAvailableLinkTargetsForTypeAsync(
        int tenantId,
        DocumentLinkedEntityType entityType,
        IReadOnlyCollection<DocumentLinkTarget>? currentlySelected = null,
        CancellationToken ct = default)
    {
        currentlySelected ??= [];
        return await LoadTargetsForTypeAsync(
            tenantId,
            entityType,
            currentlySelected.ToHashSet(),
            ct);
    }

    public async Task<IReadOnlyList<DocumentLinkSelectionViewModel>> GetAvailableLinkTargetsAsync(
        int tenantId,
        IReadOnlyCollection<DocumentLinkTarget>? currentlySelected = null,
        CancellationToken ct = default)
    {
        currentlySelected ??= [];
        var selectedSet = currentlySelected.ToHashSet();
        var items = new List<DocumentLinkSelectionViewModel>();

        foreach (var type in DocumentLinkLabels.UiTypes)
        {
            items.AddRange(await LoadTargetsForTypeAsync(tenantId, type, selectedSet, ct));
        }

        return items
            .OrderBy(i => DocumentLinkLabels.GetTypeSortOrder(i.EntityType))
            .ThenBy(i => i.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<DocumentLinkSelectionViewModel>> GetSelectionViewModelsForDocumentAsync(
        int documentId,
        int tenantId,
        CancellationToken ct = default)
    {
        var links = await GetLinksForDocumentAsync(documentId, tenantId, ct);
        if (links.Count == 0)
        {
            return [];
        }

        var displayNames = await ResolveDisplayNamesAsync(tenantId, links, ct);
        return links
            .Select(l => new DocumentLinkSelectionViewModel
            {
                EntityType = l.LinkedEntityType,
                EntityId = l.LinkedEntityId,
                DisplayName = displayNames.GetValueOrDefault((l.LinkedEntityType, l.LinkedEntityId), $"#{l.LinkedEntityId}"),
                IsSelected = true
            })
            .OrderBy(i => DocumentLinkLabels.GetTypeSortOrder(i.EntityType))
            .ThenBy(i => i.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<(bool IsValid, string? ErrorMessage)> ValidateLinkTargetAsync(
        DocumentLinkedEntityType entityType,
        int entityId,
        int tenantId,
        CancellationToken ct = default)
    {
        var exists = await EntityExistsInTenantAsync(entityType, entityId, tenantId, ct);
        return exists
            ? (true, null)
            : (false, $"Das gewählte Objekt ({DocumentLinkLabels.GetTypeLabelSingular(entityType)}) gehört nicht zu Ihrem Mandanten oder existiert nicht.");
    }

    public async Task<(UpdateDocumentLinksResult Result, string? ErrorMessage)> SetLinksForDocumentAsync(
        int documentId,
        int tenantId,
        IReadOnlyCollection<DocumentLinkTarget> selectedLinks,
        CancellationToken ct = default)
    {
        if (!await userAccess.CanEditTenantOperationalContentAsync())
        {
            return (UpdateDocumentLinksResult.PermissionDenied, "Keine Berechtigung zum Bearbeiten von Dokumentbezügen.");
        }

        var document = await db.EvidenceDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId, ct);

        if (document is null)
        {
            return (UpdateDocumentLinksResult.NotFound, "Dokument nicht gefunden.");
        }

        var normalized = selectedLinks
            .Distinct()
            .ToList();

        foreach (var target in normalized)
        {
            var (valid, error) = await ValidateLinkTargetAsync(target.EntityType, target.EntityId, tenantId, ct);
            if (!valid)
            {
                return (UpdateDocumentLinksResult.InvalidTenantReference, error);
            }
        }

        var existing = await db.DocumentLinks
            .Where(l => l.DocumentId == documentId && l.TenantId == tenantId)
            .ToListAsync(ct);

        var desired = normalized.ToHashSet();
        var existingKeys = existing
            .Select(l => new DocumentLinkTarget(l.LinkedEntityType, l.LinkedEntityId))
            .ToHashSet();

        var toRemove = existing
            .Where(l => !desired.Contains(new DocumentLinkTarget(l.LinkedEntityType, l.LinkedEntityId)))
            .ToList();

        if (toRemove.Count > 0)
        {
            db.DocumentLinks.RemoveRange(toRemove);
        }

        var userId = await currentUser.GetUserIdAsync();
        foreach (var target in desired.Where(t => !existingKeys.Contains(t)))
        {
            db.DocumentLinks.Add(new DocumentLink
            {
                TenantId = tenantId,
                DocumentId = documentId,
                LinkedEntityType = target.EntityType,
                LinkedEntityId = target.EntityId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            });
        }

        document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await LogDocumentLinkChangesAsync(
            tenantId,
            documentId,
            document.FileName,
            toRemove,
            desired.Where(t => !existingKeys.Contains(t)).ToList(),
            ct);

        return (UpdateDocumentLinksResult.Success, null);
    }

    private async Task LogDocumentLinkChangesAsync(
        int tenantId,
        int documentId,
        string fileName,
        IReadOnlyList<DocumentLink> removed,
        IReadOnlyList<DocumentLinkTarget> added,
        CancellationToken ct)
    {
        foreach (var link in removed)
        {
            if (link.LinkedEntityType == DocumentLinkedEntityType.Training)
                continue;

            await complianceAuditLog.LogDocumentUnlinkedAsync(
                documentId,
                fileName,
                tenantId,
                AuditLogPresentationHelper.ResolveDocumentLinkEntityLabel(link.LinkedEntityType),
                link.LinkedEntityId);
        }

        foreach (var target in added)
        {
            if (target.EntityType == DocumentLinkedEntityType.Training)
                continue;

            await complianceAuditLog.LogDocumentLinkedAsync(
                documentId,
                fileName,
                tenantId,
                AuditLogPresentationHelper.ResolveDocumentLinkEntityLabel(target.EntityType),
                target.EntityId);
        }

        await LogTrainingProofChangesAsync(tenantId, documentId, fileName, removed, added, ct);
    }

    private async Task LogTrainingProofChangesAsync(
        int tenantId,
        int documentId,
        string fileName,
        IReadOnlyList<DocumentLink> removed,
        IReadOnlyList<DocumentLinkTarget> added,
        CancellationToken ct)
    {
        var removedTrainingIds = removed
            .Where(l => l.LinkedEntityType == DocumentLinkedEntityType.Training)
            .Select(l => l.LinkedEntityId)
            .ToList();

        var addedTrainingIds = added
            .Where(t => t.EntityType == DocumentLinkedEntityType.Training)
            .Select(t => t.EntityId)
            .ToList();

        if (removedTrainingIds.Count == 0 && addedTrainingIds.Count == 0)
            return;

        var allIds = removedTrainingIds.Concat(addedTrainingIds).Distinct().ToList();
        var titles = await db.Trainings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId && allIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Title })
            .ToDictionaryAsync(t => t.Id, t => t.Title, ct);

        foreach (var id in addedTrainingIds)
        {
            if (titles.TryGetValue(id, out var title))
            {
                await complianceAuditLog.LogTrainingProofLinkedAsync(id, title, tenantId, documentId, fileName);
            }
        }

        foreach (var id in removedTrainingIds)
        {
            if (titles.TryGetValue(id, out var title))
            {
                await complianceAuditLog.LogTrainingProofRemovedAsync(id, title, tenantId, documentId, fileName);
            }
        }
    }

    public async Task<(UpdateDocumentLinksResult Result, string? ErrorMessage)> AddLinkAsync(
        int documentId,
        int tenantId,
        DocumentLinkTarget target,
        CancellationToken ct = default) =>
        await SetLinksForDocumentAsync(
            documentId,
            tenantId,
            (await GetLinksForDocumentAsync(documentId, tenantId, ct))
                .Select(l => new DocumentLinkTarget(l.LinkedEntityType, l.LinkedEntityId))
                .Append(target)
                .ToList(),
            ct);

    public async Task<(UpdateDocumentLinksResult Result, string? ErrorMessage)> RemoveLinkAsync(
        int documentId,
        int tenantId,
        DocumentLinkTarget target,
        CancellationToken ct = default)
    {
        if (!await userAccess.CanEditTenantOperationalContentAsync())
        {
            return (UpdateDocumentLinksResult.PermissionDenied, "Keine Berechtigung zum Bearbeiten von Dokumentbezügen.");
        }

        var link = await db.DocumentLinks
            .FirstOrDefaultAsync(l => l.DocumentId == documentId
                && l.TenantId == tenantId
                && l.LinkedEntityType == target.EntityType
                && l.LinkedEntityId == target.EntityId, ct);

        if (link is null)
        {
            return (UpdateDocumentLinksResult.NotFound, "Verknüpfung nicht gefunden.");
        }

        db.DocumentLinks.Remove(link);

        var document = await db.EvidenceDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId, ct);
        if (document is not null)
        {
            document.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        if (document is not null)
        {
            if (target.EntityType == DocumentLinkedEntityType.Training)
            {
                var title = await db.Trainings
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(t => t.TenantId == tenantId && t.Id == target.EntityId)
                    .Select(t => t.Title)
                    .FirstOrDefaultAsync(ct);

                if (title is not null)
                {
                    await complianceAuditLog.LogTrainingProofRemovedAsync(
                        target.EntityId, title, tenantId, documentId, document.FileName);
                }
            }
            else
            {
                await complianceAuditLog.LogDocumentUnlinkedAsync(
                    documentId,
                    document.FileName,
                    tenantId,
                    AuditLogPresentationHelper.ResolveDocumentLinkEntityLabel(target.EntityType),
                    target.EntityId);
            }
        }

        return (UpdateDocumentLinksResult.Success, null);
    }

    public async Task SyncEntityDocumentLinksAsync(
        int tenantId,
        DocumentLinkedEntityType entityType,
        int entityId,
        IReadOnlyCollection<int> documentIds,
        CancellationToken ct = default)
    {
        var validDocumentIds = await db.EvidenceDocuments
            .Where(d => d.TenantId == tenantId && documentIds.Contains(d.Id))
            .Select(d => d.Id)
            .ToListAsync(ct);

        var currentlyLinked = await db.DocumentLinks
            .Where(l => l.TenantId == tenantId
                && l.LinkedEntityType == entityType
                && l.LinkedEntityId == entityId)
            .ToListAsync(ct);

        var toRemove = currentlyLinked.Where(l => !validDocumentIds.Contains(l.DocumentId)).ToList();
        if (toRemove.Count > 0)
        {
            db.DocumentLinks.RemoveRange(toRemove);
        }

        var existingDocIds = currentlyLinked.Select(l => l.DocumentId).ToHashSet();
        var userId = await currentUser.GetUserIdAsync();
        var addedDocIds = new List<int>();
        foreach (var docId in validDocumentIds.Where(id => !existingDocIds.Contains(id)))
        {
            db.DocumentLinks.Add(new DocumentLink
            {
                TenantId = tenantId,
                DocumentId = docId,
                LinkedEntityType = entityType,
                LinkedEntityId = entityId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            });
            addedDocIds.Add(docId);
        }

        await db.SaveChangesAsync(ct);

        if (toRemove.Count > 0 || addedDocIds.Count > 0)
        {
            var fileNames = await db.EvidenceDocuments
                .AsNoTracking()
                .Where(d => d.TenantId == tenantId
                    && (toRemove.Select(r => r.DocumentId).Concat(addedDocIds).Contains(d.Id)))
                .Select(d => new { d.Id, d.FileName })
                .ToDictionaryAsync(d => d.Id, d => d.FileName, ct);

            var entityLabel = AuditLogPresentationHelper.ResolveDocumentLinkEntityLabel(entityType);

            foreach (var removed in toRemove)
            {
                if (fileNames.TryGetValue(removed.DocumentId, out var fileName))
                {
                    await complianceAuditLog.LogDocumentUnlinkedAsync(
                        removed.DocumentId, fileName, tenantId, entityLabel, entityId);
                }
            }

            foreach (var docId in addedDocIds)
            {
                if (fileNames.TryGetValue(docId, out var fileName))
                {
                    await complianceAuditLog.LogDocumentLinkedAsync(
                        docId, fileName, tenantId, entityLabel, entityId);
                }
            }
        }
    }

    public Task<int> CountDocumentsForEntityAsync(
        DocumentLinkedEntityType entityType,
        int entityId,
        CancellationToken ct = default) =>
        db.DocumentLinks.CountAsync(l => l.LinkedEntityType == entityType && l.LinkedEntityId == entityId, ct);

    private async Task<List<DocumentLinkSelectionViewModel>> LoadTargetsForTypeAsync(
        int tenantId,
        DocumentLinkedEntityType type,
        HashSet<DocumentLinkTarget> selectedSet,
        CancellationToken ct)
    {
        var selectedIds = selectedSet
            .Where(x => x.EntityType == type)
            .Select(x => x.EntityId)
            .ToHashSet();

        return type switch
        {
            DocumentLinkedEntityType.AuditRun => await db.AuditRuns
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(r => r.TenantId == tenantId && (!r.IsArchived || selectedIds.Contains(r.Id)))
                .OrderBy(r => r.Title)
                .Select(r => new DocumentLinkSelectionViewModel
                {
                    EntityType = type,
                    EntityId = r.Id,
                    DisplayName = r.Title,
                    IsArchived = r.IsArchived,
                    IsSelected = selectedIds.Contains(r.Id)
                })
                .ToListAsync(ct),

            DocumentLinkedEntityType.Measure => await db.Measures
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(m => m.TenantId == tenantId && (!m.IsArchived || selectedIds.Contains(m.Id)))
                .OrderBy(m => m.Title)
                .Select(m => new DocumentLinkSelectionViewModel
                {
                    EntityType = type,
                    EntityId = m.Id,
                    DisplayName = m.Title,
                    Description = m.Description,
                    IsArchived = m.IsArchived,
                    IsSelected = selectedIds.Contains(m.Id)
                })
                .ToListAsync(ct),

            DocumentLinkedEntityType.ServiceProvider => await db.ServiceProviders
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId && (!s.IsArchived || selectedIds.Contains(s.Id)))
                .OrderBy(s => s.Name)
                .Select(s => new DocumentLinkSelectionViewModel
                {
                    EntityType = type,
                    EntityId = s.Id,
                    DisplayName = s.Name,
                    Description = s.Description,
                    IsArchived = s.IsArchived,
                    IsSelected = selectedIds.Contains(s.Id)
                })
                .ToListAsync(ct),

            DocumentLinkedEntityType.ProcessingActivity => await db.ProcessingActivities
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(p => p.TenantId == tenantId && (!p.IsArchived || selectedIds.Contains(p.Id)))
                .OrderBy(p => p.Name)
                .Select(p => new DocumentLinkSelectionViewModel
                {
                    EntityType = type,
                    EntityId = p.Id,
                    DisplayName = p.Name,
                    Description = p.Description,
                    IsArchived = p.IsArchived,
                    IsSelected = selectedIds.Contains(p.Id)
                })
                .ToListAsync(ct),

            DocumentLinkedEntityType.Dsfa => await db.DataProtectionImpactAssessments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(d => d.TenantId == tenantId && (!d.IsArchived || selectedIds.Contains(d.Id)))
                .OrderBy(d => d.Title)
                .Select(d => new DocumentLinkSelectionViewModel
                {
                    EntityType = type,
                    EntityId = d.Id,
                    DisplayName = d.Title,
                    Description = d.ProcessingDescription,
                    IsArchived = d.IsArchived,
                    IsSelected = selectedIds.Contains(d.Id)
                })
                .ToListAsync(ct),

            DocumentLinkedEntityType.PrivacyIncident => await db.PrivacyIncidents
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(i => i.TenantId == tenantId && (!i.IsArchived || selectedIds.Contains(i.Id)))
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new DocumentLinkSelectionViewModel
                {
                    EntityType = type,
                    EntityId = i.Id,
                    DisplayName = i.IncidentNumber + " – " + i.Title,
                    Description = i.Description,
                    IsArchived = i.IsArchived,
                    IsSelected = selectedIds.Contains(i.Id)
                })
                .ToListAsync(ct),

            DocumentLinkedEntityType.Tom => await db.Toms
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(t => t.TenantId == tenantId && (!t.IsArchived || selectedIds.Contains(t.Id)))
                .OrderBy(t => t.Title)
                .Select(t => new DocumentLinkSelectionViewModel
                {
                    EntityType = type,
                    EntityId = t.Id,
                    DisplayName = t.Title,
                    Description = t.Description,
                    IsArchived = t.IsArchived,
                    IsSelected = selectedIds.Contains(t.Id)
                })
                .ToListAsync(ct),

            DocumentLinkedEntityType.DataSubjectRequest => (await db.DataSubjectRequests
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(r => r.TenantId == tenantId && (!r.IsArchived || selectedIds.Contains(r.Id)))
                .OrderByDescending(r => r.ReceivedAt)
                .Select(r => new
                {
                    r.Id,
                    r.RequestType,
                    r.IsArchived,
                    r.PersonalDataAnonymized
                })
                .ToListAsync(ct))
                .Select(r => new DocumentLinkSelectionViewModel
                {
                    EntityType = type,
                    EntityId = r.Id,
                    DisplayName = $"Betroffenenanfrage #{r.Id} ({Domain.DataSubjectRequestLabels.GetTypeLabel(r.RequestType)})",
                    IsArchived = r.IsArchived,
                    IsSelected = selectedIds.Contains(r.Id)
                })
                .ToList(),

            DocumentLinkedEntityType.Training => await db.Trainings
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(t => t.TenantId == tenantId && (!t.IsArchived || selectedIds.Contains(t.Id)))
                .OrderByDescending(t => t.ScheduledAt ?? t.CreatedAt)
                .ThenBy(t => t.Title)
                .Select(t => new DocumentLinkSelectionViewModel
                {
                    EntityType = type,
                    EntityId = t.Id,
                    DisplayName = t.Title,
                    Description = t.Description,
                    IsArchived = t.IsArchived,
                    IsSelected = selectedIds.Contains(t.Id)
                })
                .ToListAsync(ct),

            _ => []
        };
    }

    private async Task<bool> EntityExistsInTenantAsync(
        DocumentLinkedEntityType entityType,
        int entityId,
        int tenantId,
        CancellationToken ct) =>
        entityType switch
        {
            DocumentLinkedEntityType.AuditRun => await db.AuditRuns
                .IgnoreQueryFilters()
                .AnyAsync(r => r.Id == entityId && r.TenantId == tenantId, ct),
            DocumentLinkedEntityType.Measure => await db.Measures
                .IgnoreQueryFilters()
                .AnyAsync(m => m.Id == entityId && m.TenantId == tenantId, ct),
            DocumentLinkedEntityType.ServiceProvider => await db.ServiceProviders
                .IgnoreQueryFilters()
                .AnyAsync(s => s.Id == entityId && s.TenantId == tenantId, ct),
            DocumentLinkedEntityType.ProcessingActivity => await db.ProcessingActivities
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Id == entityId && p.TenantId == tenantId, ct),
            DocumentLinkedEntityType.Dsfa => await db.DataProtectionImpactAssessments
                .IgnoreQueryFilters()
                .AnyAsync(d => d.Id == entityId && d.TenantId == tenantId, ct),
            DocumentLinkedEntityType.PrivacyIncident => await db.PrivacyIncidents
                .IgnoreQueryFilters()
                .AnyAsync(i => i.Id == entityId && i.TenantId == tenantId, ct),
            DocumentLinkedEntityType.Tom => await db.Toms
                .IgnoreQueryFilters()
                .AnyAsync(t => t.Id == entityId && t.TenantId == tenantId, ct),
            DocumentLinkedEntityType.DataSubjectRequest => await db.DataSubjectRequests
                .IgnoreQueryFilters()
                .AnyAsync(r => r.Id == entityId && r.TenantId == tenantId, ct),
            DocumentLinkedEntityType.Training => await db.Trainings
                .IgnoreQueryFilters()
                .AnyAsync(t => t.Id == entityId && t.TenantId == tenantId, ct),
            _ => false
        };

    private async Task<Dictionary<(DocumentLinkedEntityType Type, int Id), string>> ResolveDisplayNamesAsync(
        int tenantId,
        IReadOnlyList<DocumentLink> links,
        CancellationToken ct)
    {
        var result = new Dictionary<(DocumentLinkedEntityType, int), string>();

        foreach (var group in links.GroupBy(l => l.LinkedEntityType))
        {
            var ids = group.Select(l => l.LinkedEntityId).Distinct().ToList();
            switch (group.Key)
            {
                case DocumentLinkedEntityType.AuditRun:
                    foreach (var item in await db.AuditRuns.IgnoreQueryFilters().AsNoTracking()
                        .Where(r => r.TenantId == tenantId && ids.Contains(r.Id))
                        .Select(r => new { r.Id, r.Title }).ToListAsync(ct))
                    {
                        result[(DocumentLinkedEntityType.AuditRun, item.Id)] = item.Title;
                    }
                    break;
                case DocumentLinkedEntityType.Measure:
                    foreach (var item in await db.Measures.IgnoreQueryFilters().AsNoTracking()
                        .Where(m => m.TenantId == tenantId && ids.Contains(m.Id))
                        .Select(m => new { m.Id, m.Title }).ToListAsync(ct))
                    {
                        result[(DocumentLinkedEntityType.Measure, item.Id)] = item.Title;
                    }
                    break;
                case DocumentLinkedEntityType.ServiceProvider:
                    foreach (var item in await db.ServiceProviders.IgnoreQueryFilters().AsNoTracking()
                        .Where(s => s.TenantId == tenantId && ids.Contains(s.Id))
                        .Select(s => new { s.Id, s.Name }).ToListAsync(ct))
                    {
                        result[(DocumentLinkedEntityType.ServiceProvider, item.Id)] = item.Name;
                    }
                    break;
                case DocumentLinkedEntityType.ProcessingActivity:
                    foreach (var item in await db.ProcessingActivities.IgnoreQueryFilters().AsNoTracking()
                        .Where(p => p.TenantId == tenantId && ids.Contains(p.Id))
                        .Select(p => new { p.Id, p.Name }).ToListAsync(ct))
                    {
                        result[(DocumentLinkedEntityType.ProcessingActivity, item.Id)] = item.Name;
                    }
                    break;
                case DocumentLinkedEntityType.Dsfa:
                    foreach (var item in await db.DataProtectionImpactAssessments.IgnoreQueryFilters().AsNoTracking()
                        .Where(d => d.TenantId == tenantId && ids.Contains(d.Id))
                        .Select(d => new { d.Id, d.Title }).ToListAsync(ct))
                    {
                        result[(DocumentLinkedEntityType.Dsfa, item.Id)] = item.Title;
                    }
                    break;
                case DocumentLinkedEntityType.PrivacyIncident:
                    foreach (var item in await db.PrivacyIncidents.IgnoreQueryFilters().AsNoTracking()
                        .Where(i => i.TenantId == tenantId && ids.Contains(i.Id))
                        .Select(i => new { i.Id, i.IncidentNumber, i.Title }).ToListAsync(ct))
                    {
                        result[(DocumentLinkedEntityType.PrivacyIncident, item.Id)] = $"{item.IncidentNumber} – {item.Title}";
                    }
                    break;
                case DocumentLinkedEntityType.Tom:
                    foreach (var item in await db.Toms.IgnoreQueryFilters().AsNoTracking()
                        .Where(t => t.TenantId == tenantId && ids.Contains(t.Id))
                        .Select(t => new { t.Id, t.Title }).ToListAsync(ct))
                    {
                        result[(DocumentLinkedEntityType.Tom, item.Id)] = item.Title;
                    }
                    break;
                case DocumentLinkedEntityType.DataSubjectRequest:
                    foreach (var item in await db.DataSubjectRequests.IgnoreQueryFilters().AsNoTracking()
                        .Where(r => r.TenantId == tenantId && ids.Contains(r.Id))
                        .Select(r => new { r.Id, r.RequestType }).ToListAsync(ct))
                    {
                        result[(DocumentLinkedEntityType.DataSubjectRequest, item.Id)] =
                            $"Betroffenenanfrage #{item.Id} ({Domain.DataSubjectRequestLabels.GetTypeLabel(item.RequestType)})";
                    }
                    break;
                case DocumentLinkedEntityType.Training:
                    foreach (var item in await db.Trainings.IgnoreQueryFilters().AsNoTracking()
                        .Where(t => t.TenantId == tenantId && ids.Contains(t.Id))
                        .Select(t => new { t.Id, t.Title }).ToListAsync(ct))
                    {
                        result[(DocumentLinkedEntityType.Training, item.Id)] = item.Title;
                    }
                    break;
            }
        }

        return result;
    }

    private static DocumentLinkSummaryViewModel BuildSummary(
        int totalCount,
        IReadOnlyDictionary<DocumentLinkedEntityType, int> countsByType,
        IReadOnlyList<string> detailLines)
    {
        if (totalCount == 0)
        {
            return EmptySummary();
        }

        if (totalCount == 1)
        {
            return new DocumentLinkSummaryViewModel
            {
                TotalCount = 1,
                CountsByType = countsByType,
                SingleDisplayText = detailLines[0],
                CompactDisplayText = detailLines[0],
                DetailLines = detailLines
            };
        }

        var parts = DocumentLinkLabels.AllTypes
            .Where(t => countsByType.TryGetValue(t, out var c) && c > 0)
            .Select(t => $"{DocumentLinkLabels.GetTypeLabelPlural(t)} {countsByType[t]}")
            .ToList();

        var compact = parts.Count <= 3
            ? $"{totalCount} Bezüge: {string.Join(" · ", parts)}"
            : $"{totalCount} Bezüge ausgewählt";

        return new DocumentLinkSummaryViewModel
        {
            TotalCount = totalCount,
            CountsByType = countsByType,
            CompactDisplayText = compact,
            DetailLines = detailLines
        };
    }

    private static DocumentLinkSummaryViewModel EmptySummary() => new()
    {
        TotalCount = 0,
        CompactDisplayText = "—"
    };
}
