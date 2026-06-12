using System.ComponentModel.DataAnnotations;
using Dsms.Web.Domain.Entities;

namespace Dsms.Web.Services.Tenants;

/// <summary>
/// Gemeinsame Mapping-, Validierungs- und Snapshot-Logik für DSGVO-Mandanten-Stammdaten.
/// </summary>
internal static class TenantComplianceFields
{
    public static void Apply(Tenant tenant, TenantComplianceInfoSaveModel model)
    {
        tenant.LegalName = NormalizeOptional(model.LegalName);
        tenant.Street = NormalizeOptional(model.Street);
        tenant.HouseNumber = NormalizeOptional(model.HouseNumber);
        tenant.PostalCode = NormalizeOptional(model.PostalCode);
        tenant.City = NormalizeOptional(model.City);
        tenant.Country = NormalizeOptional(model.Country);
        tenant.ContactName = NormalizeOptional(model.ContactName);
        tenant.VatId = NormalizeOptional(model.VatId);
        tenant.Phone = NormalizeOptional(model.Phone);
        tenant.Email = NormalizeOptional(model.Email);
        tenant.Website = NormalizeOptional(model.Website);
        tenant.DpoName = NormalizeOptional(model.DpoName);
        tenant.DpoStreet = NormalizeOptional(model.DpoStreet);
        tenant.DpoHouseNumber = NormalizeOptional(model.DpoHouseNumber);
        tenant.DpoPostalCode = NormalizeOptional(model.DpoPostalCode);
        tenant.DpoCity = NormalizeOptional(model.DpoCity);
        tenant.DpoPhone = NormalizeOptional(model.DpoPhone);
        tenant.DpoEmail = NormalizeOptional(model.DpoEmail);
    }

    public static void Apply(Tenant tenant, TenantSaveModel model)
    {
        tenant.Name = model.Name.Trim();
        Apply(tenant, FromSaveModel(model));
    }

    public static TenantComplianceInfoSaveModel FromSaveModel(TenantSaveModel model) => new()
    {
        LegalName = model.LegalName,
        Street = model.Street,
        HouseNumber = model.HouseNumber,
        PostalCode = model.PostalCode,
        City = model.City,
        Country = model.Country,
        ContactName = model.ContactName,
        VatId = model.VatId,
        Phone = model.Phone,
        Email = model.Email,
        Website = model.Website,
        DpoName = model.DpoName,
        DpoStreet = model.DpoStreet,
        DpoHouseNumber = model.DpoHouseNumber,
        DpoPostalCode = model.DpoPostalCode,
        DpoCity = model.DpoCity,
        DpoPhone = model.DpoPhone,
        DpoEmail = model.DpoEmail
    };

    public static TenantComplianceInfoSaveModel FromTenant(Tenant tenant) => new()
    {
        LegalName = tenant.LegalName,
        Street = tenant.Street,
        HouseNumber = tenant.HouseNumber,
        PostalCode = tenant.PostalCode,
        City = tenant.City,
        Country = tenant.Country,
        ContactName = tenant.ContactName,
        VatId = tenant.VatId,
        Phone = tenant.Phone,
        Email = tenant.Email,
        Website = tenant.Website,
        DpoName = tenant.DpoName,
        DpoStreet = tenant.DpoStreet,
        DpoHouseNumber = tenant.DpoHouseNumber,
        DpoPostalCode = tenant.DpoPostalCode,
        DpoCity = tenant.DpoCity,
        DpoPhone = tenant.DpoPhone,
        DpoEmail = tenant.DpoEmail
    };

    public static TenantComplianceInfoDto ToDto(Tenant tenant) => new()
    {
        TenantName = tenant.Name,
        ContractDateUtc = tenant.CreatedAt,
        LegalName = tenant.LegalName,
        Street = tenant.Street,
        HouseNumber = tenant.HouseNumber,
        PostalCode = tenant.PostalCode,
        City = tenant.City,
        Country = tenant.Country,
        ContactName = tenant.ContactName,
        VatId = tenant.VatId,
        Phone = tenant.Phone,
        Email = tenant.Email,
        Website = tenant.Website,
        DpoName = tenant.DpoName,
        DpoStreet = tenant.DpoStreet,
        DpoHouseNumber = tenant.DpoHouseNumber,
        DpoPostalCode = tenant.DpoPostalCode,
        DpoCity = tenant.DpoCity,
        DpoPhone = tenant.DpoPhone,
        DpoEmail = tenant.DpoEmail
    };

    public static TenantOperationResult? Validate(TenantComplianceInfoSaveModel model)
    {
        if (!IsValidOptionalEmail(model.Email))
        {
            return TenantOperationResult.Fail("Ungültige E-Mail-Adresse des Verantwortlichen.");
        }

        if (!IsValidOptionalEmail(model.DpoEmail))
        {
            return TenantOperationResult.Fail("Ungültige E-Mail-Adresse der Datenschutzbeauftragten Person.");
        }

        return null;
    }

    public static object Snapshot(Tenant tenant) => new
    {
        tenant.LegalName,
        tenant.Street,
        tenant.HouseNumber,
        tenant.PostalCode,
        tenant.City,
        tenant.Country,
        tenant.ContactName,
        tenant.VatId,
        tenant.Phone,
        tenant.Email,
        tenant.Website,
        tenant.DpoName,
        tenant.DpoStreet,
        tenant.DpoHouseNumber,
        tenant.DpoPostalCode,
        tenant.DpoCity,
        tenant.DpoPhone,
        tenant.DpoEmail
    };

    private static bool IsValidOptionalEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) || new EmailAddressAttribute().IsValid(email.Trim());

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
