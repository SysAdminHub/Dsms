namespace Dsms.Web.Models.Dashboard;

/// <summary>Detailkennzahl in der Legende einer Dashboard-Kachel.</summary>
public sealed class DashboardLegendItem
{
    public string Label { get; init; } = string.Empty;
    public int Value { get; init; }
    public string Color { get; init; } = string.Empty;
    public string CssClass { get; init; } = string.Empty;
    public string? Url { get; init; }
}
