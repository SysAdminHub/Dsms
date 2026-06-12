namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Tarifvorlage für spätere Kundenlizenzen.
/// Limits mit <c>null</c> bedeuten unbegrenzt.
/// Änderungen an Plänen wirken sich nicht auf bestehende <see cref="License"/>-Einträge aus.
/// </summary>
public class SubscriptionPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFree { get; set; }
    /// <summary>
    /// Wenn true, wird der Plan auf der öffentlichen Registrierungsseite angezeigt und auswählbar.
    /// </summary>
    public bool IsPublicSignupEnabled { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public decimal? PriceMonthly { get; set; }
    public decimal? PriceYearly { get; set; }
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Wenn true, werden auf der Registrierungsseite optionale Sonderpreise angezeigt.
    /// Änderungen wirken sich nicht auf bestehende Lizenzen aus.
    /// </summary>
    public bool IsPromotionalPriceEnabled { get; set; }
    public decimal? PromotionalMonthlyPrice { get; set; }
    public decimal? PromotionalYearlyPrice { get; set; }
    public string? PromotionalBadgeText { get; set; }

    public string? ExternalProductId { get; set; }
    public string? ExternalMonthlyPriceId { get; set; }
    public string? ExternalYearlyPriceId { get; set; }
    public string? InternalNote { get; set; }

    // Lizenzweite Limits
    public int? MaxTenants { get; set; }
    public int? MaxAdmins { get; set; }

    // Mandantenbezogene Limits (gelten je Mandant)
    public int? MaxUsersPerTenant { get; set; }
    public int? MaxAuditorsPerTenant { get; set; }
    public int? MaxCustomAuditTemplatesPerTenant { get; set; }
    public int? MaxActiveAuditsPerTenant { get; set; }
    public int? MaxProcessingActivitiesPerTenant { get; set; }
    public int? MaxDpiaPerTenant { get; set; }
    public int? MaxTomsPerTenant { get; set; }
    public int? MaxProcessorsPerTenant { get; set; }
    public int? MaxActiveMeasuresPerTenant { get; set; }

    // Sonstige Limits
    public int? MaxStorageMb { get; set; }
    public int? MaxEmailRemindersPerMonth { get; set; }
}
