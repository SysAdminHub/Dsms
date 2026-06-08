namespace Dsms.Web.Services.TenantDeletion;

public interface ITenantDeletionService
{
    Task<TenantDeletionResult> RequestDeletionAsync(int tenantId, string confirmedTenantName, string userId, CancellationToken ct = default);

    Task<TenantDeletionResult> CancelDeletionRequestAsync(int tenantId, CancellationToken ct = default);
}

public sealed class TenantDeletionResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static TenantDeletionResult Ok(string message) => new() { Success = true, Message = message };
    public static TenantDeletionResult Fail(string message) => new() { Success = false, Message = message };
}
