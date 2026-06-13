using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Data.Seed;

/// <summary>Idempotente Demo-Schulungen für Entwicklung.</summary>
public static class TrainingSeeder
{
    private const string DemoTrainingTitle = "Grundlagenschulung Datenschutz 2026";
    private const string GlobalTemplateTitle = "Grundlagenschulung Datenschutz";
    private const string DemoTenantName = "Demo Mandant Hauptsitz";

    public static async Task SeedDemoTrainingAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Name == DemoTenantName, ct);
        if (tenant is null)
            return;

        if (await db.Trainings
                .IgnoreQueryFilters()
                .AnyAsync(t => t.TenantId == tenant.Id && t.Title == DemoTrainingTitle, ct))
        {
            return;
        }

        var template = await db.TrainingTemplates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.IsGlobal && t.Title == GlobalTemplateTitle, ct);

        var training = new Training
        {
            TenantId = tenant.Id,
            TrainingTemplateId = template?.Id,
            Title = DemoTrainingTitle,
            Description = "Jährliche Grundlagenschulung für alle Mitarbeitenden.",
            TrainingType = TrainingType.PrivacyBasics,
            TargetAudience = "Alle Mitarbeitenden",
            Status = TrainingStatus.Inactive,
            ParticipantCount = 0,
            ProofMissing = false,
            CreatedAt = DateTime.UtcNow
        };

        db.Trainings.Add(training);
        await db.SaveChangesAsync(ct);
    }
}
