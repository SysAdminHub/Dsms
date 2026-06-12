using Dsms.Web.Data;
using Dsms.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsms.Web.Services.Legal;

public interface ILegalAcceptanceService
{
    Task AddWithinTransactionAsync(
        ApplicationDbContext db,
        LegalAcceptanceInputDto input,
        int tenantId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, LegalAcceptanceSummaryDto>> GetSummariesByPendingSignupIdsAsync(
        IReadOnlyCollection<Guid> pendingSignupIds,
        CancellationToken cancellationToken = default);

    Task<LegalAcceptanceSummaryDto?> GetSummaryForSignupAsync(
        Guid pendingSignupId,
        int? tenantId,
        CancellationToken cancellationToken = default);
}

public sealed class LegalAcceptanceService(IDbContextFactory<ApplicationDbContext> dbFactory) : ILegalAcceptanceService
{
    public async Task AddWithinTransactionAsync(
        ApplicationDbContext db,
        LegalAcceptanceInputDto input,
        int tenantId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!input.AcceptedTerms || !input.AcceptedPrivacyPolicy || !input.AcceptedDataProcessingAgreement)
        {
            throw new InvalidOperationException("Alle rechtlichen Zustimmungen müssen bestätigt sein.");
        }

        if (string.IsNullOrWhiteSpace(input.LegalVersion))
        {
            throw new InvalidOperationException("Legal-Version fehlt.");
        }

        if (input.PendingSignupId is Guid pendingSignupId)
        {
            var existingForSignup = await db.LegalAcceptances
                .AnyAsync(l => l.PendingSignupId == pendingSignupId, cancellationToken);
            if (existingForSignup)
            {
                return;
            }
        }

        var existingForTenant = await db.LegalAcceptances
            .AnyAsync(
                l => l.TenantId == tenantId && l.LegalVersion == input.LegalVersion,
                cancellationToken);
        if (existingForTenant)
        {
            return;
        }

        var now = DateTime.UtcNow;
        db.LegalAcceptances.Add(new LegalAcceptance
        {
            TenantId = tenantId,
            UserId = userId,
            LegalVersion = input.LegalVersion.Trim(),
            EffectiveDate = input.EffectiveDate.Trim(),
            AcceptedTerms = input.AcceptedTerms,
            AcceptedPrivacyPolicy = input.AcceptedPrivacyPolicy,
            AcceptedDataProcessingAgreement = input.AcceptedDataProcessingAgreement,
            AcceptedAtUtc = input.AcceptedAtUtc,
            AnonymizedIpAddress = NormalizeOptional(input.AnonymizedIpAddress, 45),
            UserAgent = NormalizeOptional(input.UserAgent, 512),
            PendingSignupId = input.PendingSignupId,
            SignupEmail = NormalizeOptional(input.SignupEmail, 255),
            TenantNameSnapshot = NormalizeOptional(input.TenantNameSnapshot, 200),
            CompanyNameSnapshot = NormalizeOptional(input.CompanyNameSnapshot, 200),
            CreatedAt = now
        });
    }

    public async Task<IReadOnlyDictionary<Guid, LegalAcceptanceSummaryDto>> GetSummariesByPendingSignupIdsAsync(
        IReadOnlyCollection<Guid> pendingSignupIds,
        CancellationToken cancellationToken = default)
    {
        if (pendingSignupIds.Count == 0)
        {
            return new Dictionary<Guid, LegalAcceptanceSummaryDto>();
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var acceptances = await db.LegalAcceptances
            .AsNoTracking()
            .Where(l => l.PendingSignupId != null && pendingSignupIds.Contains(l.PendingSignupId.Value))
            .OrderByDescending(l => l.AcceptedAtUtc)
            .ToListAsync(cancellationToken);

        return acceptances
            .Where(l => l.PendingSignupId.HasValue)
            .GroupBy(l => l.PendingSignupId!.Value)
            .ToDictionary(g => g.Key, g => MapSummary(g.First()));
    }

    public async Task<LegalAcceptanceSummaryDto?> GetSummaryForSignupAsync(
        Guid pendingSignupId,
        int? tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var bySignup = await db.LegalAcceptances
            .AsNoTracking()
            .Where(l => l.PendingSignupId == pendingSignupId)
            .OrderByDescending(l => l.AcceptedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (bySignup is not null)
        {
            return MapSummary(bySignup);
        }

        if (tenantId is int tid)
        {
            var byTenant = await db.LegalAcceptances
                .AsNoTracking()
                .Where(l => l.TenantId == tid)
                .OrderByDescending(l => l.AcceptedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (byTenant is not null)
            {
                return MapSummary(byTenant);
            }
        }

        return null;
    }

    private static LegalAcceptanceSummaryDto MapSummary(LegalAcceptance entity) => new()
    {
        HasAcceptance = true,
        LegalVersion = entity.LegalVersion,
        EffectiveDate = entity.EffectiveDate,
        AcceptedAtUtc = entity.AcceptedAtUtc,
        AcceptedTerms = entity.AcceptedTerms,
        AcceptedPrivacyPolicy = entity.AcceptedPrivacyPolicy,
        AcceptedDataProcessingAgreement = entity.AcceptedDataProcessingAgreement,
        TenantId = entity.TenantId,
        UserId = entity.UserId,
        AnonymizedIpAddress = entity.AnonymizedIpAddress,
        UserAgent = entity.UserAgent
    };

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
