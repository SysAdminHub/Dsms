namespace Dsms.Web.Domain.Entities;

/// <summary>
/// Globale Email-Vorlage der Plattform (nicht mandantenbezogen).
/// </summary>
public class EmailTemplate : EntityBase
{
    public string TemplateKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? UpdatedByUserId { get; set; }
}
