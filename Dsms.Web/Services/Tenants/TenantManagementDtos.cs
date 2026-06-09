using Dsms.Web.Services.Licenses;

namespace Dsms.Web.Services.Tenants;

public sealed class TenantListItemDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? LegalName { get; init; }
    public bool IsActive { get; init; }
    public Guid? LicenseId { get; init; }
    public string? LicenseNumber { get; init; }
    public string? LicenseCustomerName { get; init; }
    public string? LicensePlanName { get; init; }
    public string LicenseDisplayName { get; init; } = LicenseDisplayHelper.NoLicenseText;
}

public sealed class TenantSaveModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? LicenseId { get; set; }
}

public sealed class TenantOperationResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }

    public static TenantOperationResult Ok() => new() { Succeeded = true };

    public static TenantOperationResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
