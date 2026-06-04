namespace Dsms.Web.Domain.Enums;

/// <summary>Prüf- und Freigabestatus einer Datenschutz-Folgenabschätzung (DSFA).</summary>
public enum DpiaStatus
{
    Draft = 0,
    InReview = 1,
    Approved = 2,
    Rejected = 3,
    RevisionRequired = 4,
    Archived = 5
}
