namespace Dsms.Web.Domain.Enums;

/// <summary>Status einer Community-Einreichung für eine Mandantenvorlage.</summary>
public enum CommunityStatus
{
    /// <summary>Nicht zur Community eingereicht.</summary>
    None = 0,

    /// <summary>Zur Prüfung eingereicht – noch nicht global sichtbar.</summary>
    Submitted = 1,

    /// <summary>Vom Superuser freigegeben.</summary>
    Approved = 2,

    /// <summary>Vom Superuser abgelehnt.</summary>
    Rejected = 3
}
