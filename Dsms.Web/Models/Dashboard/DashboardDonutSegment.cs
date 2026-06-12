namespace Dsms.Web.Models.Dashboard;

/// <summary>Ein farbiges Segment im Donut-Diagramm einer Dashboard-Kachel.</summary>
public sealed class DashboardDonutSegment
{
    public string Label { get; init; } = string.Empty;
    public int Value { get; init; }
    public string Color { get; init; } = string.Empty;
    public string CssClass { get; init; } = string.Empty;
    public string Tooltip { get; init; } = string.Empty;
}
