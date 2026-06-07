namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Kennzeichnet mandantenbezogene Fach-Entities mit Soft-Delete (Archivierung).
/// Physisches Löschen ist nicht vorgesehen – stattdessen IsArchived = true.
/// </summary>
public interface IArchivable
{
    bool IsArchived { get; set; }
    DateTime? ArchivedAt { get; set; }
    string? ArchivedByUserId { get; set; }
}
