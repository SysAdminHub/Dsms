namespace Dsms.Web.Services.TenantDeletion;

public interface ITenantDeletionService
{
    Task<TenantDeletionResult> RequestDeletionAsync(
        int tenantId,
        string userId,
        bool confirmed,
        CancellationToken ct = default);

    Task<TenantDeletionResult> CancelDeletionRequestAsync(int tenantId, CancellationToken ct = default);

    Task<TenantDeletionResult> ExecuteDeletionAsync(
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

    public static TenantDeletionResult Fail(string message) =>
        new() { Success = false, Message = message };
}

public sealed class TenantDeletionStatusDto
{
    public bool IsDeletionRequested { get; init; }
    public DateTime? DeletionRequestedAt { get; init; }
    public string? DeletionRequestedByUserId { get; init; }
    public string? DeletionRequestedByEmail { get; init; }
    public string? DeletionRequestedByDisplayName { get; init; }
}
