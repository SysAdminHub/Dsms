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

    public async Task<bool> CanViewAsync(TrainingTemplate template, CancellationToken ct = default)
    {
        if (IsGlobalTemplate(template))
            return await access.GetCurrentTenantIdAsync() is not null || await access.IsSuperuserAsync();

        var tenantId = await access.GetCurrentTenantIdAsync();
        return tenantId.HasValue && template.TenantId == tenantId;
    }

    public async Task<bool> CanEditAsync(TrainingTemplate template, CancellationToken ct = default)
    {
        if (!await CanViewAsync(template, ct))
            return false;

        if (IsGlobalTemplate(template))
            return await access.IsSuperuserAsync();

        if (template.CommunityStatus == CommunityTemplateStatus.Submitted)
            return await access.IsSuperuserAsync();

        if (await access.IsSuperuserAsync())
            return true;

        return await access.CanEditComplianceContentAsync();
    }

    public async Task<bool> CanCreateTenantTemplateAsync(CancellationToken ct = default)
    {
        if (await access.GetCurrentTenantIdAsync() is null)
            return false;

        if (await access.IsSuperuserAsync())
            return true;

        return await access.CanEditComplianceContentAsync();
    }

    public async Task<TrainingTemplate?> GetTemplateByIdAsync(
        int templateId, int? tenantId, bool includeGlobal = true, CancellationToken ct = default)
    {
        var template = await db.TrainingTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, ct);

        if (template is null || !await CanViewAsync(template, ct))
            return null;

        if (!includeGlobal && IsGlobalTemplate(template))
            return null;

        if (!IsGlobalTemplate(template) && tenantId.HasValue && template.TenantId != tenantId)
            return null;

        return template;
    }

    public IQueryable<TrainingTemplate> VisibleTemplatesQuery(int tenantId, bool includeGlobal) =>
        includeGlobal
            ? db.TrainingTemplates.Where(t =>
                (t.IsGlobal && t.TenantId == null)
                || (t.TenantId == tenantId && !t.IsGlobal))
            : db.TrainingTemplates.Where(t => t.TenantId == tenantId && !t.IsGlobal);
}
