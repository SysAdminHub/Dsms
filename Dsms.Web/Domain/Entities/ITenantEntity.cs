namespace Dsms.Web.Domain.Entities;

/// <summary>Mandantenbezogene Entity mit TenantId-Fremdschlüssel.</summary>
public interface ITenantEntity
{
    int TenantId { get; set; }
}
