using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.Licenses;
using Dsms.Web.Services.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services;

/// <inheritdoc />
public sealed class AuditTemplateService(
    ApplicationDbContext db,
    IUserAccessService access,
    ICurrentUserContext currentUser,
    UserManager<ApplicationUser> userManager,
    ArchiveViewContextAccessor archiveView,
    ILicenseService licenseService,
    ILicenseCreateGuard licenseCreateGuard,
    IComplianceAuditLogService complianceAuditLog) : IAuditTemplateService
{
    public async Task<bool> CanViewAsync(AuditTemplate template, CancellationToken ct = default)
    {
        if (IsGlobalTemplate(template))
        {
            if (await access.CanManageGlobalAuditTemplatesAsync())
            {
                return true;
            }

            var tenantId = await access.GetCurrentTenantIdAsync();
            return tenantId.HasValue && await access.CanAccessTenantBusinessModulesAsync();
        }

        if (!await access.CanAccessTenantBusinessModulesAsync())
        {
            return false;
        }

        var currentTenantId = await access.GetCurrentTenantIdAsync();
        return currentTenantId.HasValue && template.TenantId == currentTenantId;
    }

    public async Task<bool> CanEditAsync(AuditTemplate template, CancellationToken ct = default)
    {
        if (!await CanViewAsync(template, ct))
        {
            return false;
        }

        if (IsGlobalTemplate(template))
        {
            return await access.CanManageGlobalAuditTemplatesAsync();
        }

        if (template.CommunityStatus == CommunityStatus.Submitted)
        {
            return await access.CanManageGlobalAuditTemplatesAsync();
        }

        return await access.CanEditComplianceContentAsync();
    }

    public async Task<bool> CanArchiveAsync(AuditTemplate template, CancellationToken ct = default)
    {
        if (template.TemplateType == AuditTemplateType.Tenant
            && template.CommunityStatus == CommunityStatus.Submitted)
        {
            return false;
        }

        return await CanEditAsync(template, ct);
    }

    public async Task<bool> CanCreateTenantTemplateAsync(CancellationToken ct = default)
    {
        if (!await access.CanAccessTenantBusinessModulesAsync())
        {
            return false;
        }

        return await access.CanEditComplianceContentAsync();
    }

    public Task<bool> CanCreateOfficialTemplateAsync(CancellationToken ct = default) =>
        access.CanManageGlobalAuditTemplatesAsync();

    public Task<bool> CanReviewCommunityAsync(CancellationToken ct = default) =>
        access.CanManageGlobalAuditTemplatesAsync();

    public async Task<bool> CanSubmitToCommunityAsync(AuditTemplate template, CancellationToken ct = default)
    {
        if (template.TemplateType != AuditTemplateType.Tenant)
        {
            return false;
        }

        if (template.CommunityStatus is not (CommunityStatus.None or CommunityStatus.Rejected))
        {
            return false;
        }

        if (!template.IsActive || template.IsArchived)
        {
            return false;
        }

        var tenantId = await access.GetCurrentTenantIdAsync();
        if (!tenantId.HasValue || template.TenantId != tenantId)
        {
            return false;
        }

        return await access.CanEditComplianceContentAsync();
    }

    public async Task<bool> CanCopyToTenantAsync(AuditTemplate template, CancellationToken ct = default)
    {
        if (!IsGlobalTemplate(template))
        {
            return false;
        }

        return await CanViewAsync(template, ct) && await CanCreateTenantTemplateAsync(ct);
    }

    public async Task<AuditTemplate?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var template = await LoadTemplateByIdAsync(id, ct);
        if (template is null || !await CanViewAsync(template, ct))
        {
            return null;
        }

        await db.Entry(template).Collection(t => t.Questions).LoadAsync(ct);
        return template;
    }

    public async Task<AuditTemplate?> GetSubmittedForReviewAsync(int id, CancellationToken ct = default)
    {
        if (!await CanReviewCommunityAsync(ct))
        {
            return null;
        }

        return await db.AuditTemplates
            .IgnoreQueryFilters()
            .Include(t => t.Questions)
            .Include(t => t.Tenant)
            .FirstOrDefaultAsync(t => t.Id == id
                && t.TemplateType == AuditTemplateType.Tenant
                && t.CommunityStatus == CommunityStatus.Submitted
                && !t.IsArchived, ct);
    }

    public async Task<IReadOnlyList<AuditTemplate>> ListVisibleAsync(CancellationToken ct = default)
    {
        var tenantId = await access.GetCurrentTenantIdAsync();
        if (tenantId is null)
        {
            return [];
        }

        return await VisibleTemplatesQuery(tenantId.Value)
            .Include(t => t.Questions)
            .OrderByDescending(t => archiveView.ShowArchivedOnly ? t.ArchivedAt : t.CreatedAt)
            .ThenBy(t => t.Title)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AuditTemplate>> ListGlobalTemplatesAsync(CancellationToken ct = default)
    {
        if (!await access.CanManageGlobalAuditTemplatesAsync())
        {
            return [];
        }

        return await db.AuditTemplates
            .IgnoreQueryFilters()
            .Where(t => t.TemplateType == AuditTemplateType.Official
                || t.TemplateType == AuditTemplateType.Community)
            .Where(t => t.IsArchived == archiveView.ShowArchivedOnly)
            .Include(t => t.Questions)
            .OrderByDescending(t => archiveView.ShowArchivedOnly ? t.ArchivedAt : t.CreatedAt)
            .ThenBy(t => t.Title)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AuditTemplate>> ListActiveForAuditStartAsync(CancellationToken ct = default)
    {
        var tenantId = await access.GetCurrentTenantIdAsync();
        if (tenantId is null)
        {
            return [];
        }

        return await VisibleTemplatesQuery(tenantId.Value)
            .Where(t => t.IsActive && !t.IsArchived)
            .OrderBy(t => t.TemplateType)
            .ThenBy(t => t.Title)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CommunitySubmissionRow>> ListSubmittedForReviewAsync(CancellationToken ct = default)
    {
        if (!await CanReviewCommunityAsync(ct))
        {
            return [];
        }

        var submissions = await db.AuditTemplates
            .IgnoreQueryFilters()
            .Include(t => t.Tenant)
            .Include(t => t.Questions)
            .Where(t => t.TemplateType == AuditTemplateType.Tenant
                && t.CommunityStatus == CommunityStatus.Submitted
                && !t.IsArchived)
            .OrderByDescending(t => t.SubmittedAt)
            .ThenBy(t => t.Title)
            .ToListAsync(ct);

        var rows = new List<CommunitySubmissionRow>();
        foreach (var template in submissions)
        {
            string? submittedByName = null;
            if (!string.IsNullOrEmpty(template.SubmittedByUserId))
            {
                var user = await userManager.FindByIdAsync(template.SubmittedByUserId);
                submittedByName = user?.DisplayName;
            }

            rows.Add(new CommunitySubmissionRow(
                template,
                template.Tenant?.Name ?? "—",
                submittedByName));
        }

        return rows;
    }

    public async Task<AuditTemplateOperationResult> SubmitToCommunityAsync(int id, CancellationToken ct = default)
    {
        var template = await db.AuditTemplates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (template is null || !await CanSubmitToCommunityAsync(template, ct))
        {
            return new AuditTemplateOperationResult(false, AuditTemplateLabels.AccessDenied);
        }

        var userId = await currentUser.GetUserIdAsync();
        template.CommunityStatus = CommunityStatus.Submitted;
        template.SubmittedAt = DateTime.UtcNow;
        template.SubmittedByUserId = userId;
        template.ReviewedAt = null;
        template.ReviewedByUserId = null;
        template.ReviewComment = null;
        template.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        if (template.TenantId is int tenantId)
        {
            await complianceAuditLog.LogAuditTemplatePublishedToCommunityAsync(template.Id, template.Title, tenantId);
        }

        return new AuditTemplateOperationResult(true, AuditTemplateLabels.SubmitSuccess);
    }

    public async Task<AuditTemplateOperationResult> ApproveCommunityAsync(int id, string? reviewComment, CancellationToken ct = default)
    {
        if (!await CanReviewCommunityAsync(ct))
        {
            return new AuditTemplateOperationResult(false, AuditTemplateLabels.AccessDenied);
        }

        var source = await db.AuditTemplates
            .IgnoreQueryFilters()
            .Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == id
                && t.TemplateType == AuditTemplateType.Tenant
                && t.CommunityStatus == CommunityStatus.Submitted, ct);

        if (source is null)
        {
            return new AuditTemplateOperationResult(false, "Einreichung wurde nicht gefunden.");
        }

        var userId = await currentUser.GetUserIdAsync();
        var now = DateTime.UtcNow;

        var communityCopy = new AuditTemplate
        {
            TemplateType = AuditTemplateType.Community,
            TenantId = null,
            CommunityStatus = CommunityStatus.Approved,
            Title = source.Title,
            Description = source.Description,
            Version = source.Version,
            IsActive = true,
            OriginalTenantId = source.TenantId,
            OriginalTemplateId = source.Id,
            ReviewedAt = now,
            ReviewedByUserId = userId,
            ReviewComment = reviewComment,
            SubmittedAt = source.SubmittedAt,
            SubmittedByUserId = source.SubmittedByUserId
        };

        foreach (var question in source.Questions.OrderBy(q => q.SortOrder))
        {
            communityCopy.Questions.Add(new AuditQuestion
            {
                SortOrder = question.SortOrder,
                Text = question.Text,
                Category = question.Category,
                IsRequired = question.IsRequired
            });
        }

        db.AuditTemplates.Add(communityCopy);

        source.CommunityStatus = CommunityStatus.Approved;
        source.ReviewedAt = now;
        source.ReviewedByUserId = userId;
        source.ReviewComment = reviewComment;
        source.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        return new AuditTemplateOperationResult(true, AuditTemplateLabels.ApproveSuccess);
    }

    public async Task<AuditTemplateOperationResult> RejectCommunityAsync(int id, string? reviewComment, CancellationToken ct = default)
    {
        if (!await CanReviewCommunityAsync(ct))
        {
            return new AuditTemplateOperationResult(false, AuditTemplateLabels.AccessDenied);
        }

        var source = await db.AuditTemplates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id
                && t.TemplateType == AuditTemplateType.Tenant
                && t.CommunityStatus == CommunityStatus.Submitted, ct);

        if (source is null)
        {
            return new AuditTemplateOperationResult(false, "Einreichung wurde nicht gefunden.");
        }

        var userId = await currentUser.GetUserIdAsync();
        var now = DateTime.UtcNow;

        source.CommunityStatus = CommunityStatus.Rejected;
        source.ReviewedAt = now;
        source.ReviewedByUserId = userId;
        source.ReviewComment = reviewComment;
        source.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        return new AuditTemplateOperationResult(true, AuditTemplateLabels.RejectSuccess);
    }

    public async Task<AuditTemplateOperationResult> CopyToTenantAsync(int sourceId, CancellationToken ct = default)
    {
        var source = await GetByIdAsync(sourceId, ct);
        if (source is null || !await CanCopyToTenantAsync(source, ct))
        {
            return new AuditTemplateOperationResult(false, AuditTemplateLabels.AccessDenied);
        }

        var tenantId = await access.GetCurrentTenantIdAsync();
        if (tenantId is null)
        {
            return new AuditTemplateOperationResult(false, AuditTemplateLabels.AccessDenied);
        }

        var limitCheck = await licenseService.CanCreateCustomAuditTemplateAsync(tenantId.Value);
        if (!await licenseCreateGuard.IsAllowedAsync(limitCheck, "AuditTemplate"))
        {
            return new AuditTemplateOperationResult(false, limitCheck.Message);
        }

        var copy = new AuditTemplate
        {
            TemplateType = AuditTemplateType.Tenant,
            TenantId = tenantId,
            CommunityStatus = CommunityStatus.None,
            Title = $"Kopie von {source.Title}",
            Description = source.Description,
            Version = source.Version,
            IsActive = source.IsActive
        };

        foreach (var question in source.Questions.OrderBy(q => q.SortOrder))
        {
            copy.Questions.Add(new AuditQuestion
            {
                SortOrder = question.SortOrder,
                Text = question.Text,
                Category = question.Category,
                IsRequired = question.IsRequired
            });
        }

        db.AuditTemplates.Add(copy);
        await db.SaveChangesAsync(ct);

        await complianceAuditLog.LogAuditTemplateImportedAsync(
            copy.Id, copy.Title, tenantId.Value, sourceId);

        return new AuditTemplateOperationResult(true, null);
    }

    public async Task<ArchiveOperationResult> ArchiveAsync(int id, CancellationToken ct = default)
    {
        var template = await db.AuditTemplates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsArchived, ct);

        if (template is null || !await CanArchiveAsync(template, ct))
        {
            return new ArchiveOperationResult(false, [], "Eintrag wurde nicht gefunden oder Zugriff verweigert.");
        }

        var warnings = await GetDependencyWarningsAsync(id, ct);
        var userId = await currentUser.GetUserIdAsync();

        template.IsArchived = true;
        template.ArchivedAt = DateTime.UtcNow;
        template.ArchivedByUserId = userId;
        template.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await complianceAuditLog.LogAuditTemplateArchivedAsync(template.Id, template.Title, template.TenantId);
        return new ArchiveOperationResult(true, warnings);
    }

    public async Task<ArchiveOperationResult> RestoreAsync(int id, CancellationToken ct = default)
    {
        var template = await db.AuditTemplates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id && t.IsArchived, ct);

        if (template is null || !await CanArchiveAsync(template, ct))
        {
            return new ArchiveOperationResult(false, [], "Eintrag wurde nicht gefunden oder Zugriff verweigert.");
        }

        template.IsArchived = false;
        template.ArchivedAt = null;
        template.ArchivedByUserId = null;
        template.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await complianceAuditLog.LogAuditTemplateRestoredAsync(template.Id, template.Title, template.TenantId);
        return new ArchiveOperationResult(true, []);
    }

    public async Task<IReadOnlyList<string>> GetDependencyWarningsAsync(int id, CancellationToken ct = default)
    {
        var runCount = await db.AuditRuns.CountAsync(r => r.AuditTemplateId == id, ct);
        return runCount > 0 ? [$"{runCount} Audit-Durchlauf/Durchläufe basieren auf dieser Vorlage"] : [];
    }

    public async Task CreateAnswerSnapshotsAsync(AuditRun run, AuditTemplate template, CancellationToken ct = default)
    {
        run.TemplateTitleSnapshot = template.Title;
        run.TemplateVersionSnapshot = template.Version;

        var questions = await db.AuditQuestions
            .Where(q => q.AuditTemplateId == template.Id)
            .OrderBy(q => q.SortOrder)
            .ToListAsync(ct);

        foreach (var question in questions)
        {
            db.AuditAnswers.Add(new AuditAnswer
            {
                AuditRunId = run.Id,
                AuditQuestionId = question.Id,
                QuestionText = question.Text,
                QuestionSortOrder = question.SortOrder,
                QuestionCategory = question.Category,
                QuestionIsRequired = question.IsRequired
            });
        }
    }

    public async Task<AuditTemplateOperationResult> AddQuestionAsync(
        int templateId, int sortOrder, string? category, string text, CancellationToken ct = default)
    {
        var template = await GetTemplateForQuestionMutationAsync(templateId, questionId: null, ct);
        if (template is null)
        {
            return new AuditTemplateOperationResult(false, AuditTemplateLabels.QuestionEditDenied);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new AuditTemplateOperationResult(false, AuditTemplateLabels.QuestionTextRequired);
        }

        var question = new AuditQuestion
        {
            AuditTemplateId = templateId,
            SortOrder = sortOrder,
            Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            Text = text.Trim(),
            IsRequired = true
        };
        db.AuditQuestions.Add(question);
        template.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        if (IsGlobalTemplate(template))
        {
            await complianceAuditLog.LogGlobalAuditTemplateQuestionCreatedAsync(
                template.Id, template.Title, question.Id);
        }

        return new AuditTemplateOperationResult(true);
    }

    public async Task<AuditTemplateOperationResult> UpdateQuestionAsync(
        int templateId, int questionId, int sortOrder, string? category, string text, CancellationToken ct = default)
    {
        var template = await GetTemplateForQuestionMutationAsync(templateId, questionId, ct);
        if (template is null)
        {
            return new AuditTemplateOperationResult(false, AuditTemplateLabels.QuestionEditDenied);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new AuditTemplateOperationResult(false, AuditTemplateLabels.QuestionTextRequired);
        }

        var question = await db.AuditQuestions
            .FirstOrDefaultAsync(q => q.Id == questionId && q.AuditTemplateId == templateId, ct);
        if (question is null)
        {
            return new AuditTemplateOperationResult(false, AuditTemplateLabels.QuestionEditDenied);
        }

        question.SortOrder = sortOrder;
        question.Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        question.Text = text.Trim();
        question.UpdatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        if (IsGlobalTemplate(template))
        {
            await complianceAuditLog.LogGlobalAuditTemplateQuestionUpdatedAsync(
                template.Id, template.Title, question.Id);
        }

        return new AuditTemplateOperationResult(true, AuditTemplateLabels.QuestionUpdated);
    }

    public async Task<AuditTemplateOperationResult> DeleteQuestionAsync(
        int templateId, int questionId, CancellationToken ct = default)
    {
        var template = await GetTemplateForQuestionMutationAsync(templateId, questionId, ct);
        if (template is null)
        {
            return new AuditTemplateOperationResult(false, AuditTemplateLabels.QuestionDeleteDenied);
        }

        var question = await db.AuditQuestions
            .FirstOrDefaultAsync(q => q.Id == questionId && q.AuditTemplateId == templateId, ct);
        if (question is null)
        {
            return new AuditTemplateOperationResult(false, AuditTemplateLabels.QuestionDeleteDenied);
        }

        var inUse = await db.AuditAnswers.AnyAsync(a => a.AuditQuestionId == questionId, ct);
        if (inUse)
        {
            return new AuditTemplateOperationResult(false, AuditTemplateLabels.QuestionDeleteInUse);
        }

        db.AuditQuestions.Remove(question);
        template.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        if (IsGlobalTemplate(template))
        {
            await complianceAuditLog.LogGlobalAuditTemplateQuestionDeletedAsync(
                template.Id, template.Title, questionId);
        }

        return new AuditTemplateOperationResult(true, AuditTemplateLabels.QuestionDeleted);
    }

    private async Task<AuditTemplate?> LoadTemplateByIdAsync(int templateId, CancellationToken ct)
    {
        if (await access.CanManageGlobalAuditTemplatesAsync() && !await access.HasTenantContextAsync())
        {
            return await db.AuditTemplates
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == templateId, ct);
        }

        return await db.AuditTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, ct);
    }

    private async Task<AuditTemplate?> GetTemplateForQuestionMutationAsync(
        int templateId, int? questionId, CancellationToken ct)
    {
        var template = await LoadTemplateByIdAsync(templateId, ct);
        if (template is null)
        {
            return null;
        }

        if (await CanEditAsync(template, ct))
        {
            return template;
        }

        if (await access.IsSuperuserAsync() && !IsGlobalTemplate(template))
        {
            await complianceAuditLog.LogTenantAuditTemplateQuestionAccessDeniedAsync(templateId, questionId);
        }

        return null;
    }

    private static bool IsGlobalTemplate(AuditTemplate template) =>
        template.TemplateType is AuditTemplateType.Official or AuditTemplateType.Community;

    private IQueryable<AuditTemplate> VisibleTemplatesQuery(int tenantId) =>
        db.AuditTemplates.Where(t =>
            t.TemplateType == AuditTemplateType.Official
            || t.TemplateType == AuditTemplateType.Community
            || (t.TemplateType == AuditTemplateType.Tenant && t.TenantId == tenantId));
}
