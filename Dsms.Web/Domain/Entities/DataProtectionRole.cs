using Dsms.Web.Data;

namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Organisatorische Datenschutzrolle je Mandant (nicht identisch mit technischen App-Rollen).
/// </summary>
public class DataProtectionRole : EntityBase, ITenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Rollenbezeichnung, z. B. „Datenschutzbeauftragte Person“.</summary>
    public string RoleTitle { get; set; } = string.Empty;

    public string? PersonName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Department { get; set; }
    public string? AreaOfResponsibility { get; set; }

    /// <summary>Berichtslinie als Freitext.</summary>
    public string? ReportsTo { get; set; }

    /// <summary>Vertretung als Freitext.</summary>
    public string? Deputy { get; set; }

    /// <summary>Optionale Verknüpfung zu einer anderen Datenschutzrolle (Berichtslinie).</summary>
    public int? ReportsToRoleId { get; set; }
    public DataProtectionRole? ReportsToRole { get; set; }

    /// <summary>Optionale Verknüpfung zu einer anderen Datenschutzrolle (Vertretung).</summary>
    public int? DeputyRoleId { get; set; }
    public DataProtectionRole? DeputyRole { get; set; }

    /// <summary>Optionale Verknüpfung zu einem App-Benutzer des Mandanten.</summary>
    public string? LinkedUserId { get; set; }
    public ApplicationUser? LinkedUser { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Remarks { get; set; }

    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public string? ArchivedByUserId { get; set; }
}
