using Dsms.Web.Domain.Entities;

namespace Dsms.Web.Services;

/// <summary>Ergebnis einer Archivierungs- oder Wiederherstellungsoperation.</summary>
public sealed record ArchiveOperationResult(
    bool Succeeded,
    IReadOnlyList<string> Warnings,
    string? ErrorMessage = null);

/// <summary>
/// Zentraler Service für Soft Delete (Archivieren/Wiederherstellen) aller archivierbaren Module.
/// </summary>
public interface IArchivingService
{
    Task<ArchiveOperationResult> ArchiveAsync<TEntity>(int id, CancellationToken ct = default)
        where TEntity : ArchivableEntityBase, ITenantEntity;

    Task<ArchiveOperationResult> RestoreAsync<TEntity>(int id, CancellationToken ct = default)
        where TEntity : ArchivableEntityBase, ITenantEntity;

    Task<IReadOnlyList<string>> GetDependencyWarningsAsync<TEntity>(int id, CancellationToken ct = default)
        where TEntity : ArchivableEntityBase, ITenantEntity;
}
