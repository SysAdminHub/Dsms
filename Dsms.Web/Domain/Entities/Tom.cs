using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Technische oder organisatorische Maßnahme (TOM) im mandantenbezogenen TOM-Verzeichnis.
/// Kann mit mehreren Verarbeitungstätigkeiten verknüpft werden (Many-to-Many über <see cref="ProcessingActivityTom"/>).
/// </summary>
public class Tom : ArchivableEntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Bezeichnung / Titel der Maßnahme.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Beschreibung der Maßnahme und ihrer Umsetzung.</summary>
    public string? Description { get; set; }

    /// <summary>Mandantenbezogene Kategorie (aus der Datenbank, ersetzt das frühere Enum).</summary>
    public int? TomCategoryId { get; set; }
    public TomCategory? TomCategory { get; set; }

    public TomProtectionGoal ProtectionGoal { get; set; } = TomProtectionGoal.Confidentiality;

    public TomImplementationStatus ImplementationStatus { get; set; } = TomImplementationStatus.Planned;

    /// <summary>Verantwortliche Person / fachlicher Owner (Freitext).</summary>
    public string? Owner { get; set; }

    /// <summary>Gültig ab (Datum der Wirksamkeit oder Dokumentation).</summary>
    public DateOnly? ValidFrom { get; set; }

    /// <summary>Nächste geplante Überprüfung der Maßnahme.</summary>
    public DateOnly? NextReviewAt { get; set; }

    /// <summary>Nachweis oder Referenz (z. B. Dokument, Ticket, Richtlinie).</summary>
    public string? EvidenceReference { get; set; }

    public string? Notes { get; set; }

    public ICollection<ProcessingActivityTom> ProcessingActivityLinks { get; set; } = [];

    /// <summary>Dienstleister, bei denen diese TOM geprüft oder vereinbart wurde.</summary>
    public ICollection<ServiceProviderTom> ServiceProviderLinks { get; set; } = [];
}
