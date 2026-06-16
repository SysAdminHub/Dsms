namespace Dsms.Web.Domain.Enums;

/// <summary>Quelle / Entdeckungsweg eines Datenschutzvorfalls.</summary>
public enum PrivacyIncidentSource
{
    Internal,
    ServiceProvider,
    DataSubject,
    CustomerOrPartner,
    Authority,
    SecuritySystem,
    Other
}
