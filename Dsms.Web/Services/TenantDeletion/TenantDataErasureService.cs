using Dsms.Web.Data;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.Training;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.TenantDeletion;

public interface ITenantDataErasureService
{
    /// <summary>
    /// Entfernt alle mandantenbezogenen Fachdaten und den Mandanten selbst.
    /// Aufbewahrungspflichtige Daten werden anonymisiert, nicht pauschal gelöscht.
    /// </summary>
    Task<TenantDataErasureResult> EraseTenantDataAsync(
        ApplicationDbContext db,
        int tenantId,
        CancellationToken ct = default);
}

public sealed class TenantDataErasureResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyDictionary<string, int> DeletedCounts { get; init; } =
        new Dictionary<string, int>();

    public IReadOnlyList<string> FileDeletionErrors { get; init; } = [];

    public static TenantDataErasureResult Ok(
        IReadOnlyDictionary<string, int> counts,
        IReadOnlyList<string>? fileErrors = null) =>
        new()
        {
            Success = true,
            DeletedCounts = counts,
            FileDeletionErrors = fileErrors ?? []
        };

    public static TenantDataErasureResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}

public sealed class TenantDataErasureService(
    DocumentStorageService documentStorage,
    TrainingAssetStorageService trainingAssetStorage,
    ILogger<TenantDataErasureService> logger) : ITenantDataErasureService
{
    public async Task<TenantDataErasureResult> EraseTenantDataAsync(
        ApplicationDbContext db,
        int tenantId,
        CancellationToken ct = default)
    {
        var tenantExists = await db.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Id == tenantId, ct);

        if (!tenantExists)
        {
            return TenantDataErasureResult.Fail("Mandant nicht gefunden.");
        }

        var counts = new Dictionary<string, int>();
        var fileErrors = new List<string>();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        try
        {
            await CollectAndDeleteStoredFilesAsync(db, tenantId, fileErrors, ct);

            counts[nameof(db.TrainingQuizAnswers)] =
                await DeleteTenantRowsAsync(db.TrainingQuizAnswers, tenantId, ct);
            counts[nameof(db.TrainingQuizAttempts)] =
                await DeleteTenantRowsAsync(db.TrainingQuizAttempts, tenantId, ct);
            counts[nameof(db.TrainingAssignmentSectionProgress)] =
                await DeleteTenantRowsAsync(db.TrainingAssignmentSectionProgress, tenantId, ct);
            counts[nameof(db.TrainingAssignments)] =
                await DeleteTenantRowsAsync(db.TrainingAssignments, tenantId, ct);
            counts[nameof(db.Trainings)] =
                await DeleteTenantRowsAsync(db.Trainings, tenantId, ct);
            counts[nameof(db.TrainingParticipants)] =
                await DeleteTenantRowsAsync(db.TrainingParticipants, tenantId, ct);

            counts[nameof(db.TrainingQuestionOptions)] =
                await DeleteTrainingChildRowsAsync(db.TrainingQuestionOptions, tenantId, ct);
            counts[nameof(db.TrainingQuestions)] =
                await DeleteTrainingChildRowsAsync(db.TrainingQuestions, tenantId, ct);
            counts[nameof(db.TrainingTemplateAssets)] =
                await DeleteTrainingChildRowsAsync(db.TrainingTemplateAssets, tenantId, ct);
            counts[nameof(db.TrainingTemplateSections)] =
                await DeleteTrainingChildRowsAsync(db.TrainingTemplateSections, tenantId, ct);
            counts[nameof(db.TrainingTemplates)] =
                await DeleteTenantTrainingTemplatesAsync(db, tenantId, ct);

            counts[nameof(db.DataSubjectRequestProcessingActivities)] =
                await DeleteTenantRowsAsync(db.DataSubjectRequestProcessingActivities, tenantId, ct);
            counts[nameof(db.DataSubjectRequestMeasures)] =
                await DeleteTenantRowsAsync(db.DataSubjectRequestMeasures, tenantId, ct);
            counts[nameof(db.DataSubjectRequestServiceProviders)] =
                await DeleteTenantRowsAsync(db.DataSubjectRequestServiceProviders, tenantId, ct);
            counts[nameof(db.DataSubjectRequests)] =
                await DeleteTenantRowsAsync(db.DataSubjectRequests, tenantId, ct);

            counts[nameof(db.PrivacyIncidentProcessingActivities)] =
                await DeleteTenantRowsAsync(db.PrivacyIncidentProcessingActivities, tenantId, ct);
            counts[nameof(db.PrivacyIncidentServiceProviders)] =
                await DeleteTenantRowsAsync(db.PrivacyIncidentServiceProviders, tenantId, ct);
            counts[nameof(db.PrivacyIncidentMeasures)] =
                await DeleteTenantRowsAsync(db.PrivacyIncidentMeasures, tenantId, ct);
            counts[nameof(db.PrivacyIncidentToms)] =
                await DeleteTenantRowsAsync(db.PrivacyIncidentToms, tenantId, ct);
            counts[nameof(db.PrivacyIncidents)] =
                await DeleteTenantRowsAsync(db.PrivacyIncidents, tenantId, ct);

            counts[nameof(db.ProcessingActivityAuditAnswers)] =
                await DeleteTenantRowsAsync(db.ProcessingActivityAuditAnswers, tenantId, ct);
            counts[nameof(db.ProcessingActivityMeasures)] =
                await DeleteTenantRowsAsync(db.ProcessingActivityMeasures, tenantId, ct);
            counts[nameof(db.ProcessingActivityServiceProviders)] =
                await DeleteTenantRowsAsync(db.ProcessingActivityServiceProviders, tenantId, ct);
            counts[nameof(db.ProcessingActivityToms)] =
                await DeleteTenantRowsAsync(db.ProcessingActivityToms, tenantId, ct);
            counts[nameof(db.ServiceProviderToms)] =
                await DeleteTenantRowsAsync(db.ServiceProviderToms, tenantId, ct);
            counts[nameof(db.DataProtectionImpactAssessments)] =
                await DeleteTenantRowsAsync(db.DataProtectionImpactAssessments, tenantId, ct);
            counts[nameof(db.ProcessingActivities)] =
                await DeleteTenantRowsAsync(db.ProcessingActivities, tenantId, ct);
            counts[nameof(db.Toms)] =
                await DeleteTenantRowsAsync(db.Toms, tenantId, ct);
            counts[nameof(db.ServiceProviders)] =
                await DeleteTenantRowsAsync(db.ServiceProviders, tenantId, ct);

            counts[nameof(db.DocumentLinks)] =
                await DeleteTenantRowsAsync(db.DocumentLinks, tenantId, ct);
            counts[nameof(db.EvidenceDocuments)] =
                await DeleteTenantRowsAsync(db.EvidenceDocuments, tenantId, ct);
            counts[nameof(db.DocumentCategories)] =
                await DeleteTenantRowsAsync(db.DocumentCategories, tenantId, ct);

            counts[nameof(db.AuditAnswers)] =
                await DeleteTenantAuditAnswersAsync(db, tenantId, ct);
            counts[nameof(db.Measures)] =
                await DeleteTenantRowsAsync(db.Measures, tenantId, ct);
            counts[nameof(db.AuditRuns)] =
                await DeleteTenantRowsAsync(db.AuditRuns, tenantId, ct);
            counts[nameof(db.AuditQuestions)] =
                await DeleteTenantAuditQuestionsAsync(db, tenantId, ct);
            counts[nameof(db.AuditTemplates)] =
                await DeleteTenantAuditTemplatesAsync(db, tenantId, ct);

            counts[nameof(db.DataProtectionRoles)] =
                await DeleteTenantDataProtectionRolesAsync(db, tenantId, ct);
            counts[nameof(db.TenantOnboardingTasks)] =
                await DeleteTenantRowsAsync(db.TenantOnboardingTasks, tenantId, ct);
            counts[nameof(db.SupportAccessGrants)] =
                await DeleteTenantRowsAsync(db.SupportAccessGrants, tenantId, ct);
            counts[nameof(db.LegalAcceptances)] =
                await DeleteTenantRowsAsync(db.LegalAcceptances, tenantId, ct);

            var (userTenantsDeleted, usersDeleted) =
                await CleanupTenantUsersAsync(db, tenantId, ct);
            counts[nameof(db.UserTenants)] = userTenantsDeleted;
            counts[nameof(ApplicationUser)] = usersDeleted;

            counts[nameof(db.PendingSignups)] =
                await AnonymizePendingSignupsAsync(db, tenantId, ct);
            counts[nameof(db.LogEntries)] =
                await AnonymizeLogEntriesAsync(db, tenantId, ct);

            await ClearCommunityTemplateReferencesAsync(db, tenantId, ct);

            counts[nameof(db.Tenants)] =
                await db.Tenants
                    .IgnoreQueryFilters()
                    .Where(t => t.Id == tenantId)
                    .ExecuteDeleteAsync(ct);

            await transaction.CommitAsync(ct);

            DeleteTenantDirectories(tenantId, fileErrors);

            return TenantDataErasureResult.Ok(counts, fileErrors);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            logger.LogError(ex, "Endgültige Mandantenlöschung fehlgeschlagen. TenantId={TenantId}", tenantId);
            return TenantDataErasureResult.Fail("Die endgültige Löschung ist fehlgeschlagen.");
        }
    }

    private static async Task<int> DeleteTenantRowsAsync<TEntity>(
        DbSet<TEntity> set,
        int tenantId,
        CancellationToken ct)
        where TEntity : class
    {
        return await set
            .IgnoreQueryFilters()
            .Where(e => EF.Property<int>(e, "TenantId") == tenantId)
            .ExecuteDeleteAsync(ct);
    }

    private static async Task<int> DeleteTrainingChildRowsAsync<TEntity>(
        DbSet<TEntity> set,
        int tenantId,
        CancellationToken ct)
        where TEntity : Domain.Entities.EntityBase
    {
        return await set
            .IgnoreQueryFilters()
            .Where(e => EF.Property<int?>(e, "TenantId") == tenantId)
            .ExecuteDeleteAsync(ct);
    }

    private static async Task<int> DeleteTenantTrainingTemplatesAsync(
        ApplicationDbContext db,
        int tenantId,
        CancellationToken ct) =>
        await db.TrainingTemplates
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId && !t.IsGlobal)
            .ExecuteDeleteAsync(ct);

    private static async Task<int> DeleteTenantAuditAnswersAsync(
        ApplicationDbContext db,
        int tenantId,
        CancellationToken ct)
    {
        var runIds = db.AuditRuns
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId)
            .Select(r => r.Id);

        return await db.AuditAnswers
            .IgnoreQueryFilters()
            .Where(a => runIds.Contains(a.AuditRunId))
            .ExecuteDeleteAsync(ct);
    }

    private static async Task<int> DeleteTenantAuditQuestionsAsync(
        ApplicationDbContext db,
        int tenantId,
        CancellationToken ct)
    {
        var templateIds = db.AuditTemplates
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId && t.TemplateType == AuditTemplateType.Tenant)
            .Select(t => t.Id);

        return await db.AuditQuestions
            .IgnoreQueryFilters()
            .Where(q => templateIds.Contains(q.AuditTemplateId))
            .ExecuteDeleteAsync(ct);
    }

    private static async Task<int> DeleteTenantAuditTemplatesAsync(
        ApplicationDbContext db,
        int tenantId,
        CancellationToken ct) =>
        await db.AuditTemplates
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId && t.TemplateType == AuditTemplateType.Tenant)
            .ExecuteDeleteAsync(ct);

    private static async Task<int> DeleteTenantDataProtectionRolesAsync(
        ApplicationDbContext db,
        int tenantId,
        CancellationToken ct)
    {
        await db.DataProtectionRoles
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(r => r.ReportsToRoleId, (int?)null)
                    .SetProperty(r => r.DeputyRoleId, (int?)null),
                ct);

        return await DeleteTenantRowsAsync(db.DataProtectionRoles, tenantId, ct);
    }

    /// <summary>
    /// Entfernt Mandantenzuordnungen und exklusive Benutzer vollständig im Transaktions-DbContext.
    /// </summary>
    private static async Task<(int UserTenantsDeleted, int UsersDeleted)> CleanupTenantUsersAsync(
        ApplicationDbContext db,
        int tenantId,
        CancellationToken ct)
    {
        var linkedUserIds = await db.UserTenants
            .IgnoreQueryFilters()
            .Where(ut => ut.TenantId == tenantId)
            .Select(ut => ut.UserId)
            .Distinct()
            .ToListAsync(ct);

        var legacyLinkedUserIds = await db.Users
            .Where(u => u.TenantId == tenantId)
            .Select(u => u.Id)
            .ToListAsync(ct);

        linkedUserIds = linkedUserIds.Union(legacyLinkedUserIds).Distinct().ToList();
        if (linkedUserIds.Count == 0)
        {
            return (0, 0);
        }

        var multiTenantUserIds = await db.UserTenants
            .IgnoreQueryFilters()
            .Where(ut => ut.TenantId != tenantId && linkedUserIds.Contains(ut.UserId))
            .Select(ut => ut.UserId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var userId in multiTenantUserIds)
        {
            var fallbackTenantId = await db.UserTenants
                .IgnoreQueryFilters()
                .Where(ut => ut.UserId == userId && ut.TenantId != tenantId)
                .Select(ut => (int?)ut.TenantId)
                .FirstOrDefaultAsync(ct);

            await db.Users
                .Where(u => u.Id == userId && u.TenantId == tenantId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.TenantId, fallbackTenantId), ct);
        }

        var exclusiveUserIds = linkedUserIds.Except(multiTenantUserIds).ToList();

        var userTenantsDeleted = await db.UserTenants
            .IgnoreQueryFilters()
            .Where(ut => ut.TenantId == tenantId)
            .ExecuteDeleteAsync(ct);

        if (exclusiveUserIds.Count == 0)
        {
            return (userTenantsDeleted, 0);
        }

        await db.Set<IdentityUserRole<string>>()
            .Where(r => exclusiveUserIds.Contains(r.UserId))
            .ExecuteDeleteAsync(ct);
        await db.Set<IdentityUserClaim<string>>()
            .Where(c => exclusiveUserIds.Contains(c.UserId))
            .ExecuteDeleteAsync(ct);
        await db.Set<IdentityUserLogin<string>>()
            .Where(l => exclusiveUserIds.Contains(l.UserId))
            .ExecuteDeleteAsync(ct);
        await db.Set<IdentityUserToken<string>>()
            .Where(t => exclusiveUserIds.Contains(t.UserId))
            .ExecuteDeleteAsync(ct);

        var usersDeleted = await db.Users
            .Where(u => exclusiveUserIds.Contains(u.Id))
            .ExecuteDeleteAsync(ct);

        return (userTenantsDeleted, usersDeleted);
    }

    /// <summary>
    /// Registrierungs-/Bestelldaten: steuerlich relevante Beträge bleiben erhalten,
    /// personenbezogene Felder werden anonymisiert (Aufbewahrungspflichten).
    /// </summary>
    private static async Task<int> AnonymizePendingSignupsAsync(
        ApplicationDbContext db,
        int tenantId,
        CancellationToken ct)
    {
        const string anonymized = "[gelöscht]";

        return await db.PendingSignups
            .Where(p => p.ProvisionedTenantId == tenantId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(p => p.ProvisionedTenantId, (int?)null)
                    .SetProperty(p => p.ProvisionedTenantName, anonymized)
                    .SetProperty(p => p.ProvisionedAdminUserId, (string?)null)
                    .SetProperty(p => p.TenantName, anonymized)
                    .SetProperty(p => p.TenantLegalName, anonymized)
                    .SetProperty(p => p.TenantEmail, (string?)null)
                    .SetProperty(p => p.TenantPhone, (string?)null)
                    .SetProperty(p => p.TenantAddress, (string?)null)
                    .SetProperty(p => p.CustomerName, anonymized)
                    .SetProperty(p => p.CustomerEmail, (string?)null)
                    .SetProperty(p => p.AdminEmail, anonymized)
                    .SetProperty(p => p.AdminDisplayName, anonymized)
                    .SetProperty(p => p.AdminFirstName, (string?)null)
                    .SetProperty(p => p.AdminLastName, (string?)null)
                    .SetProperty(p => p.BillingCompanyName, anonymized)
                    .SetProperty(p => p.BillingEmail, (string?)null)
                    .SetProperty(p => p.BillingStreet, (string?)null)
                    .SetProperty(p => p.BillingPostalCode, (string?)null)
                    .SetProperty(p => p.BillingCity, (string?)null)
                    .SetProperty(p => p.BillingCountry, (string?)null)
                    .SetProperty(p => p.BillingVatId, (string?)null)
                    .SetProperty(p => p.BillingReference, (string?)null),
                ct);
    }

    /// <summary>
    /// Plattform-/Systemprotokolle: technische Referenzen bleiben, personenbezogene Felder werden minimiert.
    /// </summary>
    private static async Task<int> AnonymizeLogEntriesAsync(
        ApplicationDbContext db,
        int tenantId,
        CancellationToken ct)
    {
        const string anonymized = "[gelöscht]";

        return await db.LogEntries
            .Where(l => l.TenantId == tenantId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(l => l.TenantName, anonymized)
                    .SetProperty(l => l.UserEmail, (string?)null)
                    .SetProperty(l => l.UserDisplayName, (string?)null)
                    .SetProperty(l => l.EntityName, (string?)null)
                    .SetProperty(l => l.IpAddressAnonymized, (string?)null)
                    .SetProperty(l => l.UserAgent, (string?)null),
                ct);
    }

    private static async Task ClearCommunityTemplateReferencesAsync(
        ApplicationDbContext db,
        int tenantId,
        CancellationToken ct)
    {
        await db.TrainingTemplates
            .IgnoreQueryFilters()
            .Where(t => t.CommunitySubmittedByTenantId == tenantId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.CommunitySubmittedByTenantId, (int?)null),
                ct);

        await db.AuditTemplates
            .IgnoreQueryFilters()
            .Where(t => t.OriginalTenantId == tenantId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.OriginalTenantId, (int?)null),
                ct);
    }

    private async Task CollectAndDeleteStoredFilesAsync(
        ApplicationDbContext db,
        int tenantId,
        List<string> fileErrors,
        CancellationToken ct)
    {
        var documentPaths = await db.EvidenceDocuments
            .IgnoreQueryFilters()
            .Where(d => d.TenantId == tenantId)
            .Select(d => d.StoragePath)
            .ToListAsync(ct);

        var assetPaths = await db.TrainingTemplateAssets
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == tenantId)
            .Select(a => a.StoragePath)
            .ToListAsync(ct);

        foreach (var path in documentPaths.Concat(assetPaths).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            TryDeleteFile(documentStorage.GetFullPath(path), path, fileErrors);
        }
    }

    private void DeleteTenantDirectories(int tenantId, List<string> fileErrors)
    {
        var uploadFolder = Path.Combine(documentStorage.GetUploadRootPath(), tenantId.ToString());
        TryDeleteDirectory(uploadFolder, fileErrors);

        var trainingFolder = Path.Combine(trainingAssetStorage.GetStorageRootPath(), tenantId.ToString());
        TryDeleteDirectory(trainingFolder, fileErrors);
    }

    private void TryDeleteFile(string fullPath, string relativePath, List<string> fileErrors)
    {
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (Exception ex)
        {
            var message = $"Datei konnte nicht gelöscht werden: {relativePath}";
            fileErrors.Add(message);
            logger.LogError(ex, "{Message} (Pfad={Path})", message, relativePath);
        }
    }

    private void TryDeleteDirectory(string directoryPath, List<string> fileErrors)
    {
        try
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
        catch (Exception ex)
        {
            var message = $"Verzeichnis konnte nicht gelöscht werden: {directoryPath}";
            fileErrors.Add(message);
            logger.LogError(ex, "{Message}", message);
        }
    }
}
