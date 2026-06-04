using Dsms.Web.Domain.Enums;

namespace Dsms.Web.Domain;

/// <summary>Deutsche Anzeigelabels für Maßnahmen-Status in der UI.</summary>
public static class MeasureLabels
{
    public static string GetStatusLabel(MeasureStatus status) => status switch
    {
        MeasureStatus.Open => "Offen",
        MeasureStatus.InProgress => "In Bearbeitung",
        MeasureStatus.Done => "Erledigt",
        MeasureStatus.Cancelled => "Abgebrochen",
        _ => status.ToString()
    };

    public static string GetStatusVariant(MeasureStatus status) => status switch
    {
        MeasureStatus.Done => "success",
        MeasureStatus.InProgress => "primary",
        MeasureStatus.Cancelled => "muted",
        _ => "warning"
    };

    public static bool IsOpen(MeasureStatus status) =>
        status is MeasureStatus.Open or MeasureStatus.InProgress;
}
