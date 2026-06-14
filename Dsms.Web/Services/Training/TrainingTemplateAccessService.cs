using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Training;

/// <summary>Gemeinsame Mandanten- und Berechtigungslogik für Schulungsvorlagen.</summary>
public class TrainingTemplateAccessService(
    ApplicationDbContext db,
    IUserAccessService access)
{
    public static bool IsGlobalTemplate(TrainingTemplate template) =>
        template.IsGlobal && template.TenantId is null;

    public static bool IsTenantOwnedTemplate(TrainingTemplate template) =>
        !template.IsGlobal && template.TenantId is not null;

    /// <summary>
    /// Plattform-Administration oder Community-Prüfung ohne Mandantenkontext:
    /// Query-Filter für Vorlagen und Kind-Entitäten umgehen.
    /// </summary>
    public async Task<bool> RequiresUnfilteredQueriesAsync(CancellationToken ct = default)
    {
        if (await access.HasTenantContextAsync())
            return false;

        return await access.CanManageGlobalTrainingTemplatesAsync()
            || await access.CanReviewCommunityTrainingTemplatesAsync();
    }

    public async Task<bool> CanViewAsync(TrainingTemplate template, CancellationToken ct = default)
    {
        if (IsGlobalTemplate(template))
        {
            return await access.CanManageGlobalTrainingTemplatesAsync()
                || await access.CanAccessTenantBusinessModulesAsync();
        }

        if (await access.CanReviewCommunityTrainingTemplatesAsync()
            && template.CommunityStatus == CommunityTemplateStatus.Submitted
            && IsTenantOwnedTemplate(template))
        {
            return true;
        }

        if (!await access.CanAccessTenantBusinessModulesAsync())
            return false;

        var tenantId = await access.GetCurrentTenantIdAsync();
        return tenantId.HasValue && template.TenantId == tenantId;
    }

    public async Task<bool> CanEditAsync(TrainingTemplate template, CancellationToken ct = default)
    {
        if (!await CanViewAsync(template, ct))
            return false;

        if (IsGlobalTemplate(template))
            return await access.CanManageGlobalTrainingTemplatesAsync();

        if (template.CommunityStatus == CommunityTemplateStatus.Submitted)
            return await access.CanReviewCommunityTrainingTemplatesAsync();

        if (await access.CanManageGlobalTrainingTemplatesAsync()
            && await access.IsSupportModeAsync()
            && template.TenantId is int supportTenantId)
        {
            var currentTenantId = await access.GetCurrentTenantIdAsync();
            if (currentTenantId == supportTenantId)
                return await access.HasEffectiveTenantAdminPermissionsAsync();
        }

        return await access.CanEditComplianceContentAsync();
    }

    public async Task<bool> CanArchiveAsync(TrainingTemplate template, CancellationToken ct = default)
    {
        if (IsTenantOwnedTemplate(template) && template.CommunityStatus == CommunityTemplateStatus.Submitted)
            return false;

        return await CanEditAsync(template, ct);
    }

    public async Task<bool> CanCreateTenantTemplateAsync(CancellationToken ct = default)
    {
        if (!await access.CanAccessTenantBusinessModulesAsync())
            return false;

        return await access.CanEditComplianceContentAsync();
    }

    public Task<bool> CanCreateGlobalTemplateAsync(CancellationToken ct = default) =>
        access.CanManageGlobalTrainingTemplatesAsync();

    public Task<bool> CanReviewCommunityAsync(CancellationToken ct = default) =>
        access.CanReviewCommunityTrainingTemplatesAsync();

    public async Task<bool> CanSubmitToCommunityAsync(TrainingTemplate template, CancellationToken ct = default)
    {
        if (IsGlobalTemplate(template))
            return false;

        if (template.IsCommunityTemplate && template.CommunityStatus == CommunityTemplateStatus.Approved)
            return false;

        if (template.CommunityStatus is not (CommunityTemplateStatus.None or CommunityTemplateStatus.Rejected))
            return false;

        if (!template.IsActive || template.IsArchived)
            return false;

        var tenantId = await access.GetCurrentTenantIdAsync();
        if (!tenantId.HasValue || template.TenantId != tenantId)
            return false;

        return await access.CanEditComplianceContentAsync();
    }

    public async Task<TrainingTemplate?> GetTemplateByIdAsync(
        int templateId, int? tenantId, bool includeGlobal = true, CancellationToken ct = default)
    {
        var template = await LoadTemplateByIdAsync(templateId, ct);
        if (template is null || !await CanViewAsync(template, ct))
            return null;

        if (!includeGlobal && IsGlobalTemplate(template))
            return null;

        if (!IsGlobalTemplate(template) && tenantId.HasValue && template.TenantId != tenantId)
            return null;

        return template;
    }

    public async Task<TrainingTemplate?> GetTemplateForMutationAsync(int templateId, CancellationToken ct = default)
    {
        var template = await LoadTemplateByIdAsync(templateId, ct);
        if (template is null || !await CanEditAsync(template, ct))
            return null;

        return template;
    }

    public async Task<IQueryable<T>> ApplyQueryScopeAsync<T>(IQueryable<T> query, CancellationToken ct = default)
        where T : class
    {
        if (await RequiresUnfilteredQueriesAsync(ct))
            return query.IgnoreQueryFilters();

        return query;
    }

    public IQueryable<TrainingTemplate> VisibleTemplatesQuery(int tenantId, bool includeGlobal) =>
        includeGlobal
            ? db.TrainingTemplates.Where(t =>
                (t.IsGlobal && t.TenantId == null)
                || (t.TenantId == tenantId && !t.IsGlobal))
            : db.TrainingTemplates.Where(t => t.TenantId == tenantId && !t.IsGlobal);

    private async Task<TrainingTemplate?> LoadTemplateByIdAsync(int templateId, CancellationToken ct)
    {
        var query = db.TrainingTemplates.AsQueryable();
        if (await RequiresUnfilteredQueriesAsync(ct))
            query = query.IgnoreQueryFilters();

        return await query.FirstOrDefaultAsync(t => t.Id == templateId, ct);
    }
}
