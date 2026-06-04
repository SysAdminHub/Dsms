using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Data.Seed;

/// <summary>
/// Wendet ausstehende Migrationen an und legt Demo-Daten an, wenn die Datenbank leer ist.
/// Wird beim Anwendungsstart aus <c>Program.cs</c> aufgerufen – nur für Entwicklung/Demo gedacht.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Idempotent für Rollen; Fachdaten nur, wenn noch kein Mandant existiert.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        foreach (var role in DsmsRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Bereits befüllte DB nicht erneut seeden (z. B. nach erstem Start).
        if (await db.Tenants.AnyAsync())
        {
            return;
        }

        var tenant = new Tenant
        {
            Name = "Demo GmbH",
            LegalName = "Demo GmbH",
            IsActive = true
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var template = new AuditTemplate
        {
            TenantId = tenant.Id,
            Title = "DSGVO-Basisaudit",
            Description = "Einfache Vorlage für den Einstieg in Datenschutz-Audits.",
            Version = "1.0",
            IsActive = true,
            Questions =
            [
                new AuditQuestion { SortOrder = 1, Category = "Organisation", Text = "Gibt es eine benannte verantwortliche Person für Datenschutz?", IsRequired = true },
                new AuditQuestion { SortOrder = 2, Category = "Dokumentation", Text = "Wird ein Verarbeitungsverzeichnis geführt?", IsRequired = true },
                new AuditQuestion { SortOrder = 3, Category = "Technik", Text = "Sind Zugriffe auf personenbezogene Daten rollenbasiert beschränkt?", IsRequired = true },
                new AuditQuestion { SortOrder = 4, Category = "Prozesse", Text = "Gibt es einen dokumentierten Prozess für Betroffenenanfragen?", IsRequired = false }
            ]
        };
        db.AuditTemplates.Add(template);
        await db.SaveChangesAsync();

        var auditRun = new AuditRun
        {
            TenantId = tenant.Id,
            AuditTemplateId = template.Id,
            Title = "Audit Q1 2026",
            Status = AuditRunStatus.InProgress,
            StartedAt = DateTime.UtcNow.AddDays(-7)
        };
        db.AuditRuns.Add(auditRun);
        await db.SaveChangesAsync();

        // Leere Antwort-Zeilen für jede Vorlagenfrage – werden in der UI befüllt.
        var questions = await db.AuditQuestions.Where(q => q.AuditTemplateId == template.Id).ToListAsync();
        foreach (var question in questions)
        {
            db.AuditAnswers.Add(new AuditAnswer
            {
                AuditRunId = auditRun.Id,
                AuditQuestionId = question.Id,
                ComplianceLevel = question.SortOrder == 1 ? ComplianceLevel.Compliant : ComplianceLevel.Open
            });
        }

        db.Measures.AddRange(
            new Measure
            {
                TenantId = tenant.Id,
                AuditRunId = auditRun.Id,
                Title = "Verarbeitungsverzeichnis aktualisieren",
                Description = "Fehlende Verarbeitungstätigkeiten ergänzen.",
                Status = MeasureStatus.Open,
                DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14))
            },
            new Measure
            {
                TenantId = tenant.Id,
                AuditRunId = auditRun.Id,
                Title = "Zugriffskonzept prüfen",
                Status = MeasureStatus.InProgress,
                DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7))
            });

        // Beispiel-Eintrag im Verzeichnis von Verarbeitungstätigkeiten (VVT).
        var processingActivity = new ProcessingActivity
        {
            TenantId = tenant.Id,
            Name = "Personalverwaltung",
            Description = "Verarbeitung von Mitarbeiterstammdaten im HR-Bereich.",
            Purpose = "Durchführung des Beschäftigungsverhältnisses und Lohnabrechnung.",
            ResponsibleDepartment = "Personal / HR",
            LegalBasis = "Art. 6 Abs. 1 lit. b DSGVO (Vertragserfüllung); Art. 6 Abs. 1 lit. c (rechtliche Verpflichtung).",
            DataSubjectCategories = "Beschäftigte, Bewerber",
            PersonalDataCategories = "Stammdaten, Vertragsdaten, Lohn- und Gehaltsdaten",
            Recipients = "Interne HR-Abteilung; externer Lohnbuchhalter (Auftragsverarbeiter)",
            ThirdCountryTransfer = false,
            RetentionPeriod = "10 Jahre nach Beendigung des Beschäftigungsverhältnisses (steuerrechtlich).",
            DpiaRequired = false,
            Status = ProcessingActivityStatus.Active,
            Owner = "Leitung Personal"
        };
        db.ProcessingActivities.Add(processingActivity);

        var demoTom = new Tom
        {
            TenantId = tenant.Id,
            Title = "Rollenbasierte Zugriffskontrolle auf HR-Systeme",
            Description = "Zugriffe auf Personalakten nur für berechtigte HR-Mitarbeiter; jährliche Berechtigungsprüfung.",
            Category = TomCategory.AccessControl,
            ProtectionGoal = TomProtectionGoal.Confidentiality,
            ImplementationStatus = TomImplementationStatus.Implemented,
            Owner = "IT-Sicherheit",
            ValidFrom = DateOnly.FromDateTime(DateTime.Today.AddMonths(-6)),
            NextReviewAt = DateOnly.FromDateTime(DateTime.Today.AddMonths(6)),
            EvidenceReference = "Richtlinie IT-Zugriff v2.1; Berechtigungsmatrix HR-System"
        };
        db.Toms.Add(demoTom);

        await db.SaveChangesAsync();

        db.ProcessingActivityToms.Add(new ProcessingActivityTom
        {
            TenantId = tenant.Id,
            TomId = demoTom.Id,
            ProcessingActivityId = processingActivity.Id,
            CreatedAt = DateTime.UtcNow
        });

        var payrollProvider = new Domain.Entities.ServiceProvider
        {
            TenantId = tenant.Id,
            Name = "Lohnbuchhaltung Müller GmbH",
            Description = "Externe Lohnabrechnung für Beschäftigte.",
            ProviderType = ServiceProviderType.Payroll,
            ServicePurpose = "Lohn- und Gehaltsabrechnung",
            Country = "Deutschland",
            IsDataProcessor = true,
            DataProcessingAgreementExists = true,
            DataProcessingAgreementDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-12)),
            DataProcessingAgreementReviewedAt = DateOnly.FromDateTime(DateTime.Today.AddMonths(-2)),
            DataProcessingAgreementReviewResult = "AVV geprüft, TOM-Anlage vorhanden.",
            TomsReviewed = true,
            TomsReviewedAt = DateOnly.FromDateTime(DateTime.Today.AddMonths(-2)),
            ThirdCountryInvolvement = false,
            RiskAssessment = ServiceProviderRiskAssessment.Medium,
            Status = ServiceProviderStatus.Approved,
            ResponsiblePerson = "Leitung Personal"
        };
        db.ServiceProviders.Add(payrollProvider);

        await db.SaveChangesAsync();

        db.ProcessingActivityServiceProviders.Add(new ProcessingActivityServiceProvider
        {
            TenantId = tenant.Id,
            ServiceProviderId = payrollProvider.Id,
            ProcessingActivityId = processingActivity.Id,
            RoleInProcessing = ProcessingRole.DataProcessor,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        await CreateUserAsync(userManager, "admin@demo.local", "Admin Demo", "Demo123!", tenant.Id, DsmsRoles.Admin);
        await CreateUserAsync(userManager, "auditor@demo.local", "Auditor Demo", "Demo123!", tenant.Id, DsmsRoles.Auditor);
        await CreateUserAsync(userManager, "user@demo.local", "Benutzer Demo", "Demo123!", tenant.Id, DsmsRoles.User);

        auditRun.AssignedUserId = (await userManager.FindByEmailAsync("auditor@demo.local"))!.Id;
        await db.SaveChangesAsync();
    }

    private static async Task CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string displayName,
        string password,
        int tenantId,
        string role)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true, // Demo: kein E-Mail-Bestätigungsflow
            DisplayName = displayName,
            TenantId = tenantId
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, role);
    }
}
