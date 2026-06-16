using Dsms.Web.Data;

namespace Dsms.Web.Domain.Entities;

/// <summary>Nachweis der rechtlichen Zustimmung bei der Registrierung (unveränderlicher Audit-Datensatz).</summary>
public class LegalAcceptance : EntityBase
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string LegalVersion { get; set; } = string.Empty;
    public string EffectiveDate { get; set; } = string.Empty;

    public bool AcceptedTerms { get; set; }
    public bool AcceptedPrivacyPolicy { get; set; }
    public bool AcceptedDataProcessingAgreement { get; set; }

    public DateTime AcceptedAtUtc { get; set; }
    /// <summary>Anonymisierte Client-IP (IPv4: /24, IPv6: /64).</summary>
    public string? AnonymizedIpAddress { get; set; }
    public string? UserAgent { get; set; }

    public Guid? PendingSignupId { get; set; }
    public string? SignupEmail { get; set; }
    public string? TenantNameSnapshot { get; set; }
    public string? CompanyNameSnapshot { get; set; }
}
