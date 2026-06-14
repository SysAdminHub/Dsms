using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrainingEntity = Dsms.Web.Domain.Entities.Training;

namespace Dsms.Web.Services.Training;

/// <summary>Verwaltung konkreter Schulungsdurchführungen pro Mandant.</summary>
public class TrainingService(
    ApplicationDbContext db,
    IUserAccessService access,
    ICurrentUserContext currentUser,
    ArchiveViewContextAccessor archiveView,
    TrainingTemplateAccessService templateAccess,
    TrainingAssignmentService assignmentService,
    IOptions<TrainingAccessOptions> accessOptions,
    IComplianceAuditLogService complianceAuditLog)
{
    private readonly TrainingAccessOptions _accessOptions = accessOptions.Value;
    public async Task<bool> CanViewAsync(int trainingId, CancellationToken ct = default)
    {
        var training = await db.Trainings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == trainingId, ct);

        return training is not null && await CanViewAsync(training, ct);
    }

    public async Task<bool> CanViewAsync(TrainingEntity training, CancellationToken ct = default)
    {
        if (!await access.CanAccessTenantBusinessModulesAsync())
            return false;

        var tenantId = await access.GetCurrentTenantIdAsync();
        return tenantId.HasValue && training.TenantId == tenantId;
    }

    public async Task<bool> CanEditAsync(CancellationToken ct = default) =>
        await access.CanEditComplianceContentAsync();

    public async Task<TrainingEntity?> GetByIdAsync(int id, int tenantId, CancellationToken ct = default)
    {
        var training = await db.Trainings
            .Include(t => t.TrainingTemplate)
            .Include(t => t.ResponsibleUser)
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, ct);

        if (training is null || !await CanViewAsync(training, ct))
            return null;

        training.Status = TrainingStatusMapper.Normalize(training.Status);
        return training;
    }

    public async Task<IReadOnlyList<TrainingListRow>> GetListRowsAsync(int tenantId, CancellationToken ct = default)
    {
        if (!await access.CanAccessTenantAsync(tenantId))
            return [];

        var trainings = await db.Trainings
            .Include(t => t.TrainingTemplate)
            .Include(t => t.ResponsibleUser)
            .Where(t => t.TenantId == tenantId && t.IsArchived == archiveView.ShowArchivedOnly)
            .OrderByDescending(t => archiveView.ShowArchivedOnly ? t.ArchivedAt : t.ScheduledAt ?? t.CreatedAt)
            .ThenBy(t => t.Title)
            .ToListAsync(ct);

        if (trainings.Count == 0)
            return [];

        var trainingIds = trainings.Select(t => t.Id).ToList();
        var docCounts = await db.DocumentLinks
            .Where(l => l.TenantId == tenantId
                && l.LinkedEntityType == DocumentLinkedEntityType.Training
                && trainingIds.Contains(l.LinkedEntityId))
            .GroupBy(l => l.LinkedEntityId)
            .Select(g => new { TrainingId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TrainingId, x => x.Count, ct);

        var participantStats = await assignmentService.GetStatsForTrainingsAsync(tenantId, trainingIds, ct);

        var canEdit = await CanEditAsync(ct);
        return trainings.Select(t => new TrainingListRow(
            t,
            t.TrainingTemplate?.Title,
            GetResponsibleDisplay(t),
            docCounts.GetValueOrDefault(t.Id),
            participantStats.GetValueOrDefault(t.Id, new TrainingParticipantStats(0, 0, 0)),
            canEdit)).ToList();
    }

    public async Task<IReadOnlyList<TrainingTemplateSelectionItem>> GetAvailableTemplatesForTrainingCreationAsync(
        int tenantId, CancellationToken ct = default)
    {
        if (!await CanEditAsync(ct) || !await access.CanAccessTenantAsync(tenantId))
            return [];

        var templates = await templateAccess.VisibleTemplatesQuery(tenantId, includeGlobal: true)
            .Where(t => t.IsActive && !t.IsArchived)
            .OrderBy(t => t.Title)
            .ToListAsync(ct);

        return templates.Select(t => new TrainingTemplateSelectionItem(
            t.Id,
            t.Title,
            t.TrainingType,
            t.TargetAudience,
            t.Description,
            t.RecommendedRepeatAfterMonths,
            TrainingTemplateAccessService.IsGlobalTemplate(t),
            TrainingLabels.GetOriginLabel(t),
            $"{t.Title} — {TrainingLabels.GetTrainingTypeLabel(t.TrainingType)} — {TrainingLabels.GetOriginLabel(t)}"
        )).ToList();
    }

    public async Task<TrainingTemplateLoadResult> GetTemplateForTrainingCreationAsync(
        int templateId, int tenantId, CancellationToken ct = default)
    {
        var template = await db.TrainingTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId, ct);

        if (template is null)
            return TrainingTemplateLoadResult.NotFound();

        if (!await templateAccess.CanViewAsync(template, ct))
            return TrainingTemplateLoadResult.NotAvailable();

        if (!TrainingTemplateAccessService.IsGlobalTemplate(template) && template.TenantId != tenantId)
            return TrainingTemplateLoadResult.NotAvailable();

        if (!template.IsActive || template.IsArchived)
            return TrainingTemplateLoadResult.NotAvailable();

        return TrainingTemplateLoadResult.Ok(template);
    }

    public int GetDefaultAccessCodeValidityDays() => _accessOptions.DefaultValidityDays;

    public int GetMaxAccessCodeValidityDays() => _accessOptions.MaxValidityDays;

    public async Task<TrainingOperationResult> CreateFreeAsync(TrainingEntity model, CancellationToken ct = default)
    {
        var tenantId = await access.GetCurrentTenantIdAsync();
        if (tenantId is null || !await CanEditAsync(ct))
            return TrainingOperationResult.Fail(TrainingLabels.TrainingAccessDenied);

        if (await ValidateTrainingTemplateIdAsync(model.TrainingTemplateId, tenantId.Value, ct) is { } validationError)
            return validationError;

        if (await ResolveTrainingTypeAsync(model, tenantId.Value, ct) is { } typeError)
            return typeError;

        if (ValidateTrainingStatus(model.Status) is { } statusError)
            return statusError;

        if (ValidateAccessCodeValidityDays(model.AccessCodeValidityDays) is { } validityError)
            return validityError;

        var userId = await currentUser.GetUserIdAsync();
        var now = DateTime.UtcNow;

        model.TenantId = tenantId.Value;
        model.ParticipantCount = 0;
        model.ProofMissing = false;
        model.AccessCodeValidityDays = NormalizeAccessCodeValidityDays(model.AccessCodeValidityDays);
        model.CreatedAt = now;
        model.CreatedByUserId = userId;
        model.UpdatedAt = now;
        model.UpdatedByUserId = userId;
        NormalizeStatusFields(model);

        db.Trainings.Add(model);
        await db.SaveChangesAsync(ct);

        if (model.TrainingTemplateId is int templateId)
        {
            var template = await templateAccess.GetTemplateByIdAsync(templateId, tenantId, ct: ct);
            await complianceAuditLog.LogTrainingCreatedFromTemplateAsync(
                model.Id, model.Title, model.TenantId, templateId, template?.Title ?? model.Title);
        }
        else
        {
            await complianceAuditLog.LogTrainingCreatedAsync(model.Id, model.Title, model.TenantId);
        }

        return TrainingOperationResult.Ok(model.Id);
    }

    public async Task<TrainingOperationResult> CreateFromTemplateAsync(
        int templateId, CancellationToken ct = default)
    {
        var tenantId = await access.GetCurrentTenantIdAsync();
        if (tenantId is null || !await CanEditAsync(ct))
            return TrainingOperationResult.Fail(TrainingLabels.TrainingAccessDenied);

        var template = await templateAccess.GetTemplateByIdAsync(templateId, tenantId, ct: ct);
        if (template is null)
            return TrainingOperationResult.Fail(TrainingLabels.TemplateNotFound);

        var userId = await currentUser.GetUserIdAsync();
        var now = DateTime.UtcNow;

        var training = new TrainingEntity
        {
            TenantId = tenantId.Value,
            TrainingTemplateId = template.Id,
            Title = template.Title,
            Description = template.Description,
            TrainingType = template.TrainingType,
            TargetAudience = template.TargetAudience,
            Status = TrainingStatus.Inactive,
            ParticipantCount = 0,
            ProofMissing = false,
            AccessCodeValidityDays = _accessOptions.DefaultValidityDays,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        db.Trainings.Add(training);
        await db.SaveChangesAsync(ct);

        await complianceAuditLog.LogTrainingCreatedFromTemplateAsync(
            training.Id, training.Title, training.TenantId, template.Id, template.Title);
        return TrainingOperationResult.Ok(training.Id);
    }

    public async Task<TrainingOperationResult> UpdateAsync(TrainingEntity model, CancellationToken ct = default)
    {
        if (!await CanEditAsync(ct))
            return TrainingOperationResult.Fail(TrainingLabels.TrainingAccessDenied);

        var tracked = await db.Trainings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == model.Id && t.TenantId == model.TenantId, ct);

        if (tracked is null || !await CanViewAsync(tracked, ct))
            return TrainingOperationResult.Fail(TrainingLabels.TrainingNotFound);

        if (tracked.IsArchived)
            return TrainingOperationResult.Fail("Archivierte Schulungen können nicht bearbeitet werden.");

        if (await ValidateTrainingTemplateIdAsync(model.TrainingTemplateId, tracked.TenantId, ct) is { } validationError)
            return validationError;

        if (await ResolveTrainingTypeAsync(model, tracked.TenantId, ct) is { } typeError)
            return typeError;

        if (ValidateTrainingStatus(model.Status) is { } statusError)
            return statusError;

        if (ValidateAccessCodeValidityDays(model.AccessCodeValidityDays) is { } validityError)
            return validityError;

        var oldStatus = tracked.Status;
        var userId = await currentUser.GetUserIdAsync();
        var now = DateTime.UtcNow;

        if (model.TrainingTemplateId != tracked.TrainingTemplateId)
        {
            tracked.TrainingTemplateId = model.TrainingTemplateId;
        }

        tracked.Title = model.Title.Trim();
        tracked.Description = model.Description;
        tracked.TrainingType = model.TrainingType;
        tracked.TargetAudience = model.TargetAudience;
        tracked.ScheduledAt = model.ScheduledAt;
        tracked.CompletedAt = model.CompletedAt;
        tracked.RepeatDueAt = model.RepeatDueAt;
        tracked.Status = model.Status;
        tracked.ResponsibleUserId = string.IsNullOrWhiteSpace(model.ResponsibleUserId)
            ? null
            : model.ResponsibleUserId;
        tracked.ResponsibleName = model.ResponsibleName;
        tracked.ProofMissing = false;
        tracked.Notes = model.Notes;
        tracked.AccessCodeValidityDays = NormalizeAccessCodeValidityDays(model.AccessCodeValidityDays);
        tracked.UpdatedAt = now;
        tracked.UpdatedByUserId = userId;

        NormalizeStatusFields(tracked);

        await db.SaveChangesAsync(ct);

        await complianceAuditLog.LogTrainingUpdatedAsync(tracked.Id, tracked.Title, tracked.TenantId, []);
        if (oldStatus != tracked.Status)
        {
            await complianceAuditLog.LogTrainingStatusChangedAsync(
                tracked.Id, tracked.Title, tracked.TenantId, oldStatus, tracked.Status);
        }

        return TrainingOperationResult.Ok(tracked.Id);
    }

    public async Task<TrainingOperationResult> RecalculateRepeatDueFromTemplateAsync(
        int trainingId, int tenantId, CancellationToken ct = default)
    {
        var training = await GetByIdAsync(trainingId, tenantId, ct);
        if (training is null || !await CanEditAsync(ct))
            return TrainingOperationResult.Fail(TrainingLabels.TrainingAccessDenied);

        if (training.TrainingTemplateId is not int templateId)
            return TrainingOperationResult.Fail("Keine Vorlage verknüpft.");

        var template = await templateAccess.GetTemplateByIdAsync(templateId, tenantId, ct: ct);
        if (template is null)
            return TrainingOperationResult.Fail(TrainingLabels.TemplateNotFound);

        training.RepeatDueAt = TrainingRepeatDueHelper.CalculateRepeatDueAt(
            training.CompletedAt,
            training.ScheduledAt,
            template.RecommendedRepeatAfterMonths);
        training.UpdatedAt = DateTime.UtcNow;
        training.UpdatedByUserId = await currentUser.GetUserIdAsync();

        await db.SaveChangesAsync(ct);
        return TrainingOperationResult.Ok(training.Id);
    }

    public async Task<IReadOnlyList<ApplicationUser>> GetTenantUsersAsync(int tenantId, CancellationToken ct = default)
    {
        if (!await access.CanAccessTenantAsync(tenantId))
            return [];

        return await db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && (u.TenantId == tenantId
                || db.UserTenants.Any(ut => ut.UserId == u.Id && ut.TenantId == tenantId)))
            .OrderBy(u => u.DisplayName ?? u.Email)
            .ToListAsync(ct);
    }

    public static string GetResponsibleDisplay(TrainingEntity training)
    {
        if (!string.IsNullOrWhiteSpace(training.ResponsibleUser?.DisplayName))
            return training.ResponsibleUser.DisplayName!;

        if (!string.IsNullOrWhiteSpace(training.ResponsibleUser?.Email))
            return training.ResponsibleUser.Email!;

        if (!string.IsNullOrWhiteSpace(training.ResponsibleName))
            return training.ResponsibleName;

        return "—";
    }

    private async Task<TrainingOperationResult?> ResolveTrainingTypeAsync(
        TrainingEntity model, int tenantId, CancellationToken ct)
    {
        if (model.TrainingTemplateId is int templateId)
        {
            var result = await GetTemplateForTrainingCreationAsync(templateId, tenantId, ct);
            if (result.Status != TrainingTemplateLoadStatus.Ok || result.Template is null)
            {
                return result.Status switch
                {
                    TrainingTemplateLoadStatus.NotFound =>
                        TrainingOperationResult.Fail("Die ausgewählte Vorlage konnte nicht gefunden werden."),
                    _ => TrainingOperationResult.Fail("Die ausgewählte Vorlage ist nicht verfügbar.")
                };
            }

            model.TrainingType = result.Template.TrainingType;
            return null;
        }

        if (!TrainingLabels.AllTrainingTypes.Contains(model.TrainingType))
            return TrainingOperationResult.Fail("Bitte wählen Sie einen Schulungstyp aus.");

        return null;
    }

    private async Task<TrainingOperationResult?> ValidateTrainingTemplateIdAsync(
        int? trainingTemplateId, int tenantId, CancellationToken ct)
    {
        if (!trainingTemplateId.HasValue)
            return null;

        var result = await GetTemplateForTrainingCreationAsync(trainingTemplateId.Value, tenantId, ct);
        return result.Status switch
        {
            TrainingTemplateLoadStatus.NotFound =>
                TrainingOperationResult.Fail("Die ausgewählte Vorlage konnte nicht gefunden werden."),
            TrainingTemplateLoadStatus.NotAvailable =>
                TrainingOperationResult.Fail("Die ausgewählte Vorlage ist nicht verfügbar."),
            _ => null
        };
    }

    private static TrainingOperationResult? ValidateTrainingStatus(TrainingStatus status) =>
        TrainingStatusMapper.IsValid(TrainingStatusMapper.Normalize(status))
            ? null
            : TrainingOperationResult.Fail("Bitte wählen Sie einen gültigen Status aus.");

    private TrainingOperationResult? ValidateAccessCodeValidityDays(int days)
    {
        var max = _accessOptions.MaxValidityDays;
        if (days < 1 || days > max)
        {
            return TrainingOperationResult.Fail(
                $"Die Gültigkeit des Zugangscodes muss zwischen 1 und {max} Tagen liegen.");
        }

        return null;
    }

    private int NormalizeAccessCodeValidityDays(int days) =>
        days >= 1 && days <= _accessOptions.MaxValidityDays
            ? days
            : _accessOptions.DefaultValidityDays;

    public static void NormalizeStatusFields(TrainingEntity training)
    {
        training.Status = TrainingStatusMapper.Normalize(training.Status);
        training.ProofMissing = false;
    }

    public static bool MatchesQuickFilter(TrainingEntity training, TrainingQuickFilter filter, DateTime todayUtc)
    {
        var status = TrainingStatusMapper.Normalize(training.Status);
        return filter switch
        {
            TrainingQuickFilter.All => true,
            TrainingQuickFilter.Active => status == TrainingStatus.Active && !training.IsArchived,
            TrainingQuickFilter.Inactive => status == TrainingStatus.Inactive && !training.IsArchived,
            TrainingQuickFilter.Archived => training.IsArchived || status == TrainingStatus.Archived,
            _ => true
        };
    }
}
