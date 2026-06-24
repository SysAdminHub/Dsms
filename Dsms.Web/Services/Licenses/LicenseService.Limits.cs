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

    public Task<LicenseLimitCheckResult> CanCreateAdminAsync(Guid licenseId, int? tenantId = null) =>
        // Im bezahlten Zugang gibt es kein getrenntes Admin-Limit mehr; Admins zählen gemeinsam mit
        // normalen Benutzern als lizenzierte Zugänge gegen LicensedUserCount. Ist ein Mandant bekannt
        // (z. B. Mandanten-Admin legt einen weiteren Admin an), wird der aktuelle Mandant herangezogen;
        // ohne Mandantenkontext (Superuser, lizenzweite Sicht) wird lizenzweit gezählt.
        // Free-Zugänge nutzen weiterhin MaxAdmins.
        CheckLicenseWideLimitAsync(
            licenseId,
            "Admins",
            l => l.MaxAdmins,
            async (db, id) =>
            {
                var roleIds = await GetRoleIdsAsync(db);
                return await CountLicenseAdminsAsync(db, id, roleIds);
            },
            treatAsLicensedAccessWhenPaid: true,
            licensedAccessTenantId: tenantId);

    public Task<LicenseLimitCheckResult> CanCreateUserAsync(int tenantId) =>
        // Im bezahlten Zugang werden Benutzer nicht mehr je Mandant limitiert, sondern lizenzweit
        // als Zugänge gezählt. Free-Zugänge nutzen weiterhin MaxUsersPerTenant.
        CheckTenantLimitAsync(
            tenantId,
            "Benutzer",
            l => l.MaxUsersPerTenant,
            (db, tid, roleIds) => CountRoleUsersForTenantAsync(db, tid, roleIds[DsmsRoles.User], roleIds[DsmsRoles.Superuser]),
            treatAsLicensedAccessWhenPaid: true);

    public Task<LicenseLimitCheckResult> CanCreateAuditorAsync(int tenantId) =>
        // Im bezahlten Zugang sind Auditoren rein lesende Zugänge und werden NICHT auf die
        // lizenzierten Zugänge (Admin + Benutzer) angerechnet; sie sind daher unbegrenzt
        // anlegbar. Free-Zugänge nutzen weiterhin das getrennte MaxAuditorsPerTenant-Limit.
        CheckTenantLimitAsync(
            tenantId,
            "Auditoren",
            l => l.MaxAuditorsPerTenant,
            (db, tid, roleIds) => CountRoleUsersForTenantAsync(db, tid, roleIds[DsmsRoles.Auditor], roleIds[DsmsRoles.Superuser]),
            waiveWhenPaid: true);

    public Task<LicenseLimitCheckResult> CanCreateCustomAuditTemplateAsync(int tenantId) =>
        // Im bezahlten Zugang entfällt das Limit für eigene Auditvorlagen (Fair-Use-Modell);
        // Free-Zugänge nutzen weiterhin MaxCustomAuditTemplatesPerTenant.
        CheckTenantLimitAsync(
            tenantId,
            "eigene Auditvorlagen",
            l => l.MaxCustomAuditTemplatesPerTenant,
            async (db, tid, _) => await db.AuditTemplates
                .IgnoreQueryFilters()
                .CountAsync(t => t.TenantId == tid
                    && !t.IsArchived
                    && t.TemplateType == AuditTemplateType.Tenant),
            waiveWhenPaid: true);

    public Task<LicenseLimitCheckResult> CanCreateActiveAuditAsync(int tenantId) =>
        // Im bezahlten Zugang entfällt das Limit für laufende Audits (Fair-Use-Modell);
        // Free-Zugänge nutzen weiterhin MaxActiveAuditsPerTenant.
        CheckTenantLimitAsync(
            tenantId,
            "laufende Audits",
            l => l.MaxActiveAuditsPerTenant,
            async (db, tid, _) => await db.AuditRuns
                .IgnoreQueryFilters()
                .CountAsync(r => r.TenantId == tid
                    && !r.IsArchived
                    && (r.Status == AuditRunStatus.Draft || r.Status == AuditRunStatus.InProgress)),
            waiveWhenPaid: true);

    public Task<LicenseLimitCheckResult> CanCreateProcessingActivityAsync(int tenantId) =>
        CheckTenantLimitAsync(
            tenantId,
            "Verarbeitungstätigkeiten",
            l => l.MaxProcessingActivitiesPerTenant,
            (db, tid, _) => CountArchivableForTenantAsync<ProcessingActivity>(db, tid),
            waiveWhenPaid: true);

    public Task<LicenseLimitCheckResult> CanCreateDpiaAsync(int tenantId) =>
        CheckTenantLimitAsync(
            tenantId,
            "DSFA",
            l => l.MaxDpiaPerTenant,
            (db, tid, _) => CountArchivableForTenantAsync<DataProtectionImpactAssessment>(db, tid),
            waiveWhenPaid: true);

    public Task<LicenseLimitCheckResult> CanCreateTomAsync(int tenantId) =>
        CheckTenantLimitAsync(
            tenantId,
            "TOMs",
            l => l.MaxTomsPerTenant,
            (db, tid, _) => CountArchivableForTenantAsync<Tom>(db, tid),
            waiveWhenPaid: true);

    public Task<LicenseLimitCheckResult> CanCreateProcessorAsync(int tenantId) =>
        CheckTenantLimitAsync(
            tenantId,
            "Dienstleister",
            l => l.MaxProcessorsPerTenant,
            (db, tid, _) => CountArchivableForTenantAsync<ServiceProviderEntity>(db, tid),
            waiveWhenPaid: true);

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
                    && m.Status != MeasureStatus.Cancelled),
            waiveWhenPaid: true);

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

        // Im bezahlten Zugang werden Dokumente ausschließlich durch den verfügbaren
        // Speicherplatz begrenzt (enthaltener + zusätzlicher Speicher); Free-Zugänge nutzen MaxStorageMb.
        var storageLimitMb = LicenseProductRules.IsPaidPlanActive(license)
            ? LicenseProductRules.GetTotalStorageMb(license)
            : license.MaxStorageMb;

        if (!storageLimitMb.HasValue)
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
            storageLimitMb,
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
        Func<ApplicationDbContext, Guid, Task<int>> countAsync,
        bool treatAsLicensedAccessWhenPaid = false,
        int? licensedAccessTenantId = null)
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

        if (treatAsLicensedAccessWhenPaid && LicenseProductRules.IsPaidPlanActive(license))
        {
            return await BuildLicensedAccessResultAsync(db, license, licenseId, licensedAccessTenantId);
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
        Func<ApplicationDbContext, int, Dictionary<string, string>, Task<int>> countAsync,
        bool waiveWhenPaid = false,
        bool treatAsLicensedAccessWhenPaid = false)
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

        // Im bezahlten Zugang zählen alle aktiven Zugänge gemeinsam gegen LicensedUserCount;
        // es gibt keine getrennten Benutzer-/Auditoren-Limits je Mandant mehr.
        if (treatAsLicensedAccessWhenPaid && LicenseProductRules.IsPaidPlanActive(license))
        {
            return await BuildLicensedAccessResultAsync(db, license, licenseId, tenantId);
        }

        var roleIds = await GetRoleIdsAsync(db);
        var current = await countAsync(db, tenantId, roleIds);

        // Im bezahlten Zugang entfallen die fachlichen Objekt-Limits (Fair-Use-Modell);
        // Free-Zugänge nutzen weiterhin die konfigurierten Per-Mandant-Limits.
        var effectiveLimit = waiveWhenPaid && LicenseProductRules.AreBusinessObjectLimitsWaived(license)
            ? (int?)null
            : getLimit(license);

        return LicenseLimitHelper.BuildCheckResult(
            limitName,
            current,
            effectiveLimit,
            LicenseLimitHelper.SpecificBlockedMessage(limitName),
            licenseId,
            tenantId);
    }

    private static async Task<LicenseLimitCheckResult> BuildLicensedAccessResultAsync(
        ApplicationDbContext db,
        License license,
        Guid licenseId,
        int? tenantId = null)
    {
        var roleIds = await GetRoleIdsAsync(db);

        // Ist ein Mandant bekannt, werden nur die Zugänge dieses Mandanten gewertet
        // (fachliche Anforderung: "lizenzierte Zugänge" beziehen sich auf den aktuellen Mandanten).
        // Ohne Mandantenkontext (z. B. lizenzweite Superuser-Sicht) wird lizenzweit gezählt.
        var current = tenantId is int tid
            ? await CountActiveTenantAccessesAsync(db, tid, roleIds)
            : await CountActiveLicenseAccessesAsync(db, licenseId, roleIds);

        return LicenseLimitHelper.BuildCheckResult(
            LicenseLimitHelper.LicensedAccessLimitName,
            current,
            license.LicensedUserCount,
            LicenseLimitHelper.LicensedAccessLimitReachedMessage,
            licenseId,
            tenantId);
    }

    /// <summary>
    /// Zählt im bezahlten Zugang die aktiven, lizenzierten Zugänge eines EINZELNEN Mandanten.
    /// Gezählt werden nur aktive Benutzer mit Rolle <see cref="DsmsRoles.Admin"/> oder
    /// <see cref="DsmsRoles.User"/>. Auditoren (rein lesend), deaktivierte Benutzer, Superuser
    /// sowie Benutzer anderer Mandanten zählen NICHT mit. Über UserTenants und die Legacy-TenantId
    /// zugeordnete Benutzer werden vereinigt; jeder Benutzer wird über die Benutzer-ID genau einmal
    /// gezählt (Distinct gegen Doppelzählung bei mehreren Mappings/Rollen).
    /// </summary>
    private static async Task<int> CountActiveTenantAccessesAsync(
        ApplicationDbContext db,
        int tenantId,
        Dictionary<string, string> roleIds)
    {
        var adminRoleId = roleIds[DsmsRoles.Admin];
        var userRoleId = roleIds[DsmsRoles.User];
        var superuserRoleId = roleIds[DsmsRoles.Superuser];

        var fromUserTenants = await (
            from ut in db.UserTenants.IgnoreQueryFilters()
            join u in db.Users on ut.UserId equals u.Id
            where ut.TenantId == tenantId
                && u.IsActive
                && db.UserRoles.Any(ur => ur.UserId == u.Id
                    && (ur.RoleId == adminRoleId || ur.RoleId == userRoleId))
                && !db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == superuserRoleId)
            select u.Id
        ).ToListAsync();

        var fromLegacy = await db.Users
            .Where(u => u.TenantId == tenantId
                && u.IsActive
                && db.UserRoles.Any(ur => ur.UserId == u.Id
                    && (ur.RoleId == adminRoleId || ur.RoleId == userRoleId))
                && !db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == superuserRoleId))
            .Select(u => u.Id)
            .ToListAsync();

        return fromUserTenants.Concat(fromLegacy).Distinct().Count();
    }

    /// <summary>
    /// Zählt im bezahlten Zugang die aktiven, lizenzierten Zugänge LIZENZWEIT (über alle Mandanten
    /// der Lizenz). Gezählt werden nur aktive Benutzer mit Rolle <see cref="DsmsRoles.Admin"/> oder
    /// <see cref="DsmsRoles.User"/>; Auditoren und Superuser zählen nicht mit. Wird für die
    /// Plattform-/Superuser-Lizenzübersicht ohne Mandantenkontext verwendet. Jeder Benutzer wird
    /// genau einmal gezählt.
    /// </summary>
    private static async Task<int> CountActiveLicenseAccessesAsync(
        ApplicationDbContext db,
        Guid licenseId,
        Dictionary<string, string> roleIds)
    {
        var adminRoleId = roleIds[DsmsRoles.Admin];
        var userRoleId = roleIds[DsmsRoles.User];
        var superuserRoleId = roleIds[DsmsRoles.Superuser];

        var tenantIds = await db.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.LicenseId == licenseId && t.IsActive)
            .Select(t => t.Id)
            .ToListAsync();

        // Admins / lizenzweit über LicenseId zugeordnete Benutzer (nur Admin-/Benutzer-Rolle).
        var adminUserIds = await db.Users
            .Where(u => u.LicenseId == licenseId && u.IsActive)
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                && (ur.RoleId == adminRoleId || ur.RoleId == userRoleId)))
            .Where(u => !db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == superuserRoleId))
            .Select(u => u.Id)
            .ToListAsync();

        var userIds = new HashSet<string>(adminUserIds);

        if (tenantIds.Count > 0)
        {
            var fromUserTenants = await (
                from ut in db.UserTenants.IgnoreQueryFilters()
                join u in db.Users on ut.UserId equals u.Id
                where tenantIds.Contains(ut.TenantId)
                    && u.IsActive
                    && db.UserRoles.Any(ur => ur.UserId == u.Id
                        && (ur.RoleId == adminRoleId || ur.RoleId == userRoleId))
                    && !db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == superuserRoleId)
                select u.Id
            ).ToListAsync();

            var fromLegacy = await db.Users
                .Where(u => u.TenantId != null
                    && tenantIds.Contains(u.TenantId.Value)
                    && u.IsActive)
                .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                    && (ur.RoleId == adminRoleId || ur.RoleId == userRoleId)))
                .Where(u => !db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == superuserRoleId))
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var id in fromUserTenants)
            {
                userIds.Add(id);
            }

            foreach (var id in fromLegacy)
            {
                userIds.Add(id);
            }
        }

        return userIds.Count;
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
