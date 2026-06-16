using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain;

/// <summary>Deutsche Anzeigelabels für Audit-Durchlauf-Status in der UI.</summary>
public static class AuditRunLabels
{
    public static string GetStatusLabel(AuditRunStatus status) => status switch
    {
        AuditRunStatus.Draft => "Entwurf",
        AuditRunStatus.InProgress => "Laufend",
        AuditRunStatus.Completed => "Abgeschlossen",
        _ => status.ToString()
    };

    public static string GetStatusVariant(AuditRunStatus status) => status switch
    {
        AuditRunStatus.Completed => "success",
        AuditRunStatus.InProgress => "primary",
        AuditRunStatus.Draft => "muted",
        _ => "primary"
    };
}
