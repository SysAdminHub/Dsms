using System.ComponentModel.DataAnnotations;
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

    [Required(ErrorMessage = "Name ist erforderlich.")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? LegalName { get; set; }

    [MaxLength(300)]
    public string? Street { get; set; }

    [MaxLength(20)]
    public string? HouseNumber { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(200)]
    public string? ContactName { get; set; }

    [MaxLength(50)]
    public string? VatId { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Website { get; set; }

    [MaxLength(200)]
    public string? DpoName { get; set; }

    [MaxLength(300)]
    public string? DpoStreet { get; set; }

    [MaxLength(20)]
    public string? DpoHouseNumber { get; set; }

    [MaxLength(20)]
    public string? DpoPostalCode { get; set; }

    [MaxLength(100)]
    public string? DpoCity { get; set; }

    [MaxLength(50)]
    public string? DpoPhone { get; set; }

    [MaxLength(255)]
    public string? DpoEmail { get; set; }

    public bool IsActive { get; set; } = true;
    public Guid? LicenseId { get; set; }
}

public sealed class TenantComplianceInfoDto
{
    public string TenantName { get; init; } = string.Empty;
    public DateTime ContractDateUtc { get; init; }
    public string? LegalName { get; init; }
    public string? Street { get; init; }
    public string? HouseNumber { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public string? ContactName { get; init; }
    public string? VatId { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string? DpoName { get; init; }
    public string? DpoStreet { get; init; }
    public string? DpoHouseNumber { get; init; }
    public string? DpoPostalCode { get; init; }
    public string? DpoCity { get; init; }
    public string? DpoPhone { get; init; }
    public string? DpoEmail { get; init; }
}

public sealed class TenantComplianceInfoSaveModel
{
    [MaxLength(300)]
    public string? LegalName { get; set; }

    [MaxLength(300)]
    public string? Street { get; set; }

    [MaxLength(20)]
    public string? HouseNumber { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(200)]
    public string? ContactName { get; set; }

    [MaxLength(50)]
    public string? VatId { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Website { get; set; }

    [MaxLength(200)]
    public string? DpoName { get; set; }

    [MaxLength(300)]
    public string? DpoStreet { get; set; }

    [MaxLength(20)]
    public string? DpoHouseNumber { get; set; }

    [MaxLength(20)]
    public string? DpoPostalCode { get; set; }

    [MaxLength(100)]
    public string? DpoCity { get; set; }

    [MaxLength(50)]
    public string? DpoPhone { get; set; }

    [MaxLength(255)]
    public string? DpoEmail { get; set; }
}

public sealed class TenantOperationResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }

    public static TenantOperationResult Ok() => new() { Succeeded = true };

    public static TenantOperationResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
