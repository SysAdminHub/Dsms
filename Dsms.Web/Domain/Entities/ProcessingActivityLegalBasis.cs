namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Zuordnung einer Standard-Rechtsgrundlage (DSGVO) zu einer Verarbeitungstätigkeit (1:n).
/// Ermöglicht die strukturierte Mehrfachauswahl von Rechtsgrundlagen pro VVT.
/// TenantId dient der mandantenbezogenen Absicherung – Zuordnungen bestehen nur innerhalb eines Mandanten.
/// Der <see cref="LegalBasisKey"/> verweist auf einen stabilen Key aus <see cref="Dsms.Web.Domain.LegalBasisOptions"/>.
/// </summary>
public class ProcessingActivityLegalBasis
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int ProcessingActivityId { get; set; }
    public ProcessingActivity ProcessingActivity { get; set; } = null!;

    /// <summary>Stabiler interner Key der Rechtsgrundlage (z. B. "Art6_1_b"), unabhängig vom sichtbaren Label.</summary>
    public string LegalBasisKey { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
