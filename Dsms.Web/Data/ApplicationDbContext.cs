using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServiceProviderEntity = Dsms.Web.Domain.Entities.ServiceProvider;

namespace Dsms.Web.Data;

/// <summary>
/// EF-Core-Kontext für Identity und DSMS-Fachdaten.
/// Global Query Filters isolieren Fachdaten nach <see cref="TenantContextAccessor.CurrentTenantId"/>.
/// Löschverhalten in <see cref="OnModelCreating"/> schützt referenzielle Integrität (kein Kaskaden-Löschen von Mandanten).
/// </summary>
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    TenantContextAccessor tenantContextAccessor,
    ArchiveViewContextAccessor archiveViewContextAccessor)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<License> Licenses => Set<License>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();
    public DbSet<PendingSignup> PendingSignups => Set<PendingSignup>();
    public DbSet<LegalAcceptance> LegalAcceptances => Set<LegalAcceptance>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<UserTenant> UserTenants => Set<UserTenant>();
    public DbSet<SupportAccessGrant> SupportAccessGrants => Set<SupportAccessGrant>();
    public DbSet<AuditTemplate> AuditTemplates => Set<AuditTemplate>();
    public DbSet<AuditQuestion> AuditQuestions => Set<AuditQuestion>();
    public DbSet<AuditRun> AuditRuns => Set<AuditRun>();
    public DbSet<AuditAnswer> AuditAnswers => Set<AuditAnswer>();
    public DbSet<Measure> Measures => Set<Measure>();
    public DbSet<EvidenceDocument> EvidenceDocuments => Set<EvidenceDocument>();
    public DbSet<DocumentCategory> DocumentCategories => Set<DocumentCategory>();
    public DbSet<DocumentLink> DocumentLinks => Set<DocumentLink>();
    public DbSet<ProcessingActivity> ProcessingActivities => Set<ProcessingActivity>();
    public DbSet<Tom> Toms => Set<Tom>();
    public DbSet<ProcessingActivityTom> ProcessingActivityToms => Set<ProcessingActivityTom>();
    public DbSet<ServiceProviderEntity> ServiceProviders => Set<ServiceProviderEntity>();
    public DbSet<ProcessingActivityServiceProvider> ProcessingActivityServiceProviders => Set<ProcessingActivityServiceProvider>();
    public DbSet<ServiceProviderTom> ServiceProviderToms => Set<ServiceProviderTom>();
    public DbSet<ProcessingActivityMeasure> ProcessingActivityMeasures => Set<ProcessingActivityMeasure>();
    public DbSet<ProcessingActivityAuditAnswer> ProcessingActivityAuditAnswers => Set<ProcessingActivityAuditAnswer>();
    public DbSet<DataProtectionImpactAssessment> DataProtectionImpactAssessments => Set<DataProtectionImpactAssessment>();
    public DbSet<EmailSettings> EmailSettings => Set<EmailSettings>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<LogEntry> LogEntries => Set<LogEntry>();
    public DbSet<TenantOnboardingTask> TenantOnboardingTasks => Set<TenantOnboardingTask>();
    public DbSet<PageHelpContent> PageHelpContents => Set<PageHelpContent>();
    public DbSet<PrivacyIncident> PrivacyIncidents => Set<PrivacyIncident>();
    public DbSet<PrivacyIncidentProcessingActivity> PrivacyIncidentProcessingActivities => Set<PrivacyIncidentProcessingActivity>();
    public DbSet<PrivacyIncidentServiceProvider> PrivacyIncidentServiceProviders => Set<PrivacyIncidentServiceProvider>();
    public DbSet<PrivacyIncidentMeasure> PrivacyIncidentMeasures => Set<PrivacyIncidentMeasure>();
    public DbSet<PrivacyIncidentTom> PrivacyIncidentToms => Set<PrivacyIncidentTom>();
    public DbSet<DataSubjectRequest> DataSubjectRequests => Set<DataSubjectRequest>();
    public DbSet<DataSubjectRequestProcessingActivity> DataSubjectRequestProcessingActivities => Set<DataSubjectRequestProcessingActivity>();
    public DbSet<DataSubjectRequestMeasure> DataSubjectRequestMeasures => Set<DataSubjectRequestMeasure>();
    public DbSet<DataSubjectRequestServiceProvider> DataSubjectRequestServiceProviders => Set<DataSubjectRequestServiceProvider>();
    public DbSet<DataProtectionRole> DataProtectionRoles => Set<DataProtectionRole>();
    public DbSet<TrainingTemplate> TrainingTemplates => Set<TrainingTemplate>();
    public DbSet<TrainingTemplateSection> TrainingTemplateSections => Set<TrainingTemplateSection>();
    public DbSet<TrainingTemplateAsset> TrainingTemplateAssets => Set<TrainingTemplateAsset>();
    public DbSet<TrainingQuestion> TrainingQuestions => Set<TrainingQuestion>();
    public DbSet<TrainingQuestionOption> TrainingQuestionOptions => Set<TrainingQuestionOption>();
    public DbSet<Training> Trainings => Set<Training>();
    public DbSet<TrainingParticipant> TrainingParticipants => Set<TrainingParticipant>();
    public DbSet<TrainingAssignment> TrainingAssignments => Set<TrainingAssignment>();
    public DbSet<TrainingAssignmentSectionProgress> TrainingAssignmentSectionProgress => Set<TrainingAssignmentSectionProgress>();
    public DbSet<TrainingQuizAttempt> TrainingQuizAttempts => Set<TrainingQuizAttempt>();
    public DbSet<TrainingQuizAnswer> TrainingQuizAnswers => Set<TrainingQuizAnswer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserTenant>(e =>
        {
            e.ToTable("UserTenants");
            e.HasKey(ut => new { ut.UserId, ut.TenantId });
            e.HasOne(ut => ut.User)
                .WithMany(u => u.UserTenants)
                .HasForeignKey(ut => ut.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ut => ut.Tenant)
                .WithMany(t => t.UserTenants)
                .HasForeignKey(ut => ut.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(ut => ut.TenantId);
        });

        builder.Entity<SupportAccessGrant>(e =>
        {
            e.ToTable("SupportAccessGrants");
            e.Property(g => g.Reason).HasMaxLength(500);
            e.Property(g => g.InternalNote).HasMaxLength(1000);
            e.Property(g => g.RevokedByUserId).HasMaxLength(450);
            e.HasOne(g => g.Tenant)
                .WithMany()
                .HasForeignKey(g => g.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(g => g.GrantedByUser)
                .WithMany()
                .HasForeignKey(g => g.GrantedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(g => new { g.TenantId, g.RevokedAt, g.ValidUntil });
        });

        builder.Entity<License>(e =>
        {
            e.ToTable("Licenses");
            e.HasKey(l => l.Id);
            e.Property(l => l.LicenseNumber).HasMaxLength(50).IsRequired();
            e.Property(l => l.CustomerName).HasMaxLength(200).IsRequired();
            e.Property(l => l.CustomerEmail).HasMaxLength(255);
            e.Property(l => l.PlanName).HasMaxLength(100).IsRequired();
            e.Property(l => l.Status).HasMaxLength(50).IsRequired();
            e.Property(l => l.InternalNote).HasColumnType("text");
            e.Property(l => l.PlanName).HasDefaultValue("Manual");
            e.Property(l => l.Status).HasDefaultValue("Active");
            e.Property(l => l.HasTrainingModule).HasDefaultValue(true);
            e.HasIndex(l => l.LicenseNumber).IsUnique();
            e.HasIndex(l => l.CustomerName);
            e.HasIndex(l => l.Status);
        });

        builder.Entity<SubscriptionPlan>(e =>
        {
            e.ToTable("SubscriptionPlans");
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(100).IsRequired();
            e.Property(p => p.DisplayName).HasMaxLength(200).IsRequired();
            e.Property(p => p.Description).HasColumnType("text");
            e.Property(p => p.Currency).HasMaxLength(10).IsRequired().HasDefaultValue("EUR");
            e.Property(p => p.ExternalProductId).HasMaxLength(200);
            e.Property(p => p.ExternalMonthlyPriceId).HasMaxLength(200);
            e.Property(p => p.ExternalYearlyPriceId).HasMaxLength(200);
            e.Property(p => p.InternalNote).HasColumnType("text");
            e.Property(p => p.IsActive).HasDefaultValue(true);
            e.Property(p => p.IsFree).HasDefaultValue(false);
            e.Property(p => p.IsPublicSignupEnabled).HasDefaultValue(false);
            e.Property(p => p.SortOrder).HasDefaultValue(0);
            e.Property(p => p.PriceMonthly).HasPrecision(18, 2);
            e.Property(p => p.PriceYearly).HasPrecision(18, 2);
            e.Property(p => p.IsPromotionalPriceEnabled).HasDefaultValue(false);
            e.Property(p => p.HasTrainingModule).HasDefaultValue(true);
            e.Property(p => p.PromotionalMonthlyPrice).HasPrecision(18, 2);
            e.Property(p => p.PromotionalYearlyPrice).HasPrecision(18, 2);
            e.Property(p => p.PromotionalBadgeText).HasMaxLength(100);
            e.HasIndex(p => p.Name).IsUnique();
            e.HasIndex(p => p.IsActive);
            e.HasIndex(p => p.SortOrder);
        });

        builder.Entity<DiscountCode>(e =>
        {
            e.ToTable("DiscountCodes");
            e.HasKey(d => d.Id);
            e.Property(d => d.Code).HasMaxLength(64).IsRequired();
            e.Property(d => d.Name).HasMaxLength(200).IsRequired();
            e.Property(d => d.Description).HasColumnType("text");
            e.Property(d => d.InternalNote).HasColumnType("text");
            e.Property(d => d.AppliesToBillingCycle).HasMaxLength(20);
            e.Property(d => d.PercentageValue).HasPrecision(5, 2);
            e.Property(d => d.FixedAmountValue).HasPrecision(18, 2);
            e.Property(d => d.IsActive).HasDefaultValue(true);
            e.Property(d => d.CurrentRedemptions).HasDefaultValue(0);
            e.Property(d => d.CreatedByUserId).HasMaxLength(450);
            e.Property(d => d.UpdatedByUserId).HasMaxLength(450);
            e.HasIndex(d => d.Code).IsUnique();
            e.HasIndex(d => d.IsActive);
            e.HasIndex(d => d.AppliesToPlanId);
            e.HasOne(d => d.AppliesToPlan)
                .WithMany()
                .HasForeignKey(d => d.AppliesToPlanId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<PendingSignup>(e =>
        {
            e.ToTable("PendingSignups");
            e.HasKey(p => p.Id);
            e.Property(p => p.Status).HasMaxLength(50).IsRequired();
            e.Property(p => p.PlanNameSnapshot).HasMaxLength(100);
            e.Property(p => p.PlanDisplayNameSnapshot).HasMaxLength(200);
            e.Property(p => p.CurrencySnapshot).HasMaxLength(10);
            e.Property(p => p.CustomerName).HasMaxLength(200).IsRequired();
            e.Property(p => p.CustomerEmail).HasMaxLength(255);
            e.Property(p => p.TenantName).HasMaxLength(200).IsRequired();
            e.Property(p => p.TenantLegalName).HasMaxLength(300);
            e.Property(p => p.TenantEmail).HasMaxLength(255);
            e.Property(p => p.TenantPhone).HasMaxLength(50);
            e.Property(p => p.TenantAddress).HasColumnType("text");
            e.Property(p => p.AdminEmail).HasMaxLength(255).IsRequired();
            e.Property(p => p.AdminDisplayName).HasMaxLength(200).IsRequired();
            e.Property(p => p.AdminFirstName).HasMaxLength(100);
            e.Property(p => p.AdminLastName).HasMaxLength(100);
            e.Property(p => p.PaymentProvider).HasMaxLength(50);
            e.Property(p => p.ExternalPaymentId).HasMaxLength(200);
            e.Property(p => p.ExternalCheckoutUrl).HasMaxLength(500);
            e.Property(p => p.Currency).HasMaxLength(10);
            e.Property(p => p.PlanPriceMonthlySnapshot).HasPrecision(18, 2);
            e.Property(p => p.PlanPriceYearlySnapshot).HasPrecision(18, 2);
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.Property(p => p.DiscountCodeSnapshot).HasMaxLength(64);
            e.Property(p => p.DiscountNameSnapshot).HasMaxLength(200);
            e.Property(p => p.DiscountTypeSnapshot).HasMaxLength(50);
            e.Property(p => p.DiscountValueSnapshot).HasPrecision(18, 2);
            e.Property(p => p.OriginalAmount).HasPrecision(18, 2);
            e.Property(p => p.DiscountAmount).HasPrecision(18, 2);
            e.Property(p => p.FinalAmount).HasPrecision(18, 2);
            e.Property(p => p.ProvisionedLicenseNumber).HasMaxLength(50);
            e.Property(p => p.ProvisionedTenantName).HasMaxLength(200);
            e.Property(p => p.ProvisionedAdminUserId).HasMaxLength(450);
            e.Property(p => p.ErrorMessage).HasColumnType("text");
            e.Property(p => p.InternalNote).HasColumnType("text");
            e.Property(p => p.Source).HasMaxLength(100);
            e.Property(p => p.MetadataJson).HasColumnType("text");
            e.Property(p => p.BillingCompanyName).HasMaxLength(200);
            e.Property(p => p.BillingEmail).HasMaxLength(255);
            e.Property(p => p.BillingStreet).HasMaxLength(300);
            e.Property(p => p.BillingPostalCode).HasMaxLength(20);
            e.Property(p => p.BillingCity).HasMaxLength(100);
            e.Property(p => p.BillingCountry).HasMaxLength(100);
            e.Property(p => p.BillingVatId).HasMaxLength(50);
            e.Property(p => p.BillingReference).HasMaxLength(100);
            e.Property(p => p.BillingCycle).HasMaxLength(20);
            e.Property(p => p.BillingStatus).HasMaxLength(50);
            e.Property(p => p.BillingNote).HasColumnType("text");
            e.Property(p => p.CurrentBillingAmount).HasPrecision(18, 2);
            e.Property(p => p.CurrentBillingCurrency).HasMaxLength(10);
            e.Property(p => p.CurrentBillingCycle).HasMaxLength(20);
            e.Property(p => p.CurrentBillingAmountUpdatedByUserId).HasMaxLength(450);
            e.Property(p => p.Status).HasDefaultValue(PendingSignupStatuses.Draft);
            e.HasIndex(p => p.CreatedAt);
            e.HasIndex(p => p.Status);
            e.HasIndex(p => p.PlanId);
            e.HasIndex(p => p.AdminEmail);
            e.HasIndex(p => p.CustomerEmail);
            e.HasIndex(p => p.ExternalPaymentId);
            e.HasIndex(p => p.ProvisionedLicenseId);
            e.HasIndex(p => p.ExpiresAt);
        });

        builder.Entity<LegalAcceptance>(e =>
        {
            e.ToTable("LegalAcceptances");
            e.HasKey(l => l.Id);
            e.Property(l => l.UserId).HasMaxLength(450).IsRequired();
            e.Property(l => l.LegalVersion).HasMaxLength(32).IsRequired();
            e.Property(l => l.EffectiveDate).HasMaxLength(32).IsRequired();
            e.Property(l => l.AnonymizedIpAddress).HasMaxLength(45);
            e.Property(l => l.UserAgent).HasMaxLength(512);
            e.Property(l => l.SignupEmail).HasMaxLength(255);
            e.Property(l => l.TenantNameSnapshot).HasMaxLength(200);
            e.Property(l => l.CompanyNameSnapshot).HasMaxLength(200);
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => l.UserId);
            e.HasIndex(l => l.LegalVersion);
            e.HasIndex(l => new { l.TenantId, l.LegalVersion });
            e.HasIndex(l => l.PendingSignupId);
            e.HasOne(l => l.Tenant)
                .WithMany()
                .HasForeignKey(l => l.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.LicenseId);
            e.HasOne<License>()
                .WithMany()
                .HasForeignKey(u => u.LicenseId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(u => u.LicenseId);
        });

        builder.Entity<Tenant>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(200).IsRequired();
            e.Property(t => t.LegalName).HasMaxLength(300);
            e.Property(t => t.Street).HasMaxLength(300);
            e.Property(t => t.HouseNumber).HasMaxLength(20);
            e.Property(t => t.PostalCode).HasMaxLength(20);
            e.Property(t => t.City).HasMaxLength(100);
            e.Property(t => t.Country).HasMaxLength(100);
            e.Property(t => t.ContactName).HasMaxLength(200);
            e.Property(t => t.VatId).HasMaxLength(50);
            e.Property(t => t.Phone).HasMaxLength(50);
            e.Property(t => t.Email).HasMaxLength(255);
            e.Property(t => t.Website).HasMaxLength(500);
            e.Property(t => t.DpoName).HasMaxLength(200);
            e.Property(t => t.DpoStreet).HasMaxLength(300);
            e.Property(t => t.DpoHouseNumber).HasMaxLength(20);
            e.Property(t => t.DpoPostalCode).HasMaxLength(20);
            e.Property(t => t.DpoCity).HasMaxLength(100);
            e.Property(t => t.DpoPhone).HasMaxLength(50);
            e.Property(t => t.DpoEmail).HasMaxLength(255);
            e.Property(t => t.DeletionRequestedByUserId).HasMaxLength(450);
            e.Property(t => t.IsActive).HasDefaultValue(true);
            e.Property(t => t.IsDeletionRequested).HasDefaultValue(false);
            e.HasOne(t => t.License)
                .WithMany(l => l.Tenants)
                .HasForeignKey(t => t.LicenseId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(t => t.LicenseId);
        });

        ApplyTenantQueryFilters(builder);

        builder.Entity<AuditTemplate>(e =>
        {
            e.Property(t => t.Title).HasMaxLength(200).IsRequired();
            e.Property(t => t.Version).HasMaxLength(20);
            e.Property(t => t.ArchivedByUserId).HasMaxLength(450);
            e.Property(t => t.SubmittedByUserId).HasMaxLength(450);
            e.Property(t => t.ReviewedByUserId).HasMaxLength(450);
            e.Property(t => t.ReviewComment).HasMaxLength(2000);
            e.HasOne(t => t.Tenant).WithMany(t => t.AuditTemplates).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditQuestion>(e =>
        {
            e.Property(q => q.Text).HasMaxLength(2000).IsRequired();
            e.Property(q => q.Category).HasMaxLength(100);
            // Fragen werden mit der Vorlage mitgelöscht.
            e.HasOne(q => q.AuditTemplate).WithMany(t => t.Questions).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditRun>(e =>
        {
            e.Property(r => r.Title).HasMaxLength(200).IsRequired();
            e.Property(r => r.TemplateTitleSnapshot).HasMaxLength(200);
            e.Property(r => r.TemplateVersionSnapshot).HasMaxLength(20);
            e.Property(r => r.ArchivedByUserId).HasMaxLength(450);
            e.HasOne(r => r.Tenant).WithMany(t => t.AuditRuns).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.AuditTemplate).WithMany(t => t.AuditRuns).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditAnswer>(e =>
        {
            e.Property(a => a.QuestionText).HasMaxLength(2000);
            e.Property(a => a.QuestionCategory).HasMaxLength(100);
            e.HasOne(a => a.AuditRun).WithMany(r => r.Answers).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.AuditQuestion).WithMany(q => q.Answers).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(a => new { a.AuditRunId, a.AuditQuestionId }).IsUnique();
        });

        builder.Entity<Measure>(e =>
        {
            e.Property(m => m.Title).HasMaxLength(200).IsRequired();
            e.Property(m => m.ArchivedByUserId).HasMaxLength(450);
            e.HasOne(m => m.Tenant).WithMany(t => t.Measures).OnDelete(DeleteBehavior.Restrict);
            // Maßnahme bleibt erhalten, Verknüpfung zum Audit wird aufgehoben.
            e.HasOne(m => m.AuditRun).WithMany(r => r.Measures).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(m => m.AuditAnswer).WithMany(a => a.Measures).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(m => m.AuditAnswerId);
        });

        builder.Entity<EvidenceDocument>(e =>
        {
            e.Property(d => d.DocumentType).IsRequired();
            e.Property(d => d.FileName).HasMaxLength(255).IsRequired();
            e.Property(d => d.StoragePath).HasMaxLength(500).IsRequired();
            e.Property(d => d.ArchivedByUserId).HasMaxLength(450);
            e.HasOne(d => d.Tenant).WithMany(t => t.Documents).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.DocumentCategory).WithMany(c => c.Documents).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(d => d.DocumentCategoryId);
        });

        builder.Entity<DocumentCategory>(e =>
        {
            e.ToTable("DocumentCategories");
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Description).HasMaxLength(500);
            e.Property(c => c.Color).HasMaxLength(20);
            e.Property(c => c.CreatedByUserId).HasMaxLength(450);
            e.Property(c => c.UpdatedByUserId).HasMaxLength(450);
            e.HasOne(c => c.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(c => c.TenantId);
            e.HasIndex(c => new { c.TenantId, c.Name }).IsUnique();
        });

        builder.Entity<DataProtectionRole>(e =>
        {
            e.ToTable("DataProtectionRoles");
            e.Property(r => r.RoleTitle).HasMaxLength(200).IsRequired();
            e.Property(r => r.PersonName).HasMaxLength(200);
            e.Property(r => r.Email).HasMaxLength(256);
            e.Property(r => r.Phone).HasMaxLength(50);
            e.Property(r => r.Department).HasMaxLength(200);
            e.Property(r => r.AreaOfResponsibility).HasMaxLength(2000);
            e.Property(r => r.ReportsTo).HasMaxLength(500);
            e.Property(r => r.Deputy).HasMaxLength(500);
            e.Property(r => r.Remarks).HasMaxLength(4000);
            e.Property(r => r.LinkedUserId).HasMaxLength(450);
            e.Property(r => r.CreatedByUserId).HasMaxLength(450);
            e.Property(r => r.UpdatedByUserId).HasMaxLength(450);
            e.Property(r => r.ArchivedByUserId).HasMaxLength(450);
            e.HasOne(r => r.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.LinkedUser).WithMany().HasForeignKey(r => r.LinkedUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.ReportsToRole).WithMany().HasForeignKey(r => r.ReportsToRoleId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.DeputyRole).WithMany().HasForeignKey(r => r.DeputyRoleId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(r => r.TenantId);
            e.HasIndex(r => new { r.TenantId, r.IsActive });
            e.HasIndex(r => r.LinkedUserId);
            e.HasIndex(r => r.RoleTitle);
        });

        builder.Entity<TrainingTemplate>(e =>
        {
            e.ToTable("TrainingTemplates");
            e.Property(t => t.Title).HasMaxLength(200).IsRequired();
            e.Property(t => t.Description).HasColumnType("text");
            e.Property(t => t.TargetAudience).HasMaxLength(200);
            e.Property(t => t.PassingScorePercent).HasDefaultValue(80);
            e.Property(t => t.IsActive).HasDefaultValue(true);
            e.Property(t => t.ArchivedByUserId).HasMaxLength(450);
            e.Property(t => t.CreatedByUserId).HasMaxLength(450);
            e.Property(t => t.UpdatedByUserId).HasMaxLength(450);
            e.Property(t => t.CommunitySubmittedByUserId).HasMaxLength(450);
            e.Property(t => t.CommunityReviewedByUserId).HasMaxLength(450);
            e.Property(t => t.CommunityReviewNote).HasMaxLength(2000);
            e.Property(t => t.CommunitySubmissionNote).HasMaxLength(2000);
            e.Property(t => t.CommunityRejectionReason).HasMaxLength(2000);
            e.HasOne(t => t.Tenant).WithMany(t => t.TrainingTemplates).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.CommunitySubmittedByTenant).WithMany().HasForeignKey(t => t.CommunitySubmittedByTenantId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.SourceTemplate).WithMany().HasForeignKey(t => t.SourceTemplateId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(t => t.TenantId);
            e.HasIndex(t => t.IsGlobal);
            e.HasIndex(t => t.IsActive);
            e.HasIndex(t => t.TrainingType);
            e.HasIndex(t => t.CommunityStatus);
            e.HasIndex(t => t.SourceTemplateId);
            e.HasIndex(t => new { t.TenantId, t.IsActive });
            e.HasIndex(t => new { t.IsGlobal, t.IsActive });
        });

        builder.Entity<TrainingTemplateSection>(e =>
        {
            e.ToTable("TrainingTemplateSections");
            e.Property(s => s.Title).HasMaxLength(200).IsRequired();
            e.Property(s => s.ContentMarkdown).HasColumnType("longtext");
            e.Property(s => s.CreatedByUserId).HasMaxLength(450);
            e.Property(s => s.UpdatedByUserId).HasMaxLength(450);
            e.HasOne(s => s.TrainingTemplate).WithMany(t => t.Sections).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => s.TrainingTemplateId);
            e.HasIndex(s => s.TenantId);
            e.HasIndex(s => new { s.TrainingTemplateId, s.SortOrder });
        });

        builder.Entity<TrainingTemplateAsset>(e =>
        {
            e.ToTable("TrainingTemplateAssets");
            e.Property(a => a.AssetKey).HasMaxLength(100).IsRequired();
            e.Property(a => a.OriginalFileName).HasMaxLength(255).IsRequired();
            e.Property(a => a.StoredFileName).HasMaxLength(255).IsRequired();
            e.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
            e.Property(a => a.StoragePath).HasMaxLength(500).IsRequired();
            e.Property(a => a.AltText).HasMaxLength(500);
            e.Property(a => a.CreatedByUserId).HasMaxLength(450);
            e.Property(a => a.UpdatedByUserId).HasMaxLength(450);
            e.HasOne(a => a.TrainingTemplate).WithMany(t => t.Assets).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => a.TrainingTemplateId);
            e.HasIndex(a => a.TenantId);
            e.HasIndex(a => a.AssetKey);
            e.HasIndex(a => new { a.TrainingTemplateId, a.AssetKey }).IsUnique();
        });

        builder.Entity<TrainingQuestion>(e =>
        {
            e.ToTable("TrainingQuestions");
            e.Property(q => q.QuestionText).HasMaxLength(2000).IsRequired();
            e.Property(q => q.Explanation).HasMaxLength(2000);
            e.Property(q => q.Points).HasDefaultValue(1);
            e.Property(q => q.CreatedByUserId).HasMaxLength(450);
            e.Property(q => q.UpdatedByUserId).HasMaxLength(450);
            e.HasOne(q => q.TrainingTemplate).WithMany(t => t.Questions).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(q => q.TrainingTemplateId);
            e.HasIndex(q => q.TenantId);
            e.HasIndex(q => new { q.TrainingTemplateId, q.SortOrder });
        });

        builder.Entity<TrainingQuestionOption>(e =>
        {
            e.ToTable("TrainingQuestionOptions");
            e.Property(o => o.AnswerText).HasMaxLength(1000).IsRequired();
            e.Property(o => o.Explanation).HasMaxLength(2000);
            e.Property(o => o.CreatedByUserId).HasMaxLength(450);
            e.Property(o => o.UpdatedByUserId).HasMaxLength(450);
            e.HasOne(o => o.TrainingQuestion).WithMany(q => q.Options).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(o => o.TrainingQuestionId);
            e.HasIndex(o => o.TenantId);
            e.HasIndex(o => new { o.TrainingQuestionId, o.SortOrder });
        });

        builder.Entity<Training>(e =>
        {
            e.ToTable("Trainings");
            e.Property(t => t.Title).HasMaxLength(200).IsRequired();
            e.Property(t => t.Description).HasColumnType("text");
            e.Property(t => t.TargetAudience).HasMaxLength(200);
            e.Property(t => t.ResponsibleName).HasMaxLength(200);
            e.Property(t => t.Notes).HasColumnType("text");
            e.Property(t => t.AccessCodeValidityDays).HasDefaultValue(14);
            e.Property(t => t.CreatedByUserId).HasMaxLength(450);
            e.Property(t => t.UpdatedByUserId).HasMaxLength(450);
            e.Property(t => t.ArchivedByUserId).HasMaxLength(450);
            e.HasOne(t => t.Tenant).WithMany(t => t.Trainings).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.TrainingTemplate).WithMany().HasForeignKey(t => t.TrainingTemplateId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.ResponsibleUser).WithMany().HasForeignKey(t => t.ResponsibleUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(t => t.TenantId);
            e.HasIndex(t => t.TrainingTemplateId);
            e.HasIndex(t => t.Status);
            e.HasIndex(t => t.TrainingType);
            e.HasIndex(t => t.ScheduledAt);
            e.HasIndex(t => t.CompletedAt);
            e.HasIndex(t => t.RepeatDueAt);
            e.HasIndex(t => new { t.TenantId, t.Status });
            e.HasIndex(t => new { t.TenantId, t.RepeatDueAt });
        });

        builder.Entity<TrainingParticipant>(e =>
        {
            e.ToTable("TrainingParticipants");
            e.Property(p => p.Name).HasMaxLength(200);
            e.Property(p => p.Email).HasMaxLength(255).IsRequired();
            e.Property(p => p.NormalizedEmail).HasMaxLength(255).IsRequired();
            e.Property(p => p.Department).HasMaxLength(200);
            e.Property(p => p.ExternalReference).HasMaxLength(100);
            e.Property(p => p.CreatedByUserId).HasMaxLength(450);
            e.Property(p => p.UpdatedByUserId).HasMaxLength(450);
            e.Property(p => p.ArchivedByUserId).HasMaxLength(450);
            e.HasOne(p => p.Tenant).WithMany(t => t.TrainingParticipants).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(p => p.TenantId);
            e.HasIndex(p => p.NormalizedEmail);
            e.HasIndex(p => new { p.TenantId, p.NormalizedEmail }).IsUnique();
            e.HasIndex(p => new { p.TenantId, p.IsActive });
        });

        builder.Entity<TrainingAssignment>(e =>
        {
            e.ToTable("TrainingAssignments");
            e.Property(a => a.ParticipantNameSnapshot).HasMaxLength(200);
            e.Property(a => a.ParticipantEmailSnapshot).HasMaxLength(255).IsRequired();
            e.Property(a => a.AccessCodeHash).HasMaxLength(500);
            e.Property(a => a.InvitationSentByUserId).HasMaxLength(450);
            e.Property(a => a.CreatedByUserId).HasMaxLength(450);
            e.Property(a => a.UpdatedByUserId).HasMaxLength(450);
            e.Property(a => a.ArchivedByUserId).HasMaxLength(450);
            e.HasOne(a => a.Tenant).WithMany(t => t.TrainingAssignments).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Training).WithMany(t => t.Assignments).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.TrainingParticipant).WithMany(p => p.Assignments)
                .HasForeignKey(a => a.TrainingParticipantId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(a => a.InvitationSentByUser).WithMany()
                .HasForeignKey(a => a.InvitationSentByUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(a => a.CertificateDocument).WithMany()
                .HasForeignKey(a => a.CertificateDocumentId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(a => a.CertificateDocumentId);
            e.HasIndex(a => a.TrainingId);
            e.HasIndex(a => a.TrainingParticipantId);
            e.HasIndex(a => a.ParticipantEmailSnapshot);
            e.HasIndex(a => a.Status);
            e.HasIndex(a => a.AccessCodeExpiresAtUtc);
            e.HasIndex(a => a.LockedUntilUtc);
            e.HasIndex(a => new { a.TenantId, a.TrainingId });
        });

        builder.Entity<TrainingAssignmentSectionProgress>(e =>
        {
            e.ToTable("TrainingAssignmentSectionProgress");
            e.HasOne(p => p.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.TrainingAssignment).WithMany(a => a.SectionProgress).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.TrainingTemplateSection).WithMany().OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => p.TenantId);
            e.HasIndex(p => p.TrainingAssignmentId);
            e.HasIndex(p => p.TrainingTemplateSectionId);
            e.HasIndex(p => new { p.TrainingAssignmentId, p.TrainingTemplateSectionId }).IsUnique();
        });

        builder.Entity<TrainingQuizAttempt>(e =>
        {
            e.ToTable("TrainingQuizAttempts");
            e.HasOne(a => a.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.TrainingAssignment).WithMany(x => x.QuizAttempts).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => a.TenantId);
            e.HasIndex(a => a.TrainingAssignmentId);
            e.HasIndex(a => a.AttemptNumber);
            e.HasIndex(a => a.SubmittedAtUtc);
            e.HasIndex(a => a.Passed);
        });

        builder.Entity<TrainingQuizAnswer>(e =>
        {
            e.ToTable("TrainingQuizAnswers");
            e.Property(a => a.AnswerTextSnapshot).HasMaxLength(1000);
            e.HasOne(a => a.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.TrainingQuizAttempt).WithMany(x => x.Answers).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.TrainingQuestion).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.TrainingQuestionOption).WithMany().OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(a => a.TenantId);
            e.HasIndex(a => a.TrainingQuizAttemptId);
            e.HasIndex(a => a.TrainingQuestionId);
            e.HasIndex(a => a.TrainingQuestionOptionId);
        });

        builder.Entity<DocumentLink>(e =>
        {
            e.ToTable("DocumentLinks");
            e.Property(l => l.CreatedByUserId).HasMaxLength(450);
            e.HasOne(l => l.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.Document).WithMany(d => d.Links).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => l.DocumentId);
            e.HasIndex(l => new { l.LinkedEntityType, l.LinkedEntityId });
            e.HasIndex(l => new { l.TenantId, l.LinkedEntityType, l.LinkedEntityId });
            e.HasIndex(l => new { l.TenantId, l.DocumentId, l.LinkedEntityType, l.LinkedEntityId }).IsUnique();
        });

        builder.Entity<PrivacyIncident>(e =>
        {
            e.ToTable("PrivacyIncidents");
            e.Property(i => i.IncidentNumber).HasMaxLength(50).IsRequired();
            e.Property(i => i.Title).HasMaxLength(200).IsRequired();
            e.Property(i => i.ResponsiblePerson).HasMaxLength(200);
            e.Property(i => i.InternalReference).HasMaxLength(100);
            e.Property(i => i.CreatedByUserId).HasMaxLength(450);
            e.Property(i => i.UpdatedByUserId).HasMaxLength(450);
            e.Property(i => i.ArchivedByUserId).HasMaxLength(450);
            e.Property(i => i.SupervisoryAuthorityName).HasMaxLength(200);
            e.Property(i => i.SupervisoryAuthorityReference).HasMaxLength(200);
            e.Property(i => i.DataSubjectsNotificationMethod).HasMaxLength(200);
            e.Property(i => i.Description).HasColumnType("text");
            e.Property(i => i.HowDetected).HasColumnType("text");
            e.Property(i => i.Cause).HasColumnType("text");
            e.Property(i => i.AffectedSystems).HasColumnType("text");
            e.Property(i => i.BreachTypeDescription).HasColumnType("text");
            e.Property(i => i.AffectedDataCategories).HasColumnType("text");
            e.Property(i => i.AffectedPersonGroups).HasColumnType("text");
            e.Property(i => i.LikelyConsequences).HasColumnType("text");
            e.Property(i => i.RiskAssessmentReason).HasColumnType("text");
            e.Property(i => i.SupervisoryAuthorityNotificationReason).HasColumnType("text");
            e.Property(i => i.NotificationDelayReason).HasColumnType("text");
            e.Property(i => i.DataSubjectsNotificationReason).HasColumnType("text");
            e.Property(i => i.DataSubjectsNotificationSummary).HasColumnType("text");
            e.Property(i => i.ImmediateActions).HasColumnType("text");
            e.Property(i => i.RemediationActions).HasColumnType("text");
            e.Property(i => i.PreventiveActions).HasColumnType("text");
            e.Property(i => i.ClosureSummary).HasColumnType("text");
            e.HasOne(i => i.Tenant).WithMany(t => t.PrivacyIncidents).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(i => i.TenantId);
            e.HasIndex(i => new { i.TenantId, i.IncidentNumber }).IsUnique();
            e.HasIndex(i => new { i.TenantId, i.Status });
        });

        builder.Entity<PrivacyIncidentProcessingActivity>(e =>
        {
            e.ToTable("PrivacyIncidentProcessingActivities");
            e.HasOne(l => l.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.PrivacyIncident)
                .WithMany(i => i.ProcessingActivityLinks)
                .HasForeignKey(l => l.PrivacyIncidentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.ProcessingActivity)
                .WithMany()
                .HasForeignKey(l => l.ProcessingActivityId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => new { l.PrivacyIncidentId, l.ProcessingActivityId }).IsUnique();
        });

        builder.Entity<PrivacyIncidentServiceProvider>(e =>
        {
            e.ToTable("PrivacyIncidentServiceProviders");
            e.HasOne(l => l.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.PrivacyIncident)
                .WithMany(i => i.ServiceProviderLinks)
                .HasForeignKey(l => l.PrivacyIncidentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.ServiceProvider)
                .WithMany()
                .HasForeignKey(l => l.ServiceProviderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => new { l.PrivacyIncidentId, l.ServiceProviderId }).IsUnique();
        });

        builder.Entity<PrivacyIncidentMeasure>(e =>
        {
            e.ToTable("PrivacyIncidentMeasures");
            e.HasOne(l => l.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.PrivacyIncident)
                .WithMany(i => i.MeasureLinks)
                .HasForeignKey(l => l.PrivacyIncidentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Measure)
                .WithMany()
                .HasForeignKey(l => l.MeasureId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => new { l.PrivacyIncidentId, l.MeasureId }).IsUnique();
        });

        builder.Entity<PrivacyIncidentTom>(e =>
        {
            e.ToTable("PrivacyIncidentToms");
            e.HasOne(l => l.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.PrivacyIncident)
                .WithMany(i => i.TomLinks)
                .HasForeignKey(l => l.PrivacyIncidentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Tom)
                .WithMany()
                .HasForeignKey(l => l.TomId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => new { l.PrivacyIncidentId, l.TomId }).IsUnique();
        });

        builder.Entity<DataSubjectRequest>(e =>
        {
            e.ToTable("DataSubjectRequests");
            e.Property(r => r.DataSubjectName).HasMaxLength(200);
            e.Property(r => r.DataSubjectEmail).HasMaxLength(320);
            e.Property(r => r.DataSubjectPhone).HasMaxLength(50);
            e.Property(r => r.DataSubjectReference).HasMaxLength(200);
            e.Property(r => r.ContactChannel).HasMaxLength(200);
            e.Property(r => r.Description).HasColumnType("text");
            e.Property(r => r.ResultSummary).HasColumnType("text");
            e.Property(r => r.InternalNotes).HasColumnType("text");
            e.Property(r => r.IdentityVerificationNote).HasColumnType("text");
            e.Property(r => r.DeadlineExtensionReason).HasColumnType("text");
            e.Property(r => r.AnonymizationNote).HasColumnType("text");
            e.Property(r => r.AssignedUserId).HasMaxLength(450);
            e.Property(r => r.CreatedByUserId).HasMaxLength(450);
            e.Property(r => r.UpdatedByUserId).HasMaxLength(450);
            e.Property(r => r.PersonalDataAnonymizedByUserId).HasMaxLength(450);
            e.Property(r => r.ArchivedByUserId).HasMaxLength(450);
            e.HasOne(r => r.Tenant).WithMany(t => t.DataSubjectRequests).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(r => r.TenantId);
            e.HasIndex(r => r.Status);
            e.HasIndex(r => r.RequestType);
            e.HasIndex(r => r.DueAt);
            e.HasIndex(r => r.PersonalDataAnonymized);
            e.HasIndex(r => new { r.TenantId, r.Status });
            e.HasIndex(r => new { r.TenantId, r.DueAt });
        });

        builder.Entity<DataSubjectRequestProcessingActivity>(e =>
        {
            e.ToTable("DataSubjectRequestProcessingActivities");
            e.HasOne(l => l.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.DataSubjectRequest)
                .WithMany(r => r.ProcessingActivityLinks)
                .HasForeignKey(l => l.DataSubjectRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.ProcessingActivity)
                .WithMany()
                .HasForeignKey(l => l.ProcessingActivityId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(l => l.CreatedByUserId).HasMaxLength(450);
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => l.DataSubjectRequestId);
            e.HasIndex(l => l.ProcessingActivityId);
            e.HasIndex(l => new { l.DataSubjectRequestId, l.ProcessingActivityId }).IsUnique();
        });

        builder.Entity<DataSubjectRequestMeasure>(e =>
        {
            e.ToTable("DataSubjectRequestMeasures");
            e.HasOne(l => l.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.DataSubjectRequest)
                .WithMany(r => r.MeasureLinks)
                .HasForeignKey(l => l.DataSubjectRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Measure)
                .WithMany()
                .HasForeignKey(l => l.MeasureId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(l => l.CreatedByUserId).HasMaxLength(450);
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => l.DataSubjectRequestId);
            e.HasIndex(l => l.MeasureId);
            e.HasIndex(l => new { l.DataSubjectRequestId, l.MeasureId }).IsUnique();
        });

        builder.Entity<DataSubjectRequestServiceProvider>(e =>
        {
            e.ToTable("DataSubjectRequestServiceProviders");
            e.HasOne(l => l.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.DataSubjectRequest)
                .WithMany(r => r.ServiceProviderLinks)
                .HasForeignKey(l => l.DataSubjectRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.ServiceProvider)
                .WithMany()
                .HasForeignKey(l => l.ServiceProviderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(l => l.CreatedByUserId).HasMaxLength(450);
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => l.DataSubjectRequestId);
            e.HasIndex(l => l.ServiceProviderId);
            e.HasIndex(l => new { l.DataSubjectRequestId, l.ServiceProviderId }).IsUnique();
        });

        // Verzeichnis von Verarbeitungstätigkeiten (VVT) – mandantenbezogen, kein Kaskaden-Löschen des Mandanten.
        // Lange Textfelder als MySQL TEXT (nicht VARCHAR), sonst überschreitet die Zeile das Limit von 65535 Bytes bei utf8mb4.
        builder.Entity<ProcessingActivity>(e =>
        {
            e.ToTable("ProcessingActivities");
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.Description).HasColumnType("text");
            e.Property(p => p.Purpose).HasColumnType("text");
            e.Property(p => p.ResponsibleDepartment).HasMaxLength(200);
            e.Property(p => p.LegalBasis).HasColumnType("text");
            e.Property(p => p.DataSubjectCategories).HasColumnType("text");
            e.Property(p => p.PersonalDataCategories).HasColumnType("text");
            e.Property(p => p.Recipients).HasColumnType("text");
            e.Property(p => p.ThirdCountryTransferDescription).HasColumnType("text");
            e.Property(p => p.RetentionPeriod).HasColumnType("text");
            e.Property(p => p.Owner).HasMaxLength(200);
            e.Property(p => p.ArchivedByUserId).HasMaxLength(450);
            e.HasOne(p => p.Tenant).WithMany(t => t.ProcessingActivities).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(p => p.TenantId);
        });

        // TOM-Verzeichnis – mandantenbezogene Schutzmaßnahmen.
        builder.Entity<Tom>(e =>
        {
            e.ToTable("Toms");
            e.Property(t => t.Title).HasMaxLength(200).IsRequired();
            e.Property(t => t.Description).HasColumnType("text");
            e.Property(t => t.Owner).HasMaxLength(200);
            e.Property(t => t.EvidenceReference).HasColumnType("text");
            e.Property(t => t.Notes).HasColumnType("text");
            e.Property(t => t.ArchivedByUserId).HasMaxLength(450);
            e.HasOne(t => t.Tenant).WithMany(tenant => tenant.Toms).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(t => t.TenantId);
            e.HasIndex(t => new { t.TenantId, t.ImplementationStatus });
        });

        // Many-to-Many TOM ↔ Verarbeitungstätigkeit mit Mandantenschutz auf Verknüpfungsebene.
        builder.Entity<ProcessingActivityTom>(e =>
        {
            e.ToTable("ProcessingActivityToms");
            e.HasOne(l => l.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.Tom)
                .WithMany(t => t.ProcessingActivityLinks)
                .HasForeignKey(l => l.TomId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.ProcessingActivity)
                .WithMany(p => p.TomLinks)
                .HasForeignKey(l => l.ProcessingActivityId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => new { l.TomId, l.ProcessingActivityId }).IsUnique();
        });

        // Dienstleister / Auftragsverarbeiter – mandantenbezogenes Verzeichnis externer Stellen.
        builder.Entity<ServiceProviderEntity>(e =>
        {
            e.ToTable("ServiceProviders");
            e.Property(s => s.Name).HasMaxLength(200).IsRequired();
            e.Property(s => s.Description).HasColumnType("text");
            e.Property(s => s.ServicePurpose).HasColumnType("text");
            e.Property(s => s.ContactPerson).HasMaxLength(200);
            e.Property(s => s.Email).HasMaxLength(200);
            e.Property(s => s.Phone).HasMaxLength(50);
            e.Property(s => s.Website).HasMaxLength(300);
            e.Property(s => s.Address).HasColumnType("text");
            e.Property(s => s.Country).HasMaxLength(100);
            e.Property(s => s.DataProcessingAgreementReviewResult).HasColumnType("text");
            e.Property(s => s.TomsReviewResult).HasColumnType("text");
            e.Property(s => s.SubProcessorsDescription).HasColumnType("text");
            e.Property(s => s.ThirdCountry).HasMaxLength(100);
            e.Property(s => s.ThirdCountryTransferGuarantees).HasColumnType("text");
            e.Property(s => s.ResponsiblePerson).HasMaxLength(200);
            e.Property(s => s.Notes).HasColumnType("text");
            e.Property(s => s.ArchivedByUserId).HasMaxLength(450);
            e.HasOne(s => s.Tenant).WithMany(t => t.ServiceProviders).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(s => s.TenantId);
            e.HasIndex(s => new { s.TenantId, s.Status });
            e.HasIndex(s => new { s.TenantId, s.IsDataProcessor });
        });

        // Many-to-Many Dienstleister ↔ Verarbeitungstätigkeit inkl. Rolle in der Verarbeitung.
        builder.Entity<ProcessingActivityServiceProvider>(e =>
        {
            e.ToTable("ProcessingActivityServiceProviders");
            e.HasOne(l => l.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.ServiceProvider)
                .WithMany(s => s.ProcessingActivityLinks)
                .HasForeignKey(l => l.ServiceProviderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.ProcessingActivity)
                .WithMany(p => p.ServiceProviderLinks)
                .HasForeignKey(l => l.ProcessingActivityId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => new { l.ServiceProviderId, l.ProcessingActivityId }).IsUnique();
        });

        // Many-to-Many Dienstleister ↔ TOM (geprüfte/vereinbarte Maßnahmen beim Dienstleister).
        builder.Entity<ServiceProviderTom>(e =>
        {
            e.ToTable("ServiceProviderToms");
            e.HasOne(l => l.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.ServiceProvider)
                .WithMany(s => s.TomLinks)
                .HasForeignKey(l => l.ServiceProviderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Tom)
                .WithMany(t => t.ServiceProviderLinks)
                .HasForeignKey(l => l.TomId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => new { l.ServiceProviderId, l.TomId }).IsUnique();
        });

        // Many-to-Many Verarbeitungstätigkeit ↔ Maßnahme.
        builder.Entity<ProcessingActivityMeasure>(e =>
        {
            e.ToTable("ProcessingActivityMeasures");
            e.HasOne(l => l.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.ProcessingActivity)
                .WithMany(p => p.MeasureLinks)
                .HasForeignKey(l => l.ProcessingActivityId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Measure)
                .WithMany(m => m.ProcessingActivityLinks)
                .HasForeignKey(l => l.MeasureId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => new { l.ProcessingActivityId, l.MeasureId }).IsUnique();
        });

        // Many-to-Many Verarbeitungstätigkeit ↔ Audit-Antwort (Durchlauf bleibt über AuditAnswer erhalten).
        builder.Entity<ProcessingActivityAuditAnswer>(e =>
        {
            e.ToTable("ProcessingActivityAuditAnswers");
            e.HasOne(l => l.Tenant).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.ProcessingActivity)
                .WithMany(p => p.AuditAnswerLinks)
                .HasForeignKey(l => l.ProcessingActivityId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.AuditAnswer)
                .WithMany(a => a.ProcessingActivityLinks)
                .HasForeignKey(l => l.AuditAnswerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => new { l.ProcessingActivityId, l.AuditAnswerId }).IsUnique();
        });

        // DSFA – mandantenbezogen, 1:n zu Verarbeitungstätigkeit; Mandant und VVT werden nicht kaskadiert gelöscht.
        builder.Entity<DataProtectionImpactAssessment>(e =>
        {
            e.ToTable("DataProtectionImpactAssessments");
            e.Property(d => d.Title).HasMaxLength(200).IsRequired();
            e.Property(d => d.ProcessingDescription).HasColumnType("text");
            e.Property(d => d.ReasonForDpia).HasColumnType("text");
            e.Property(d => d.NecessityAndProportionality).HasColumnType("text");
            e.Property(d => d.RiskAssessment).HasColumnType("text");
            e.Property(d => d.ProtectiveMeasures).HasColumnType("text");
            e.Property(d => d.ResponsiblePerson).HasMaxLength(200);
            e.Property(d => d.ReviewedBy).HasMaxLength(200);
            e.Property(d => d.ArchivedByUserId).HasMaxLength(450);
            e.HasOne(d => d.Tenant).WithMany(t => t.DpiaAssessments).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.ProcessingActivity)
                .WithMany(p => p.DpiaAssessments)
                .HasForeignKey(d => d.ProcessingActivityId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(d => d.TenantId);
            e.HasIndex(d => d.ProcessingActivityId);
            e.HasIndex(d => new { d.TenantId, d.ProcessingActivityId });
        });

        builder.Entity<EmailSettings>(e =>
        {
            e.ToTable("EmailSettings");
            e.Property(s => s.SmtpHost).HasMaxLength(255);
            e.Property(s => s.SmtpUsername).HasMaxLength(255);
            e.Property(s => s.EncryptedSmtpPassword).HasMaxLength(2000);
            e.Property(s => s.SenderEmail).HasMaxLength(255);
            e.Property(s => s.SenderName).HasMaxLength(200);
            e.Property(s => s.SystemNotificationRecipientEmail).HasMaxLength(255);
            e.Property(s => s.SystemNotificationsEnabled).HasDefaultValue(false);
            e.Property(s => s.UpdatedByUserId).HasMaxLength(450);
        });

        builder.Entity<EmailTemplate>(e =>
        {
            e.ToTable("EmailTemplates");
            e.Property(t => t.TemplateKey).HasMaxLength(100).IsRequired();
            e.Property(t => t.DisplayName).HasMaxLength(200).IsRequired();
            e.Property(t => t.Subject).HasMaxLength(500).IsRequired();
            e.Property(t => t.HtmlContent).HasColumnType("text");
            e.Property(t => t.TextContent).HasColumnType("text");
            e.Property(t => t.UpdatedByUserId).HasMaxLength(450);
            e.HasIndex(t => t.TemplateKey).IsUnique();
        });

        builder.Entity<PageHelpContent>(e =>
        {
            e.ToTable("PageHelpContents");
            e.Property(h => h.Key).HasMaxLength(100).IsRequired();
            e.Property(h => h.Title).HasMaxLength(200).IsRequired();
            e.Property(h => h.LegalReference).HasMaxLength(500);
            e.Property(h => h.ShortDescription).HasMaxLength(500);
            e.Property(h => h.Content).HasColumnType("text").IsRequired();
            e.Property(h => h.UpdatedByUserId).HasMaxLength(450);
            e.HasIndex(h => h.Key).IsUnique();
        });

        builder.Entity<TenantOnboardingTask>(e =>
        {
            e.ToTable("TenantOnboardingTasks");
            e.Property(t => t.Key).HasMaxLength(100).IsRequired();
            e.Property(t => t.Title).HasMaxLength(200).IsRequired();
            e.Property(t => t.Description).HasColumnType("text").IsRequired();
            e.Property(t => t.TargetUrl).HasMaxLength(500).IsRequired();
            e.Property(t => t.CompletedByUserId).HasMaxLength(450);
            e.HasOne(t => t.Tenant)
                .WithMany()
                .HasForeignKey(t => t.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(t => t.TenantId);
            e.HasIndex(t => new { t.TenantId, t.Key }).IsUnique();
        });

        builder.Entity<LogEntry>(e =>
        {
            e.ToTable("LogEntries");
            e.HasKey(l => l.Id);
            e.Property(l => l.LogCategory).HasMaxLength(50).IsRequired();
            e.Property(l => l.Severity).HasMaxLength(50).IsRequired();
            e.Property(l => l.Action).HasMaxLength(200).IsRequired();
            e.Property(l => l.Description).HasMaxLength(2000).IsRequired();
            e.Property(l => l.EntityType).HasMaxLength(100);
            e.Property(l => l.EntityId).HasMaxLength(100);
            e.Property(l => l.EntityName).HasMaxLength(300);
            e.Property(l => l.UserId).HasMaxLength(450);
            e.Property(l => l.UserEmail).HasMaxLength(255);
            e.Property(l => l.UserDisplayName).HasMaxLength(200);
            e.Property(l => l.LicenseNumber).HasMaxLength(50);
            e.Property(l => l.TenantName).HasMaxLength(200);
            e.Property(l => l.IpAddressAnonymized).HasMaxLength(100);
            e.Property(l => l.UserAgent).HasMaxLength(500);
            e.Property(l => l.OldValuesJson).HasColumnType("text");
            e.Property(l => l.NewValuesJson).HasColumnType("text");
            e.Property(l => l.MetadataJson).HasColumnType("text");
            e.Property(l => l.ExceptionType).HasMaxLength(200);
            e.Property(l => l.ExceptionMessage).HasMaxLength(2000);
            e.Property(l => l.ExceptionDetails).HasColumnType("text");
            e.Property(l => l.CorrelationId).HasMaxLength(100);
            e.Property(l => l.RequestPath).HasMaxLength(500);
            e.Property(l => l.Source).HasMaxLength(200);
            e.HasIndex(l => l.CreatedAt);
            e.HasIndex(l => l.LogCategory);
            e.HasIndex(l => l.Severity);
            e.HasIndex(l => l.LicenseId);
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => l.UserId);
            e.HasIndex(l => l.EntityType);
            e.HasIndex(l => l.Action);
            e.HasIndex(l => l.IsVisibleToAdmin);
            e.HasIndex(l => new { l.LogCategory, l.IsVisibleToAdmin, l.LicenseId });
        });
    }

    /// <summary>
    /// Filtert alle mandantenbezogenen Fach-Entities nach dem aktiven Mandantenkontext und Archivstatus.
    /// Ohne gesetzten Kontext werden keine Datensätze zurückgegeben (Datenisolation).
    /// ShowArchivedOnly steuert Aktiv- vs. Archivansicht (IsArchived == ShowArchivedOnly).
    /// Verwaltungsabfragen nutzen <see cref="EntityFrameworkQueryableExtensions.IgnoreQueryFilters{TEntity}"/>.
    /// </summary>
    private void ApplyTenantQueryFilters(ModelBuilder builder)
    {
        builder.Entity<AuditTemplate>()
            .HasQueryFilter(t => tenantContextAccessor.CurrentTenantId.HasValue
                && t.IsArchived == archiveViewContextAccessor.ShowArchivedOnly
                && (t.TemplateType == AuditTemplateType.Official
                    || t.TemplateType == AuditTemplateType.Community
                    || (t.TemplateType == AuditTemplateType.Tenant
                        && t.TenantId == tenantContextAccessor.CurrentTenantId)));

        ApplyArchivableTenantFilter<AuditRun>(builder);
        ApplyArchivableTenantFilter<Measure>(builder);
        ApplyArchivableTenantFilter<EvidenceDocument>(builder);
        ApplyArchivableTenantFilter<ProcessingActivity>(builder);
        ApplyArchivableTenantFilter<Tom>(builder);
        ApplyArchivableTenantFilter<ServiceProviderEntity>(builder);
        ApplyArchivableTenantFilter<DataProtectionImpactAssessment>(builder);
        ApplyArchivableTenantFilter<PrivacyIncident>(builder);
        ApplyArchivableTenantFilter<DataSubjectRequest>(builder);
        ApplyArchivableTenantFilter<Training>(builder);
        ApplyArchivableTenantFilter<TrainingParticipant>(builder);
        ApplyArchivableTenantFilter<TrainingAssignment>(builder);

        builder.Entity<ProcessingActivityTom>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<ProcessingActivityServiceProvider>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<ServiceProviderTom>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<ProcessingActivityMeasure>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<ProcessingActivityAuditAnswer>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<TenantOnboardingTask>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<PrivacyIncidentProcessingActivity>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<PrivacyIncidentServiceProvider>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<PrivacyIncidentMeasure>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<PrivacyIncidentTom>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<DataSubjectRequestProcessingActivity>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<DataSubjectRequestMeasure>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<DataSubjectRequestServiceProvider>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<DocumentLink>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<DocumentCategory>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<DataProtectionRole>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<TrainingTemplate>()
            .HasQueryFilter(t => tenantContextAccessor.CurrentTenantId.HasValue
                && t.IsArchived == archiveViewContextAccessor.ShowArchivedOnly
                && ((t.IsGlobal && t.TenantId == null)
                    || (t.TenantId == tenantContextAccessor.CurrentTenantId && !t.IsGlobal)));

        ApplyTrainingChildTenantFilter<TrainingTemplateSection>(builder);
        ApplyTrainingChildTenantFilter<TrainingTemplateAsset>(builder);
        ApplyTrainingChildTenantFilter<TrainingQuestion>(builder);
        ApplyTrainingChildTenantFilter<TrainingQuestionOption>(builder);

        builder.Entity<TrainingAssignmentSectionProgress>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<TrainingQuizAttempt>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);

        builder.Entity<TrainingQuizAnswer>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId);
    }

    private void ApplyTrainingChildTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : EntityBase
    {
        if (typeof(TEntity) == typeof(TrainingTemplateSection))
        {
            builder.Entity<TrainingTemplateSection>()
                .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                    && (e.TenantId == tenantContextAccessor.CurrentTenantId || e.TenantId == null)
                    && e.IsActive);
        }
        else if (typeof(TEntity) == typeof(TrainingTemplateAsset))
        {
            builder.Entity<TrainingTemplateAsset>()
                .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                    && (e.TenantId == tenantContextAccessor.CurrentTenantId || e.TenantId == null)
                    && e.IsActive);
        }
        else if (typeof(TEntity) == typeof(TrainingQuestion))
        {
            builder.Entity<TrainingQuestion>()
                .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                    && (e.TenantId == tenantContextAccessor.CurrentTenantId || e.TenantId == null)
                    && e.IsActive);
        }
        else if (typeof(TEntity) == typeof(TrainingQuestionOption))
        {
            builder.Entity<TrainingQuestionOption>()
                .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                    && (e.TenantId == tenantContextAccessor.CurrentTenantId || e.TenantId == null)
                    && e.IsActive);
        }
    }

    private void ApplyArchivableTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : ArchivableEntityBase, ITenantEntity
    {
        builder.Entity<TEntity>()
            .HasQueryFilter(e => tenantContextAccessor.CurrentTenantId.HasValue
                && e.TenantId == tenantContextAccessor.CurrentTenantId
                && e.IsArchived == archiveViewContextAccessor.ShowArchivedOnly);
    }
}
