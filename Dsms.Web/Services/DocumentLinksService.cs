using Dsms.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

public enum UpdateDocumentLinksResult
{
    Success,
    NotFound,
    InvalidTenantReference,
    Failed
}

public record DocumentLinksDto(
    int? AuditRunId,
    int? MeasureId,
    int? ServiceProviderId,
    int? ProcessingActivityId,
    int? DataProtectionImpactAssessmentId,
    int? PrivacyIncidentId);

/// <summary>
/// Aktualisiert optionale Verknüpfungen eines Nachweisdokuments (nur FK-Felder, nicht die Datei).
/// </summary>
public class DocumentLinksService(ApplicationDbContext db)
{
    public async Task<(UpdateDocumentLinksResult Result, string? ErrorMessage)> UpdateLinksAsync(
        int documentId,
        int tenantId,
        DocumentLinksDto links,
        CancellationToken ct = default)
    {
        var document = await db.EvidenceDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId, ct);

        if (document is null)
            return (UpdateDocumentLinksResult.NotFound, "Dokument nicht gefunden.");

        if (links.AuditRunId.HasValue)
        {
            var ok = await db.AuditRuns.AnyAsync(r => r.Id == links.AuditRunId && r.TenantId == tenantId, ct);
            if (!ok)
                return (UpdateDocumentLinksResult.InvalidTenantReference, "Der gewählte Audit-Durchlauf gehört nicht zu Ihrem Mandanten.");
        }

        if (links.MeasureId.HasValue)
        {
            var ok = await db.Measures.AnyAsync(m => m.Id == links.MeasureId && m.TenantId == tenantId, ct);
            if (!ok)
                return (UpdateDocumentLinksResult.InvalidTenantReference, "Die gewählte Maßnahme gehört nicht zu Ihrem Mandanten.");
        }

        if (links.ServiceProviderId.HasValue)
        {
            var ok = await db.ServiceProviders.AnyAsync(s => s.Id == links.ServiceProviderId && s.TenantId == tenantId, ct);
            if (!ok)
                return (UpdateDocumentLinksResult.InvalidTenantReference, "Der gewählte Dienstleister gehört nicht zu Ihrem Mandanten.");
        }

        if (links.ProcessingActivityId.HasValue)
        {
            var ok = await db.ProcessingActivities.AnyAsync(p => p.Id == links.ProcessingActivityId && p.TenantId == tenantId, ct);
            if (!ok)
                return (UpdateDocumentLinksResult.InvalidTenantReference, "Die gewählte Verarbeitungstätigkeit gehört nicht zu Ihrem Mandanten.");
        }

        if (links.DataProtectionImpactAssessmentId.HasValue)
        {
            var ok = await db.DataProtectionImpactAssessments.AnyAsync(d => d.Id == links.DataProtectionImpactAssessmentId && d.TenantId == tenantId, ct);
            if (!ok)
                return (UpdateDocumentLinksResult.InvalidTenantReference, "Die gewählte DSFA gehört nicht zu Ihrem Mandanten.");
        }

        if (links.PrivacyIncidentId.HasValue)
        {
            var ok = await db.PrivacyIncidents.AnyAsync(i => i.Id == links.PrivacyIncidentId && i.TenantId == tenantId, ct);
            if (!ok)
                return (UpdateDocumentLinksResult.InvalidTenantReference, "Der gewählte Datenschutzvorfall gehört nicht zu Ihrem Mandanten.");
        }

        document.AuditRunId = links.AuditRunId;
        document.MeasureId = links.MeasureId;
        document.ServiceProviderId = links.ServiceProviderId;
        document.ProcessingActivityId = links.ProcessingActivityId;
        document.DataProtectionImpactAssessmentId = links.DataProtectionImpactAssessmentId;
        document.PrivacyIncidentId = links.PrivacyIncidentId;
        document.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return (UpdateDocumentLinksResult.Success, null);
    }
}
