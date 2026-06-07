using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain;

/// <summary>Deutsche Anzeigelabels für Compliance-Bewertungen von Audit-Antworten.</summary>
public static class ComplianceLabels
{
    public static string GetLevelLabel(ComplianceLevel level) => level switch
    {
        ComplianceLevel.Open => "Offen",
        ComplianceLevel.Compliant => "Konform",
        ComplianceLevel.Partial => "Teilweise",
        ComplianceLevel.NonCompliant => "Nicht konform",
        ComplianceLevel.NotApplicable => "Nicht anwendbar",
        _ => level.ToString()
    };

    public static string GetLevelVariant(ComplianceLevel level) => level switch
    {
        ComplianceLevel.Compliant => "success",
        ComplianceLevel.Partial => "warning",
        ComplianceLevel.NonCompliant => "danger",
        ComplianceLevel.Open => "default",
        _ => "default"
    };

    /// <summary>Bewertungen, die auf der VVT-Detailseite als Warnhinweis gelten.</summary>
    public static bool IsProblematic(ComplianceLevel level) =>
        level is ComplianceLevel.Open or ComplianceLevel.Partial or ComplianceLevel.NonCompliant;

    /// <summary>Handlungsbedarf: Button „Maßnahme anlegen“ im Auditdurchlauf anzeigen.</summary>
    public static bool ShouldShowCreateMeasureButton(ComplianceLevel level) => IsProblematic(level);
}
