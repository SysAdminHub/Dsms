using Dsms.Web.Data;
using Dsms.Web.Domain;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ServiceProviderEntity = Dsms.Web.Domain.Entities.ServiceProvider;

namespace Dsms.Web.Services.Licenses;

public sealed partial class LicenseService
{
    public Task<LicenseLimitCheckResult> CanCreateTenantAsync(Guid licenseId) =>
        CheckLicenseWideLimitAsync(
            licenseId,
            "Mandanten",
            l => l.MaxTenants,
            async (db, id) => await db.Tenants
                .IgnoreQueryFilters()
                .CountAsync(t => t.LicenseId == id && t.IsActive));

    public Task<LicenseLimitCheckResult> CanCreateAdminAsync(Guid licenseId) =>
        CheckLicenseWideLimitAsync(
            licenseId,
            "Admins",
            l => l.MaxAdmins,
            async (db, id) =>
            {
                var roleIds = await GetRoleIdsAsync(db);
                return await CountLicenseAdminsAsync(db, id, roleIds);
            });

    public Task<LicenseLimitCheckResult> CanCreateUserAsync(int tenantId) =>
        CheckTenantLimitAsync(
            tenantId,
            "Benutzer",
            l => l.MaxUsersPerTenant,
            (db, tid, roleIds) => CountRoleUsersForTenantAsync(db, tid, roleIds[DsmsRoles.User], roleIds[DsmsRoles.Superuser]));

    public Task<LicenseLimitCheckResult> CanCreateAuditorAsync(int tenantId) =>
        CheckTenantLimitAsync(
            tenantId,
            "Auditoren",
            l => l.MaxAuditorsPerTenant,
            (db, tid, roleIds) => CountRoleUsersForTenantAsync(db, tid, roleIds[DsmsRoles.Auditor], roleIds[DsmsRoles.Superuser]));

    public Task<LicenseLimitCheckResult> CanCreateCustomAuditTemplateAsync(int tenantId) =>
        CheckTenantLimitAsync(
            tenantId,
            "eigene Auditvorlagen",
            l => l.MaxCustomAuditTemplatesPerTenant,
            async (db, tid, _) => await db.AuditTemplates
                .IgnoreQueryFilters()
                .CountAsync(t => t.TenantId == tid
                    && !t.IsArchived
                    && t.TemplateType == AuditTemplateType.Tenant));

    public Task<LicenseLimitCheckResult> CanCreateActiveAuditAsync(int tenantId) =>
        CheckTenantLimitAsync(
            tenantId,
            "laufende Audits",
            l => l.MaxActiveAuditsPerTenant,
            async (db, tid, _) => await db.AuditRuns
                .IgnoreQueryFilters()
                .CountAsync(r => r.TenantId == tid
                    && !r.IsArchived
                    && (r.Status == AuditRunStatus.Draft || r.Status == AuditRunStatus.InProgress)));

    public Task<LicenseLimitCheckResult> CanCreateProcessingActivityAsync(int tenantId) =>
        CheckTenantLimitAsync(
            tenantId,
            "Verarbeitungstätigkeiten",
            l => l.MaxProcessingActivitiesPerTenant,
            (db, tid, _) => CountArchivableForTenantAsync<ProcessingActivity>(db, tid));

    public Task<LicenseLimitCheckResult> CanCreateDpiaAsync(int tenantId) =>
        CheckTenantLimitAsync(
            tenantId,
            "DSFA",
            l => l.MaxDpiaPerTenant,
            (db, tid, _) => CountArchivableForTenantAsync<DataProtectionImpactAssessment>(db, tid));

    public Task<LicenseLimitCheckResult> CanCreateTomAsync(int tenantId) =>
        CheckTenantLimitAsync(
            tenantId,
            "TOMs",
            l => l.MaxTomsPerTenant,
            (db, tid, _) => CountArchivableForTenantAsync<Tom>(db, tid));

    public Task<LicenseLimitCheckResult> CanCreateProcessorAsync(int tenantId) =>
        CheckTenantLimitAsync(
            tenantId,
            "Dienstleister",
            l => l.MaxProcessorsPerTenant,
            (db, tid, _) => CountArchivableForTenantAsync<ServiceProviderEntity>(db, tid));

    public Task<LicenseLimitCheckResult> CanCreateMeasureAsync(int tenantId) =>
        CheckTenantLimitAsync(
            tenantId,
            "laufende Maßnahmen",
            l => l.MaxActiveMeasuresPerTenant,
            async (db, tid, _) => await db.Measures
                .IgnoreQueryFilters()
                .CountAsync(m => m.TenantId == tid
                    && !m.IsArchived
                    && m.Status != MeasureStatus.Done
                    && m.Status != MeasureStatus.Cancelled));

    public async Task<LicenseLimitCheckResult> CanUseStorageAsync(Guid licenseId, long additionalBytes)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var license = await db.Licenses.AsNoTracking().FirstOrDefaultAsync(l => l.Id == licenseId);
        if (license is null)
        {
            return LicenseLimitHelper.LicenseNotFound();
        }

        if (TryBuildUsabilityBlock(license, licenseId) is { } usabilityBlock)
        {
            return usabilityBlock;
        }

