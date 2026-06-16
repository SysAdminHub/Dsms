using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Onboarding;

public sealed class TenantOnboardingService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ICurrentUserContext currentUser) : ITenantOnboardingService
{
    private sealed record DefaultTaskDefinition(
        string Key,
        string Title,
        string Description,
        string TargetUrl,
        int SortOrder);

    private static readonly DefaultTaskDefinition[] DefaultTasks =
    [
        new(
            "processing-activities",
            "Verarbeitungstätigkeiten anlegen",
            "Dokumentieren Sie die wichtigsten Verarbeitungstätigkeiten Ihrer Organisation.",
            "/processing-activities",
            1),
        new(
            "toms",
            "TOMs dokumentieren",
            "Erfassen Sie technische und organisatorische Maßnahmen zum Schutz personenbezogener Daten.",
            "/toms",
            2),
        new(
            "service-providers",
            "Dienstleister und Auftragsverarbeiter erfassen",
            "Dokumentieren Sie externe Dienstleister, Auftragsverarbeiter und AVV-Informationen.",
            "/service-providers",
            3),
        new(
            "dpia",
            "DSFA-Bedarf prüfen",
            "Prüfen Sie, ob für bestimmte Verarbeitungstätigkeiten eine Datenschutz-Folgenabschätzung erforderlich ist.",
            "/dsfa",
            4),
        new(
            "audit",
            "Erstes Audit durchführen",
            "Starten oder bearbeiten Sie einen Audit-Durchlauf zur Datenschutzprüfung.",
            "/audit-runs",
            5),
        new(
            "documents",
            "Nachweisdokumente hochladen",
            "Laden Sie wichtige Nachweise, Verträge oder Datenschutzdokumente hoch.",
            "/documents",
            6)
    ];

    public async Task EnsureDefaultTasksForTenantAsync(int tenantId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var existingKeys = await db.TenantOnboardingTasks
            .Where(t => t.TenantId == tenantId)
            .Select(t => t.Key)
            .ToListAsync(ct);

        var existingKeySet = existingKeys.ToHashSet(StringComparer.Ordinal);
        var now = DateTime.UtcNow;
        var added = false;

        foreach (var definition in DefaultTasks)
        {
            if (existingKeySet.Contains(definition.Key))
            {
                continue;
            }

            db.TenantOnboardingTasks.Add(new TenantOnboardingTask
            {
                TenantId = tenantId,
                Key = definition.Key,
                Title = definition.Title,
                Description = definition.Description,
                TargetUrl = definition.TargetUrl,
                SortOrder = definition.SortOrder,
                IsCompleted = false,
                CreatedAt = now
            });
            added = true;
        }

        if (added)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<TenantOnboardingTask>> GetTasksForCurrentTenantAsync(CancellationToken ct = default)
    {
        var tenantId = await currentUser.GetTenantIdAsync();
        if (tenantId is null)
        {
            return [];
        }

        await EnsureDefaultTasksForTenantAsync(tenantId.Value, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        return await db.TenantOnboardingTasks
            .Where(t => t.TenantId == tenantId.Value)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Id)
            .ToListAsync(ct);
    }

    public async Task<TenantOnboardingTask?> ToggleTaskCompletedAsync(
        int taskId,
        bool isCompleted,
        CancellationToken ct = default)
    {
        var tenantId = await currentUser.GetTenantIdAsync();
        if (tenantId is null)
        {
            return null;
        }

        var userId = await currentUser.GetUserIdAsync();

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var task = await db.TenantOnboardingTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.TenantId == tenantId.Value, ct);

        if (task is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        task.IsCompleted = isCompleted;
        task.CompletedAt = isCompleted ? now : null;
        task.CompletedByUserId = isCompleted ? userId : null;
        task.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        return task;
    }
}
