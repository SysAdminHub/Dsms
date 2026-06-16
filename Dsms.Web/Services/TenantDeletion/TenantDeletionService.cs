using Dsms.Web.Data;
using Dsms.Web.Services.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.TenantDeletion;

public class TenantDeletionService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IUserAccessService access,
    ITenantDeletionNotificationService notificationService,
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
        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

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
        tenant.DeletionRequestedAt = now;
        tenant.DeletionRequestedByUserId = userId;
        tenant.DeletionScheduledAt = now.Add(DeletionGracePeriod);
        tenant.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        await TryLogAuditAsync(
            "TenantDeletionRequested",
            "Mandantenlöschung wurde angefordert.",
            tenant,
            userId);

        logger.LogWarning(
            "Löschung angefordert: TenantId={TenantId}, UserId={UserId}, ScheduledAt={ScheduledAt}",
            tenantId,
            userId,
            tenant.DeletionScheduledAt);

        var emailSent = await notificationService.TrySendDeletionRequestedNotificationAsync(tenant, userId, ct);

        var message = "Ihre Mandantenlöschung wurde angefordert. Die tatsächliche Löschung erfolgt nach manueller Prüfung. Sie erhalten bei Bedarf weitere Informationen.";
        return TenantDeletionResult.Ok(message, emailNotificationFailed: !emailSent);
    }

    public async Task<TenantDeletionResult> CancelDeletionRequestAsync(int tenantId, CancellationToken ct = default)
    {
        if (!await access.CanManageTenantsAsync())
        {
            return TenantDeletionResult.Fail("Nur Superuser können eine Löschanforderung abbrechen.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null)
        {
            return TenantDeletionResult.Fail("Mandant nicht gefunden.");
        }

        if (!tenant.IsDeletionRequested)
        {
            return TenantDeletionResult.Fail("Für diesen Mandanten liegt keine Löschanforderung vor.");
        }

        tenant.IsDeletionRequested = false;
        tenant.DeletionRequestedAt = null;
        tenant.DeletionRequestedByUserId = null;
        tenant.DeletionScheduledAt = null;
        tenant.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        var userId = await currentUser.GetUserIdAsync();
        if (userId is not null)
        {
            await TryLogAuditAsync(
                "TenantDeletionRequestCancelled",
                "Löschanforderung für Mandant wurde abgebrochen.",
                tenant,
                userId);
        }

        logger.LogInformation("Löschanforderung abgebrochen: TenantId={TenantId}", tenantId);

        return TenantDeletionResult.Ok("Die Löschanforderung wurde abgebrochen.");
    }

    public async Task<TenantDeletionResult> ExecuteDeletionAsync(
        int tenantId,
        string confirmedTenantName,
        string userId,
        CancellationToken ct = default)
    {
        if (!await access.CanManageTenantsAsync())
        {
            return TenantDeletionResult.Fail("Nur Superuser können einen Mandanten löschen.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null)
        {
            return TenantDeletionResult.Fail("Mandant nicht gefunden.");
        }

        if (!tenant.IsActive)
        {
            return TenantDeletionResult.Fail("Dieser Mandant ist bereits deaktiviert.");
        }

        if (!TenantNamesMatch(tenant.Name, confirmedTenantName))
        {
            return TenantDeletionResult.Fail("Der eingegebene Mandantenname stimmt nicht überein.");
        }

        var now = DateTime.UtcNow;
        var wasDeletionRequested = tenant.IsDeletionRequested;

        tenant.IsActive = false;
        tenant.IsDeletionRequested = false;
        tenant.DeletionRequestedAt = null;
        tenant.DeletionRequestedByUserId = null;
        tenant.DeletionScheduledAt = null;
        tenant.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        await TryLogAuditAsync(
            "TenantDeletedBySuperuser",
            "Mandant wurde durch Superuser gelöscht.",
            tenant,
            userId,
            new { WasDeletionRequested = wasDeletionRequested, DeactivatedAtUtc = now });

        logger.LogWarning(
            "Mandant deaktiviert durch Superuser: TenantId={TenantId}, UserId={UserId}",
            tenantId,
            userId);

        return TenantDeletionResult.Ok(
            "Der Mandant wurde deaktiviert. Benutzer können nicht mehr regulär auf diesen Mandanten zugreifen.");
    }

    public async Task<TenantDeletionStatusDto?> GetDeletionStatusAsync(int tenantId, CancellationToken ct = default)
    {
        if (!await access.IsSuperuserAsync())
        {
            return null;
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

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

        return new TenantDeletionStatusDto
        {
            IsDeletionRequested = tenant.IsDeletionRequested,
            DeletionRequestedAt = tenant.DeletionRequestedAt,
            DeletionRequestedByUserId = tenant.DeletionRequestedByUserId,
            DeletionRequestedByEmail = email,
            DeletionRequestedByDisplayName = displayName
        };
    }

    internal static bool TenantNamesMatch(string actual, string confirmed) =>
        string.Equals(NormalizeTenantName(actual), NormalizeTenantName(confirmed), StringComparison.Ordinal);

    private static string NormalizeTenantName(string name) => name.Trim();

    private async Task TryLogAuditAsync(
        string action,
        string description,
        Domain.Entities.Tenant tenant,
        string userId,
        object? metadata = null)
    {
        try
        {
            await logService.LogAuditAsync(
                action: action,
                description: description,
                entityType: "Tenant",
                entityId: tenant.Id.ToString(),
                entityName: tenant.Name,
                tenantId: tenant.Id,
                licenseId: tenant.LicenseId,
                metadata: metadata ?? new
                {
                    RequestedByUserId = userId,
                    tenant.IsDeletionRequested,
                    tenant.DeletionRequestedAt
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Auditlog für Mandantenlöschung fehlgeschlagen (TenantId={TenantId})", tenant.Id);
        }
    }
}