        if (!license.MaxStorageMb.HasValue)
        {
            return LicenseLimitHelper.BuildCheckResult(
                "Speicher",
                0,
                null,
                LicenseLimitHelper.DefaultBlockedMessage,
                licenseId);
        }

        var tenantIds = await db.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.LicenseId == licenseId)
            .Select(t => t.Id)
            .ToListAsync();

        var currentBytes = tenantIds.Count == 0
            ? 0L
            : await db.EvidenceDocuments
                .IgnoreQueryFilters()
                .Where(d => tenantIds.Contains(d.TenantId) && !d.IsArchived)
                .SumAsync(d => (long?)d.FileSizeBytes) ?? 0L;

        var currentMb = (int)Math.Ceiling(currentBytes / (1024.0 * 1024.0));
        var additionalMb = (int)Math.Ceiling(additionalBytes / (1024.0 * 1024.0));
        var projectedMb = currentMb + additionalMb;

        return LicenseLimitHelper.BuildCheckResult(
            "Speicher",
            projectedMb,
            license.MaxStorageMb,
            LicenseLimitHelper.SpecificBlockedMessage("Speicher"),
            licenseId);
    }

    public async Task<LicenseLimitCheckResult> CanSendEmailReminderAsync(Guid licenseId)
    {
        // TODO: E-Mail-Erinnerungen pro Monat zählen, sobald Reminder-Logs/Entität vorhanden sind.
        await using var db = await dbFactory.CreateDbContextAsync();
        var license = await db.Licenses.AsNoTracking().FirstOrDefaultAsync(l => l.Id == licenseId);
        if (license is null)
        {
            return LicenseLimitHelper.LicenseNotFound();
        }

        if (TryBuildUsabilityBlock(license, licenseId) is { } usabilityBlock)
        {
            return usabilityBlock;
        }

        return LicenseLimitHelper.BuildCheckResult(
            "E-Mail-Erinnerungen",
            0,
            license.MaxEmailRemindersPerMonth,
            LicenseLimitHelper.SpecificBlockedMessage("E-Mail-Erinnerungen"),
            licenseId);
    }

    private async Task<LicenseLimitCheckResult> CheckLicenseWideLimitAsync(
        Guid licenseId,
        string limitName,
        Func<License, int?> getLimit,
        Func<ApplicationDbContext, Guid, Task<int>> countAsync)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var license = await db.Licenses.AsNoTracking().FirstOrDefaultAsync(l => l.Id == licenseId);
        if (license is null)
        {
            return LicenseLimitHelper.LicenseNotFound();
        }

        if (TryBuildUsabilityBlock(license, licenseId) is { } usabilityBlock)
        {
            return usabilityBlock;
        }

        var limit = getLimit(license);
        var current = await countAsync(db, licenseId);
        return LicenseLimitHelper.BuildCheckResult(
            limitName,
            current,
            limit,
            LicenseLimitHelper.SpecificBlockedMessage(limitName),
            licenseId);
    }

    private async Task<LicenseLimitCheckResult> CheckTenantLimitAsync(
        int tenantId,
        string limitName,
        Func<License, int?> getLimit,
        Func<ApplicationDbContext, int, Dictionary<string, string>, Task<int>> countAsync)
    {
        if (tenantId <= 0)
        {
            return LicenseLimitHelper.NoTenantContext();
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant?.LicenseId is not Guid licenseId)
        {
            return LicenseLimitHelper.NoLicenseAssigned(tenantId);
        }

        var license = await db.Licenses.AsNoTracking().FirstOrDefaultAsync(l => l.Id == licenseId);
        if (license is null)
        {
            return LicenseLimitHelper.LicenseNotFound(tenantId);
        }

        if (TryBuildUsabilityBlock(license, licenseId, tenantId) is { } usabilityBlock)
        {
            return usabilityBlock;
        }

        var roleIds = await GetRoleIdsAsync(db);
        var current = await countAsync(db, tenantId, roleIds);
        return LicenseLimitHelper.BuildCheckResult(
            limitName,
            current,
            getLimit(license),
            LicenseLimitHelper.SpecificBlockedMessage(limitName),
            licenseId,
            tenantId);
    }

    private static async Task<Dictionary<string, string>> GetRoleIdsAsync(ApplicationDbContext db) =>
        await db.Roles
            .Where(r => r.Name == DsmsRoles.Admin || r.Name == DsmsRoles.Superuser
                || r.Name == DsmsRoles.User || r.Name == DsmsRoles.Auditor)
            .ToDictionaryAsync(r => r.Name!, r => r.Id);

    private static async Task<int> CountRoleUsersForTenantAsync(
        ApplicationDbContext db,
        int tenantId,
        string roleId,
        string superuserRoleId)
    {
        var counts = await CountUsersByRolePerTenantAsync(db, [tenantId], roleId, superuserRoleId);
        return counts.GetValueOrDefault(tenantId);
    }

    private static Task<int> CountArchivableForTenantAsync<TEntity>(ApplicationDbContext db, int tenantId)
        where TEntity : ArchivableEntityBase, ITenantEntity =>
        db.Set<TEntity>()
            .IgnoreQueryFilters()
            .CountAsync(e => e.TenantId == tenantId && !e.IsArchived);
}
