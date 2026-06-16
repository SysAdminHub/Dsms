using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

/// <summary>
/// Synchronisiert Many-to-Many-Verknüpfungen von Datenschutzvorfällen.
/// Alle Ziel-IDs werden mandantenseitig validiert.
/// </summary>
public class PrivacyIncidentRelationsService(ApplicationDbContext db, ILogger<PrivacyIncidentRelationsService> logger)
{
    public async Task<(bool Success, string? ErrorMessage)> SaveLinksAsync(
        int privacyIncidentId,
        int tenantId,
        IReadOnlyCollection<int> processingActivityIds,
        IReadOnlyCollection<int> serviceProviderIds,
        IReadOnlyCollection<int> measureIds,
        IReadOnlyCollection<int> tomIds,
        CancellationToken ct = default)
    {
        var incidentExists = await db.PrivacyIncidents
            .AnyAsync(i => i.Id == privacyIncidentId && i.TenantId == tenantId, ct);
        if (!incidentExists)
            return (false, "Vorfall nicht gefunden.");

        var distinctProcessingActivityIds = processingActivityIds.Distinct().ToList();
        var distinctServiceProviderIds = serviceProviderIds.Distinct().ToList();
        var distinctMeasureIds = measureIds.Distinct().ToList();
        var distinctTomIds = tomIds.Distinct().ToList();

        if (distinctProcessingActivityIds.Count > 0)
        {
            var validPaCount = await db.ProcessingActivities
                .CountAsync(p => p.TenantId == tenantId && distinctProcessingActivityIds.Contains(p.Id), ct);
            if (validPaCount != distinctProcessingActivityIds.Count)
                return (false, "Eine oder mehrere Verarbeitungstätigkeiten gehören nicht zu Ihrem Mandanten.");
        }

        if (distinctServiceProviderIds.Count > 0)
        {
            var validSpCount = await db.ServiceProviders
                .CountAsync(s => s.TenantId == tenantId && distinctServiceProviderIds.Contains(s.Id), ct);
            if (validSpCount != distinctServiceProviderIds.Count)
                return (false, "Ein oder mehrere Dienstleister gehören nicht zu Ihrem Mandanten.");
        }

        if (distinctMeasureIds.Count > 0)
        {
            var validMeasureCount = await db.Measures
                .CountAsync(m => m.TenantId == tenantId && distinctMeasureIds.Contains(m.Id), ct);
            if (validMeasureCount != distinctMeasureIds.Count)
                return (false, "Eine oder mehrere Maßnahmen gehören nicht zu Ihrem Mandanten.");
        }

        if (distinctTomIds.Count > 0)
        {
            var validTomCount = await db.Toms
                .CountAsync(t => t.TenantId == tenantId && distinctTomIds.Contains(t.Id), ct);
            if (validTomCount != distinctTomIds.Count)
                return (false, "Eine oder mehrere TOMs gehören nicht zu Ihrem Mandanten.");
        }

        try
        {
            await SyncProcessingActivitiesAsync(privacyIncidentId, tenantId, distinctProcessingActivityIds, ct);
            await SyncServiceProvidersAsync(privacyIncidentId, tenantId, distinctServiceProviderIds, ct);
            await SyncMeasuresAsync(privacyIncidentId, tenantId, distinctMeasureIds, ct);
            await SyncTomsAsync(privacyIncidentId, tenantId, distinctTomIds, ct);
            await db.SaveChangesAsync(ct);
            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Fehler beim Speichern der Verknüpfungen für Datenschutzvorfall {PrivacyIncidentId} (Mandant {TenantId})",
                privacyIncidentId,
                tenantId);
            return (false, "Die Verknüpfungen konnten nicht gespeichert werden. Bitte versuchen Sie es erneut.");
        }
    }

    /// <summary>Verknüpft eine neu angelegte Maßnahme mit einem Vorfall (idempotent).</summary>
    public async Task<(bool Success, string? ErrorMessage)> LinkMeasureToIncidentAsync(
        int privacyIncidentId,
        int measureId,
        int tenantId,
        CancellationToken ct = default)
    {
        var incidentOk = await db.PrivacyIncidents
            .AnyAsync(i => i.Id == privacyIncidentId && i.TenantId == tenantId, ct);
        if (!incidentOk)
            return (false, "Vorfall nicht gefunden.");

        var measureOk = await db.Measures
            .AnyAsync(m => m.Id == measureId && m.TenantId == tenantId, ct);
        if (!measureOk)
            return (false, "Maßnahme gehört nicht zu Ihrem Mandanten.");

        var exists = await db.PrivacyIncidentMeasures
            .AnyAsync(l => l.PrivacyIncidentId == privacyIncidentId && l.MeasureId == measureId && l.TenantId == tenantId, ct);
        if (!exists)
        {
            db.Add(new PrivacyIncidentMeasure
            {
                TenantId = tenantId,
                PrivacyIncidentId = privacyIncidentId,
                MeasureId = measureId
            });
            await db.SaveChangesAsync(ct);
        }

        return (true, null);
    }

    public static async Task<string> GenerateIncidentNumberAsync(ApplicationDbContext db, int tenantId, CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"INC-{year}-";

        var existingNumbers = await db.PrivacyIncidents
            .IgnoreQueryFilters()
            .Where(i => i.TenantId == tenantId && i.IncidentNumber.StartsWith(prefix))
            .Select(i => i.IncidentNumber)
            .ToListAsync(ct);

        var maxSeq = 0;
        foreach (var number in existingNumbers)
        {
            var suffix = number[prefix.Length..];
            if (int.TryParse(suffix, out var seq) && seq > maxSeq)
                maxSeq = seq;
        }

        return $"{prefix}{(maxSeq + 1):D4}";
    }

    private async Task SyncProcessingActivitiesAsync(
        int privacyIncidentId, int tenantId, IReadOnlyCollection<int> targetIds, CancellationToken ct)
    {
        var existing = await db.PrivacyIncidentProcessingActivities
            .Where(l => l.PrivacyIncidentId == privacyIncidentId && l.TenantId == tenantId)
            .ToListAsync(ct);

        var existingIds = existing.Select(l => l.ProcessingActivityId).ToHashSet();
        var targetSet = targetIds.ToHashSet();

        db.RemoveRange(existing.Where(l => !targetSet.Contains(l.ProcessingActivityId)));

        foreach (var id in targetSet.Where(id => !existingIds.Contains(id)))
        {
            db.Add(new PrivacyIncidentProcessingActivity
            {
                TenantId = tenantId,
                PrivacyIncidentId = privacyIncidentId,
                ProcessingActivityId = id
            });
        }
    }

    private async Task SyncServiceProvidersAsync(
        int privacyIncidentId, int tenantId, IReadOnlyCollection<int> targetIds, CancellationToken ct)
    {
        var existing = await db.PrivacyIncidentServiceProviders
            .Where(l => l.PrivacyIncidentId == privacyIncidentId && l.TenantId == tenantId)
            .ToListAsync(ct);

        var existingIds = existing.Select(l => l.ServiceProviderId).ToHashSet();
        var targetSet = targetIds.ToHashSet();

        db.RemoveRange(existing.Where(l => !targetSet.Contains(l.ServiceProviderId)));

        foreach (var id in targetSet.Where(id => !existingIds.Contains(id)))
        {
            db.Add(new PrivacyIncidentServiceProvider
            {
                TenantId = tenantId,
                PrivacyIncidentId = privacyIncidentId,
                ServiceProviderId = id
            });
        }
    }

    private async Task SyncMeasuresAsync(
        int privacyIncidentId, int tenantId, IReadOnlyCollection<int> targetIds, CancellationToken ct)
    {
        var existing = await db.PrivacyIncidentMeasures
            .Where(l => l.PrivacyIncidentId == privacyIncidentId && l.TenantId == tenantId)
            .ToListAsync(ct);

        var existingIds = existing.Select(l => l.MeasureId).ToHashSet();
        var targetSet = targetIds.ToHashSet();

        db.RemoveRange(existing.Where(l => !targetSet.Contains(l.MeasureId)));

        foreach (var id in targetSet.Where(id => !existingIds.Contains(id)))
        {
            db.Add(new PrivacyIncidentMeasure
            {
                TenantId = tenantId,
                PrivacyIncidentId = privacyIncidentId,
                MeasureId = id
            });
        }
    }

    private async Task SyncTomsAsync(
        int privacyIncidentId, int tenantId, IReadOnlyCollection<int> targetIds, CancellationToken ct)
    {
        var existing = await db.PrivacyIncidentToms
            .Where(l => l.PrivacyIncidentId == privacyIncidentId && l.TenantId == tenantId)
            .ToListAsync(ct);

        var existingIds = existing.Select(l => l.TomId).ToHashSet();
        var targetSet = targetIds.ToHashSet();

        db.RemoveRange(existing.Where(l => !targetSet.Contains(l.TomId)));

        foreach (var id in targetSet.Where(id => !existingIds.Contains(id)))
        {
            db.Add(new PrivacyIncidentTom
            {
                TenantId = tenantId,
                PrivacyIncidentId = privacyIncidentId,
                TomId = id
            });
        }
    }
}
