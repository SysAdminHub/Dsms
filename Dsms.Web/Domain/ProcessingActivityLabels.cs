using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain;

/// <summary>Deutsche Anzeigelabels für VVT-Status und DSFA-Hinweise in der UI.</summary>
public static class ProcessingActivityLabels
{
    public static string GetStatusLabel(ProcessingActivityStatus status) => status switch
    {
        ProcessingActivityStatus.Draft => "Entwurf",
        ProcessingActivityStatus.Active => "Aktiv",
        ProcessingActivityStatus.InReview => "In Prüfung",
        ProcessingActivityStatus.Archived => "Archiviert",
        _ => status.ToString()
    };

    public static string GetStatusVariant(ProcessingActivityStatus status) => status switch
    {
        ProcessingActivityStatus.Active => "success",
        ProcessingActivityStatus.InReview => "primary",
        ProcessingActivityStatus.Archived => "default",
        _ => "warning"
    };

    public static string FormatDpiaRequired(bool required) => required ? "Ja" : "Nein";
}
