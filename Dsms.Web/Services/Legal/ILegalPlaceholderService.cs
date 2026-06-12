using Dsms.Web.Domain.Entities;

namespace Dsms.Web.Services.Legal;

public interface ILegalPlaceholderService
{
    Task<IReadOnlyDictionary<string, string>> BuildReplacementsAsync(
        string legalVersion,
        CancellationToken cancellationToken = default);

    IReadOnlyDictionary<string, string> BuildAnonymousReplacements(string legalVersion);

    IReadOnlyDictionary<string, string> BuildReplacementsForTenant(Tenant tenant, string legalVersion);

    string ApplyPlaceholders(string markdown, IReadOnlyDictionary<string, string> replacements);
}
