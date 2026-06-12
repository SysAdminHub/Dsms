using System.Globalization;
using System.Text.RegularExpressions;
using Dsms.Web.Domain.Entities;
using Microsoft.AspNetCore.Components.Authorization;

namespace Dsms.Web.Services.Legal;

public sealed partial class LegalPlaceholderService(
    AuthenticationStateProvider authenticationStateProvider,
    ITenantService tenantService,
    IUserAccessService userAccess) : ILegalPlaceholderService
{
    [GeneratedRegex(@"\{\{([A-Za-z0-9_]+)\}\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    private static readonly Dictionary<string, string> AnonymousDefaults = new(StringComparer.Ordinal)
    {
        ["CustomerCompanyName"] = "[Name/Firma des Verantwortlichen]",
        ["CustomerLegalName"] = "[Rechtlicher Name des Verantwortlichen]",
        ["CustomerStreet"] = "[Straße und Hausnummer]",
        ["CustomerPostalCode"] = "[Postleitzahl]",
        ["CustomerCity"] = "[Ort]",
        ["CustomerCountry"] = "[Land]",
        ["CustomerContactName"] = "[Ansprechpartner]",
        ["CustomerEmail"] = "[E-Mail-Adresse]",
        ["TenantName"] = "[Mandant]",
        ["ContractDate"] = "[Datum des Vertragsschlusses]"
    };

    public async Task<IReadOnlyDictionary<string, string>> BuildReplacementsAsync(
        string legalVersion,
        CancellationToken cancellationToken = default)
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated != true)
        {
            return BuildAnonymousReplacements(legalVersion);
        }

        var tenant = await tenantService.GetCurrentTenantAsync();
        if (tenant is null)
        {
            await tenantService.EnsureTenantContextAsync();
            tenant = await tenantService.GetCurrentTenantAsync();
        }

        if (tenant is not null && !await userAccess.CanAccessTenantAsync(tenant.Id))
        {
            tenant = null;
        }

        return tenant is null
            ? BuildAnonymousReplacements(legalVersion)
            : BuildReplacementsForTenant(tenant, legalVersion);
    }

    public IReadOnlyDictionary<string, string> BuildAnonymousReplacements(string legalVersion)
    {
        var replacements = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["LegalVersion"] = legalVersion,
            ["LegalVersionDate"] = legalVersion
        };

        foreach (var (key, value) in AnonymousDefaults)
        {
            replacements[key] = value;
        }

        return replacements;
    }

    public IReadOnlyDictionary<string, string> BuildReplacementsForTenant(Tenant tenant, string legalVersion)
    {
        var replacements = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["LegalVersion"] = legalVersion,
            ["LegalVersionDate"] = legalVersion
        };

        ApplyTenantValues(replacements, tenant);
        return replacements;
    }

    public string ApplyPlaceholders(string markdown, IReadOnlyDictionary<string, string> replacements)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return markdown;
        }

        return PlaceholderRegex().Replace(markdown, match =>
        {
            var key = match.Groups[1].Value;
            return replacements.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    private static void ApplyTenantValues(IDictionary<string, string> replacements, Tenant tenant)
    {
        replacements["CustomerCompanyName"] = ValueOrMissing(tenant.LegalName ?? tenant.Name);
        replacements["CustomerLegalName"] = ValueOrMissing(tenant.LegalName ?? tenant.Name);
        replacements["CustomerStreet"] = ValueOrMissing(FormatStreet(tenant));
        replacements["CustomerPostalCode"] = ValueOrMissing(tenant.PostalCode);
        replacements["CustomerCity"] = ValueOrMissing(tenant.City);
        replacements["CustomerCountry"] = ValueOrMissing(tenant.Country);
        replacements["CustomerContactName"] = ValueOrMissing(tenant.ContactName);
        replacements["CustomerEmail"] = ValueOrMissing(tenant.Email);
        replacements["TenantName"] = ValueOrMissing(tenant.Name);
        replacements["ContractDate"] = tenant.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
    }

    private static string FormatStreet(Tenant tenant)
    {
        var street = tenant.Street?.Trim();
        var houseNumber = tenant.HouseNumber?.Trim();

        if (string.IsNullOrWhiteSpace(street))
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(houseNumber)
            ? street
            : $"{street} {houseNumber}";
    }

    private static string ValueOrMissing(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "[nicht angegeben]" : value.Trim();
}
