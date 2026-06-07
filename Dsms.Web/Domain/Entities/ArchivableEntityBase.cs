namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Basis für archivierbare Fach-Entities (Soft Delete).
/// Standard: IsArchived = false – nur aktive Datensätze in der Normalansicht.
/// </summary>
public abstract class ArchivableEntityBase : EntityBase, IArchivable
{
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public string? ArchivedByUserId { get; set; }
}
