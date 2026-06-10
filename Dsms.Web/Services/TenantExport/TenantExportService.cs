using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dsms.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.TenantExport;

public class TenantExportService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    UserManager<ApplicationUser> userManager,
    DocumentStorageService documentStorage,
    ILogger<TenantExportService> logger) : ITenantExportService
{
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
        var auditTemplates = await LoadAuditTemplatesAsync(db, tenantId, ct);
        var auditRuns = await LoadAuditRunsAsync(db, tenantId, ct);
        var (documents, fileEntries) = await LoadDocumentsAsync(db, tenantId, warnings, ct);

        var exportInfo = new ExportInfoDto
        {
            ExportCreatedAt = DateTime.UtcNow,
            ExportCreatedByUserId = userId,
            ExportCreatedByEmail = exportUser?.Email,
            TenantId = tenantId,
            TenantName = tenant.Name,
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
            AddJsonEntry(archive, "audit-templates.json", auditTemplates);
            AddJsonEntry(archive, "audit-runs.json", auditRuns);
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
        var fileName = BuildZipFileName(tenant.Name);

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
            Category = t.Category.ToString(),
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
            ProviderType = s.ProviderType.ToString(),
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

        var metadata = new List<DocumentMetadataExportDto>();
        var files = new List<DocumentFileEntry>();

        foreach (var doc in docs)
        {
            var safeName = SanitizeFileName(doc.FileName);
            var zipPath = $"documents/files/{doc.Id}-{safeName}";
            var fullPath = documentStorage.GetFullPath(doc.StoragePath);
            var fileExists = File.Exists(fullPath);

            if (!fileExists)
            {
                warnings.Add($"Dokument {doc.Id} ({doc.FileName}): Datei im Storage nicht gefunden.");
            }

            metadata.Add(new DocumentMetadataExportDto
            {
                DocumentId = doc.Id,
                Title = doc.FileName,
                FileName = doc.FileName,
                ContentType = doc.ContentType,
                FileSizeBytes = doc.FileSizeBytes,
                ModuleReference = ResolveModuleReference(doc),
                EntityId = ResolveEntityId(doc),
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

    private static string? ResolveModuleReference(Domain.Entities.EvidenceDocument doc)
    {
        if (doc.ProcessingActivityId.HasValue) return "ProcessingActivity";
        if (doc.DataProtectionImpactAssessmentId.HasValue) return "Dsfa";
        if (doc.ServiceProviderId.HasValue) return "ServiceProvider";
        if (doc.AuditRunId.HasValue) return "AuditRun";
        if (doc.MeasureId.HasValue) return "Measure";
        return null;
    }

    private static int? ResolveEntityId(Domain.Entities.EvidenceDocument doc) =>
        doc.ProcessingActivityId
        ?? doc.DataProtectionImpactAssessmentId
        ?? doc.ServiceProviderId
        ?? doc.AuditRunId
        ?? doc.MeasureId;

    private static string BuildZipFileName(string tenantName)
    {
        var safe = SanitizeFileName(tenantName);
        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = "tenant";
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmm");
        return $"dsms-tenant-export-{safe}-{timestamp}.zip";
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
