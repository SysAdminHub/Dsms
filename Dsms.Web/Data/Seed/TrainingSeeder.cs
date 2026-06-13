using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Data.Seed;

/// <summary>Idempotente Demo-Schulungen und Demo-Teilnehmer für Entwicklung.</summary>
public static class TrainingSeeder
{
    private const string DemoTrainingTitle = "Grundlagenschulung Datenschutz 2026";
    private const string GlobalTemplateTitle = "Grundlagenschulung Datenschutz";
    private const string DemoTenantName = "Demo Mandant Hauptsitz";
    private const string DemoParticipant1Email = "demo.teilnehmer1@example.com";
    private const string DemoParticipant2Email = "demo.teilnehmer2@example.com";

    public static async Task SeedDemoTrainingAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Name == DemoTenantName, ct);
        if (tenant is null)
            return;

        var training = await db.Trainings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TenantId == tenant.Id && t.Title == DemoTrainingTitle, ct);

        if (training is null)
        {
            var template = await db.TrainingTemplates
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.IsGlobal && t.Title == GlobalTemplateTitle, ct);

            training = new Training
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
                AccessCodeValidityDays = 14,
                CreatedAt = DateTime.UtcNow
            };

            db.Trainings.Add(training);
            await db.SaveChangesAsync(ct);
        }

        await SeedDemoParticipantsAsync(db, tenant.Id, training.Id, ct);
    }

    private static async Task SeedDemoParticipantsAsync(
        ApplicationDbContext db,
        int tenantId,
        int trainingId,
        CancellationToken ct)
    {
        var demoData = new[]
        {
            (Name: "Demo Teilnehmer 1", Email: DemoParticipant1Email, Department: "IT"),
            (Name: "Demo Teilnehmer 2", Email: DemoParticipant2Email, Department: "HR")
        };

        foreach (var (name, email, department) in demoData)
        {
            var participant = await db.TrainingParticipants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Email == email, ct);

            if (participant is null)
            {
                participant = new TrainingParticipant
                {
                    TenantId = tenantId,
                    Name = name,
                    Email = email,
                    Department = department,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                db.TrainingParticipants.Add(participant);
                await db.SaveChangesAsync(ct);
            }

            var hasAssignment = await db.TrainingAssignments
                .IgnoreQueryFilters()
                .AnyAsync(a => a.TrainingId == trainingId
                    && a.ParticipantEmailSnapshot == email
                    && a.Status != TrainingAssignmentStatus.Cancelled, ct);

            if (!hasAssignment)
            {
                db.TrainingAssignments.Add(new TrainingAssignment
                {
                    TenantId = tenantId,
                    TrainingId = trainingId,
                    TrainingParticipantId = participant.Id,
                    ParticipantNameSnapshot = participant.Name,
                    ParticipantEmailSnapshot = participant.Email,
                    Status = TrainingAssignmentStatus.Assigned,
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
