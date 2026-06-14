using Dsms.Web.Domain.Entities;

namespace Dsms.Web.Services.Support;

public sealed record SupportAccessGrantListItem(
    int Id,
    int TenantId,
    string TenantName,
    bool IsActive,
    DateTime ValidUntil,
    DateTime GrantedAt,
    string GrantedByDisplayName,
    string? Reason,
    bool IsRevoked);

public sealed record SupportAccessGrantDetail(
    int Id,
    int TenantId,
    string TenantName,
    bool IsActive,
    DateTime ValidUntil,
    DateTime GrantedAt,
    string GrantedByDisplayName,
    string? Reason,
    DateTime? RevokedAt,
    string? RevokedByDisplayName);

public sealed record SupportAccessSessionInfo(
    int GrantId,
    int TenantId,
    string TenantName,
    DateTime ValidUntil);

public sealed record SupportAccessOperationResult(
    bool Succeeded,
    string? Message = null,
    SupportAccessGrant? Grant = null)
{
    public static SupportAccessOperationResult Ok(SupportAccessGrant? grant = null, string? message = null) =>
        new(true, message, grant);

    public static SupportAccessOperationResult Fail(string message) =>
        new(false, message);
}

public interface ISupportAccessService
{
    Task<bool> CanManageSupportAccessForCurrentTenantAsync();

    Task<SupportAccessGrantDetail?> GetActiveGrantForCurrentTenantAsync();

    Task<SupportAccessOperationResult> GrantAccessAsync(TimeSpan duration, string? reason);

    Task<SupportAccessOperationResult> RevokeAccessAsync(int grantId);

    Task<IReadOnlyList<SupportAccessGrantListItem>> ListGrantsForPlatformAsync();

    Task<SupportAccessOperationResult> ActivateSupportModeAsync(int grantId);

    Task ExitSupportModeAsync();

    Task<int?> EnsureSuperuserSupportContextAsync();

    Task<bool> HasValidSupportAccessForCurrentTenantAsync();

    Task<bool> IsSupportModeActiveAsync();

    Task<SupportAccessSessionInfo?> GetCurrentSupportSessionAsync();

    Task<SupportAccessGrant?> ValidateGrantAsync(int grantId);
}
