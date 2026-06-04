using Dsms.Web.Data;
using Microsoft.AspNetCore.Identity;

namespace Dsms.Web.Services;

/// <summary>Serverseitige Benutzerverwaltung inkl. Mandanten- und Rollenvalidierung.</summary>
public interface IUserManagementService
{
    Task<IReadOnlyList<UserListItem>> ListUsersAsync(bool includeInactive);

    Task<ApplicationUser?> GetUserForEditAsync(string userId);

    Task<UserOperationResult> CreateUserAsync(UserCreateModel model);

    Task<UserOperationResult> UpdateUserAsync(string userId, UserEditModel model);

    Task<UserOperationResult> SetUserActiveAsync(string userId, bool isActive);
}

public sealed record UserListItem(
    ApplicationUser User,
    string TenantName,
    IList<string> Roles,
    bool IsActive);

public sealed class UserCreateModel
{
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "";
    public int? TenantId { get; set; }
}

public sealed class UserEditModel
{
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "";
    public int? TenantId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UserOperationResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public IEnumerable<string> IdentityErrors { get; init; } = [];

    public static UserOperationResult Ok() => new() { Succeeded = true };

    public static UserOperationResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };

    public static UserOperationResult FromIdentity(IdentityResult result) =>
        result.Succeeded
            ? Ok()
            : new()
            {
                Succeeded = false,
                ErrorMessage = "Speichern fehlgeschlagen.",
                IdentityErrors = result.Errors.Select(e => e.Description)
            };
}
