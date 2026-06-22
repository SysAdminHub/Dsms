using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dsms.Web.Configuration;
using Dsms.Web.Data;
using Dsms.Web.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dsms.Web.Services.TenantExport;

public class TenantExportService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    UserManager<ApplicationUser> userManager,
    DocumentStorageService documentStorage,
    DataProtectionRoleService dataProtectionRoleService,
    ILogger<TenantExportService> logger,
    IOptions<AppBrandingOptions> brandingOptions) : ITenantExportService
{
    private readonly AppBrandingOptions _branding = brandingOptions.Value;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<TenantExportResult?> CreateExportAsync(int tenantId, string userId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null)
        {
            return null;
        }

        var exportUser = await userManager.FindByIdAsync(userId);
        var warnings = new List<string>();

        var tenantDto = MapTenant(tenant);
        var users = await LoadUsersAsync(db, tenantId, ct);
        var processingActivities = await LoadProcessingActivitiesAsync(db, tenantId, ct);
        var dsfa = await LoadDsfaAsync(db, tenantId, ct);
        var toms = await LoadTomsAsync(db, tenantId, ct);
        var processors = await LoadProcessorsAsync(db, tenantId, ct);
        var measures = await LoadMeasuresAsync(db, tenantId, ct);
        var trainings = await LoadTrainingsAsync(db, tenantId, ct);
        var privacyIncidents = await LoadPrivacyIncidentsAsync(db, tenantId, ct);
        var auditTemplates = await LoadAuditTemplatesAsync(db, tenantId, ct);
        var auditRuns = await LoadAuditRunsAsync(db, tenantId, ct);
        var (documents, fileEntries) = await LoadDocumentsAsync(db, tenantId, warnings, ct);
        var dataProtectionRoles = await dataProtectionRoleService.GetExportDataAsync(tenantId, ct);

        var exportInfo = new ExportInfoDto
        {
            ExportCreatedAt = DateTime.UtcNow,
            ExportCreatedByUserId = userId,
            ExportCreatedByEmail = exportUser?.Email,
            TenantId = tenantId,
            TenantName = tenant.Name,
            ApplicationName = _branding.ProductName,
            Warnings = warnings
        };

        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddJsonEntry(archive, "export-info.json", exportInfo);
            AddJsonEntry(archive, "tenant.json", tenantDto);
            AddJsonEntry(archive, "users.json", users);
            AddJsonEntry(archive, "processing-activities.json", processingActivities);
            AddJsonEntry(archive, "dsfa.json", dsfa);
            AddJsonEntry(archive, "toms.json", toms);
            AddJsonEntry(archive, "processors.json", processors);
            AddJsonEntry(archive, "measures.json", measures);
            AddJsonEntry(archive, "trainings.json", trainings);
            AddJsonEntry(archive, "privacy-incidents.json", privacyIncidents);
            AddJsonEntry(archive, "audit-templates.json", auditTemplates);
            AddJsonEntry(archive, "audit-runs.json", auditRuns);
            AddJsonEntry(archive, "data-protection-roles.json", dataProtectionRoles);
            AddJsonEntry(archive, "documents/metadata.json", documents);

            foreach (var file in fileEntries)
            {
                try
                {
                    var entry = archive.CreateEntry(file.ZipPath, CompressionLevel.Optimal);
                    await using var entryStream = entry.Open();
                    await using var source = File.OpenRead(file.FullPath);
                    await source.CopyToAsync(entryStream, ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Datei {Path} konnte nicht in ZIP aufgenommen werden", file.FullPath);
                    warnings.Add($"Datei {file.FileName} (ID {file.DocumentId}) konnte nicht gelesen werden.");
                }
            }
        }

        zipStream.Position = 0;
        var fileName = BuildZipFileName(tenant.Name, _branding.ProductName);

        logger.LogInformation("Tenant-Export erstellt: TenantId={TenantId}, UserId={UserId}, Size={Size}",
            tenantId, userId, zipStream.Length);

        return new TenantExportResult
        {
            FileName = fileName,
            Content = zipStream.ToArray()
        };
    }

    private static void AddJsonEntry<T>(ZipArchive archive, string path, T data)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        JsonSerializer.Serialize(stream, data, JsonOptions);
    }

    private static TenantExportDto MapTenant(Domain.Entities.Tenant tenant) => new()
    {
        Id = tenant.Id,
        Name = tenant.Name,
        LegalName = tenant.LegalName,
        Street = tenant.Street,
        HouseNumber = tenant.HouseNumber,
        PostalCode = tenant.PostalCode,
        City = tenant.City,
        Phone = tenant.Phone,
        Email = tenant.Email,
        Website = tenant.Website,
        DpoName = tenant.DpoName,
        DpoStreet = tenant.DpoStreet,
        DpoHouseNumber = tenant.DpoHouseNumber,
        DpoPostalCode = tenant.DpoPostalCode,
        DpoCity = tenant.DpoCity,
        DpoPhone = tenant.DpoPhone,
        DpoEmail = tenant.DpoEmail,
        IsActive = tenant.IsActive,
        IsDeletionRequested = tenant.IsDeletionRequested,
        DeletionRequestedAt = tenant.DeletionRequestedAt,
        DeletionScheduledAt = tenant.DeletionScheduledAt,
        CreatedAt = tenant.CreatedAt,
        UpdatedAt = tenant.UpdatedAt
    };

    private async Task<IReadOnlyList<UserExportDto>> LoadUsersAsync(
        ApplicationDbContext db, int tenantId, CancellationToken ct)
    {
        var userIds = await db.UserTenants
            .IgnoreQueryFilters()
            .Where(ut => ut.TenantId == tenantId)
            .Select(ut => ut.UserId)
            .ToListAsync(ct);

        var legacyUserIds = await db.Users
            .Where(u => u.TenantId == tenantId)
            .Select(u => u.Id)
            .ToListAsync(ct);

        var allIds = userIds.Union(legacyUserIds).Distinct().ToList();
        if (allIds.Count == 0)
        {
            return [];
        }

        var users = await db.Users
            .AsNoTracking()
            .Where(u => allIds.Contains(u.Id))
            .ToListAsync(ct);

        var result = new List<UserExportDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var assignedTenants = await db.UserTenants
                .IgnoreQueryFilters()
                .Where(ut => ut.UserId == user.Id)
                .Select(ut => ut.TenantId)
                .ToListAsync(ct);

            if (user.TenantId.HasValue && !assignedTenants.Contains(user.TenantId.Value))
            {
                assignedTenants.Add(user.TenantId.Value);
            }

            result.Add(new UserExportDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                Roles = roles.ToList(),
                IsActive = user.IsActive,
                AssignedTenantIds = assignedTenants,
                CreatedAt = user.CreatedAt
            });
        }

        return result;
    }

    private static async Task<IReadOnlyList<ProcessingActivityExportDto>> LoadProcessingActivitiesAsync(
        ApplicationDbContext db, int tenantId, CancellationToken ct)
    {
        var items = await db.ProcessingActivities
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(ct);

        var tomLinks = await db.ProcessingActivityToms
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct);

        var spLinks = await db.ProcessingActivityServiceProviders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct);

        var measureLinks = await db.ProcessingActivityMeasures
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct);

        var auditAnswerLinks = await db.ProcessingActivityAuditAnswers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct);

        return items.Select(p => new ProcessingActivityExportDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Purpose = p.Purpose,
            ResponsibleDepartment = p.ResponsibleDepartment,
            LegalBasis = p.LegalBasis,
            DataSubjectCategories = p.DataSubjectCategories,
            PersonalDataCategories = p.PersonalDataCategories,
            Recipients = p.Recipients,
            ThirdCountryTransfer = p.ThirdCountryTransfer,
            ThirdCountryTransferDescription = p.ThirdCountryTransferDescription,
            RetentionPeriod = p.RetentionPeriod,
            DpiaRequired = p.DpiaRequired,
            Status = p.Status.ToString(),
            Owner = p.Owner,
            IsArchived = p.IsArchived,
            ArchivedAt = p.ArchivedAt,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            LinkedTomIds = tomLinks.Where(l => l.ProcessingActivityId == p.Id).Select(l => l.TomId).ToList(),
            LinkedServiceProviders = spLinks.Where(l => l.ProcessingActivityId == p.Id)
                .Select(l => new ProcessingActivityServiceProviderLinkExportDto
                {
                    ServiceProviderId = l.ServiceProviderId,
                    RoleInProcessing = l.RoleInProcessing.ToString()
                }).ToList(),
            LinkedMeasureIds = measureLinks.Where(l => l.ProcessingActivityId == p.Id).Select(l => l.MeasureId).ToList(),
            LinkedAuditAnswerIds = auditAnswerLinks.Where(l => l.ProcessingActivityId == p.Id).Select(l => l.AuditAnswerId).ToList()
        }).ToList();
    }

    private static async Task<IReadOnlyList<DsfaExportDto>> LoadDsfaAsync(
        ApplicationDbContext db, int tenantId, CancellationToken ct)
    {
        var items = await db.DataProtectionImpactAssessments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .ToListAsync(ct);

        return items.Select(d => new DsfaExportDto
        {
            Id = d.Id,
            ProcessingActivityId = d.ProcessingActivityId,
            Title = d.Title,
            ProcessingDescription = d.ProcessingDescription,
            ReasonForDpia = d.ReasonForDpia,
            NecessityAndProportionality = d.NecessityAndProportionality,
            RiskAssessment = d.RiskAssessment,
            ProtectiveMeasures = d.ProtectiveMeasures,
            ResidualRisk = d.ResidualRisk.ToString(),
            Outcome = d.Outcome.ToString(),
            Status = d.Status.ToString(),
            ResponsiblePerson = d.ResponsiblePerson,
            ReviewedBy = d.ReviewedBy,
            ReviewedAt = d.ReviewedAt?.ToString("yyyy-MM-dd"),
            NextReviewAt = d.NextReviewAt?.ToString("yyyy-MM-dd"),
            IsArchived = d.IsArchived,
            ArchivedAt = d.ArchivedAt,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        }).ToList();
    }

    private static async Task<IReadOnlyList<TomExportDto>> LoadTomsAsync(
        ApplicationDbContext db, int tenantId, CancellationToken ct)
    {
        var items = await db.Toms
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(t => t.TomCategory)
            .Where(t => t.TenantId == tenantId)
            .ToListAsync(ct);

        var paLinks = await db.ProcessingActivityToms
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct);

        var spLinks = await db.ServiceProviderToms
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct);

        return items.Select(t => new TomExportDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            Category = t.TomCategory?.Name ?? string.Empty,
            ProtectionGoal = t.ProtectionGoal.ToString(),
            ImplementationStatus = t.ImplementationStatus.ToString(),
            Owner = t.Owner,
            ValidFrom = t.ValidFrom?.ToString("yyyy-MM-dd"),
            NextReviewAt = t.NextReviewAt?.ToString("yyyy-MM-dd"),
            EvidenceReference = t.EvidenceReference,
            Notes = t.Notes,
            IsArchived = t.IsArchived,
            ArchivedAt = t.ArchivedAt,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            LinkedProcessingActivityIds = paLinks.Where(l => l.TomId == t.Id).Select(l => l.ProcessingActivityId).ToList(),
            LinkedServiceProviderIds = spLinks.Where(l => l.TomId == t.Id).Select(l => l.ServiceProviderId).ToList()
        }).ToList();
    }

    private static async Task<IReadOnlyList<ProcessorExportDto>> LoadProcessorsAsync(
        ApplicationDbContext db, int tenantId, CancellationToken ct)
    {
        var items = await db.ServiceProviders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(s => s.ServiceProviderCategory)
            .Where(s => s.TenantId == tenantId)
            .ToListAsync(ct);

        var paLinks = await db.ProcessingActivityServiceProviders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct);

        var tomLinks = await db.ServiceProviderToms
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct);

        return items.Select(s => new ProcessorExportDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            ProviderType = s.ServiceProviderCategory?.Name ?? string.Empty,
            ServicePurpose = s.ServicePurpose,
            ContactPerson = s.ContactPerson,
            Email = s.Email,
            Phone = s.Phone,
            Website = s.Website,
            Address = s.Address,
            Country = s.Country,
            IsDataProcessor = s.IsDataProcessor,
            DataProcessingAgreementExists = s.DataProcessingAgreementExists,
            DataProcessingAgreementDate = s.DataProcessingAgreementDate?.ToString("yyyy-MM-dd"),
            DataProcessingAgreementReviewedAt = s.DataProcessingAgreementReviewedAt?.ToString("yyyy-MM-dd"),
            DataProcessingAgreementReviewResult = s.DataProcessingAgreementReviewResult,
            TomsReviewed = s.TomsReviewed,
            TomsReviewedAt = s.TomsReviewedAt?.ToString("yyyy-MM-dd"),
            TomsReviewResult = s.TomsReviewResult,
            SubProcessorsAllowed = s.SubProcessorsAllowed,
            SubProcessorsDescription = s.SubProcessorsDescription,
            ThirdCountryInvolvement = s.ThirdCountryInvolvement,
            ThirdCountry = s.ThirdCountry,
            ThirdCountryLegalBasis = s.ThirdCountryLegalBasis.ToString(),
            ThirdCountryTransferGuarantees = s.ThirdCountryTransferGuarantees,
            RiskAssessment = s.RiskAssessment.ToString(),
            Status = s.Status.ToString(),
            ResponsiblePerson = s.ResponsiblePerson,
            Notes = s.Notes,
            IsArchived = s.IsArchived,
            ArchivedAt = s.ArchivedAt,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            LinkedProcessingActivityIds = paLinks.Where(l => l.ServiceProviderId == s.Id).Select(l => l.ProcessingActivityId).ToList(),
            LinkedTomIds = tomLinks.Where(l => l.ServiceProviderId == s.Id).Select(l => l.TomId).ToList()
        }).ToList();
    }

    private static async Task<IReadOnlyList<MeasureExportDto>> LoadMeasuresAsync(
        ApplicationDbContext db, int tenantId, CancellationToken ct)
    {
        var items = await db.Measures
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .ToListAsync(ct);

        var paLinks = await db.ProcessingActivityMeasures
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct);

        return items.Select(m => new MeasureExportDto
        {
            Id = m.Id,
            AuditRunId = m.AuditRunId,
            AuditAnswerId = m.AuditAnswerId,
            Title = m.Title,
            Description = m.Description,
            Status = m.Status.ToString(),
            DueDate = m.DueDate?.ToString("yyyy-MM-dd"),
            CompletedAt = m.CompletedAt,
            AssignedUserId = m.AssignedUserId,
            IsArchived = m.IsArchived,
            ArchivedAt = m.ArchivedAt,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt,
            LinkedProcessingActivityIds = paLinks.Where(l => l.MeasureId == m.Id).Select(l => l.ProcessingActivityId).ToList()
        }).ToList();
    }

    private static async Task<IReadOnlyList<TrainingExportDto>> LoadTrainingsAsync(
        ApplicationDbContext db, int tenantId, CancellationToken ct)
    {
        var items = await db.Trainings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(t => t.TrainingTemplate)
            .Where(t => t.TenantId == tenantId)
            .ToListAsync(ct);

        return items.Select(t => new TrainingExportDto
        {
            Id = t.Id,
            TrainingTemplateId = t.TrainingTemplateId,
            TemplateTitle = t.TrainingTemplate?.Title,
            Title = t.Title,
            Description = t.Description,
            TrainingType = t.TrainingType.ToString(),
            TargetAudience = t.TargetAudience,
            Status = t.Status.ToString(),
            ScheduledAt = t.ScheduledAt,
            CompletedAt = t.CompletedAt,
            RepeatDueAt = t.RepeatDueAt,
            ResponsibleUserId = t.ResponsibleUserId,
            ResponsibleName = t.ResponsibleName,
            ParticipantCount = t.ParticipantCount,
            ProofMissing = t.ProofMissing,
            Notes = t.Notes,
            IsArchived = t.IsArchived,
            ArchivedAt = t.ArchivedAt,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        }).ToList();
    }

    private static async Task<IReadOnlyList<PrivacyIncidentExportDto>> LoadPrivacyIncidentsAsync(
        ApplicationDbContext db, int tenantId, CancellationToken ct)
    {
        var items = await db.PrivacyIncidents
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId)
            .ToListAsync(ct);

        var paLinks = await db.PrivacyIncidentProcessingActivities
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct);

        var spLinks = await db.PrivacyIncidentServiceProviders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct);

        var measureLinks = await db.PrivacyIncidentMeasures
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct);

        var tomLinks = await db.PrivacyIncidentToms
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct);

        return items.Select(i => new PrivacyIncidentExportDto
        {
            Id = i.Id,
            IncidentNumber = i.IncidentNumber,
            Title = i.Title,
            Status = i.Status.ToString(),
            Severity = i.Severity.ToString(),
            Source = i.Source.ToString(),
            OwnRole = i.OwnRole.ToString(),
            DiscoveredAt = i.DiscoveredAt,
            OccurredAt = i.OccurredAt,
            ReportedToUsAt = i.ReportedToUsAt,
            ResponsiblePerson = i.ResponsiblePerson,
            InternalReference = i.InternalReference,
            Description = i.Description,
            HowDetected = i.HowDetected,
            Cause = i.Cause,
            AffectedSystems = i.AffectedSystems,
            IncidentStillActive = i.IncidentStillActive,
            IncidentStoppedAt = i.IncidentStoppedAt,
            ConfidentialityAffected = i.ConfidentialityAffected,
            IntegrityAffected = i.IntegrityAffected,
            AvailabilityAffected = i.AvailabilityAffected,
            BreachTypeDescription = i.BreachTypeDescription,
            AffectedDataCategories = i.AffectedDataCategories,
            AffectedPersonGroups = i.AffectedPersonGroups,
            ApproxAffectedPersons = i.ApproxAffectedPersons,
            ApproxAffectedRecords = i.ApproxAffectedRecords,
            SpecialCategoriesAffected = i.SpecialCategoriesAffected,
            LikelyConsequences = i.LikelyConsequences,
            RiskLevel = i.RiskLevel.ToString(),
            RiskAssessmentReason = i.RiskAssessmentReason,
            SupervisoryAuthorityNotificationRequired = i.SupervisoryAuthorityNotificationRequired.ToString(),
            SupervisoryAuthorityNotificationReason = i.SupervisoryAuthorityNotificationReason,
            SupervisoryAuthorityName = i.SupervisoryAuthorityName,
            SupervisoryAuthorityNotifiedAt = i.SupervisoryAuthorityNotifiedAt,
            SupervisoryAuthorityReference = i.SupervisoryAuthorityReference,
            NotificationDelayReason = i.NotificationDelayReason,
            DataSubjectsNotificationRequired = i.DataSubjectsNotificationRequired.ToString(),
            DataSubjectsNotificationReason = i.DataSubjectsNotificationReason,
            DataSubjectsNotifiedAt = i.DataSubjectsNotifiedAt,
            DataSubjectsNotificationMethod = i.DataSubjectsNotificationMethod,
            DataSubjectsNotificationSummary = i.DataSubjectsNotificationSummary,
            ImmediateActions = i.ImmediateActions,
            RemediationActions = i.RemediationActions,
            PreventiveActions = i.PreventiveActions,
            ClosureSummary = i.ClosureSummary,
            ClosedAt = i.ClosedAt,
            CreatedByUserId = i.CreatedByUserId,
            UpdatedByUserId = i.UpdatedByUserId,
            IsArchived = i.IsArchived,
            ArchivedAt = i.ArchivedAt,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt,
            LinkedProcessingActivityIds = paLinks.Where(l => l.PrivacyIncidentId == i.Id).Select(l => l.ProcessingActivityId).ToList(),
            LinkedServiceProviderIds = spLinks.Where(l => l.PrivacyIncidentId == i.Id).Select(l => l.ServiceProviderId).ToList(),
            LinkedMeasureIds = measureLinks.Where(l => l.PrivacyIncidentId == i.Id).Select(l => l.MeasureId).ToList(),
            LinkedTomIds = tomLinks.Where(l => l.PrivacyIncidentId == i.Id).Select(l => l.TomId).ToList()
        }).ToList();
    }

    private static async Task<IReadOnlyList<AuditTemplateExportDto>> LoadAuditTemplatesAsync(
        ApplicationDbContext db, int tenantId, CancellationToken ct)
    {
        var items = await db.AuditTemplates
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .ToListAsync(ct);

        var templateIds = items.Select(t => t.Id).ToList();
        var questions = templateIds.Count == 0
            ? []
            : await db.AuditQuestions
                .AsNoTracking()
                .Where(q => templateIds.Contains(q.AuditTemplateId))
                .ToListAsync(ct);

        var questionsByTemplate = questions.GroupBy(q => q.AuditTemplateId).ToDictionary(g => g.Key, g => g.ToList());

        return items.Select(t => new AuditTemplateExportDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            Version = t.Version,
            IsActive = t.IsActive,
            IsArchived = t.IsArchived,
            ArchivedAt = t.ArchivedAt,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            Questions = (questionsByTemplate.GetValueOrDefault(t.Id) ?? [])
                .OrderBy(q => q.SortOrder)
                .Select(q => new AuditQuestionExportDto
                {
                    Id = q.Id,
                    SortOrder = q.SortOrder,
                    Text = q.Text,
                    Category = q.Category,
                    IsRequired = q.IsRequired
                }).ToList()
        }).ToList();
    }

    private static async Task<IReadOnlyList<AuditRunExportDto>> LoadAuditRunsAsync(
        ApplicationDbContext db, int tenantId, CancellationToken ct)
    {
        var items = await db.AuditRuns
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(ct);

        if (items.Count == 0)
        {
            return [];
        }

        var runIds = items.Select(r => r.Id).ToList();
        var templateIds = items.Select(r => r.AuditTemplateId).Distinct().ToList();

        // Nur benötigte Felder projizieren – vermeidet InvalidCast bei NULL in IsArchived/IsActive.
        var templates = await db.AuditTemplates
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => templateIds.Contains(t.Id))
            .Select(t => new AuditTemplateRef
            {
                Id = t.Id,
                Title = t.Title,
                Version = t.Version
            })
            .ToDictionaryAsync(t => t.Id, ct);

        var answers = await db.AuditAnswers
            .AsNoTracking()
            .Where(a => runIds.Contains(a.AuditRunId))
            .Select(a => new
            {
                a.Id,
                a.AuditRunId,
                a.AuditQuestionId,
                a.AnswerText,
                a.ComplianceLevel,
                a.Notes,
                a.AnsweredAt
            })
            .ToListAsync(ct);

        var questionIds = answers.Select(a => a.AuditQuestionId).Distinct().ToList();
        var questions = questionIds.Count == 0
            ? new Dictionary<int, AuditQuestionRef>()
            : await db.AuditQuestions
                .AsNoTracking()
                .Where(q => questionIds.Contains(q.Id))
                .Select(q => new AuditQuestionRef
                {
                    Id = q.Id,
                    Text = q.Text,
                    Category = q.Category,
                    SortOrder = q.SortOrder
                })
                .ToDictionaryAsync(q => q.Id, ct);

        var answerIds = answers.Select(a => a.Id).ToList();
        var paLinks = answerIds.Count == 0
            ? []
            : await db.ProcessingActivityAuditAnswers
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(l => l.TenantId == tenantId && answerIds.Contains(l.AuditAnswerId))
                .ToListAsync(ct);

        var measureLinks = answerIds.Count == 0
            ? []
            : await db.Measures
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(m => m.TenantId == tenantId && m.AuditAnswerId != null && answerIds.Contains(m.AuditAnswerId.Value))
                .Select(m => new { m.Id, m.AuditAnswerId })
                .ToListAsync(ct);

        var answersByRun = answers.GroupBy(a => a.AuditRunId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var paLinksByAnswer = paLinks.GroupBy(l => l.AuditAnswerId).ToDictionary(g => g.Key, g => g.ToList());
        var measuresByAnswer = measureLinks
            .Where(m => m.AuditAnswerId.HasValue)
            .GroupBy(m => m.AuditAnswerId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        return items.Select(r =>
        {
            templates.TryGetValue(r.AuditTemplateId, out var template);
            var runAnswers = answersByRun.GetValueOrDefault(r.Id) ?? [];

            return new AuditRunExportDto
            {
                Id = r.Id,
                AuditTemplateId = r.AuditTemplateId,
                TemplateTitle = template?.Title,
                TemplateVersion = template?.Version,
                Title = r.Title,
                Status = r.Status.ToString(),
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt,
                AssignedUserId = r.AssignedUserId,
                IsArchived = r.IsArchived,
                ArchivedAt = r.ArchivedAt,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                Answers = runAnswers.Select(a =>
                {
                    questions.TryGetValue(a.AuditQuestionId, out var question);
                    return new AuditAnswerExportDto
                    {
                        Id = a.Id,
                        AuditQuestionId = a.AuditQuestionId,
                        QuestionText = question?.Text ?? string.Empty,
                        QuestionCategory = question?.Category,
                        QuestionSortOrder = question?.SortOrder ?? 0,
                        AnswerText = a.AnswerText,
                        ComplianceLevel = a.ComplianceLevel.ToString(),
                        Notes = a.Notes,
                        AnsweredAt = a.AnsweredAt,
                        LinkedProcessingActivityIds = (paLinksByAnswer.GetValueOrDefault(a.Id) ?? [])
                            .Select(l => l.ProcessingActivityId).ToList(),
                        LinkedMeasureIds = measuresByAnswer.GetValueOrDefault(a.Id) ?? []
                    };
                }).ToList()
            };
        }).ToList();
    }

    private async Task<(IReadOnlyList<DocumentMetadataExportDto> Metadata, List<DocumentFileEntry> Files)> LoadDocumentsAsync(
        ApplicationDbContext db, int tenantId, List<string> warnings, CancellationToken ct)
    {
        var docs = await db.EvidenceDocuments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .ToListAsync(ct);

        var allLinks = await db.DocumentLinks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct);

        var linksByDocument = allLinks.GroupBy(l => l.DocumentId).ToDictionary(g => g.Key, g => g.ToList());

        var metadata = new List<DocumentMetadataExportDto>();
        var files = new List<DocumentFileEntry>();

        foreach (var doc in docs)
        {
            var docLinks = linksByDocument.GetValueOrDefault(doc.Id) ?? [];
            var linkedEntities = docLinks
                .Select(l => new DocumentLinkExportDto
                {
                    EntityType = MapEntityTypeForExport(l.LinkedEntityType),
                    EntityId = l.LinkedEntityId
                })
                .ToList();

            var safeName = SanitizeFileName(doc.FileName);
            var zipPath = $"documents/files/{doc.Id}-{safeName}";
            var fullPath = documentStorage.GetFullPath(doc.StoragePath);
            var fileExists = File.Exists(fullPath);

            if (!fileExists)
            {
                warnings.Add($"Dokument {doc.Id} ({doc.FileName}): Datei im Storage nicht gefunden.");
            }

            var firstLink = linkedEntities.FirstOrDefault();
            metadata.Add(new DocumentMetadataExportDto
            {
                DocumentId = doc.Id,
                Title = doc.FileName,
                FileName = doc.FileName,
                ContentType = doc.ContentType,
                FileSizeBytes = doc.FileSizeBytes,
                ModuleReference = firstLink?.EntityType,
                EntityId = firstLink?.EntityId,
                LinkedEntities = linkedEntities,
                CreatedAt = doc.CreatedAt,
                CreatedByUserId = doc.UploadedByUserId,
                RelativePathInZip = zipPath,
                FileIncluded = fileExists,
                FileWarning = fileExists ? null : "Datei im Storage nicht gefunden."
            });

            if (fileExists)
            {
                files.Add(new DocumentFileEntry(doc.Id, doc.FileName, zipPath, fullPath));
            }
        }

        return (metadata, files);
    }

    private static string MapEntityTypeForExport(DocumentLinkedEntityType type) => type switch
    {
        DocumentLinkedEntityType.AuditRun => "AuditRun",
        DocumentLinkedEntityType.Measure => "Measure",
        DocumentLinkedEntityType.ServiceProvider => "ServiceProvider",
        DocumentLinkedEntityType.ProcessingActivity => "ProcessingActivity",
        DocumentLinkedEntityType.Dsfa => "Dsfa",
        DocumentLinkedEntityType.PrivacyIncident => "PrivacyIncident",
        DocumentLinkedEntityType.Tom => "Tom",
        DocumentLinkedEntityType.Training => "Training",
        _ => type.ToString()
    };

    private static string BuildZipFileName(string tenantName, string productName)
    {
        var safe = SanitizeFileName(tenantName);
        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = "tenant";
        }

        var productSlug = SanitizeFileName(productName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(productSlug))
        {
            productSlug = "export";
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmm");
        return $"{productSlug}-tenant-export-{safe}-{timestamp}.zip";
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name
            .Select(c => invalid.Contains(c) || c == ' ' ? '_' : c)
            .ToArray();
        var result = new string(chars).Trim('_');
        return string.IsNullOrEmpty(result) ? "unnamed" : result;
    }

    private sealed record DocumentFileEntry(int DocumentId, string FileName, string ZipPath, string FullPath);
}
