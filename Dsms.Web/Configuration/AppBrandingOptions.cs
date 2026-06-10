namespace Dsms.Web.Configuration;

/// <summary>Sichtbares Produkt-Branding (Name, URLs, Taglines).</summary>
public sealed class AppBrandingOptions
{
    public const string SectionName = "AppBranding";

    public string ProductName { get; set; } = "Datenschutz-Cloud";

    public string ShortName { get; set; } = "DC";

    public string Tagline { get; set; } = "Datenschutzmanagement einfach verwalten";

    public string Description { get; set; } =
        "Ihr Datenschutz-Management-System für VVT, TOMs, DSFA, Dienstleister, Audits und Nachweise.";

    public string WebsiteUrl { get; set; } = "https://www.datenschutz-cloud.eu";

    public string AppUrl { get; set; } = "https://app.datenschutz-cloud.eu";

    public string SupportEmail { get; set; } = "support@datenschutz-cloud.eu";
}
