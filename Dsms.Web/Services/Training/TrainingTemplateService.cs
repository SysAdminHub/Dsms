using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.CommunityTemplates;
using Dsms.Web.Services.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Training;

/// <summary>Verwaltung von Schulungsvorlagen inkl. Karten und Archivierung.</summary>
public class TrainingTemplateService(
    ApplicationDbContext db,
    IUserAccessService access,
    ICurrentUserContext currentUser,
    UserManager<ApplicationUser> userManager,
    ArchiveViewContextAccessor archiveView,
    TrainingTemplateAccessService templateAccess,
    TrainingTemplateAssetService assetService,
    TrainingQuestionService questionService,
    IComplianceAuditLogService complianceAuditLog,
    ICommunityTemplateNotificationService communityTemplateNotification)
{
    public Task<bool> CanViewAsync(TrainingTemplate template, CancellationToken ct = default) =>
        templateAccess.CanViewAsync(template, ct);

    public Task<bool> CanEditAsync(TrainingTemplate template, CancellationToken ct = default) =>
        templateAccess.CanEditAsync(template, ct);

    public Task<bool> CanCreateTenantTemplateAsync(CancellationToken ct = default) =>
        templateAccess.CanCreateTenantTemplateAsync(ct);

    public Task<bool> CanCreateGlobalTemplateAsync(CancellationToken ct = default) =>
        templateAccess.CanCreateGlobalTemplateAsync(ct);

    public Task<bool> CanReviewCommunityAsync(CancellationToken ct = default) =>
        templateAccess.CanReviewCommunityAsync(ct);

    public Task<bool> CanSubmitToCommunityAsync(TrainingTemplate template, CancellationToken ct = default) =>
        templateAccess.CanSubmitToCommunityAsync(template, ct);

    public async Task<bool> CanCopyToTenantAsync(TrainingTemplate template, CancellationToken ct = default)
    {
        if (!TrainingTemplateAccessService.IsGlobalTemplate(template))
            return false;

        if (!await templateAccess.CanViewAsync(template, ct))
            return false;

        return await templateAccess.CanCreateTenantTemplateAsync(ct);
    }

    public Task<bool> CanArchiveAsync(TrainingTemplate template, CancellationToken ct = default) =>
        templateAccess.CanArchiveAsync(template, ct);

    public async Task<IReadOnlyList<TrainingTemplateListRow>> GetGlobalListRowsAsync(CancellationToken ct = default)
    {
        if (!await access.CanManageGlobalTrainingTemplatesAsync())
            return [];

        var templates = await db.TrainingTemplates
            .IgnoreQueryFilters()
            .Include(t => t.Sections)
            .Include(t => t.Questions)
            .Include(t => t.Assets)
            .Where(t => t.IsGlobal && t.TenantId == null)
            .Where(t => t.IsArchived == archiveView.ShowArchivedOnly)
            .OrderByDescending(t => archiveView.ShowArchivedOnly ? t.ArchivedAt : t.CreatedAt)
            .ThenBy(t => t.Title)
            .ToListAsync(ct);

        var rows = new List<TrainingTemplateListRow>();
        foreach (var template in templates)
        {
            rows.Add(new TrainingTemplateListRow(
                template,
                template.Sections.Count(s => s.IsActive),
                template.Questions.Count(q => q.IsActive),
                template.Assets.Count(a => a.IsActive),
                await templateAccess.CanEditAsync(template, ct),
                await templateAccess.CanArchiveAsync(template, ct),
                false));
        }

        return rows;
    }

    public async Task<IReadOnlyList<TrainingTemplateListRow>> GetListRowsAsync(
        int tenantId, CancellationToken ct = default)
    {
        if (!await access.CanAccessTenantAsync(tenantId))
            return [];

        var templates = await db.TrainingTemplates
            .Include(t => t.Sections)
            .Include(t => t.Questions)
            .Include(t => t.Assets)
            .Where(t => (t.IsGlobal && t.TenantId == null) || (t.TenantId == tenantId && !t.IsGlobal))
            .Where(t => t.IsArchived == archiveView.ShowArchivedOnly)
            .OrderByDescending(t => archiveView.ShowArchivedOnly ? t.ArchivedAt : t.CreatedAt)
            .ThenBy(t => t.Title)
            .ToListAsync(ct);

        var rows = new List<TrainingTemplateListRow>();
        foreach (var template in templates)
        {
            rows.Add(new TrainingTemplateListRow(
                template,
                template.Sections.Count(s => s.IsActive),
                template.Questions.Count(q => q.IsActive),
                template.Assets.Count(a => a.IsActive),
                await templateAccess.CanEditAsync(template, ct),
                await CanArchiveAsync(template, ct),
                await CanCopyToTenantAsync(template, ct)));
        }

        return rows;
    }

    public async Task<TrainingTemplateOperationResult> CopyToCurrentTenantAsync(
        int sourceTemplateId, CancellationToken ct = default)
    {
        var tenantId = await access.GetCurrentTenantIdAsync();
        if (tenantId is null)
            return TrainingTemplateOperationResult.Fail(TrainingLabels.AccessDenied);

        return await CopyTemplateAsync(sourceTemplateId, tenantId.Value, ct: ct);
    }

    public async Task<TrainingTemplateOperationResult> RestoreTemplateAsync(
        int templateId, CancellationToken ct = default)
    {
        var template = await db.TrainingTemplates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == templateId && t.IsArchived, ct);

        if (template is null || !await templateAccess.CanEditAsync(template, ct))
            return TrainingTemplateOperationResult.Fail(TrainingLabels.AccessDenied);

        var userId = await currentUser.GetUserIdAsync();
        template.IsArchived = false;
        template.ArchivedAt = null;
        template.ArchivedByUserId = null;
        template.IsActive = true;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = userId;

        await db.SaveChangesAsync(ct);
        await complianceAuditLog.LogTrainingTemplateUpdatedAsync(template.Id, template.Title, template.TenantId, []);
        return TrainingTemplateOperationResult.Ok(template.Id);
    }

    public async Task<TrainingTemplateOperationResult> DeactivateSectionAsync(
        int templateId, int sectionId, CancellationToken ct = default)
    {
        var sectionsQuery = await templateAccess.ApplyQueryScopeAsync(
            db.TrainingTemplateSections.Include(s => s.TrainingTemplate), ct);
        var section = await sectionsQuery
            .FirstOrDefaultAsync(s => s.Id == sectionId && s.TrainingTemplateId == templateId && s.IsActive, ct);

        if (section is null || !await templateAccess.CanEditAsync(section.TrainingTemplate, ct))
            return TrainingTemplateOperationResult.Fail(TrainingLabels.AccessDenied);

        var userId = await currentUser.GetUserIdAsync();
        section.IsActive = false;
        section.UpdatedAt = DateTime.UtcNow;
        section.UpdatedByUserId = userId;
        section.TrainingTemplate.UpdatedAt = DateTime.UtcNow;
        section.TrainingTemplate.UpdatedByUserId = userId;

        await db.SaveChangesAsync(ct);
        await complianceAuditLog.LogTrainingTemplateSectionChangedAsync(
            templateId, section.TrainingTemplate.Title, section.TrainingTemplate.TenantId, sectionId, section.Title);
        return TrainingTemplateOperationResult.Ok(templateId);
    }

    public async Task<TrainingTemplateValidationReport> BuildValidationReportAsync(
        int templateId, int? tenantId, CancellationToken ct = default)
    {
        var report = new TrainingTemplateValidationReport();
        var template = await GetTemplateByIdAsync(templateId, tenantId, ct: ct);
        if (template is null)
        {
            report.Errors.Add("Schulungsvorlage wurde nicht gefunden.");
            return report;
        }

        if (string.IsNullOrWhiteSpace(template.Title))
            report.Errors.Add("Titel fehlt.");
        else
            report.Successes.Add("Titel vorhanden");

        if (template.PassingScorePercent is < 0 or > 100)
            report.Errors.Add("Bestehensgrenze muss zwischen 0 und 100 liegen.");

        if (template.RecommendedRepeatAfterMonths is <= 0)
            report.Warnings.Add("Empfohlene Wiederholung ist nicht gesetzt oder ungültig.");

        var sections = await GetSectionsAsync(templateId, tenantId, ct);
        if (sections.Count == 0)
            report.Errors.Add("Mindestens eine aktive Schulungskarte ist erforderlich.");
        else
            report.Successes.Add($"{sections.Count} aktive Karte(n)");

        var assetsQuery = await templateAccess.ApplyQueryScopeAsync(db.TrainingTemplateAssets.AsQueryable(), ct);
        var assets = await assetsQuery
            .Where(a => a.TrainingTemplateId == templateId && a.IsActive)
            .ToListAsync(ct);

        var usedAssetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var section in sections)
        {
            foreach (var key in TrainingMarkdownAssetResolver.ExtractAssetKeys(section.ContentMarkdown))
            {
                usedAssetKeys.Add(key);
                if (assets.All(a => !a.AssetKey.Equals(key, StringComparison.OrdinalIgnoreCase)))
                    report.Errors.Add($"Fehlendes Asset in Karte „{section.Title}“: {key}");
            }
        }

        if (usedAssetKeys.Count > 0 && !report.Errors.Any(e => e.Contains("Fehlendes Asset")))
            report.Successes.Add("Alle Bildverweise gültig");

        foreach (var asset in assets.Where(a => !usedAssetKeys.Contains(a.AssetKey)))
            report.Warnings.Add($"Bild „{asset.AssetKey}“ wird aktuell nicht verwendet.");

        if (template.IsQuizRequired)
        {
            var quizValidation = await questionService.ValidateQuizAsync(templateId, tenantId, ct);
            report.Errors.AddRange(quizValidation.Errors);
            if (quizValidation.IsValid)
            {
                var questionCount = await db.TrainingQuestions.CountAsync(
                    q => q.TrainingTemplateId == templateId && q.IsActive, ct);
                report.Successes.Add($"{questionCount} aktive Frage(n)");
            }
        }
        else if (template.Questions.Count > 0)
        {
            report.Warnings.Add("Quiz ist nicht erforderlich, es existieren aber Fragen.");
        }

        return report;
    }

    public async Task<IReadOnlyList<TrainingTemplate>> GetTemplatesForTenantAsync(
        int tenantId, bool includeGlobal = true, CancellationToken ct = default)
    {
        if (!await access.CanAccessTenantAsync(tenantId))
            return [];

        return await templateAccess.VisibleTemplatesQuery(tenantId, includeGlobal)
            .OrderByDescending(t => archiveView.ShowArchivedOnly ? t.ArchivedAt : t.CreatedAt)
            .ThenBy(t => t.Title)
            .ToListAsync(ct);
    }

    public async Task<TrainingTemplate?> GetTemplateByIdAsync(
        int templateId, int? tenantId, bool includeGlobal = true, CancellationToken ct = default)
    {
        var query = db.TrainingTemplates.AsQueryable();
        if (await templateAccess.RequiresUnfilteredQueriesAsync(ct))
            query = query.IgnoreQueryFilters();

        var template = await query
            .Include(t => t.Sections.Where(s => s.IsActive))
            .Include(t => t.Assets.Where(a => a.IsActive))
            .Include(t => t.Questions.Where(q => q.IsActive))
                .ThenInclude(q => q.Options.Where(o => o.IsActive))
            .FirstOrDefaultAsync(t => t.Id == templateId, ct);

        if (template is null || !await templateAccess.CanViewAsync(template, ct))
            return null;

        if (!includeGlobal && TrainingTemplateAccessService.IsGlobalTemplate(template))
            return null;

        if (!TrainingTemplateAccessService.IsGlobalTemplate(template) && tenantId.HasValue && template.TenantId != tenantId)
            return null;

        return template;
    }

    public async Task<TrainingTemplate?> GetSubmittedForReviewAsync(int templateId, CancellationToken ct = default)
    {
        if (!await templateAccess.CanReviewCommunityAsync(ct))
            return null;

        var template = await db.TrainingTemplates
            .IgnoreQueryFilters()
            .Include(t => t.Tenant)
            .Include(t => t.Sections.Where(s => s.IsActive))
            .Include(t => t.Assets.Where(a => a.IsActive))
            .Include(t => t.Questions.Where(q => q.IsActive))
                .ThenInclude(q => q.Options.Where(o => o.IsActive))
            .FirstOrDefaultAsync(t => t.Id == templateId
                && !t.IsGlobal
                && t.TenantId != null
                && t.CommunityStatus == CommunityTemplateStatus.Submitted
                && !t.IsArchived, ct);

        return template;
    }

    public async Task<CommunityTrainingTemplateReviewViewModel?> GetCommunityReviewViewModelAsync(
        int templateId, CancellationToken ct = default)
    {
        var template = await GetSubmittedForReviewAsync(templateId, ct);
        if (template is null)
            return null;

        var sections = template.Sections.OrderBy(s => s.SortOrder).ToList();
        var sectionPreviewHtml = await PrepareSectionPreviewHtmlAsync(
            template.Id, template.TenantId, sections, ct);
        var questions = template.Questions
            .Where(q => q.IsActive)
            .OrderBy(q => q.SortOrder)
            .ToList();

        await complianceAuditLog.LogTrainingTemplateCommunityReviewOpenedAsync(template.Id, template.Title);
        if (questions.Count > 0)
        {
            await complianceAuditLog.LogTrainingTemplateCommunityQuizReviewOpenedAsync(
                template.Id, template.Title, template.TenantId, questions.Count);
        }

        return new CommunityTrainingTemplateReviewViewModel(
            template,
            template.Tenant?.Name ?? "—",
            sections,
            sectionPreviewHtml,
            questions);
    }

    public async Task<IReadOnlyList<TrainingCommunitySubmissionRow>> ListSubmittedForReviewAsync(
        CancellationToken ct = default)
    {
        if (!await templateAccess.CanReviewCommunityAsync(ct))
            return [];

        var submissions = await db.TrainingTemplates
            .IgnoreQueryFilters()
            .Include(t => t.Tenant)
            .Include(t => t.Sections)
            .Include(t => t.Questions)
            .Where(t => !t.IsGlobal
                && t.TenantId != null
                && t.CommunityStatus == CommunityTemplateStatus.Submitted
                && !t.IsArchived)
            .OrderByDescending(t => t.CommunitySubmittedAt)
            .ThenBy(t => t.Title)
            .ToListAsync(ct);

        var rows = new List<TrainingCommunitySubmissionRow>();
        foreach (var template in submissions)
        {
            string? submittedByName = null;
            if (!string.IsNullOrEmpty(template.CommunitySubmittedByUserId))
            {
                var user = await userManager.FindByIdAsync(template.CommunitySubmittedByUserId);
                submittedByName = user?.DisplayName;
            }

            rows.Add(new TrainingCommunitySubmissionRow(
                template,
                template.Tenant?.Name ?? "—",
                submittedByName,
                template.Sections.Count(s => s.IsActive),
                template.Questions.Count(q => q.IsActive)));
        }

        return rows;
    }

    public async Task<TrainingTemplateOperationResult> SubmitToCommunityAsync(
        int templateId, string? submissionNote, CancellationToken ct = default)
    {
        var template = await db.TrainingTemplates
            .IgnoreQueryFilters()
            .Include(t => t.Tenant)
            .FirstOrDefaultAsync(t => t.Id == templateId, ct);

        if (template is null || !await templateAccess.CanSubmitToCommunityAsync(template, ct))
            return TrainingTemplateOperationResult.Fail(TrainingLabels.AccessDenied);

        var activeSections = await db.TrainingTemplateSections
            .IgnoreQueryFilters()
            .CountAsync(s => s.TrainingTemplateId == templateId && s.IsActive, ct);
        if (activeSections == 0)
            return TrainingTemplateOperationResult.Fail("Mindestens eine aktive Schulungskarte ist erforderlich.");

        var previousStatus = template.CommunityStatus;
        var userId = await currentUser.GetUserIdAsync();
        var now = DateTime.UtcNow;
        var tenantId = template.TenantId!.Value;

        template.IsCommunityTemplate = true;
        template.CommunityStatus = CommunityTemplateStatus.Submitted;
        template.CommunitySubmittedAt = now;
        template.CommunitySubmittedByUserId = userId;
        template.CommunitySubmittedByTenantId = tenantId;
        template.CommunitySubmissionNote = submissionNote?.Trim();
        template.CommunityReviewedAt = null;
        template.CommunityReviewedByUserId = null;
        template.CommunityReviewNote = null;
        template.CommunityRejectionReason = null;
        template.UpdatedAt = now;
        template.UpdatedByUserId = userId;

        await db.SaveChangesAsync(ct);
        await complianceAuditLog.LogTrainingTemplateSubmittedToCommunityAsync(template.Id, template.Title, tenantId);

        if (previousStatus is CommunityTemplateStatus.None or CommunityTemplateStatus.Rejected)
        {
            ApplicationUser? user = userId is not null
                ? await userManager.FindByIdAsync(userId)
                : null;
            await communityTemplateNotification.TryNotifyCommunityTemplateSubmittedAsync(
                new CommunityTemplateNotificationModel
                {
                    TemplateType = CommunityTemplateNotificationType.Training,
                    TemplateId = template.Id,
                    TemplateTitle = template.Title,
                    TenantId = tenantId,
                    TenantName = template.Tenant?.Name ?? "—",
                    SubmittedByUserId = userId ?? "—",
                    SubmittedByName = user?.DisplayName ?? user?.UserName,
                    SubmittedByEmail = user?.Email,
                    SubmittedAt = now
                },
                ct);
        }

        return TrainingTemplateOperationResult.Ok(template.Id, TrainingLabels.CommunitySubmitSuccess);
    }

    public async Task<TrainingTemplateOperationResult> ApproveCommunityAsync(
        int templateId, string? reviewComment, CancellationToken ct = default)
    {
        if (!await templateAccess.CanReviewCommunityAsync(ct))
            return TrainingTemplateOperationResult.Fail(TrainingLabels.AccessDenied);

        var source = await db.TrainingTemplates
            .IgnoreQueryFilters()
            .Include(t => t.Sections.Where(s => s.IsActive))
            .Include(t => t.Assets.Where(a => a.IsActive))
            .Include(t => t.Questions.Where(q => q.IsActive))
                .ThenInclude(q => q.Options.Where(o => o.IsActive))
            .FirstOrDefaultAsync(t => t.Id == templateId
                && !t.IsGlobal
                && t.TenantId != null
                && t.CommunityStatus == CommunityTemplateStatus.Submitted, ct);

        if (source is null)
            return TrainingTemplateOperationResult.Fail("Einreichung wurde nicht gefunden.");

        var userId = await currentUser.GetUserIdAsync();
        var now = DateTime.UtcNow;
        var sourceTenantId = source.TenantId;
        var questionCountCopied = source.Questions.Count;

        var globalCopy = new TrainingTemplate
        {
            TenantId = null,
            Title = source.Title,
            Description = source.Description,
            TrainingType = source.TrainingType,
            TargetAudience = source.TargetAudience,
            EstimatedDurationMinutes = source.EstimatedDurationMinutes,
            RecommendedRepeatAfterMonths = source.RecommendedRepeatAfterMonths,
            PassingScorePercent = source.PassingScorePercent,
            IsQuizRequired = source.IsQuizRequired,
            IsGlobal = true,
            IsCommunityTemplate = true,
            CommunityStatus = CommunityTemplateStatus.Approved,
            SourceTemplateId = source.Id,
            CommunitySubmittedAt = source.CommunitySubmittedAt,
            CommunitySubmittedByUserId = source.CommunitySubmittedByUserId,
            CommunitySubmittedByTenantId = source.CommunitySubmittedByTenantId,
            CommunitySubmissionNote = source.CommunitySubmissionNote,
            CommunityReviewedAt = now,
            CommunityReviewedByUserId = userId,
            CommunityReviewNote = reviewComment?.Trim(),
            IsActive = true,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            CreatedAt = now
        };

        db.TrainingTemplates.Add(globalCopy);
        await db.SaveChangesAsync(ct);

        await CopyTemplateChildrenAsync(source, globalCopy, null, userId, ct);
        await assetService.CopyAssetsFromTemplateAsync(source.Id, globalCopy.Id, null, userId, ct);

        source.CommunityStatus = CommunityTemplateStatus.Approved;
        source.CommunityReviewedAt = now;
        source.CommunityReviewedByUserId = userId;
        source.CommunityReviewNote = reviewComment?.Trim();
        source.UpdatedAt = now;
        source.UpdatedByUserId = userId;

        await db.SaveChangesAsync(ct);
        await complianceAuditLog.LogTrainingTemplateCommunityApprovedAsync(
            globalCopy.Id, globalCopy.Title, source.Id, sourceTenantId, questionCountCopied);

        if (!string.IsNullOrEmpty(source.CommunitySubmittedByUserId))
        {
            ApplicationUser? submitter = await userManager.FindByIdAsync(source.CommunitySubmittedByUserId);
            await communityTemplateNotification.TryNotifyCommunityTemplateReviewedAsync(
                new CommunityTemplateReviewNotificationModel
                {
                    TemplateType = CommunityTemplateNotificationType.Training,
                    TemplateId = source.Id,
                    TemplateTitle = source.Title,
                    TenantId = sourceTenantId,
                    SubmittedByUserId = source.CommunitySubmittedByUserId,
                    SubmittedByName = submitter?.DisplayName ?? submitter?.UserName,
                    SubmittedByEmail = submitter?.Email,
                    IsApproved = true,
                    ReviewComment = reviewComment?.Trim(),
                    ReviewedAt = now
                },
                ct);
        }

        return TrainingTemplateOperationResult.Ok(globalCopy.Id, TrainingLabels.CommunityApproveSuccess);
    }

    public async Task<TrainingTemplateOperationResult> RejectCommunityAsync(
        int templateId, string? rejectionReason, CancellationToken ct = default)
    {
        if (!await templateAccess.CanReviewCommunityAsync(ct))
            return TrainingTemplateOperationResult.Fail(TrainingLabels.AccessDenied);

        var source = await db.TrainingTemplates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == templateId
                && !t.IsGlobal
                && t.TenantId != null
                && t.CommunityStatus == CommunityTemplateStatus.Submitted, ct);

        if (source is null)
            return TrainingTemplateOperationResult.Fail("Einreichung wurde nicht gefunden.");

        var userId = await currentUser.GetUserIdAsync();
        var now = DateTime.UtcNow;
        var tenantId = source.TenantId!.Value;

        source.CommunityStatus = CommunityTemplateStatus.Rejected;
        source.CommunityReviewedAt = now;
        source.CommunityReviewedByUserId = userId;
        source.CommunityRejectionReason = rejectionReason?.Trim();
        source.UpdatedAt = now;
        source.UpdatedByUserId = userId;

        await db.SaveChangesAsync(ct);
        await complianceAuditLog.LogTrainingTemplateCommunityRejectedAsync(source.Id, source.Title, tenantId);

        if (!string.IsNullOrEmpty(source.CommunitySubmittedByUserId))
        {
            ApplicationUser? submitter = await userManager.FindByIdAsync(source.CommunitySubmittedByUserId);
            await communityTemplateNotification.TryNotifyCommunityTemplateReviewedAsync(
                new CommunityTemplateReviewNotificationModel
                {
                    TemplateType = CommunityTemplateNotificationType.Training,
                    TemplateId = source.Id,
                    TemplateTitle = source.Title,
                    TenantId = tenantId,
                    SubmittedByUserId = source.CommunitySubmittedByUserId,
                    SubmittedByName = submitter?.DisplayName ?? submitter?.UserName,
                    SubmittedByEmail = submitter?.Email,
                    IsApproved = false,
                    ReviewComment = rejectionReason?.Trim(),
                    ReviewedAt = now
                },
                ct);
        }

        return TrainingTemplateOperationResult.Ok(source.Id, TrainingLabels.CommunityRejectSuccess);
    }

    public async Task<TrainingTemplateOperationResult> CreateTemplateAsync(
        TrainingTemplate template, CancellationToken ct = default)
    {
        if (template.IsGlobal)
        {
            if (!await templateAccess.CanCreateGlobalTemplateAsync(ct))
                return TrainingTemplateOperationResult.Fail(TrainingLabels.AccessDenied);

            template.TenantId = null;
        }
        else
        {
            if (!await templateAccess.CanCreateTenantTemplateAsync(ct))
                return TrainingTemplateOperationResult.Fail(TrainingLabels.AccessDenied);

            var tenantId = await access.GetCurrentTenantIdAsync();
            if (tenantId is null)
                return TrainingTemplateOperationResult.Fail(TrainingLabels.AccessDenied);

            template.TenantId = tenantId;
            template.IsGlobal = false;
        }

        var userId = await currentUser.GetUserIdAsync();
        template.CreatedByUserId = userId;
        template.UpdatedByUserId = userId;
        template.CreatedAt = DateTime.UtcNow;

        db.TrainingTemplates.Add(template);
        await db.SaveChangesAsync(ct);

        await complianceAuditLog.LogTrainingTemplateCreatedAsync(template.Id, template.Title, template.TenantId);
        return TrainingTemplateOperationResult.Ok(template.Id);
    }

    public async Task<TrainingTemplateOperationResult> UpdateTemplateAsync(
        TrainingTemplate model, CancellationToken ct = default)
    {
        var template = await templateAccess.GetTemplateForMutationAsync(model.Id, ct);
        if (template is null)
            return TrainingTemplateOperationResult.Fail(TrainingLabels.AccessDenied);

        template.Title = model.Title.Trim();
        template.Description = model.Description?.Trim();
        template.TrainingType = model.TrainingType;
        template.TargetAudience = model.TargetAudience?.Trim();
        template.EstimatedDurationMinutes = model.EstimatedDurationMinutes;
        template.RecommendedRepeatAfterMonths = model.RecommendedRepeatAfterMonths;
        template.PassingScorePercent = model.PassingScorePercent;
        template.IsQuizRequired = model.IsQuizRequired;
        template.IsActive = model.IsActive;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = await currentUser.GetUserIdAsync();

        await db.SaveChangesAsync(ct);
        await complianceAuditLog.LogTrainingTemplateUpdatedAsync(template.Id, template.Title, template.TenantId, []);
        return TrainingTemplateOperationResult.Ok(template.Id);
    }

    public async Task<TrainingTemplateOperationResult> ArchiveTemplateAsync(int templateId, CancellationToken ct = default)
    {
        var template = await db.TrainingTemplates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == templateId && !t.IsArchived, ct);

        if (template is null || !await templateAccess.CanArchiveAsync(template, ct))
            return TrainingTemplateOperationResult.Fail(TrainingLabels.AccessDenied);

        var userId = await currentUser.GetUserIdAsync();
        template.IsArchived = true;
        template.ArchivedAt = DateTime.UtcNow;
        template.ArchivedByUserId = userId;
        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = userId;

        await DeactivateTemplateChildrenAsync(template.Id, userId, ct);
        await db.SaveChangesAsync(ct);

        await complianceAuditLog.LogTrainingTemplateArchivedAsync(template.Id, template.Title, template.TenantId);
        return TrainingTemplateOperationResult.Ok(template.Id);
    }

    public async Task<IReadOnlyList<TrainingTemplateSection>> GetSectionsAsync(
        int templateId, int? tenantId, CancellationToken ct = default)
    {
        if (await templateAccess.GetTemplateByIdAsync(templateId, tenantId, ct: ct) is null)
            return [];

        var sectionsQuery = await templateAccess.ApplyQueryScopeAsync(db.TrainingTemplateSections.AsQueryable(), ct);
        return await sectionsQuery
            .Where(s => s.TrainingTemplateId == templateId && s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<TrainingTemplateOperationResult> SaveSectionAsync(
        TrainingTemplateSection section, CancellationToken ct = default)
    {
        var template = await templateAccess.GetTemplateForMutationAsync(section.TrainingTemplateId, ct);
        if (template is null)
            return TrainingTemplateOperationResult.Fail(TrainingLabels.AccessDenied);

        var userId = await currentUser.GetUserIdAsync();
        if (section.Id == 0)
        {
            section.TenantId = template.TenantId;
            section.CreatedByUserId = userId;
            section.CreatedAt = DateTime.UtcNow;
            db.TrainingTemplateSections.Add(section);
        }
        else
        {
            var sectionsQuery = await templateAccess.ApplyQueryScopeAsync(db.TrainingTemplateSections.AsQueryable(), ct);
            var existing = await sectionsQuery
                .FirstOrDefaultAsync(s => s.Id == section.Id && s.TrainingTemplateId == section.TrainingTemplateId, ct);
            if (existing is null)
                return TrainingTemplateOperationResult.Fail("Schulungskarte wurde nicht gefunden.");

            existing.SortOrder = section.SortOrder;
            existing.Title = section.Title.Trim();
            existing.ContentMarkdown = section.ContentMarkdown;
            existing.IsActive = section.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = userId;
        }

        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = userId;
        await db.SaveChangesAsync(ct);

        await complianceAuditLog.LogTrainingTemplateSectionChangedAsync(
            template.Id, template.Title, template.TenantId, section.Id, section.Title);
        return TrainingTemplateOperationResult.Ok(template.Id);
    }

    public async Task<TrainingTemplateOperationResult> ReorderSectionsAsync(
        int templateId, IReadOnlyList<int> sectionIdsInOrder, CancellationToken ct = default)
    {
        var template = await templateAccess.GetTemplateForMutationAsync(templateId, ct);
        if (template is null)
            return TrainingTemplateOperationResult.Fail(TrainingLabels.AccessDenied);

        var sectionsQuery = await templateAccess.ApplyQueryScopeAsync(db.TrainingTemplateSections.AsQueryable(), ct);
        var sections = await sectionsQuery
            .Where(s => s.TrainingTemplateId == templateId && s.IsActive)
            .ToListAsync(ct);

        for (var i = 0; i < sectionIdsInOrder.Count; i++)
        {
            var section = sections.FirstOrDefault(s => s.Id == sectionIdsInOrder[i]);
            if (section is not null)
                section.SortOrder = i + 1;
        }

        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = await currentUser.GetUserIdAsync();
        await db.SaveChangesAsync(ct);
        return TrainingTemplateOperationResult.Ok(templateId);
    }

    public async Task<TrainingTemplateValidationResult> ValidateTemplateAsync(
        int templateId, int? tenantId, CancellationToken ct = default)
    {
        var template = await GetTemplateByIdAsync(templateId, tenantId, ct: ct);
        if (template is null)
            return TrainingTemplateValidationResult.Fail(["Schulungsvorlage wurde nicht gefunden."]);

        var errors = new List<string>();
        var sections = await GetSectionsAsync(templateId, tenantId, ct);
        if (sections.Count == 0)
            errors.Add("Mindestens eine aktive Schulungskarte ist erforderlich.");

        foreach (var section in sections)
        {
            var keys = TrainingMarkdownAssetResolver.ExtractAssetKeys(section.ContentMarkdown);
            foreach (var key in keys)
            {
                var assetExists = await (await templateAccess.ApplyQueryScopeAsync(db.TrainingTemplateAssets.AsQueryable(), ct))
                    .AnyAsync(a => a.TrainingTemplateId == templateId && a.AssetKey == key && a.IsActive, ct);
                if (!assetExists)
                    errors.Add($"Fehlendes Asset in Karte „{section.Title}“: {key}");
            }
        }

        if (template.IsQuizRequired)
        {
            var quizValidation = await questionService.ValidateQuizAsync(templateId, tenantId, ct);
            errors.AddRange(quizValidation.Errors);
        }

        return errors.Count == 0
            ? TrainingTemplateValidationResult.Ok()
            : TrainingTemplateValidationResult.Fail(errors);
    }

    public async Task<TrainingTemplateOperationResult> CopyTemplateAsync(
        int sourceTemplateId,
        int targetTenantId,
        string? currentUserId = null,
        CancellationToken ct = default)
    {
        if (!await access.CanAccessTenantAsync(targetTenantId))
            return TrainingTemplateOperationResult.Fail(TrainingLabels.AccessDenied);

        var source = await db.TrainingTemplates
            .IgnoreQueryFilters()
            .Include(t => t.Sections)
            .Include(t => t.Assets)
            .Include(t => t.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(t => t.Id == sourceTemplateId, ct);

        if (source is null || !await templateAccess.CanViewAsync(source, ct))
            return TrainingTemplateOperationResult.Fail(TrainingLabels.AccessDenied);

        if (!TrainingTemplateAccessService.IsGlobalTemplate(source) && source.TenantId != targetTenantId)
            return TrainingTemplateOperationResult.Fail(TrainingLabels.AccessDenied);

        var userId = currentUserId ?? await currentUser.GetUserIdAsync();

        var copy = new TrainingTemplate
        {
            TenantId = targetTenantId,
            Title = $"Kopie von {source.Title}",
            Description = source.Description,
            TrainingType = source.TrainingType,
            TargetAudience = source.TargetAudience,
            EstimatedDurationMinutes = source.EstimatedDurationMinutes,
            RecommendedRepeatAfterMonths = source.RecommendedRepeatAfterMonths,
            PassingScorePercent = source.PassingScorePercent,
            IsQuizRequired = source.IsQuizRequired,
            IsGlobal = false,
            IsCommunityTemplate = false,
            CommunityStatus = CommunityTemplateStatus.None,
            IsActive = source.IsActive,
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };

        db.TrainingTemplates.Add(copy);
        await db.SaveChangesAsync(ct);

        await CopyTemplateChildrenAsync(source, copy, targetTenantId, userId, ct);
        await assetService.CopyAssetsFromTemplateAsync(sourceTemplateId, copy.Id, targetTenantId, userId, ct);

        await complianceAuditLog.LogTrainingTemplateCopiedAsync(copy.Id, copy.Title, targetTenantId, sourceTemplateId);
        return TrainingTemplateOperationResult.Ok(copy.Id);
    }

    public async Task<string> PrepareMarkdownForRenderingAsync(
        string markdown,
        int templateId,
        int? tenantId,
        CancellationToken ct = default)
    {
        if (await templateAccess.GetTemplateByIdAsync(templateId, tenantId, ct: ct) is null)
            return markdown;

        var assets = await LoadActiveAssetsAsync(templateId, ct);
        return ResolveMarkdownWithAssets(markdown, templateId, assets);
    }

    /// <summary>
    /// Bereitet Markdown-Vorschauen für mehrere Karten sequenziell vor (ein DbContext-Zugriff).
    /// </summary>
    public async Task<IReadOnlyDictionary<int, string>> PrepareSectionPreviewHtmlAsync(
        int templateId,
        int? tenantId,
        IReadOnlyList<TrainingTemplateSection> sections,
        CancellationToken ct = default)
    {
        if (await templateAccess.GetTemplateByIdAsync(templateId, tenantId, ct: ct) is null)
            return new Dictionary<int, string>();

        var assets = await LoadActiveAssetsAsync(templateId, ct);
        var result = new Dictionary<int, string>();

        foreach (var section in sections)
        {
            var resolved = ResolveMarkdownWithAssets(section.ContentMarkdown, templateId, assets);
            result[section.Id] = TrainingMarkdownRenderer.ToHtml(resolved);
        }

        return result;
    }

    private async Task<List<TrainingTemplateAsset>> LoadActiveAssetsAsync(int templateId, CancellationToken ct)
    {
        var assetQuery = await templateAccess.ApplyQueryScopeAsync(db.TrainingTemplateAssets.AsQueryable(), ct);
        return await assetQuery
            .Where(a => a.TrainingTemplateId == templateId && a.IsActive)
            .ToListAsync(ct);
    }

    private static string ResolveMarkdownWithAssets(
        string markdown,
        int templateId,
        IReadOnlyList<TrainingTemplateAsset> assets)
    {
        var availableKeys = assets.Select(a => a.AssetKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var altTexts = assets.ToDictionary(a => a.AssetKey, a => a.AltText ?? a.AssetKey, StringComparer.OrdinalIgnoreCase);

        return TrainingMarkdownAssetResolver.ResolveAssetPlaceholdersWithAvailability(
            markdown, templateId, availableKeys, altTexts);
    }

    private async Task CopyTemplateChildrenAsync(
        TrainingTemplate source,
        TrainingTemplate target,
        int? targetTenantId,
        string? userId,
        CancellationToken ct)
    {
        foreach (var section in source.Sections.Where(s => s.IsActive).OrderBy(s => s.SortOrder))
        {
            target.Sections.Add(new TrainingTemplateSection
            {
                TenantId = targetTenantId,
                SortOrder = section.SortOrder,
                Title = section.Title,
                ContentMarkdown = section.ContentMarkdown,
                IsActive = true,
                CreatedByUserId = userId
            });
        }

        foreach (var question in source.Questions.Where(q => q.IsActive).OrderBy(q => q.SortOrder))
        {
            var questionCopy = new TrainingQuestion
            {
                TenantId = targetTenantId,
                SortOrder = question.SortOrder,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                Explanation = question.Explanation,
                Points = question.Points,
                IsRequired = question.IsRequired,
                IsActive = true,
                CreatedByUserId = userId
            };

            foreach (var option in question.Options.Where(o => o.IsActive).OrderBy(o => o.SortOrder))
            {
                questionCopy.Options.Add(new TrainingQuestionOption
                {
                    TenantId = targetTenantId,
                    SortOrder = option.SortOrder,
                    AnswerText = option.AnswerText,
                    IsCorrect = option.IsCorrect,
                    Explanation = option.Explanation,
                    IsActive = true,
                    CreatedByUserId = userId
                });
            }

            target.Questions.Add(questionCopy);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task DeactivateTemplateChildrenAsync(int templateId, string? userId, CancellationToken ct)
    {
        var sectionsQuery = await templateAccess.ApplyQueryScopeAsync(db.TrainingTemplateSections.AsQueryable(), ct);
        var sections = await sectionsQuery
            .Where(s => s.TrainingTemplateId == templateId && s.IsActive)
            .ToListAsync(ct);
        foreach (var section in sections)
        {
            section.IsActive = false;
            section.UpdatedAt = DateTime.UtcNow;
            section.UpdatedByUserId = userId;
        }

        var assetsQuery = await templateAccess.ApplyQueryScopeAsync(db.TrainingTemplateAssets.AsQueryable(), ct);
        var assets = await assetsQuery
            .Where(a => a.TrainingTemplateId == templateId && a.IsActive)
            .ToListAsync(ct);
        foreach (var asset in assets)
        {
            asset.IsActive = false;
            asset.UpdatedAt = DateTime.UtcNow;
            asset.UpdatedByUserId = userId;
        }

        var questionsQuery = await templateAccess.ApplyQueryScopeAsync(db.TrainingQuestions.AsQueryable(), ct);
        var questions = await questionsQuery
            .Where(q => q.TrainingTemplateId == templateId && q.IsActive)
            .ToListAsync(ct);
        foreach (var question in questions)
        {
            question.IsActive = false;
            question.UpdatedAt = DateTime.UtcNow;
            question.UpdatedByUserId = userId;

            var optionsQuery = await templateAccess.ApplyQueryScopeAsync(db.TrainingQuestionOptions.AsQueryable(), ct);
            var options = await optionsQuery
                .Where(o => o.TrainingQuestionId == question.Id && o.IsActive)
                .ToListAsync(ct);
            foreach (var option in options)
            {
                option.IsActive = false;
                option.UpdatedAt = DateTime.UtcNow;
                option.UpdatedByUserId = userId;
            }
        }
    }
}

public readonly record struct TrainingTemplateOperationResult(
    bool Success,
    string? Message = null,
    int? TemplateId = null)
{
    public static TrainingTemplateOperationResult Ok(int templateId, string? message = null) =>
        new(true, message, templateId);

    public static TrainingTemplateOperationResult Fail(string message) =>
        new(false, message);
}

public readonly record struct TrainingTemplateValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static TrainingTemplateValidationResult Ok() => new(true, []);
    public static TrainingTemplateValidationResult Fail(IReadOnlyList<string> errors) => new(false, errors);
}
