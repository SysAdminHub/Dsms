namespace Dsms.Web.Services.Legal;

public sealed class LegalAcceptanceInputDto
{
    public bool AcceptedTerms { get; set; }
    public bool AcceptedPrivacyPolicy { get; set; }
    public bool AcceptedDataProcessingAgreement { get; set; }
    public string LegalVersion { get; set; } = string.Empty;
    public string EffectiveDate { get; set; } = string.Empty;
    public DateTime AcceptedAtUtc { get; set; }
    public string? AnonymizedIpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? PendingSignupId { get; set; }
    public string? SignupEmail { get; set; }
    public string? TenantNameSnapshot { get; set; }
    public string? CompanyNameSnapshot { get; set; }
}

public sealed class LegalAcceptanceSummaryDto
{
    public bool HasAcceptance { get; init; }
    public string? LegalVersion { get; init; }
    public string? EffectiveDate { get; init; }
    public DateTime? AcceptedAtUtc { get; init; }
    public bool AcceptedTerms { get; init; }
    public bool AcceptedPrivacyPolicy { get; init; }
    public bool AcceptedDataProcessingAgreement { get; init; }
    public int? TenantId { get; init; }
    public string? UserId { get; init; }
    public string? AnonymizedIpAddress { get; init; }
    public string? UserAgent { get; init; }

    public static LegalAcceptanceSummaryDto None() => new() { HasAcceptance = false };
}

public static class LegalAcceptanceDisplayHelper
{
    public static string FormatListSummary(LegalAcceptanceSummaryDto? acceptance)
    {
        if (acceptance is null || !acceptance.HasAcceptance)
        {
            return "Keine Legal-Zustimmung gefunden";
        }

        var acceptedAt = acceptance.AcceptedAtUtc?.ToLocalTime().ToString("g") ?? "—";
        return $"✓ Version {acceptance.LegalVersion}\n{acceptedAt}";
    }

    public static string FormatYesNo(bool value) => value ? "Ja" : "Nein";
}
