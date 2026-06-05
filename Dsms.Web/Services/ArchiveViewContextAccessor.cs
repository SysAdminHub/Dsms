namespace Dsms.Web.Services;

/// <summary>
/// Scoped-Halter für die Listenansicht (Aktiv vs. Archiv).
/// Wird von EF Global Query Filters gelesen: ShowArchivedOnly steuert IsArchived-Filter.
/// </summary>
public class ArchiveViewContextAccessor
{
    /// <summary>
    /// false = nur aktive Einträge (IsArchived == false), true = nur Archiv (IsArchived == true).
    /// </summary>
    public bool ShowArchivedOnly { get; set; }
}
