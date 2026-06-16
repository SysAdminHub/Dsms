using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Dsms.Web.Domain.Enums;
using Dsms.Web.Services.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.TenantDeletion;

public class TenantDeletionService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IUserAccessService access,
    ITenantDeletionNotificationService notificationService,
    ITenantDataErasureService dataErasureService,
    UserManager<ApplicationUser> userManager,
    ICurrentUserContext currentUser,
    ILogService logService,
    ILogger<TenantDeletionService> logger) : ITenantDeletionService
{
    private static readonly TimeSpan DeletionGracePeriod = TimeSpan.FromDays(7);

    public async Task<TenantDeletionResult> RequestDeletionAsync(
        int tenantId,
        string userId,
        bool confirmed,
        CancellationToken ct = default)
    {
        if (!confirmed)
        {
            return TenantDeletionResult.Fail("Bitte bestätigen Sie die Löschanforderung.");
        }

        if (!await access.CanManageTenantDataAsync())
        {
            return TenantDeletionResult.Fail("Keine Berechtigung für diese Aktion.");
        }

        var currentTenantId = await access.GetCurrentTenantIdAsync();
        if (!currentTenantId.HasValue || currentTenantId.Value != tenantId)
        {
            return TenantDeletionResult.Fail("Ungültiger Mandantenkontext.");
        }

        if (!await access.CanAccessTenantAsync(tenantId))
        {
            return TenantDeletionResult.Fail("Kein Zugriff auf diesen Mandanten.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var tenant = await LoadTenantAsync(db, tenantId, ct);
        if (tenant is null)
        {
            return TenantDeletionResult.Fail("Mandant nicht gefunden.");
        }

        if (tenant.IsDeletionRequested)
        {
            return TenantDeletionResult.Fail("Für diesen Mandanten wurde bereits eine Löschung angefordert.");
        }

        if (!tenant.IsActive)
        {
            return TenantDeletionResult.Fail("Dieser Mandant ist nicht mehr aktiv.");
        }

        var now = DateTime.UtcNow;
        tenant.IsDeletionRequested = true;
        tenant.IsActive = false;
        tenant.DeletionRequestedAt = now;
        tenant.DeletionRequestedByUserId = userId;
        tenant.DeletionScheduledAt = now.Add(DeletionGracePeriod);
        tenant.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        await TryLogPlatformLifecycleAsync(
            "MandantLoeschungAngefordert",
            "Mandantenlöschung wurde durch Mandanten-Admin angefordert.",
            tenant,
            userId,
            TenantLifecycleAction.LoeschungAngefordert,
            success: true);

        logger.LogWarning(
            "Löschung angefordert: TenantId={TenantId}, UserId={UserId}, ScheduledAt={ScheduledAt}",
            tenantId,
            userId,
            tenant.DeletionScheduledAt);

        var emailSent = await notificationService.TrySendDeletionRequestedNotificationAsync(tenant, userId, ct);

        const string message =
            "Deine Löschanfrage wurde erfasst. Der Mandant wird nach Prüfung gelöscht. " +
            "Der Zugriff auf diesen Mandanten ist bis dahin gesperrt.";
        return TenantDeletionResult.Ok(message, emailNotificationFailed: !emailSent);
    }

    public async Task<TenantDeletionResult> CancelDeletionRequestAsync(int tenantId, CancellationToken ct = default)
    {
        if (!await access.CanManageTenantsAsync())
        {
            return TenantDeletionResult.Fail("Nur Plattform-Administratoren können eine Löschanforderung abbrechen.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var tenant = await LoadTenantAsync(db, tenantId, ct);
        if (tenant is null)
        {
            return TenantDeletionResult.Fail("Mandant nicht gefunden.");
        }

        if (!tenant.IsDeletionRequested)
        {
            return TenantDeletionResult.Fail("Für diesen Mandanten liegt keine Löschanforderung vor.");
        }

        tenant.IsDeletionRequested = false;
        tenant.IsActive = true;
        tenant.DeletionRequestedAt = null;
        tenant.DeletionRequestedByUserId = null;
        tenant.DeletionScheduledAt = null;
        tenant.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        var userId = await currentUser.GetUserIdAsync();
        if (userId is not null)
        {
            await TryLogPlatformLifecycleAsync(
                "MandantLoeschungAbgebrochen",
                "Löschanforderung für Mandant wurde abgebrochen.",
                tenant,
                userId,
                TenantLifecycleAction.LoeschvormerkungAbgebrochen,
                success: true);
        }

        logger.LogInformation("Löschanforderung abgebrochen: TenantId={TenantId}", tenantId);

        return TenantDeletionResult.Ok("Die Löschanforderung wurde abgebrochen. Der Mandant ist wieder aktiv.");
    }

    public async Task<TenantDeletionResult> DeactivateTenantAsync(
        int tenantId,
        string userId,
        CancellationToken ct = default)
    {
        if (!await access.CanManageTenantsAsync())
        {
            return TenantDeletionResult.Fail("Nur Plattform-Administratoren können einen Mandanten deaktivieren.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var tenant = await LoadTenantAsync(db, tenantId, ct);
        if (tenant is null)
        {
            return TenantDeletionResult.Fail("Mandant nicht gefunden.");
        }

        if (!tenant.IsActive && !tenant.IsDeletionRequested)
        {
            return TenantDeletionResult.Fail("Dieser Mandant ist bereits deaktiviert.");
        }

        tenant.IsActive = false;
        tenant.IsDeletionRequested = false;
        tenant.DeletionRequestedAt = null;
        tenant.DeletionRequestedByUserId = null;
        tenant.DeletionScheduledAt = null;
        tenant.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        await TryLogPlatformLifecycleAsync(
            "MandantDeaktiviert",
            "Mandant wurde deaktiviert.",
            tenant,
            userId,
            TenantLifecycleAction.Deaktiviert,
            success: true);

        logger.LogWarning("Mandant deaktiviert: TenantId={TenantId}, UserId={UserId}", tenantId, userId);

        return TenantDeletionResult.Ok(
            "Der Mandant wurde deaktiviert. Benutzer können nicht mehr auf diesen Mandanten zugreifen. " +
            "Alle Daten bleiben erhalten.");
    }

    public async Task<TenantDeletionResult> MarkForDeletionAsync(
        int tenantId,
        string userId,
        DateTime? scheduledDeletionAt,
        CancellationToken ct = default)
    {
        if (!await access.CanManageTenantsAsync())
        {
            return TenantDeletionResult.Fail("Nur Plattform-Administratoren können einen Mandanten zur Löschung vormerken.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var tenant = await LoadTenantAsync(db, tenantId, ct);
        if (tenant is null)
        {
            return TenantDeletionResult.Fail("Mandant nicht gefunden.");
        }

        if (tenant.IsDeletionRequested)
        {
            return TenantDeletionResult.Fail("Dieser Mandant ist bereits zur Löschung vorgemerkt.");
        }

        var now = DateTime.UtcNow;
        tenant.IsDeletionRequested = true;
        tenant.IsActive = false;
        tenant.DeletionRequestedAt = now;
        tenant.DeletionRequestedByUserId = userId;
        tenant.DeletionScheduledAt = scheduledDeletionAt ?? now.Add(DeletionGracePeriod);
        tenant.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        await TryLogPlatformLifecycleAsync(
            "MandantZurLoeschungVorgemerkt",
            "Mandant wurde zur Löschung vorgemerkt.",
            tenant,
            userId,
            TenantLifecycleAction.ZurLoeschungVorgemerkt,
            success: true);

        var emailSent = await notificationService.TrySendMarkedForDeletionNotificationAsync(tenant, userId, ct);

        logger.LogWarning(
            "Mandant zur Löschung vorgemerkt: TenantId={TenantId}, UserId={UserId}, ScheduledAt={ScheduledAt}",
            tenantId,
            userId,
            tenant.DeletionScheduledAt);

        var message =
            "Der Mandant wurde zur Löschung vorgemerkt. Der Zugriff ist gesperrt. " +
            "Die endgültige Löschung muss separat durch einen Plattform-Administrator bestätigt werden.";
        return TenantDeletionResult.Ok(message, emailNotificationFailed: !emailSent);
    }

    public async Task<TenantDeletionResult> ExecutePermanentDeletionAsync(
        int tenantId,
        string confirmedTenantName,
        string userId,
        CancellationToken ct = default)
    {
        if (!await access.CanManageTenantsAsync())
        {
            return TenantDeletionResult.Fail("Nur Plattform-Administratoren können einen Mandanten endgültig löschen.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var tenant = await LoadTenantAsync(db, tenantId, ct);
        if (tenant is null)
        {
            return TenantDeletionResult.Fail("Mandant nicht gefunden.");
        }

        if (!TenantNamesMatch(tenant.Name, confirmedTenantName))
        {
            return TenantDeletionResult.Fail("Der eingegebene Mandantenname stimmt nicht überein.");
        }

        var tenantName = tenant.Name;
        var licenseId = tenant.LicenseId;
        var wasDeletionRequested = tenant.IsDeletionRequested;
        var scheduledAt = tenant.DeletionScheduledAt;

        await TryLogPlatformLifecycleAsync(
            "MandantEndgueltigGeloeschtGestartet",
            "Endgültige Mandantenlöschung wurde gestartet.",
            tenant,
            userId,
            TenantLifecycleAction.EndgueltigGeloescht,
            success: true,
            metadata: new
            {
                WasDeletionRequested = wasDeletionRequested,
                ScheduledDeletionAt = scheduledAt
            });

        var erasureResult = await dataErasureService.EraseTenantDataAsync(db, tenantId, ct);
        if (!erasureResult.Success)
        {
            await TryLogPlatformLifecycleAsync(
                "MandantEndgueltigGeloeschtFehlgeschlagen",
                "Endgültige Mandantenlöschung ist fehlgeschlagen.",
                tenantName,
                tenantId,
                licenseId,
                userId,
                TenantLifecycleAction.EndgueltigGeloescht,
                success: false,
                metadata: new { Error = erasureResult.ErrorMessage });

            var notifyFailed = await notificationService.TrySendPermanentDeletionFailedNotificationAsync(
                tenantName,
                tenantId,
                licenseId,
                userId,
                ct);

            return TenantDeletionResult.Fail(
                "Die endgültige Löschung ist fehlgeschlagen. Bitte prüfen Sie die Systemprotokolle.",
                emailNotificationFailed: !notifyFailed);
        }

        await TryLogPlatformLifecycleAsync(
            "MandantEndgueltigGeloescht",
            "Mandant und mandantenbezogene Daten wurden endgültig gelöscht.",
            tenantName,
            tenantId,
            licenseId,
            userId,
            TenantLifecycleAction.EndgueltigGeloescht,
            success: true,
            metadata: new
            {
                WasDeletionRequested = wasDeletionRequested,
                DeletedCounts = erasureResult.DeletedCounts,
                FileDeletionErrorCount = erasureResult.FileDeletionErrors.Count
            });

        if (erasureResult.FileDeletionErrors.Count > 0)
        {
            logger.LogWarning(
                "Mandant gelöscht, aber {Count} Datei(en) konnten nicht entfernt werden. TenantId={TenantId}",
                erasureResult.FileDeletionErrors.Count,
                tenantId);
        }

        logger.LogWarning(
            "Mandant endgültig gelöscht: TenantId={TenantId}, Name={TenantName}, UserId={UserId}",
            tenantId,
            tenantName,
            userId);

        var emailSent = await notificationService.TrySendPermanentDeletionNotificationAsync(
            tenantName,
            tenantId,
            licenseId,
            userId,
            ct);

        var resultMessage =
            "Der Mandant und alle zugehörigen mandantenbezogenen Daten wurden endgültig gelöscht. " +
            "Dieser Vorgang kann nicht rückgängig gemacht werden.";
        if (erasureResult.FileDeletionErrors.Count > 0)
        {
            resultMessage += " Einige Dateien konnten nicht entfernt werden; Details stehen im Systemprotokoll.";
        }
        if (!emailSent)
        {
            resultMessage += " Die Systembenachrichtigung konnte nicht versendet werden.";
        }

        return TenantDeletionResult.Ok(resultMessage, emailNotificationFailed: !emailSent);
    }

    public async Task<TenantDeletionStatusDto?> GetDeletionStatusAsync(int tenantId, CancellationToken ct = default)
    {
        if (!await access.IsSuperuserAsync())
        {
            return null;
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var tenant = await LoadTenantAsync(db, tenantId, ct);
        if (tenant is null)
        {
            return null;
        }

        string? email = null;
        string? displayName = null;
        if (!string.IsNullOrEmpty(tenant.DeletionRequestedByUserId))
        {
            var user = await userManager.FindByIdAsync(tenant.DeletionRequestedByUserId);
            email = user?.Email;
            displayName = user?.DisplayName ?? user?.UserName;
        }

        var lifecycleStatus = TenantLifecycleStatusHelper.GetStatus(tenant);

        return new TenantDeletionStatusDto
        {
            IsActive = tenant.IsActive,
            IsDeletionRequested = tenant.IsDeletionRequested,
            DeletionRequestedAt = tenant.DeletionRequestedAt,
            DeletionScheduledAt = tenant.DeletionScheduledAt,
            DeletionRequestedByUserId = tenant.DeletionRequestedByUserId,
            DeletionRequestedByEmail = email,
            DeletionRequestedByDisplayName = displayName,
            LifecycleStatus = lifecycleStatus,
            LifecycleStatusDisplayName = TenantLifecycleStatusHelper.GetDisplayName(lifecycleStatus)
        };
    }

    internal static bool TenantNamesMatch(string actual, string confirmed) =>
        string.Equals(NormalizeTenantName(actual), NormalizeTenantName(confirmed), StringComparison.Ordinal);

    private static string NormalizeTenantName(string name) => name.Trim();

    private static Task<Tenant?> LoadTenantAsync(
        ApplicationDbContext db,
        int tenantId,
        CancellationToken ct) =>
        db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

    private async Task TryLogPlatformLifecycleAsync(
        string action,
        string description,
        Tenant tenant,
        string userId,
        TenantLifecycleAction lifecycleAction,
        bool success,
        object? metadata = null) =>
        await TryLogPlatformLifecycleAsync(
            action,
            description,
            tenant.Name,
            tenant.Id,
            tenant.LicenseId,
            userId,
            lifecycleAction,
            success,
            metadata);

    private async Task TryLogPlatformLifecycleAsync(
        string action,
        string description,
        string tenantName,
        int tenantId,
        Guid? licenseId,
        string userId,
        TenantLifecycleAction lifecycleAction,
        bool success,
        object? metadata = null)
    {
        try
        {
            await logService.LogSystemAsync(
                action: action,
                description: description,
                severity: success ? "Info" : "Error",
                entityType: "Mandant",
                entityId: tenantId.ToString(),
                tenantId: tenantId,
                licenseId: licenseId,
                metadata: metadata ?? new
                {
                    MandantenId = tenantId,
                    Mandantenname = tenantName,
                    AusloesenderBenutzer = userId,
                    Aktion = lifecycleAction.ToString(),
                    Ergebnis = success ? "erfolgreich" : "fehlgeschlagen"
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Plattform-Löschprotokoll fehlgeschlagen (TenantId={TenantId})", tenantId);
        }
    }

    private enum TenantLifecycleAction
    {
        Deaktiviert,
        LoeschungAngefordert,
        ZurLoeschungVorgemerkt,
        LoeschvormerkungAbgebrochen,
        EndgueltigGeloescht
    }
}
