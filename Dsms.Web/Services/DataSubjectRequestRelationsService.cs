using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

/// <summary>Synchronisiert Many-to-Many-Verknüpfungen von Betroffenenanfragen.</summary>
public class DataSubjectRequestRelationsService(ApplicationDbContext db, ICurrentUserContext currentUser)
{
    public async Task<(bool Success, string? ErrorMessage)> SaveLinksAsync(
        int dataSubjectRequestId,
        int tenantId,
        IReadOnlyCollection<int> processingActivityIds,
        IReadOnlyCollection<int> measureIds,
        IReadOnlyCollection<int> serviceProviderIds,
        CancellationToken ct = default)
    {
        var requestExists = await db.DataSubjectRequests
            .AnyAsync(r => r.Id == dataSubjectRequestId && r.TenantId == tenantId, ct);
        if (!requestExists)
        {
            return (false, "Betroffenenanfrage nicht gefunden.");
        }

        if (processingActivityIds.Count > 0)
        {
            var validPaCount = await db.ProcessingActivities
                .CountAsync(p => p.TenantId == tenantId && processingActivityIds.Contains(p.Id), ct);
            if (validPaCount != processingActivityIds.Count)
            {
                return (false, "Eine oder mehrere Verarbeitungstätigkeiten gehören nicht zu Ihrem Mandanten.");
            }
        }

        if (measureIds.Count > 0)
        {
            var validMeasureCount = await db.Measures
                .CountAsync(m => m.TenantId == tenantId && measureIds.Contains(m.Id), ct);
            if (validMeasureCount != measureIds.Count)
            {
                return (false, "Eine oder mehrere Maßnahmen gehören nicht zu Ihrem Mandanten.");
            }
        }

        if (serviceProviderIds.Count > 0)
        {
            var validSpCount = await db.ServiceProviders
                .CountAsync(s => s.TenantId == tenantId && serviceProviderIds.Contains(s.Id), ct);
            if (validSpCount != serviceProviderIds.Count)
            {
                return (false, "Ein oder mehrere Dienstleister gehören nicht zu Ihrem Mandanten.");
            }
        }

        var userId = await currentUser.GetUserIdAsync();
        await SyncProcessingActivitiesAsync(dataSubjectRequestId, tenantId, processingActivityIds, userId, ct);
        await SyncMeasuresAsync(dataSubjectRequestId, tenantId, measureIds, userId, ct);
        await SyncServiceProvidersAsync(dataSubjectRequestId, tenantId, serviceProviderIds, userId, ct);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> LinkMeasureToRequestAsync(
        int dataSubjectRequestId,
        int measureId,
        int tenantId,
        CancellationToken ct = default)
    {
        var requestOk = await db.DataSubjectRequests
            .AnyAsync(r => r.Id == dataSubjectRequestId && r.TenantId == tenantId, ct);
        if (!requestOk)
        {
            return (false, "Betroffenenanfrage nicht gefunden.");
        }

        var measureOk = await db.Measures
            .AnyAsync(m => m.Id == measureId && m.TenantId == tenantId, ct);
        if (!measureOk)
        {
            return (false, "Maßnahme gehört nicht zu Ihrem Mandanten.");
        }

        var exists = await db.DataSubjectRequestMeasures
            .AnyAsync(l => l.DataSubjectRequestId == dataSubjectRequestId && l.MeasureId == measureId && l.TenantId == tenantId, ct);
        if (!exists)
        {
            var userId = await currentUser.GetUserIdAsync();
            db.Add(new DataSubjectRequestMeasure
            {
                TenantId = tenantId,
                DataSubjectRequestId = dataSubjectRequestId,
                MeasureId = measureId,
                CreatedByUserId = userId
            });
            await db.SaveChangesAsync(ct);
        }

        return (true, null);
    }

    private async Task SyncProcessingActivitiesAsync(
        int dataSubjectRequestId, int tenantId, IReadOnlyCollection<int> targetIds, string? userId, CancellationToken ct)
    {
        var existing = await db.DataSubjectRequestProcessingActivities
            .Where(l => l.DataSubjectRequestId == dataSubjectRequestId && l.TenantId == tenantId)
            .ToListAsync(ct);

        var existingIds = existing.Select(l => l.ProcessingActivityId).ToHashSet();
        var targetSet = targetIds.ToHashSet();

        db.RemoveRange(existing.Where(l => !targetSet.Contains(l.ProcessingActivityId)));

        foreach (var id in targetSet.Where(id => !existingIds.Contains(id)))
        {
            db.Add(new DataSubjectRequestProcessingActivity
            {
                TenantId = tenantId,
                DataSubjectRequestId = dataSubjectRequestId,
                ProcessingActivityId = id,
                CreatedByUserId = userId
            });
        }
    }

    private async Task SyncMeasuresAsync(
        int dataSubjectRequestId, int tenantId, IReadOnlyCollection<int> targetIds, string? userId, CancellationToken ct)
    {
        var existing = await db.DataSubjectRequestMeasures
            .Where(l => l.DataSubjectRequestId == dataSubjectRequestId && l.TenantId == tenantId)
            .ToListAsync(ct);

        var existingIds = existing.Select(l => l.MeasureId).ToHashSet();
        var targetSet = targetIds.ToHashSet();

        db.RemoveRange(existing.Where(l => !targetSet.Contains(l.MeasureId)));

        foreach (var id in targetSet.Where(id => !existingIds.Contains(id)))
        {
            db.Add(new DataSubjectRequestMeasure
            {
                TenantId = tenantId,
                DataSubjectRequestId = dataSubjectRequestId,
                MeasureId = id,
                CreatedByUserId = userId
            });
        }
    }

    private async Task SyncServiceProvidersAsync(
        int dataSubjectRequestId, int tenantId, IReadOnlyCollection<int> targetIds, string? userId, CancellationToken ct)
    {
        var existing = await db.DataSubjectRequestServiceProviders
            .Where(l => l.DataSubjectRequestId == dataSubjectRequestId && l.TenantId == tenantId)
            .ToListAsync(ct);

        var existingIds = existing.Select(l => l.ServiceProviderId).ToHashSet();
        var targetSet = targetIds.ToHashSet();

        db.RemoveRange(existing.Where(l => !targetSet.Contains(l.ServiceProviderId)));

        foreach (var id in targetSet.Where(id => !existingIds.Contains(id)))
        {
            db.Add(new DataSubjectRequestServiceProvider
            {
                TenantId = tenantId,
                DataSubjectRequestId = dataSubjectRequestId,
                ServiceProviderId = id,
                CreatedByUserId = userId
            });
        }
    }
}
