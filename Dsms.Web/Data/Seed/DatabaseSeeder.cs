using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Data.Seed;

/// <summary>
/// Wendet ausstehende Migrationen an und legt Demo-Daten idempotent an.
/// Wird beim Anwendungsstart aus <c>Program.cs</c> aufgerufen – nur für Entwicklung/Demo gedacht.
/// </summary>
public static class DatabaseSeeder
{
    private const string DemoLicenseNumber = "LIC-DEMO-000001";
    private const string DemoCustomerEmail = "demo@example.local";
    private const string DemoTenantHauptsitz = "Demo Mandant Hauptsitz";
    private const string DemoTenantSued = "Demo Mandant Niederlassung Süd";
    private const string LegacyTenantName = "Demo GmbH";
    private const string DemoAuditTemplateTitle = "DSGVO-Basisaudit";
    private const string DemoPassword = "Demo123!";

    /// <summary>
    /// Rollen und Email-Vorlagen immer idempotent; Demo-Lizenz und -Umgebung ebenfalls idempotent.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();
        await EmailTemplateSeeder.SeedAsync(db);
        await SeedRolesAsync(roleManager);

        var demoLicense = await EnsureDemoLicenseAsync(db);
        await EnsureDemoEnvironmentAsync(db, userManager, demoLicense);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in DsmsRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task<License> EnsureDemoLicenseAsync(ApplicationDbContext db)
    {
        var license = await db.Licenses
            .FirstOrDefaultAsync(l => l.LicenseNumber == DemoLicenseNumber);

        var validFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var validUntil = DateTime.UtcNow.Date.AddYears(1);

        if (license is null)
        {
            license = new License
            {
                Id = Guid.NewGuid(),
                LicenseNumber = DemoLicenseNumber,
                CreatedAt = DateTime.UtcNow
            };
            db.Licenses.Add(license);
        }

        license.CustomerName = "Demo Kunde GmbH";
        license.CustomerEmail = DemoCustomerEmail;
        license.PlanName = "Demo";
        license.Status = "Active";
        license.ValidFrom = validFrom;
        license.ValidUntil = validUntil;
        license.InternalNote = "Automatisch durch Demo-Seeding erstellt.";
        license.MaxTenants = 3;
        license.MaxAdmins = 3;
        license.MaxUsersPerTenant = 10;
        license.MaxAuditorsPerTenant = 3;
        license.MaxCustomAuditTemplatesPerTenant = 5;
        license.MaxActiveAuditsPerTenant = 5;
        license.MaxProcessingActivitiesPerTenant = 25;
        license.MaxDpiaPerTenant = 10;
        license.MaxTomsPerTenant = 25;
        license.MaxProcessorsPerTenant = 15;
        license.MaxActiveMeasuresPerTenant = 20;
        license.MaxStorageMb = 500;
        license.MaxEmailRemindersPerMonth = 100;
        license.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return license;
    }

    private static async Task EnsureDemoEnvironmentAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        License demoLicense)
    {
        var hauptsitz = await EnsureDemoTenantAsync(db, demoLicense.Id, DemoTenantHauptsitz);
        var sued = await EnsureDemoTenantAsync(db, demoLicense.Id, DemoTenantSued);

        await EnsureDemoBusinessDataAsync(db, hauptsitz);
        await EnsureDemoSouthTenantDataAsync(db, sued);

        await EnsureUserAsync(db, userManager, "superuser@demo.local", "Superuser Demo",
            DemoPassword, tenantId: null, licenseId: null, DsmsRoles.Superuser);
        await EnsureUserAsync(db, userManager, "admin@demo.local", "Admin Demo",
            DemoPassword, hauptsitz.Id, demoLicense.Id, DsmsRoles.Admin);
        await EnsureUserAsync(db, userManager, "auditor@demo.local", "Auditor Demo",
            DemoPassword, hauptsitz.Id, licenseId: null, DsmsRoles.Auditor);
        await EnsureUserAsync(db, userManager, "user@demo.local", "Benutzer Demo",
            DemoPassword, hauptsitz.Id, licenseId: null, DsmsRoles.User);
        await EnsureUserAsync(db, userManager, "auditor.sued@demo.local", "Auditor Süd Demo",
            DemoPassword, sued.Id, licenseId: null, DsmsRoles.Auditor);
        await EnsureUserAsync(db, userManager, "user.sued@demo.local", "Benutzer Süd Demo",
            DemoPassword, sued.Id, licenseId: null, DsmsRoles.User);
    }

    private static async Task<Tenant> EnsureDemoTenantAsync(
        ApplicationDbContext db,
        Guid licenseId,
        string name)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Name == name);

        if (tenant is null && name == DemoTenantHauptsitz)
        {
            tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Name == LegacyTenantName);
            if (tenant is not null)
            {
                tenant.Name = DemoTenantHauptsitz;
            }
        }

        if (tenant is null)
        {
            tenant = new Tenant
            {
                Name = name,
                LegalName = name,
                IsActive = true,
                LicenseId = licenseId
            };
            db.Tenants.Add(tenant);
        }
        else
        {
            tenant.LicenseId = licenseId;
            tenant.IsActive = true;
            if (string.IsNullOrWhiteSpace(tenant.LegalName))
            {
                tenant.LegalName = name;
            }
        }

        await db.SaveChangesAsync();
        return tenant;
    }

    private static async Task EnsureDemoBusinessDataAsync(ApplicationDbContext db, Tenant tenant)
    {
        if (await db.AuditTemplates
                .IgnoreQueryFilters()
                .AnyAsync(t => t.TenantId == tenant.Id && t.Title == DemoAuditTemplateTitle))
        {
            await EnsureDemoDocumentAsync(db, tenant);
            return;
        }

        var template = new AuditTemplate
        {
            TenantId = tenant.Id,
            TemplateType = AuditTemplateType.Tenant,
            Title = DemoAuditTemplateTitle,
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
            StartedAt = DateTime.UtcNow.AddDays(-7),
            TemplateTitleSnapshot = template.Title,
            TemplateVersionSnapshot = template.Version
        };
        db.AuditRuns.Add(auditRun);
        await db.SaveChangesAsync();

        var questions = await db.AuditQuestions.Where(q => q.AuditTemplateId == template.Id).ToListAsync();
        foreach (var question in questions)
        {
            db.AuditAnswers.Add(new AuditAnswer
            {
                AuditRunId = auditRun.Id,
                AuditQuestionId = question.Id,
                QuestionText = question.Text,
                QuestionSortOrder = question.SortOrder,
                QuestionCategory = question.Category,
                QuestionIsRequired = question.IsRequired,
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

        db.DataProtectionImpactAssessments.Add(new DataProtectionImpactAssessment
        {
            TenantId = tenant.Id,
            ProcessingActivityId = processingActivity.Id,
            Title = "DSFA Personalverwaltung",
            ProcessingDescription = "Bewertung der HR-Verarbeitung inkl. Lohnabrechnung.",
            ReasonForDpia = "Regelmäßige Überprüfung bei sensiblen Beschäftigtendaten.",
            Status = DpiaStatus.Draft,
            ResponsiblePerson = "Datenschutzbeauftragter"
        });

        await db.SaveChangesAsync();
        await EnsureDemoDocumentAsync(db, tenant);
    }

    private static async Task EnsureDemoDocumentAsync(ApplicationDbContext db, Tenant tenant)
    {
        if (await db.EvidenceDocuments
                .IgnoreQueryFilters()
                .AnyAsync(d => d.TenantId == tenant.Id && d.FileName == "demo-nachweis.pdf"))
        {
            return;
        }

        db.EvidenceDocuments.Add(new EvidenceDocument
        {
            TenantId = tenant.Id,
            FileName = "demo-nachweis.pdf",
            StoragePath = $"demo/{tenant.Id}/demo-nachweis.pdf",
            FileSizeBytes = 5 * 1024 * 1024,
            ContentType = "application/pdf"
        });
        await db.SaveChangesAsync();
    }

    private static async Task EnsureDemoSouthTenantDataAsync(ApplicationDbContext db, Tenant tenant)
    {
        if (await db.ProcessingActivities
                .IgnoreQueryFilters()
                .AnyAsync(p => p.TenantId == tenant.Id && p.Name == "Kundenverwaltung Süd"))
        {
            return;
        }

        db.ProcessingActivities.Add(new ProcessingActivity
        {
            TenantId = tenant.Id,
            Name = "Kundenverwaltung Süd",
            Description = "Verarbeitung von Kundendaten in der Niederlassung Süd.",
            Purpose = "Kundenbetreuung und Vertragsabwicklung.",
            ResponsibleDepartment = "Vertrieb Süd",
            LegalBasis = "Art. 6 Abs. 1 lit. b DSGVO",
            DataSubjectCategories = "Kunden",
            PersonalDataCategories = "Kontaktdaten, Vertragsdaten",
            ThirdCountryTransfer = false,
            Status = ProcessingActivityStatus.Active,
            Owner = "Leitung Vertrieb Süd"
        });

        db.Measures.Add(new Measure
        {
            TenantId = tenant.Id,
            Title = "Datenschutzhinweise auf Webformular ergänzen",
            Status = MeasureStatus.Open,
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(21))
        });

        await db.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        string email,
        string displayName,
        string password,
        int? tenantId,
        Guid? licenseId,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                TenantId = tenantId,
                LicenseId = role == DsmsRoles.Admin ? licenseId : null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(user, role);
        }
        else
        {
            user.DisplayName = displayName;
            user.TenantId = tenantId;
            user.IsActive = true;
            user.LicenseId = role == DsmsRoles.Admin ? licenseId : null;

            await userManager.UpdateAsync(user);

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }

        if (tenantId is int tid)
        {
            var linkExists = await db.UserTenants
                .IgnoreQueryFilters()
                .AnyAsync(ut => ut.UserId == user.Id && ut.TenantId == tid);

            if (!linkExists)
            {
                db.UserTenants.Add(new UserTenant
                {
                    UserId = user.Id,
                    TenantId = tid,
                    AssignedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        }

        if (role == DsmsRoles.Auditor && tenantId is int auditTenantId)
        {
            var auditRun = await db.AuditRuns
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.TenantId == auditTenantId && r.Title == "Audit Q1 2026");

            if (auditRun is not null && auditRun.AssignedUserId is null)
            {
                auditRun.AssignedUserId = user.Id;
                await db.SaveChangesAsync();
            }
        }
    }
}
