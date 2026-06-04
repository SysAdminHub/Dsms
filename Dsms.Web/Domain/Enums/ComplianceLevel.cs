namespace Dsms.Web.Domain.Enums;

/// <summary>Bewertung einer Audit-Antwort (Compliance-Einschätzung).</summary>
public enum ComplianceLevel
{
    Open = 0,
    Compliant = 1,
    Partial = 2,
    NonCompliant = 3,
    NotApplicable = 4
}
