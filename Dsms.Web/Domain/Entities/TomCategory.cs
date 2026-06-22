namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Mandantenbezogene Kategorie für technische und organisatorische Maßnahmen (TOMs).
/// Kategorien werden nie hart gelöscht, sondern über <see cref="IsActive"/> deaktiviert,
/// damit bestehende TOMs und Datenbankbeziehungen erhalten bleiben.
/// </summary>
public class TomCategory : EntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Anzeigename der Kategorie (pro Mandant eindeutig).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optionale Beschreibung der Kategorie.</summary>
    public string? Description { get; set; }

    /// <summary>Sortierreihenfolge in Auswahl und Verwaltung.</summary>
    public int SortOrder { get; set; }

    /// <summary>Aktive Kategorien können neuen TOMs zugeordnet werden.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Kennzeichnet eine durch das System erzeugte Standardkategorie.</summary>
    public bool IsSystemDefault { get; set; }

    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }

    public ICollection<Tom> Toms { get; set; } = [];
}
