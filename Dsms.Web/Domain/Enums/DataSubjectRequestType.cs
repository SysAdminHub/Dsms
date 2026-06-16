namespace Dsms.Web.Domain.Enums;

/// <summary>Art einer Betroffenenanfrage nach DSGVO.</summary>
public enum DataSubjectRequestType
{
    Access,
    Rectification,
    Erasure,
    Restriction,
    DataPortability,
    Objection,
    ConsentWithdrawal,
    Other
}
