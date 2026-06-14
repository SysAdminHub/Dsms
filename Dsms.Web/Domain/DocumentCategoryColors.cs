namespace Dsms.Web.Domain;

/// <summary>Feste Farbauswahl für mandantenbezogene Dokumentkategorien.</summary>
public static class DocumentCategoryColors
{
    public const string Gray = "gray";
    public const string Blue = "blue";
    public const string Green = "green";
    public const string Purple = "purple";
    public const string Orange = "orange";
    public const string Red = "red";
    public const string Cyan = "cyan";

    public static IReadOnlyList<string> All { get; } =
    [
        Gray, Blue, Green, Purple, Orange, Red, Cyan
    ];

    public static string GetLabel(string? color) => color switch
    {
        Gray => "Grau",
        Blue => "Blau",
        Green => "Grün",
        Purple => "Lila",
        Orange => "Orange",
        Red => "Rot",
        Cyan => "Türkis",
        _ => "Grau"
    };

    public static string Normalize(string? color) =>
        string.IsNullOrWhiteSpace(color) || !All.Contains(color)
            ? Gray
            : color;

    public static string GetBadgeClass(string? color) =>
        $"dsms-badge--cat-{Normalize(color)}";
}
