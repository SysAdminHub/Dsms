namespace Dsms.Web.Models.Dashboard;

/// <summary>
/// Mutually exclusive Statusgruppen für Donut-Segmente (Priorität: Kritisch → Hinweis → Gut → Neutral).
/// </summary>
public sealed record DashboardStatusGroupCounts(
    int Critical,
    int Warning,
    int Good,
    int Neutral)
{
    public int Total => Critical + Warning + Good + Neutral;
}
