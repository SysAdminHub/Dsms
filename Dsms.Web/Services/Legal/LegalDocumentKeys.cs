namespace Dsms.Web.Services.Legal;

public static class LegalDocumentKeys
{
    public const string Impressum = "impressum";
    public const string Datenschutzerklaerung = "datenschutzerklaerung";
    public const string Agb = "agb";
    public const string Avv = "avv";
    public const string Tom = "tom";
    public const string Unterauftragnehmerliste = "unterauftragnehmerliste";

    private static readonly Dictionary<string, string> RouteToKey = new(StringComparer.OrdinalIgnoreCase)
    {
        ["impressum"] = Impressum,
        ["datenschutz"] = Datenschutzerklaerung,
        ["agb"] = Agb,
        ["avv"] = Avv,
        ["tom"] = Tom,
        ["unterauftragnehmer"] = Unterauftragnehmerliste
    };

    public static bool TryResolveKeyFromRoute(string routeSegment, out string documentKey) =>
        RouteToKey.TryGetValue(routeSegment.Trim(), out documentKey!);

    public static string GetTitle(string documentKey) => documentKey switch
    {
        Impressum => "Impressum",
        Datenschutzerklaerung => "Datenschutzerklärung",
        Agb => "AGB / SaaS-Nutzungsbedingungen",
        Avv => "Auftragsverarbeitungsvertrag",
        Tom => "Technische und organisatorische Maßnahmen",
        Unterauftragnehmerliste => "Unterauftragnehmerliste",
        _ => "Rechtliches Dokument"
    };
}
