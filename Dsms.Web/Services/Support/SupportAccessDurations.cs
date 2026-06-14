namespace Dsms.Web.Services.Support;

/// <summary>Vordefinierte Laufzeiten für Supportzugriffe (keine unbegrenzte Freigabe).</summary>
public static class SupportAccessDurations
{
    public static readonly IReadOnlyList<(string Label, TimeSpan Duration)> Options =
    [
        ("1 Stunde", TimeSpan.FromHours(1)),
        ("4 Stunden", TimeSpan.FromHours(4)),
        ("24 Stunden", TimeSpan.FromHours(24)),
        ("7 Tage", TimeSpan.FromDays(7))
    ];

    public static TimeSpan Default => TimeSpan.FromHours(24);
}
