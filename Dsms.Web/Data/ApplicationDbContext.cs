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
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<UserTenant> UserTenants => Set<UserTenant>();
    public DbSet<AuditTemplate> AuditTemplates => Set<AuditTemplate>();
    public DbSet<AuditQuestion> AuditQuestions => Set<AuditQuestion>();
    public DbSet<AuditRun> AuditRuns => Set<AuditRun>();
    public DbSet<AuditAnswer> AuditAnswers => Set<AuditAnswer>();
    public DbSet<Measure> Measures => Set<Measure>();
    public DbSet<EvidenceDocument> EvidenceDocuments => Set<EvidenceDocument>();
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

        builder.Entity<Tenant>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(200).IsRequired();
            e.Property(t => t.LegalName).HasMaxLength(300);
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
            e.Property(d => d.FileName).HasMaxLength(255).IsRequired();
            e.Property(d => d.StoragePath).HasMaxLength(500).IsRequired();
            e.Property(d => d.ArchivedByUserId).HasMaxLength(450);
            e.HasOne(d => d.Tenant).WithMany(t => t.Documents).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.AuditRun).WithMany(r => r.Documents).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.Measure).WithMany(m => m.Documents).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.ServiceProvider).WithMany(sp => sp.Documents).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.ProcessingActivity).WithMany(p => p.Documents).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.DataProtectionImpactAssessment).WithMany(dpia => dpia.Documents).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(d => new { d.TenantId, d.ProcessingActivityId });
            e.HasIndex(d => new { d.TenantId, d.DataProtectionImpactAssessmentId });
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
