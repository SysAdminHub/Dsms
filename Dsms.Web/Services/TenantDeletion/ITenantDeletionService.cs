namespace Dsms.Web.Services.TenantDeletion;

public interface ITenantDeletionService
{
    /// <summary>Mandanten-Admin fordert Löschung an (kein Hard-Delete).</summary>
    Task<TenantDeletionResult> RequestDeletionAsync(
        int tenantId,
        string userId,
        bool confirmed,
        CancellationToken ct = default);

    /// <summary>Plattform-Admin: Löschanforderung zurücknehmen.</summary>
    Task<TenantDeletionResult> CancelDeletionRequestAsync(int tenantId, CancellationToken ct = default);

    /// <summary>Plattform-Admin: Mandant deaktivieren (Soft-Lock, Daten bleiben).</summary>
    Task<TenantDeletionResult> DeactivateTenantAsync(
        int tenantId,
        string userId,
        CancellationToken ct = default);

    /// <summary>Plattform-Admin: Mandant zur Löschung vormerken (Zugriff gesperrt, Daten bleiben).</summary>
    Task<TenantDeletionResult> MarkForDeletionAsync(
        int tenantId,
        string userId,
        DateTime? scheduledDeletionAt,
        CancellationToken ct = default);

    /// <summary>
    /// Plattform-Admin: Mandant und alle mandantenbezogenen Daten endgültig entfernen.
    /// Erfordert exakte Namensbestätigung.
    /// </summary>
    Task<TenantDeletionResult> ExecutePermanentDeletionAsync(
        int tenantId,
        string confirmedTenantName,
        string userId,
        CancellationToken ct = default);

    Task<TenantDeletionStatusDto?> GetDeletionStatusAsync(int tenantId, CancellationToken ct = default);
}

public sealed class TenantDeletionResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool EmailNotificationFailed { get; init; }

    public static TenantDeletionResult Ok(string message, bool emailNotificationFailed = false) =>
        new() { Success = true, Message = message, EmailNotificationFailed = emailNotificationFailed };

    public static TenantDeletionResult Fail(string message, bool emailNotificationFailed = false) =>
        new() { Success = false, Message = message, EmailNotificationFailed = emailNotificationFailed };
}

public sealed class TenantDeletionStatusDto
{
    public bool IsActive { get; init; }
    public bool IsDeletionRequested { get; init; }
    public DateTime? DeletionRequestedAt { get; init; }
    public DateTime? DeletionScheduledAt { get; init; }
    public string? DeletionRequestedByUserId { get; init; }
    public string? DeletionRequestedByEmail { get; init; }
    public string? DeletionRequestedByDisplayName { get; init; }
    public Domain.Enums.TenantLifecycleStatus LifecycleStatus { get; init; }
    public string LifecycleStatusDisplayName { get; init; } = string.Empty;
}
