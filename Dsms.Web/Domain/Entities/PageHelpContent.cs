namespace Dsms.Web.Domain.Entities;

/// <summary>Globaler Hilfetext für eine Fachseite (plattformweit, nicht mandantenspezifisch).</summary>
public class PageHelpContent : EntityBase
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? LegalReference { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public bool IsActive { get; set; } = true;
    public string? UpdatedByUserId { get; set; }
}
