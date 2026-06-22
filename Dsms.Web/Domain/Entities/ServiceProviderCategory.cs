using ServiceProviderEntity = Dsms.Web.Domain.Entities.ServiceProvider;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Mandantenbezogene Art / Kategorie eines Dienstleisters ("Art des Dienstleisters").
/// Werte werden nie hart gelöscht, sondern über <see cref="IsActive"/> deaktiviert,
/// damit bestehende Dienstleister und Datenbankbeziehungen erhalten bleiben.
/// </summary>
public class ServiceProviderCategory : EntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Anzeigename der Dienstleister-Art (pro Mandant eindeutig).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optionale Beschreibung der Dienstleister-Art.</summary>
    public string? Description { get; set; }

    /// <summary>Sortierreihenfolge in Auswahl und Verwaltung.</summary>
    public int SortOrder { get; set; }

    /// <summary>Aktive Arten können neuen Dienstleistern zugeordnet werden.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Kennzeichnet eine durch das System erzeugte Standard-Art.</summary>
    public bool IsSystemDefault { get; set; }

    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }

    public ICollection<ServiceProviderEntity> ServiceProviders { get; set; } = [];
}
